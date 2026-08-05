using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;
using static QuestPDF.Helpers.Colors;

namespace WebApplicationSampleTest2.Controllers
{
    public class BillingMasterController : Controller
    {
        private readonly IBillingMaster _IBillingMaster;
        public BillingMasterController(IBillingMaster billingMaster)
        {
            _IBillingMaster = billingMaster;
        }
        public IActionResult Index(string search, int page = 1)
        {
            int pageSize = 6;

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var data = _IBillingMaster.GetAllBillings(hospitalId, subHospitalId).AsQueryable();

            // Server-side search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                data = data.Where(x =>
                    (!string.IsNullOrEmpty(x.Name) && x.Name.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(x.Description) && x.Description.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(x.BillingType) && x.BillingType.ToLower().Contains(search)) ||
                    x.Amount.ToString().Contains(search)
                );
            }

            // Pagination
            int totalRecords = data.Count();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var pagedData = data.Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;

            return View(pagedData);
        }

        // ===================== CREATE (GET) =====================
        [HttpGet]
        public IActionResult Create()
        {
            BillingMaster model = new BillingMaster
            {
                IsActive = 1,
                isUpdate = false
            };

            return View(model);
        }

        // ===================== CREATE / UPDATE (POST) =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(BillingMaster model, bool isUpdate = false)
        {
            if (!ModelState.IsValid)
                return View("Create", model);

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            if (model.isUpdate)   // 🔥 EDIT / UPDATE
            {
                int rows = _IBillingMaster.UpdateBilling(model, hospitalId, subHospitalId);
                if (rows > 0)
                    TempData["Success"] = "Billing updated successfully";
                else
                    TempData["Error"] = "No record updated!";
            }
            else                  // 🔥 CREATE
            {
                _IBillingMaster.CreateBilling(model, hospitalId, subHospitalId);
                TempData["Success"] = "Billing added successfully";
            }

            return RedirectToAction("Index");
        }

        // ===================== EDIT (GET → SAME CREATE VIEW) =====================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (id <= 0)
                return NotFound();

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var data = _IBillingMaster.GetBillingById(id, hospitalId, subHospitalId);
            if (data == null)
                return NotFound();

            data.isUpdate = true;
            return View("Create", data); // same create view
        }

        // ===================== DELETE =====================
        [HttpGet]
        public IActionResult Delete(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid Billing Id";
                return RedirectToAction("Index");
            }

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var billing = _IBillingMaster.GetBillingById(id, hospitalId, subHospitalId);
            if (billing == null)
            {
                TempData["Error"] = "Billing not found";
                return RedirectToAction("Index");
            }

            _IBillingMaster.DeleteBilling(id, hospitalId, subHospitalId);
            TempData["Success"] = "Billing deleted successfully";

            return RedirectToAction("Index");
        }

    }
}
