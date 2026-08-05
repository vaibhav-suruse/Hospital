
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class LabReportsController : Controller
    {
        private readonly ILabReports _labRepo;
        private readonly IIPDAdmission _admissionRepo;
        private readonly IDoctor _doctorRepo;
        private readonly IHospital _hospitalRepo;

        public LabReportsController(ILabReports labRepo, IIPDAdmission admissionRepo, IDoctor doctorRepo, IHospital hospitalRepo)
        {
            _labRepo = labRepo;
            _admissionRepo = admissionRepo;
            _doctorRepo = doctorRepo;
            _hospitalRepo = hospitalRepo;
        }

        private static int ComputeDayNumber(DateTime admissionDateTime, DateTime recordedDateTime)
        {
            var days = (recordedDateTime.Date - admissionDateTime.Date).Days + 1;
            return days < 1 ? 1 : days;
        }

        // Effective end of stay for day-tab counting:
        // - a discharged patient's day tabs end at the discharge date,
        // - an admitted patient's continue up to today.
        // This stops the Clinical Details screens from showing extra "Day N"
        // tabs beyond the patient's actual length of stay after discharge.
        private static DateTime EffectiveEndDate(IPDAdmissionModel admission)
        {
            if (admission != null
                && string.Equals(admission.Status, "Discharged", StringComparison.OrdinalIgnoreCase)
                && admission.ActualDischargeDateTime.HasValue)
            {
                return admission.ActualDischargeDateTime.Value;
            }
            return DateTime.Now;
        }

        private static DateTime DateForDay(DateTime admissionDateTime, int dayNumber) =>
            admissionDateTime.Date.AddDays(Math.Max(1, dayNumber) - 1);

        private static DateTime EffectiveOrderDateTime(DateTime admissionDateTime, int dayNumber)
        {
            var targetDate = DateForDay(admissionDateTime, dayNumber);
            return targetDate == DateTime.Now.Date ? DateTime.Now : targetDate.Add(DateTime.Now.TimeOfDay);
        }

        private int HospitalId => HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        private int? SubHospitalId => HttpContext.Session.GetInt32("SubHospitalId");

        private void LoadDoctors()
        {
            var doctors = _doctorRepo.GetAllDoctor(HospitalId, SubHospitalId) ?? new List<Doctor>();
            ViewBag.Doctors = new SelectList(
                doctors.Select(d => new { Id = d.Doctor_Id, Name = "Dr. " + d.FirstName + " " + d.LastName }),
                "Id", "Name");
        }

        // ===============================================================
        // INDEX - "Lab Reports" (Clinical Details menu)
        // ===============================================================
        [HttpGet]
        public IActionResult Index(int ipdId, int? activeDay = null)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound();

            var vm = new LabReportsVM { Admission = admission };

            // For a discharged patient the day tabs must stop at the discharge
            // date; for an admitted patient they continue up to today.
            int daysSinceAdmission = ComputeDayNumber(admission.AdmissionDateTime, EffectiveEndDate(admission));
            vm.TotalDays = Math.Max(daysSinceAdmission, 1);
            vm.ActiveDay = activeDay.HasValue ? Math.Min(Math.Max(1, activeDay.Value), vm.TotalDays) : daysSinceAdmission;

            var dayDate = DateForDay(admission.AdmissionDateTime, vm.ActiveDay);
            vm.Values = _labRepo.GetLabValues(ipdId).Where(v => v.ReportDate.Date == dayDate).ToList();

            LoadDoctors();
            return View(vm);
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult LabValuesPartial(int ipdId, int dayNumber)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound();
            var dayDate = DateForDay(admission.AdmissionDateTime, dayNumber);

            var list = _labRepo.GetLabValues(ipdId).Where(v => v.ReportDate.Date == dayDate).ToList();
            ViewBag.IPDId = ipdId;
            return PartialView("_LabValuesPartial", list);
        }

        // ===============================================================
        // MASTER CATALOG - Categories & Parameters
        // (hospital-wide, not tied to any single patient or day)
        // ===============================================================
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult GetCategories()
        {
            return Json(_labRepo.GetCategories(HospitalId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCategory(string categoryName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryName))
                    return Json(new { success = false, message = "Category name is required." });

                var newId = _labRepo.InsertCategory(HospitalId, categoryName);
                return Json(new { success = true, id = newId, name = categoryName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategory(int categoryId)
        {
            try
            {
                _labRepo.DeleteCategory(categoryId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult GetParametersByCategory(int categoryId)
        {
            return Json(_labRepo.GetParametersByCategory(categoryId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddParameter(LabTestParameterModel model)
        {
            try
            {
                if (model.CategoryId <= 0)
                    return Json(new { success = false, message = "Please select a category first." });
                if (string.IsNullOrWhiteSpace(model.ParameterName))
                    return Json(new { success = false, message = "Parameter name is required." });

                var newId = _labRepo.InsertParameter(model);
                return Json(new { success = true, id = newId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateParameter(LabTestParameterModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.ParameterName))
                    return Json(new { success = false, message = "Parameter name is required." });

                _labRepo.UpdateParameter(model);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteParameter(int parameterId)
        {
            try
            {
                _labRepo.DeleteParameter(parameterId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // PATIENT LAB VALUES
        // ===============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddLabValues(int ipdId, int dayNumber, int doctorId, List<LabValueEntryModel> values)
        {
            try
            {
                if (values == null || values.Count == 0)
                    return Json(new { success = false, message = "Please enter at least one value." });

                var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
                if (admission == null) return Json(new { success = false, message = "Admission not found." });

                var effectiveDate = EffectiveOrderDateTime(admission.AdmissionDateTime, dayNumber);
                var newIds = new List<int>();
                foreach (var entry in values)
                {
                    if (string.IsNullOrWhiteSpace(entry.ResultValue)) continue; // skip parameters left blank
                    var newId = _labRepo.InsertLabValue(HospitalId, SubHospitalId, ipdId, entry.ParameterId, entry.ResultValue.Trim(), effectiveDate, doctorId > 0 ? doctorId : (int?)null);
                    newIds.Add(newId);
                }

                if (newIds.Count == 0)
                    return Json(new { success = false, message = "Please enter at least one value." });

                // Check which of the just-saved results are CRITICAL (not
                // just routinely abnormal) so the UI can raise a hard alert
                // immediately, rather than waiting for someone to notice a
                // badge in a table later.
                var savedValues = _labRepo.GetLabValues(ipdId).Where(v => newIds.Contains(v.Id)).ToList();
                var criticalAlerts = savedValues.Where(v => v.IsCritical).Select(v => new CriticalResultAlert
                {
                    LabValueId = v.Id,
                    ParameterName = v.ParameterName,
                    ResultValue = v.ResultValue,
                    Unit = v.Unit
                }).ToList();

                return Json(new { success = true, message = $"{newIds.Count} value(s) saved.", criticalAlerts });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NotifyCritical(int labValueId, int notifiedToDoctorId, string remarks)
        {
            try
            {
                if (notifiedToDoctorId <= 0)
                    return Json(new { success = false, message = "Please select which doctor was notified." });

                var userId = HttpContext.Session.GetInt32("UserId");
                _labRepo.InsertCriticalNotification(labValueId, notifiedToDoctorId, userId, remarks);
                return Json(new { success = true, message = "Notification recorded." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditLabValue(int id, string resultValue)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(resultValue))
                    return Json(new { success = false, message = "Value is required." });

                _labRepo.UpdateLabValue(id, resultValue.Trim());
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteLabValue(int id, int ipdId)
        {
            try
            {
                _labRepo.DeleteLabValue(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // PRINT
        // ===============================================================
        [HttpGet]
        public IActionResult Print(int ipdId, int? dayNumber = null)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound();

            try { ViewBag.Hospital = _hospitalRepo.GetsubandMainHospitalById(HospitalId, SubHospitalId); }
            catch { ViewBag.Hospital = null; }

            var vm = new LabReportsVM { Admission = admission };
            var allValues = _labRepo.GetLabValues(ipdId);

            int daysSinceAdmission = ComputeDayNumber(admission.AdmissionDateTime, EffectiveEndDate(admission));
            vm.TotalDays = Math.Max(daysSinceAdmission, 1);

            var valuesByDay = new Dictionary<int, List<LabReportValueVM>>();
            if (dayNumber.HasValue)
            {
                int day = Math.Min(Math.Max(1, dayNumber.Value), vm.TotalDays);
                var dayDate = DateForDay(admission.AdmissionDateTime, day);
                valuesByDay[day] = allValues.Where(v => v.ReportDate.Date == dayDate).ToList();
                vm.ActiveDay = day;
            }
            else
            {
                for (int day = 1; day <= vm.TotalDays; day++)
                {
                    var dayDate = DateForDay(admission.AdmissionDateTime, day);
                    valuesByDay[day] = allValues.Where(v => v.ReportDate.Date == dayDate).ToList();
                }
            }
            ViewBag.ValuesByDay = valuesByDay;
            ViewBag.SingleDay = dayNumber;

            return View(vm);
        }
    }
}
