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
    public class InventoryController : Controller
    {
        private readonly IInventory _inventoryRepository;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(
            IInventory inventoryRepository,
            ILogger<InventoryController> logger)
        {
            _inventoryRepository = inventoryRepository;
            _logger = logger;
        }

        public IActionResult Index(string search, int page = 1)
        {
            int pageSize = 6;

            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                _logger.LogInformation("Fetching inventory for HospitalId: {HospitalId}, SubHospitalId: {SubHospitalId}", hospitalId, subHospitalId);

                // 1️⃣ Get data
                var data = _inventoryRepository
                                .GetAllInventory(hospitalId, subHospitalId ?? 0)
                                .AsQueryable();

                // 2️⃣ Search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();

                    data = data.Where(x =>
                        (!string.IsNullOrEmpty(x.MedicineName) && x.MedicineName.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.CategoryName) && x.CategoryName.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.SupplierName) && x.SupplierName.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.Status) && x.Status.ToLower().Contains(search))
                    );
                }

                // 3️⃣ Pagination
                int totalRecords = data.Count();
                int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                var pagedData = data
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

                // 4️⃣ ViewBag
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.Search = search;

                _logger.LogInformation("Inventory fetched successfully. Page: {Page}, TotalRecords: {TotalRecords}", page, totalRecords);

                return View(pagedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while loading inventory page");

                ViewBag.Error = "Something went wrong!";
                return View(new List<InventoryModel>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            InventoryModel model = new InventoryModel
            {
                IsUpdate = false
            };

            return View("Create", model);
        }

        //[HttpPost]
        //public IActionResult Create(InventoryModel model)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return View(model);
        //        }

        //        // Session values
        //        model.HospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //        model.SubHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

        //        // Call repository
        //        _inventoryRepository.AddInventory(model);

        //        _logger.LogInformation("Inventory added successfully for MedicineId: {MedicineId}", model.MedicineId);

        //        TempData["Success"] = "Inventory added successfully!";

        //        return RedirectToAction("Index");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error while adding inventory");

        //        TempData["Error"] = "Something went wrong!";
        //        return View(model);
        //    }
        //}

        [HttpPost]
        public IActionResult Create(InventoryModel model)
        {
            if (!ModelState.IsValid)
                return View("Create", model);

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            model.HospitalId = hospitalId;
            model.SubHospitalId = subHospitalId;

            if (model.BatchId > 0)
            {
                
                _inventoryRepository.UpdateInventory(model);
                TempData["Success"] = "Inventory updated successfully";
            }
            else
            {
                
                _inventoryRepository.AddInventory(model);
                TempData["Success"] = "Inventory added successfully";
            }

            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult Edit(int batchId, int medicineId)
        {
            if (batchId <= 0 || medicineId <= 0)
                return NotFound();

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var data = _inventoryRepository.GetInventoryById(batchId, medicineId, hospitalId, subHospitalId);
            if (data == null)
                return NotFound();

            data.IsUpdate = true;

            ViewBag.Suppliers = _inventoryRepository.GetSuppliers(hospitalId, subHospitalId);
            ViewBag.Categories = _inventoryRepository.GetCategories(hospitalId, subHospitalId);
            return View("Create", data); // same view for create & edit
        }

        [HttpGet]
        public IActionResult Delete(int batchId, int medicineId)
        {
            if (batchId <= 0 || medicineId <= 0)
            {
                TempData["Error"] = "Invalid inventory id";
                return RedirectToAction("Index");
            }

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            _inventoryRepository.DeleteInventory(batchId, medicineId, hospitalId, subHospitalId);

            TempData["Success"] = "Inventory deleted successfully";

            return RedirectToAction("Index");
        }
        [HttpGet]
        public JsonResult GetSuppliers()
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var data = _inventoryRepository.GetSuppliers(hospitalId, subHospitalId);

                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching suppliers");
                return Json(new List<SupplierModel>());
            }
        }

        [HttpGet]
        public JsonResult GetCategories()
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var data = _inventoryRepository.GetCategories(hospitalId, subHospitalId);

                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories");
                return Json(new List<CategoryModel>());
            }
        }



        [HttpGet]
        public JsonResult GetInventoryStats()
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var all = _inventoryRepository.GetAllInventory(hospitalId, subHospitalId ?? 0);

                var today = DateTime.Today;
                var in30Days = today.AddDays(30);

                var stats = new
                {
                    TotalMedicines = all.Select(x => x.MedicineId).Distinct().Count(),
                    TotalBatches = all.Count,
                    LowStockCount = all.Count(x => x.Status == "Low Stock"),
                    InStockCount = all.Count(x => x.Status != "Low Stock"),
                    ExpiryIn30Days = all.Count(x => x.ExpiryDate.HasValue
                                                   && x.ExpiryDate.Value.Date >= today
                                                   && x.ExpiryDate.Value.Date <= in30Days),
                    ExpiredCount = all.Count(x => x.ExpiryDate.HasValue
                                                   && x.ExpiryDate.Value.Date < today),
                    TotalStockUnits = all.Sum(x => x.Stock),
                    CategoryBreakdown = all
                        .Where(x => !string.IsNullOrEmpty(x.CategoryName))
                        .GroupBy(x => x.CategoryName)
                        .Select(g => new { Category = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(5)
                        .ToList()
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInventoryStats");
                return Json(new { TotalMedicines = 0, LowStockCount = 0, ExpiryIn30Days = 0, InStockCount = 0, TotalBatches = 0, TotalStockUnits = 0, ExpiredCount = 0, CategoryBreakdown = new object[] { } });
            }
        }

    }
}
