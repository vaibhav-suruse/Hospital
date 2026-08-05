using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class MedicineController : Controller
    {
        private readonly IMedicine _Imedicine;

        public MedicineController(IMedicine medicine) 
        {
            _Imedicine = medicine;
        }
        public IActionResult Index(string search, int page = 1)
        {
            int pageSize = 6;

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            // 1️⃣ Get hospital wise data
            var data = _Imedicine
                            .GetAllMedicine(hospitalId, subHospitalId)
                            .AsQueryable();

            // 2️⃣ Server-side search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();

                data = data.Where(x =>
                    (!string.IsNullOrEmpty(x.MedicineName) && x.MedicineName.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(x.Type) && x.Type.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(x.Description) && x.Description.ToLower().Contains(search)) ||
                    ((x.Morning ? "morning " : "") +
                     (x.Afternoon ? "afternoon " : "") +
                     (x.Evening ? "evening" : "")).Contains(search)
                );
            }

            // 3️⃣ Pagination
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

        [HttpGet]
        public IActionResult Create()
        {
            Medicine model = new Medicine
            {
                isUpdate = false   // ADD MODE
            };
            return View("Create", model);
        }

        // ===================== CREATE / UPDATE (POST) =====================
        [HttpPost]
        public IActionResult Create(Medicine model)
        {
            if (!ModelState.IsValid)
                return View("Create", model);

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            if (model.MedicineId > 0)
            {
                // UPDATE
                _Imedicine.UpdateMedicine(model, hospitalId, subHospitalId);
                TempData["Success"] = "Medicine updated successfully";
            }
            else
            {
                // INSERT
                _Imedicine.AddMedicine(model, hospitalId, subHospitalId);
                TempData["Success"] = "Medicine added successfully";
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

            var data = _Imedicine.GetMedicineById(id, hospitalId, subHospitalId);
            if (data == null)
                return NotFound();

            data.isUpdate = true;  
            return View("Create", data);   
        }

        // ===================== DELETE =====================
        [HttpGet]
        public IActionResult Delete(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid medicine id";
                return RedirectToAction("Index");
            }

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var medicine = _Imedicine.GetMedicineById(id, hospitalId, subHospitalId);
            if (medicine == null)
            {
                TempData["Error"] = "Medicine not found";
                return RedirectToAction("Index");
            }

            _Imedicine.DeleteMedicine(id, hospitalId, subHospitalId);
            TempData["Success"] = "Medicine deleted successfully";

            return RedirectToAction("Index");
        }

    }
}
