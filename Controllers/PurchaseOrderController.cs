using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class PurchaseOrderController : Controller
    {
        private readonly IAdvancedPharmacy _pharmacy;
        private readonly IInventory _inventory;
        private readonly ISupplier _supplier;
        private readonly ILogger<PurchaseOrderController> _logger;

        public PurchaseOrderController(
            IAdvancedPharmacy pharmacy,
            IInventory inventory,
            ISupplier supplier,
            ILogger<PurchaseOrderController> logger)
        {
            _pharmacy = pharmacy;
            _inventory = inventory;
            _supplier = supplier;
            _logger = logger;
        }

        // ── LIST ─────────────────────────────────────────────────────────
        public IActionResult Index()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            var list = _pharmacy.GetPurchaseOrders(hospitalId, subHospitalId);
            return View(list);
        }

        // ── CREATE (GET) ─────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            ViewBag.Suppliers = _supplier.GetAllSuppliers(hospitalId, subHospitalId);
            ViewBag.Medicines = _inventory.GetAllInventory(hospitalId, subHospitalId);
            return View(new PurchaseOrder { Status = "Draft" });
        }

        // ── CREATE (POST) ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PurchaseOrder model)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            try
            {
                if (model.SupplierId <= 0 || model.Items == null || !model.Items.Any(i => i.Quantity > 0))
                {
                    TempData["Error"] = "Please select a supplier and add at least one item.";
                    return RedirectToAction("Create");
                }

                var poNumber = _pharmacy.CreatePurchaseOrder(model, hospitalId, subHospitalId, userId);
                TempData["Success"] = $"Purchase Order {poNumber} created successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create PO failed");
                TempData["Error"] = "Could not create purchase order: " + ex.Message;
                return RedirectToAction("Create");
            }
        }

        // ── DETAILS ──────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Details(string id)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            var po = _pharmacy.GetPurchaseOrder(id, hospitalId);
            if (po == null) return NotFound();
            return View(po);
        }

        // ── UPDATE STATUS ────────────────────────────────────────────────
        [HttpPost]
        public JsonResult UpdateStatus(string poNumber, string status)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            try
            {
                _pharmacy.UpdatePurchaseOrderStatus(poNumber, status, hospitalId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdatePOStatus failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── GENERATE DRAFT PO FROM LOW STOCK (Phase 7) ───────────────────
        [HttpGet]
        public IActionResult GenerateFromLowStock()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var lowStock = _inventory.GetAllInventory(hospitalId, subHospitalId)
                                     .Where(x => x.Stock <= x.ReorderLevel)
                                     .ToList();

            var po = new PurchaseOrder { Status = "Draft" };
            foreach (var item in lowStock)
            {
                po.Items.Add(new PurchaseOrderItem
                {
                    MedicineId = item.MedicineId,
                    MedicineName = item.MedicineName,
                    Quantity = Math.Max(item.ReorderLevel * 2 - item.Stock, 1),
                    UnitCost = item.MRP
                });
            }

            ViewBag.Suppliers = _supplier.GetAllSuppliers(hospitalId, subHospitalId);
            ViewBag.Medicines = _inventory.GetAllInventory(hospitalId, subHospitalId);
            ViewBag.FromLowStock = true;
            return View("Create", po);
        }
    }
}
