using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;
using Microsoft.Extensions.Logging;

namespace WebApplicationSampleTest2.Controllers
{
    public class RadiologyController : Controller
    {
        private readonly ILogger<RadiologyController> _logger;
        private readonly IRadiology _radiologyRepo;
        private readonly IIPDAdmission _admissionRepo;
        private readonly IDoctor _doctorRepo;
        private readonly IHospital _hospitalRepo;

        public RadiologyController(IRadiology radiologyRepo, IIPDAdmission admissionRepo, IDoctor doctorRepo, IHospital hospitalRepo, ILogger<RadiologyController> logger)
        {
            _radiologyRepo = radiologyRepo;
            _logger = logger;
            _admissionRepo = admissionRepo;
            _doctorRepo = doctorRepo;
            _hospitalRepo = hospitalRepo;
        }

        private int HospitalId => HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        private int? SubHospitalId => HttpContext.Session.GetInt32("SubHospitalId");
        private int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 1;

        private void LoadDoctors()
        {
            var doctors = _doctorRepo.GetAllDoctor(HospitalId, SubHospitalId) ?? new List<Doctor>();
            ViewBag.Doctors = new SelectList(
                doctors.Select(d => new { Id = d.Doctor_Id, Name = "Dr. " + d.FirstName + " " + d.LastName }),
                "Id", "Name");
        }

        // ===============================================================
        // INDEX - "Radiology Reports" (Clinical Details menu)
        // ===============================================================
        [HttpGet]
        public IActionResult Index(int ipdId)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound(); // multi-tenant safe - lookup is already hospital-scoped

            var vm = new RadiologyReportsVM
            {
                Admission = admission,
                Reports = _radiologyRepo.GetReportsByIPD(ipdId)
            };

            LoadDoctors();
            return View(vm);
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult ReportsPartial(int ipdId)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound();

            var list = _radiologyRepo.GetReportsByIPD(ipdId);
            ViewBag.IPDId = ipdId;
            return PartialView("_RadiologyReportsPartial", list);
        }

        // ===============================================================
        // TEMPLATES
        // ===============================================================
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult SearchTemplates(string term)
        {
            return Json(_radiologyRepo.GetTemplates(HospitalId, term));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveTemplate(RadiologyTemplateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.TemplateName) || string.IsNullOrWhiteSpace(model.TemplateText))
                    return Json(new { success = false, message = "Template name and text are required." });

                _radiologyRepo.SaveTemplate(HospitalId, model, CurrentUserId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveTemplate");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // ADD / EDIT / DELETE
        // ===============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddReport(RadiologyReportModel model, bool saveAsTemplate = false, string templateName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Title))
                    return Json(new { success = false, message = "Title is required." });
                if (string.IsNullOrWhiteSpace(model.Notes))
                    return Json(new { success = false, message = "Notes are required." });
                if (model.RadiologistDoctorId <= 0)
                    return Json(new { success = false, message = "Please select the radiologist." });

                var admission = _admissionRepo.GetIPDAdmissionById(model.IPDId, HospitalId, SubHospitalId);
                if (admission == null) return Json(new { success = false, message = "Admission not found." });

                model.ParentHospitalId = HospitalId;
                model.SubHospitalId = SubHospitalId;
                if (model.ReportDateTime == default) model.ReportDateTime = DateTime.Now;
                model.EnteredByUserId = CurrentUserId;

                var newId = _radiologyRepo.InsertReport(model);

                if (saveAsTemplate && !string.IsNullOrWhiteSpace(templateName))
                {
                    _radiologyRepo.SaveTemplate(HospitalId, new RadiologyTemplateModel
                    {
                        TemplateName = templateName,
                        TemplateText = model.Notes
                    }, CurrentUserId);
                }

                return Json(new { success = true, message = "Radiology report added." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddReport");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditReport(RadiologyReportModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Title))
                    return Json(new { success = false, message = "Title is required." });
                if (string.IsNullOrWhiteSpace(model.Notes))
                    return Json(new { success = false, message = "Notes are required." });
                if (model.RadiologistDoctorId <= 0)
                    return Json(new { success = false, message = "Please select the radiologist." });

                _radiologyRepo.UpdateReport(model);
                return Json(new { success = true, message = "Radiology report updated." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EditReport");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteReport(int id, int ipdId)
        {
            try
            {
                _radiologyRepo.DeleteReport(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteReport");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // PRINT - single report (id supplied) or the full list (no id)
        // ===============================================================
        [HttpGet]
        public IActionResult Print(int ipdId, int? id = null)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound();

            try { ViewBag.Hospital = _hospitalRepo.GetsubandMainHospitalById(HospitalId, SubHospitalId); }
            catch { ViewBag.Hospital = null; }

            var reports = _radiologyRepo.GetReportsByIPD(ipdId);
            if (id.HasValue) reports = reports.Where(r => r.Id == id.Value).ToList();

            var vm = new RadiologyReportsVM { Admission = admission, Reports = reports };
            return View(vm);
        }
    }
}
