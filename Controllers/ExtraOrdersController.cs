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
    public class ExtraOrdersController : Controller
    {
        private readonly IExtraOrders _extraOrdersRepo;
        private readonly IIPDAdmission _admissionRepo;
        private readonly IDoctor _doctorRepo;
        private readonly IDailyNotes _dailyNotesRepo; // reused: existing medicine search, proven and working

        public ExtraOrdersController(
            IExtraOrders extraOrdersRepo,
            IIPDAdmission admissionRepo,
            IDoctor doctorRepo,
            IDailyNotes dailyNotesRepo)
        {
            _extraOrdersRepo = extraOrdersRepo;
            _admissionRepo = admissionRepo;
            _doctorRepo = doctorRepo;
            _dailyNotesRepo = dailyNotesRepo;
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
        // INDEX - "Extra Orders" (Clinical Details menu)
        // ===============================================================
        [HttpGet]
        public IActionResult Index(int ipdId, int? activeDay = null)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound();

            var vm = new ExtraOrdersVM { Admission = admission };

            // For a discharged patient the day tabs must stop at the discharge
            // date; for an admitted patient they continue up to today.
            int daysSinceAdmission = ComputeDayNumber(admission.AdmissionDateTime, EffectiveEndDate(admission));
            vm.TotalDays = Math.Max(daysSinceAdmission, 1);
            vm.ActiveDay = activeDay.HasValue ? Math.Min(Math.Max(1, activeDay.Value), vm.TotalDays) : daysSinceAdmission;

            var dayDate = DateForDay(admission.AdmissionDateTime, vm.ActiveDay);
            vm.ExtraMedications = _extraOrdersRepo.GetExtraMedications(ipdId).Where(m => m.TimeGiven.Date == dayDate).ToList();
            vm.ExtraOrders = _extraOrdersRepo.GetExtraOrders(ipdId).Where(o => o.TimeGiven.Date == dayDate).ToList();

            LoadDoctors();
            return View(vm);
        }

        // ===============================================================
        // EXTRA MEDICATION
        // ===============================================================
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult ExtraMedicationPartial(int ipdId, int dayNumber)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound();
            var dayDate = DateForDay(admission.AdmissionDateTime, dayNumber);

            var list = _extraOrdersRepo.GetExtraMedications(ipdId).Where(m => m.TimeGiven.Date == dayDate).ToList();
            ViewBag.IPDId = ipdId;
            return PartialView("_ExtraMedicationPartial", list);
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult SearchMedicine(string term)
        {
            var list = _dailyNotesRepo.SearchMedicinesForDailyNotes(term);
            return Json(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddExtraMedication(ExtraMedicationModel model, int dayNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.MedicineName))
                    return Json(new { success = false, message = "Please choose or type a medicine name." });

                var admission = _admissionRepo.GetIPDAdmissionById(model.IPDId, HospitalId, SubHospitalId);
                if (admission == null) return Json(new { success = false, message = "Admission not found." });

                var effectiveDate = EffectiveOrderDateTime(admission.AdmissionDateTime, dayNumber);
                _extraOrdersRepo.InsertExtraMedication(HospitalId, SubHospitalId, model, effectiveDate);

                return Json(new { success = true, message = "Extra medication logged." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditExtraMedication(ExtraMedicationModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.MedicineName))
                    return Json(new { success = false, message = "Please choose or type a medicine name." });

                _extraOrdersRepo.UpdateExtraMedication(model);
                return Json(new { success = true, message = "Extra medication updated." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteExtraMedication(int id, int ipdId)
        {
            try
            {
                _extraOrdersRepo.DeleteExtraMedication(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // EXTRA ORDERS
        // ===============================================================
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult ExtraOrdersListPartial(int ipdId, int dayNumber)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound();
            var dayDate = DateForDay(admission.AdmissionDateTime, dayNumber);

            var list = _extraOrdersRepo.GetExtraOrders(ipdId).Where(o => o.TimeGiven.Date == dayDate).ToList();
            ViewBag.IPDId = ipdId;
            return PartialView("_ExtraOrdersListPartial", list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddExtraOrder(ExtraOrderModel model, int dayNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.OrderDetails))
                    return Json(new { success = false, message = "Order details are required." });

                var admission = _admissionRepo.GetIPDAdmissionById(model.IPDId, HospitalId, SubHospitalId);
                if (admission == null) return Json(new { success = false, message = "Admission not found." });

                var effectiveDate = EffectiveOrderDateTime(admission.AdmissionDateTime, dayNumber);
                _extraOrdersRepo.InsertExtraOrder(HospitalId, SubHospitalId, model, effectiveDate);

                return Json(new { success = true, message = "Extra order added." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditExtraOrder(ExtraOrderModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.OrderDetails))
                    return Json(new { success = false, message = "Order details are required." });

                _extraOrdersRepo.UpdateExtraOrder(model);
                return Json(new { success = true, message = "Extra order updated." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteExtraOrder(int id, int ipdId)
        {
            try
            {
                _extraOrdersRepo.DeleteExtraOrder(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
