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
    public class GoodsReceiptController : Controller
    {
        private readonly IAdvancedPharmacy _pharmacy;
        private readonly ISupplier _supplier;
        private readonly IInventory _inventory;
        private readonly ILogger<GoodsReceiptController> _logger;

        public GoodsReceiptController(
            IAdvancedPharmacy pharmacy,
            ISupplier supplier,
            IInventory inventory,
            ILogger<GoodsReceiptController> logger)
        {
            _pharmacy = pharmacy;
            _supplier = supplier;
            _inventory = inventory;
            _logger = logger;
        }

        // ── LIST ─────────────────────────────────────────────────────────
        public IActionResult Index()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            var list = _pharmacy.GetGoodsReceipts(hospitalId, subHospitalId);
            return View(list);
        }

        // ── CREATE (GET) ─────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create(string poNumber)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            ViewBag.Suppliers = _supplier.GetAllSuppliers(hospitalId, subHospitalId);
            ViewBag.Medicines = _inventory.GetAllInventory(hospitalId, subHospitalId);
            ViewBag.OpenPOs = _pharmacy.GetPurchaseOrders(hospitalId, subHospitalId)
                                       .Where(p => p.Status != "Received" && p.Status != "Cancelled")
                                       .ToList();

            var grn = new GoodsReceipt();
            if (!string.IsNullOrEmpty(poNumber))
            {
                var po = _pharmacy.GetPurchaseOrder(poNumber, hospitalId);
                if (po != null)
                {
                    grn.PONumber = po.PONumber;
                    grn.SupplierId = po.SupplierId;
                    grn.SupplierName = po.SupplierName;
                    grn.Items = po.Items.Select(i => new GoodsReceiptItem
                    {
                        MedicineId = i.MedicineId,
                        MedicineName = i.MedicineName,
                        Quantity = i.Quantity,
                        PurchasePrice = i.UnitCost
                    }).ToList();
                }
            }
            return View(grn);
        }

        // ── CREATE (POST) ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(GoodsReceipt model)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            try
            {
                if (model.SupplierId <= 0 || model.Items == null || !model.Items.Any(i => i.Quantity > 0))
                {
                    TempData["Error"] = "Please select a supplier and add at least one item with quantity.";
                    return RedirectToAction("Create");
                }

                var grnNumber = _pharmacy.CreateGoodsReceipt(model, hospitalId, subHospitalId, userId);
                TempData["Success"] = $"Goods Receipt {grnNumber} created. Stock updated.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create GRN failed");
                TempData["Error"] = "Could not create goods receipt: " + ex.Message;
                return RedirectToAction("Create");
            }
        }
    }
}
