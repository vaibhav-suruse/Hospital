using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class ReferenceDoctorController : Controller
    {
      
        private readonly IReferenceDoctor _referenceDoctor;
        private readonly ILogger<ReferenceDoctorController> _logger;

        public ReferenceDoctorController(
            IReferenceDoctor referenceDoctor,
            ILogger<ReferenceDoctorController> logger)
        {
            _referenceDoctor = referenceDoctor;
            _logger = logger;
        }

        // ===================== INDEX =====================
        public IActionResult Index(string search, int page = 1)
        {
            try
            {
                int pageSize = 6;

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var data = _referenceDoctor
                                .GetAllReferenceDoctor(hospitalId, subHospitalId)
                                .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();

                    data = data.Where(x =>
                        (!string.IsNullOrEmpty(x.DoctorName) && x.DoctorName.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.ClinicName) && x.ClinicName.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.City) && x.City.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.MobileNumber) && x.MobileNumber.Contains(search))
                    );
                }

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
                _logger.LogError(ex, "Error in ReferenceDoctor Index");
                TempData["Error"] = "Something went wrong";
                return View();
            }
        }

        // ===================== CREATE GET =====================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new ReferenceDoctorModel { IsActive = true });
        }

        // ===================== CREATE / UPDATE POST =====================
        [HttpPost]
        public IActionResult Create(ReferenceDoctorModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                if (model.ReferenceDoctorId > 0)
                {
                    _referenceDoctor.UpdateReferenceDoctor(model, hospitalId, subHospitalId);
                    TempData["Success"] = "Reference Doctor updated successfully";
                }
                else
                {
                    _referenceDoctor.AddReferenceDoctor(model, hospitalId, subHospitalId);
                    TempData["Success"] = "Reference Doctor added successfully";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create/Update ReferenceDoctor");
                TempData["Error"] = "Something went wrong";
                return View(model);
            }
        }

        // ===================== EDIT =====================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            try
            {
                if (id <= 0)
                    return NotFound();

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var data = _referenceDoctor.GetReferenceDoctorById(id, hospitalId, subHospitalId);

                if (data == null)
                    return NotFound();

                return View("Create", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Edit ReferenceDoctor");
                TempData["Error"] = "Something went wrong";
                return RedirectToAction("Index");
            }
        }

        // ===================== DELETE =====================
        [HttpGet]
        public IActionResult Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "Invalid Reference Doctor Id";
                    return RedirectToAction("Index");
                }

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var doctor = _referenceDoctor.GetReferenceDoctorById(id, hospitalId, subHospitalId);

                if (doctor == null)
                {
                    TempData["Error"] = "Reference Doctor not found";
                    return RedirectToAction("Index");
                }

                _referenceDoctor.DeleteReferenceDoctor(id, hospitalId, subHospitalId);

                TempData["Success"] = "Reference Doctor deleted successfully";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Delete ReferenceDoctor");
                TempData["Error"] = "Something went wrong";
                return RedirectToAction("Index");
            }
        }

       
    }
}
