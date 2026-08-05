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
    public class SupplierController : Controller
    {
        private readonly ISupplier _supplierRepository;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(ISupplier supplierRepository, ILogger<SupplierController> logger)
        {
            _supplierRepository = supplierRepository;
            _logger = logger;
        }

        // ─────────────────────────────────────────
        // INDEX — List with Search + Pagination
        // ─────────────────────────────────────────
        public IActionResult Index(string search, int page = 1)
        {
            int pageSize = 10;

            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                _logger.LogInformation("Loading suppliers. HospitalId: {HospitalId}", hospitalId);

                var data = _supplierRepository
                                .GetAllSuppliers(hospitalId, subHospitalId)
                                .AsQueryable();

                // Search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    data = data.Where(x =>
                        (!string.IsNullOrEmpty(x.SupplierName) && x.SupplierName.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.ContactNo) && x.ContactNo.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.Address) && x.Address.ToLower().Contains(search))
                    );
                }

                // Pagination
                int totalRecords = data.Count();
                int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                var pagedData = data
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.Search = search;

                return View(pagedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading suppliers");
                TempData["Error"] = "Something went wrong while loading suppliers!";
                return View(new List<SupplierModel>());
            }
        }

        // ─────────────────────────────────────────
        // CREATE — GET
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            var model = new SupplierModel { IsUpdate = false };
            return View("Create", model);
        }

        // ─────────────────────────────────────────
        // CREATE / UPDATE — POST
        // ─────────────────────────────────────────
        [HttpPost]
        public IActionResult Create(SupplierModel model)
        {
            if (!ModelState.IsValid)
                return View("Create", model);

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            model.HospitalId = hospitalId;
            model.SubHospitalId = subHospitalId;

            try
            {
                if (model.IsUpdate && model.SupplierId > 0)
                {
                    _supplierRepository.UpdateSupplier(model);
                    TempData["Success"] = "Supplier updated successfully!";
                }
                else
                {
                    _supplierRepository.AddSupplier(model);
                    TempData["Success"] = "Supplier added successfully!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving supplier");
                TempData["Error"] = "Something went wrong while saving supplier!";
                return View("Create", model);
            }
        }

        // ─────────────────────────────────────────
        // EDIT — GET
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (id <= 0)
                return NotFound();

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var data = _supplierRepository.GetSupplierById(id, hospitalId, subHospitalId);

            if (data == null)
                return NotFound();

            data.IsUpdate = true;

            return View("Create", data);   // shared view for Create & Edit
        }

        // ─────────────────────────────────────────
        // DELETE — GET (via modal confirmation)
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Delete(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid supplier ID.";
                return RedirectToAction("Index");
            }

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            try
            {
                _supplierRepository.DeleteSupplier(id, hospitalId, subHospitalId);
                TempData["Success"] = "Supplier deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier. Id: {Id}", id);
                TempData["Error"] = "Something went wrong while deleting supplier!";
            }

            return RedirectToAction("Index");
        }
    }
}
