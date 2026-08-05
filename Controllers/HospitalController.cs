using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class HospitalController : Controller
    {
        private readonly IHospital _IHospital;
        private readonly ILogger<HospitalController> _logger;
        public HospitalController(ILogger<HospitalController> logger, IHospital hospital)
        {
            _IHospital = hospital;
            _logger = logger;
        }
        public IActionResult Index(string search, int page = 1)
        {

            _logger.LogError("Test error logging from Index method");
            int pageSize = 6; // number of records per page

            // 1. Get all hospitals from repository
            var hospitals = _IHospital.GetAllHospitals();

            // 2. Filter by search term if provided
            if (!string.IsNullOrEmpty(search))
            {
                hospitals = hospitals
                    .Where(h => h.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                (h.PhoneNumber != null && h.PhoneNumber.Contains(search)) ||
                                (h.EmailId != null && h.EmailId.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // 3. Pagination
            int totalRecords = hospitals.Count;
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var pagedHospitals = hospitals
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 4. Pass pagination info via ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;

            return View(pagedHospitals);
        }

        // GET: Hospital/Create (new hospital)
        [HttpGet]
        public IActionResult Create()
        {
            Hospital hospital = new Hospital();
            hospital.isUpdate = false; // new hospital
            return View("Create", hospital);
        }

        // GET: Hospital/Edit/5
        [HttpGet]
        public IActionResult Edit(int id)
        {
            Hospital hospital = _IHospital.GetHospitalById(id);
            if (hospital == null)
                return NotFound();

            hospital.isUpdate = true; // edit mode
            return View("Create", hospital);
        }

        // POST: Hospital/CreateEdit
        [HttpPost]
        public IActionResult Create(Hospital model)
        {
            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            // ================= IMAGE UPLOAD =================
            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                string uploadFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/hospitals"
                );

                // folder exists?
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // unique file name
                string fileExt = Path.GetExtension(model.LogoFile.FileName);
                string fileName = "hospital_" +
                                  DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                                  fileExt;

                string filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.LogoFile.CopyTo(stream);
                }

                // save file name to DB
                model.Logo = fileName;
            }

            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            if (model.Id == 0)
            {
                // CREATE
                _IHospital.InsertHospital(model);
                TempData["SuccessMessage"] = "Hospital created successfully!";
            }
            else
            {
                // UPDATE
                _IHospital.UpdateHospital(model);
                TempData["SuccessMessage"] = "Hospital updated successfully!";
            }

            return RedirectToAction("Index");
        }

        // DELETE POST without ValidateAntiForgeryToken
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid hospital id";
                return RedirectToAction("Index");
            }

            var hospital = _IHospital.GetHospitalById(id);
            if (hospital == null)
            {
                TempData["Error"] = "Hospital not found";
                return RedirectToAction("Index");
            }

            // Delete logo file if exists
            if (!string.IsNullOrEmpty(hospital.Logo))
            {
                var imagePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/hospitals",
                    hospital.Logo
                );

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _IHospital.DeleteHospital(id);
            TempData["Success"] = "Hospital deleted successfully";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GetMainHospitals()
        {
            var hospitals = _IHospital.GetMainHospitals()
                .Select(h => new { id = h.Id, name = h.Name })
                .ToList();

            return Json(hospitals);
        }

        [HttpGet]
        public IActionResult GetHospitals(int id)
        {
            var hospitals = _IHospital.GetHospitalById(id);


            return Json(hospitals);
        }

      
    }
}
