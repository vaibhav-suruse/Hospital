using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class NurseController : Controller
    {
        
        private readonly INurse _nurseRepository; 
        private readonly ILogger<NurseController> _logger;

        public NurseController(INurse nurseRepository, ILogger<NurseController> logger)
        {
            _nurseRepository = nurseRepository;
            _logger = logger;
        }

        // ===================== INDEX / LIST =====================
        public IActionResult Index(string search, int page = 1)
        {
            try
            {
                int pageSize = 6;
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var data = _nurseRepository.GetAll(hospitalId, subHospitalId).AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    data = data.Where(x =>
                        (!string.IsNullOrEmpty(x.FirstName) && x.FirstName.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.LastName) && x.LastName.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.Department) && x.Department.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.Qualification) && x.Qualification.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.Email) && x.Email.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.PhoneNumber) && x.PhoneNumber.ToLower().Contains(search))
                    );
                }

                int totalRecords = data.Count();
                int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                var pagedData = data.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.Search = search;

                return View(pagedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Nurse list. Search: {Search}, Page: {Page}", search, page);
                TempData["Error"] = "An error occurred while loading nurses.";
                return RedirectToAction("Index", "Home");
            }
        }

        // ===================== CREATE (GET) =====================
        [HttpGet]
        public IActionResult Create()
        {
            try
            {
                NurseModel model = new NurseModel { IsActive = true };
                ViewBag.isUpdate = false;
                return View("Create", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening Create Nurse view.");
                TempData["Error"] = "Unable to open the create nurse form.";
                return RedirectToAction("Index");
            }
        }

        // ===================== CREATE / UPDATE (POST) =====================
        [HttpPost]
        public IActionResult Create(NurseModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View("Create", model);

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                if (hospitalId <= 0)
                {
                    TempData["Error"] = "Invalid hospital session.";
                    return View("Create", model);
                }

                model.ParentHospitalId = hospitalId;
                model.SubHospitalId = subHospitalId;


                if (model.NurseId > 0)
                {
                    _nurseRepository.Update(model);
                    TempData["ToastMessage"] = "Nurse Updated successfully.";
                    TempData["ToastType"] = "success";
                }
                else
                {
                    _nurseRepository.Insert(model);
                    TempData["ToastMessage"] = "Nurse Added successfully.";
                    TempData["ToastType"] = "success";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating Nurse. NurseId: {NurseId}", model.NurseId);
                TempData["Error"] = "An error occurred while saving the nurse.";
                return View("Create", model);
            }
        }

        // ===================== EDIT (GET → SAME CREATE VIEW) =====================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            try
            {
                if (id <= 0) return NotFound();

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var data = _nurseRepository.GetById(id, hospitalId, subHospitalId);
                if (data == null) return NotFound();

                ViewBag.isUpdate = true;
                return View("Create", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing NurseId: {NurseId}", id);
                TempData["Error"] = "An error occurred while loading the nurse for editing.";
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
                    TempData["Error"] = "Invalid nurse id";
                    return RedirectToAction("Index");
                }

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var nurse = _nurseRepository.GetById(id, hospitalId, subHospitalId);
                if (nurse == null)
                {
                    TempData["ToastMessage"] = "Nurse Not Found.";
                    TempData["ToastType"] = "warning";
                    return RedirectToAction("Index");
                }

                _nurseRepository.Delete(id, hospitalId, subHospitalId);
                TempData["ToastMessage"] = "Nurse deleted successfully.";
                TempData["ToastType"] = "danger";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting NurseId: {NurseId}", id);
                TempData["Error"] = "An error occurred while deleting the nurse.";
                return RedirectToAction("Index");
            }
        }
    }
}
