using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class SymptomController : Controller
    {
        private readonly ISymptom _Symptom;
       
        public SymptomController(ISymptom symptom)
        {
            _Symptom = symptom;
        }
        public IActionResult Index(string search = "", int page = 1)
        {
            int pageSize = 6; // records per page

            // 1️⃣ Get all symptoms from repository
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            List<Symptom> data = _Symptom.GetAllSymptoms(hospitalId, subHospitalId);

            // 2️⃣ Apply search filter (case-insensitive)
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                data = data.Where(x =>
                    (!string.IsNullOrEmpty(x.SymptomName) && x.SymptomName.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(x.SubName) && x.SubName.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(x.Description) && x.Description.ToLower().Contains(search))
                ).ToList();
            }

            // 3️⃣ Pagination logic
            int totalRecords = data.Count;
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            List<Symptom> pagedData = data
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 4️⃣ Pass data to view using ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchTerm = search;

            return View(pagedData);
        }


        // GET: Create or Edit
        public IActionResult Create(int id = 0)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            Symptom model = new Symptom();

            if (id > 0)
            {
                model = _Symptom.GetSymptomById(id, hospitalId, subHospitalId);
                if (model == null) return NotFound();
            }

            return View(model);
        }

        // POST: Create or Update
        [HttpPost]
        public IActionResult Create(Symptom model)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            // Optional server-side validation
            if (string.IsNullOrEmpty(model.SymptomName) ||
                string.IsNullOrEmpty(model.SubName) ||
                string.IsNullOrEmpty(model.Description))
            {
                ViewBag.Error = "All fields are required!";
                return View(model);
            }

            bool result;

            if (model.SymptomId > 0)
            {
                // UPDATE
                result = _Symptom.UpdateSymptom(model, hospitalId, subHospitalId) > 0;
            }
            else
            {
                // CREATE
                result = _Symptom.CreateSymptom(model, hospitalId, subHospitalId) > 0;
            }

            if (result)
                return RedirectToAction("Index");
            else
            {
                ViewBag.Error = "Operation failed. Try again.";
                return View(model);
            }
        }

        // POST: Delete
        [HttpPost]
        public IActionResult Delete(int id)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            bool result = _Symptom.DeleteSymptom(id, hospitalId, subHospitalId) > 0;

            if (result)
                return RedirectToAction("Index");
            else
            {
                TempData["Error"] = "Delete failed!";
                return RedirectToAction("Index");
            }
        }


    }
}
