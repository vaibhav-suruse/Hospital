using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class DoctorController : Controller
    {
        private readonly IDoctor _doctorRepo;

        public DoctorController(IDoctor doctorRepo)
        {
            _doctorRepo = doctorRepo;
        }
        public IActionResult Index(string search, int page = 1)
        {
            try
            {
                int pageSize = 6;

                //int hospitalId = Convert.ToInt32(HttpContext.Session.GetInt32("MainHospitalId"));
                //int? subHospitalId = Convert.ToInt32(HttpContext.Session.GetInt32("SubHospitalId"));
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0 if null
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var data = _doctorRepo.GetAllDoctor(hospitalId, subHospitalId).AsQueryable();

                //  Server-side search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    data = data.Where(x =>
                        (!string.IsNullOrEmpty(x.FirstName) && x.FirstName.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.LastName) && x.LastName.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.Specialization) && x.Specialization.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.MobileNo) && x.MobileNo.Contains(search))
                    );
                }

                // 📄 Pagination
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
            catch (Exception ex)
            {
                TempData["Error"] = "Something went wrong while loading doctors.";
                return RedirectToAction("Index", "Home");
            }
           
        }

        // ===================== CREATE (GET) =====================
        [HttpGet]
        public IActionResult Create()
        {
            Doctor model = new Doctor
            {
                IsActive = true,
                isUpdate = false
            };

            return View(model);
        }

        // ===================== CREATE / UPDATE (POST) =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Doctor model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View("Create", model);

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0 if null
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                if (model.isUpdate)   // 🔥 UPDATE
                {
                    int rows = _doctorRepo.UpdateDoctor(model, hospitalId, subHospitalId);
                    if (rows > 0)
                        TempData["Success"] = "Doctor updated successfully";
                    else
                        TempData["Error"] = "No record updated!";
                }
                else                  // 🔥 INSERT
                {
                    _doctorRepo.AddDoctor(model, hospitalId, subHospitalId);
                    TempData["Success"] = "Doctor added successfully";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error while saving doctor data.";
                return View("Create", model);
            }
           
        }

        // ===================== EDIT (GET → SAME CREATE VIEW) =====================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            try
            {
                if (id <= 0)
                    return NotFound();

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0 if null
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var data = _doctorRepo.GetDoctorById(id, hospitalId, subHospitalId);
                if (data == null)
                    return NotFound();

                data.isUpdate = true;
                return View("Create", data); // same view
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error while loading doctor data.";
                return RedirectToAction("Index");
            }
            
        }

        // ===================== DELETE (HARD DELETE) =====================
        [HttpGet]
        public IActionResult Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "Invalid Doctor Id";
                    return RedirectToAction("Index");
                }

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0 if null
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var doctor = _doctorRepo.GetDoctorById(id, hospitalId, subHospitalId);
                if (doctor == null)
                {
                    TempData["Error"] = "Doctor not found";
                    return RedirectToAction("Index");
                }

                _doctorRepo.DeleteDoctor(id, hospitalId, subHospitalId);
                TempData["Success"] = "Doctor deleted successfully";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error while deleting doctor.";
                return RedirectToAction("Index");
            }
           
        }
    }
}
