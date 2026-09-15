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
    public class TreatmentSheetController : Controller
    {
        private readonly ILogger<TreatmentSheetController> _logger;
        private readonly ITreatmentSheet _sheetRepo;
        private readonly IDailyNotes _dailyNotesRepo;   // reused: MAR + medicine search + discontinue (proven, working code)
        private readonly IIPDAdmission _admissionRepo;
        private readonly IDoctor _doctorRepo;
        private readonly INurse _nurseRepo;
        private readonly IAdmissionNotes _admissionNotesRepo; // reused: allergy safety banner
        private readonly IHospital _hospitalRepo; // reused: print letterhead (name/address/logo)

        public TreatmentSheetController(
            ITreatmentSheet sheetRepo,
            IDailyNotes dailyNotesRepo,
            IIPDAdmission admissionRepo,
            IDoctor doctorRepo,
            INurse nurseRepo,
            IAdmissionNotes admissionNotesRepo,
            IHospital hospitalRepo, ILogger<TreatmentSheetController> logger)
        {
            _sheetRepo = sheetRepo;
            _logger = logger;
            _dailyNotesRepo = dailyNotesRepo;
            _admissionRepo = admissionRepo;
            _doctorRepo = doctorRepo;
            _nurseRepo = nurseRepo;
            _admissionNotesRepo = admissionNotesRepo;
            _hospitalRepo = hospitalRepo;
        }

        // Day 1 = admission date. Same convention as NurseVitalsController,
        // kept identical on purpose so "Day 3" means the same thing across
        // every Clinical Details screen.
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

        // Same date range convention NurseVitals already uses: Day N = the
        // calendar date that many days after the admission date.
        private static DateTime DateForDay(DateTime admissionDateTime, int dayNumber) =>
            admissionDateTime.Date.AddDays(Math.Max(1, dayNumber) - 1);

        // If the person is entering something while looking at a past/future
        // day tab (not "today"), keep that day's date but stamp the current
        // time-of-day - identical convention to NurseVitalsController.Create.
        private static DateTime EffectiveOrderDateTime(DateTime admissionDateTime, int dayNumber)
        {
            var targetDate = DateForDay(admissionDateTime, dayNumber);
            return targetDate == DateTime.Now.Date ? DateTime.Now : targetDate.Add(DateTime.Now.TimeOfDay);
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

        private void LoadNurses()
        {
            var nurses = _nurseRepo.GetAll(HospitalId, SubHospitalId) ?? new List<NurseModel>();
            ViewBag.Nurses = new SelectList(
                nurses.Select(n => new { Id = n.NurseId, Name = n.FirstName + " " + n.LastName }),
                "Id", "Name");
        }

        // ===============================================================
        // INDEX - "Treatment Sheet" (Clinical Details menu, item 2)
        // ===============================================================
        [HttpGet]
        public IActionResult Index(int ipdId, int? activeDay = null)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null)
                return NotFound(); // multi-tenant safe - lookup is already hospital-scoped

            var vm = new TreatmentSheetVM { Admission = admission };

            // For a discharged patient the day tabs must stop at the discharge
            // date; for an admitted patient they continue up to today.
            DateTime endDate = EffectiveEndDate(admission);
            int daysSinceAdmission = ComputeDayNumber(admission.AdmissionDateTime, endDate);
            vm.TotalDays = Math.Max(daysSinceAdmission, 1);
            vm.ActiveDay = activeDay.HasValue ? Math.Min(Math.Max(1, activeDay.Value), vm.TotalDays) : daysSinceAdmission;

            var dayDate = DateForDay(admission.AdmissionDateTime, vm.ActiveDay);
            vm.GeneralOrders = _sheetRepo.GetGeneralOrders(ipdId).Where(g => g.CreatedDate.Date == dayDate).ToList();
            vm.Medications = _sheetRepo.GetMedications(ipdId).Where(m => m.CreatedDate.Date == dayDate).ToList();
            vm.Investigations = _sheetRepo.GetInvestigations(ipdId).Where(i => i.OrderedDateTime.Date == dayDate).ToList();

            try
            {
                // Best-effort: never let an allergy lookup failure block the whole sheet.
                var allergies = _admissionNotesRepo.GetAllergiesAsync(admission.PatientId, ipdId).GetAwaiter().GetResult();
                vm.Allergies = (allergies ?? new List<PatientAllergyModel>())
                    .Where(a => string.Equals(a.ClinicalStatus, "Active", StringComparison.OrdinalIgnoreCase) || a.ClinicalStatus == null)
                    .ToList();
            }
            catch { vm.Allergies = new List<PatientAllergyModel>(); }

            LoadDoctors();
            LoadNurses();

            return View(vm);
        }

        // ===============================================================
        // AJAX - MAR grid for one day (tab switching, no full reload)
        // ===============================================================
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult DayMAR(int ipdId, int dayNumber)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound();

            var scheduledDate = admission.AdmissionDateTime.Date.AddDays(dayNumber - 1);
            var mar = _dailyNotesRepo.GetOrCreateMARForDate(ipdId, scheduledDate) ?? new List<MARRowModel>();

            ViewBag.IPDId = ipdId;
            ViewBag.DayNumber = dayNumber;
            ViewBag.DayDate = scheduledDate;
            LoadNurses();

            return PartialView("_DayMARPartial", mar);
        }

        [HttpPost]
        public IActionResult RecordAdministration([FromBody] RecordAdministrationModel model)
        {
            try
            {
                if (model == null || model.MarId <= 0) return Json(new { success = false, message = "Invalid record." });
                if (model.GivenBy <= 0) return Json(new { success = false, message = "Please select the nurse recording this." });
                if (model.Status != "Given" && model.Status != "Missed" && model.Status != "Refused")
                    return Json(new { success = false, message = "Invalid status." });

                _dailyNotesRepo.RecordAdministration(model);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RecordAdministration");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // GENERAL ORDERS
        // ===============================================================
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult GeneralOrdersPartial(int ipdId, int dayNumber)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound();
            var dayDate = DateForDay(admission.AdmissionDateTime, dayNumber);

            var list = _sheetRepo.GetGeneralOrders(ipdId).Where(g => g.CreatedDate.Date == dayDate).ToList();
            ViewBag.IPDId = ipdId;
            return PartialView("_GeneralOrdersPartial", list);
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult SearchGeneralOrderTemplates(string term)
        {
            var list = _sheetRepo.GetGeneralOrderTemplates(HospitalId, term);
            return Json(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveGeneralOrderTemplate(GeneralOrderTemplateModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.TemplateName) || string.IsNullOrWhiteSpace(model.TemplateText))
                    return Json(new { success = false, message = "Template name and text are required." });

                _sheetRepo.SaveGeneralOrderTemplate(HospitalId, model, CurrentUserId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveGeneralOrderTemplate");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddGeneralOrder(GeneralOrderModel model, int dayNumber, bool saveAsTemplate = false, string templateName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.OrderDetails))
                    return Json(new { success = false, message = "Order details are required." });
                if (model.DoctorId <= 0)
                    return Json(new { success = false, message = "Please select the ordering doctor." });

                var admission = _admissionRepo.GetIPDAdmissionById(model.IPDId, HospitalId, SubHospitalId);
                if (admission == null) return Json(new { success = false, message = "Admission not found." });

                model.ParentHospitalId = HospitalId;
                model.SubHospitalId = SubHospitalId;
                var effectiveDate = EffectiveOrderDateTime(admission.AdmissionDateTime, dayNumber);
                var newId = _sheetRepo.InsertGeneralOrder(model, effectiveDate);

                if (saveAsTemplate && !string.IsNullOrWhiteSpace(templateName))
                {
                    _sheetRepo.SaveGeneralOrderTemplate(HospitalId, new GeneralOrderTemplateModel
                    {
                        TemplateName = templateName,
                        TemplateText = model.OrderDetails
                    }, CurrentUserId);
                }

                return Json(new { success = true, message = "General order added." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddGeneralOrder");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditGeneralOrder(GeneralOrderModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.OrderDetails))
                    return Json(new { success = false, message = "Order details are required." });

                _sheetRepo.UpdateGeneralOrder(model);
                return Json(new { success = true, message = "General order updated." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EditGeneralOrder");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetGeneralOrderStatus(int id, string status, int ipdId)
        {
            try
            {
                _sheetRepo.SetGeneralOrderStatus(id, status);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SetGeneralOrderStatus");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteGeneralOrder(int id, int ipdId)
        {
            try
            {
                _sheetRepo.DeleteGeneralOrder(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteGeneralOrder");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // MEDICATIONS
        // ===============================================================
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult MedicationsPartial(int ipdId, int dayNumber)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound();
            var dayDate = DateForDay(admission.AdmissionDateTime, dayNumber);

            var list = _sheetRepo.GetMedications(ipdId).Where(m => m.CreatedDate.Date == dayDate).ToList();
            ViewBag.IPDId = ipdId;
            return PartialView("_MedicationsPartial", list);
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
        public IActionResult AddMedicine(TreatmentMedicineVM model, int doctorId, int dayNumber)
        {
            try
            {
                if (model.MedicineId <= 0)
                    return Json(new { success = false, message = "Please choose a medicine from the search results." });
                if (!model.Morning && !model.Afternoon && !model.Evening && string.IsNullOrWhiteSpace(model.FrequencyText))
                    return Json(new { success = false, message = "Please select at least one dosage time or enter a custom frequency." });
                if (doctorId <= 0)
                    return Json(new { success = false, message = "Please select the ordering doctor." });

                var admission = _admissionRepo.GetIPDAdmissionById(model.IPDId, HospitalId, SubHospitalId);
                if (admission == null) return Json(new { success = false, message = "Admission not found." });

                var effectiveDate = EffectiveOrderDateTime(admission.AdmissionDateTime, dayNumber);
                var newId = _sheetRepo.InsertMedicine(HospitalId, SubHospitalId, model.IPDId, doctorId, model, effectiveDate);

                return Json(new { success = true, message = "Medicine added.", id = newId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddMedicine");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditMedicine(TreatmentMedicineVM model)
        {
            try
            {
                if (!model.Morning && !model.Afternoon && !model.Evening && string.IsNullOrWhiteSpace(model.FrequencyText))
                    return Json(new { success = false, message = "Please select at least one dosage time or enter a custom frequency." });

                _sheetRepo.UpdateMedicine(model);
                return Json(new { success = true, message = "Medicine order updated." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EditMedicine");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DiscontinueMedicine(int prescriptionId, int ipdId)
        {
            try
            {
                _dailyNotesRepo.DiscontinueMedicine(prescriptionId, HospitalId, SubHospitalId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DiscontinueMedicine");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ReorderMedicines([FromBody] List<int> orderedIds, int ipdId)
        {
            try
            {
                for (int i = 0; i < (orderedIds?.Count ?? 0); i++)
                    _sheetRepo.UpdateMedicineSortOrder(orderedIds[i], i + 1);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReorderMedicines");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // INVESTIGATIONS
        // ===============================================================
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult InvestigationsPartial(int ipdId, int dayNumber)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound();
            var dayDate = DateForDay(admission.AdmissionDateTime, dayNumber);

            var list = _sheetRepo.GetInvestigations(ipdId).Where(i => i.OrderedDateTime.Date == dayDate).ToList();
            ViewBag.IPDId = ipdId;
            return PartialView("_InvestigationsPartial", list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddInvestigation(TreatmentInvestigationVM model, int doctorId, int dayNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.TestName))
                    return Json(new { success = false, message = "Test name is required." });
                if (doctorId <= 0)
                    return Json(new { success = false, message = "Please select the ordering doctor." });

                var admission = _admissionRepo.GetIPDAdmissionById(model.IPDId, HospitalId, SubHospitalId);
                if (admission == null) return Json(new { success = false, message = "Admission not found." });

                var effectiveDate = EffectiveOrderDateTime(admission.AdmissionDateTime, dayNumber);
                var newId = _sheetRepo.InsertInvestigation(HospitalId, SubHospitalId, model.IPDId, doctorId, model, effectiveDate);

                return Json(new { success = true, message = "Investigation ordered.", id = newId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddInvestigation");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditInvestigation(TreatmentInvestigationVM model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.TestName))
                    return Json(new { success = false, message = "Test name is required." });

                _sheetRepo.UpdateInvestigation(model);
                return Json(new { success = true, message = "Investigation updated." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EditInvestigation");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteInvestigation(int id, int ipdId)
        {
            try
            {
                _sheetRepo.DeleteInvestigation(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteInvestigation");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // PRINT - single day (with MAR) or the whole stay
        // ===============================================================
        [HttpGet]
        public IActionResult Print(int ipdId, int? dayNumber = null)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound();

            try { ViewBag.Hospital = _hospitalRepo.GetsubandMainHospitalById(HospitalId, SubHospitalId); }
            catch { ViewBag.Hospital = null; }

            var vm = new TreatmentSheetVM { Admission = admission };
            var allOrders = _sheetRepo.GetGeneralOrders(ipdId);
            var allMeds = _sheetRepo.GetMedications(ipdId);
            var allInvestigations = _sheetRepo.GetInvestigations(ipdId);

            int daysSinceAdmission = ComputeDayNumber(admission.AdmissionDateTime, EffectiveEndDate(admission));
            vm.TotalDays = Math.Max(daysSinceAdmission, 1);

            var marByDay = new Dictionary<int, List<MARRowModel>>();
            var ordersByDay = new Dictionary<int, List<GeneralOrderModel>>();
            var medsByDay = new Dictionary<int, List<TreatmentMedicineVM>>();
            var investigationsByDay = new Dictionary<int, List<TreatmentInvestigationVM>>();

            void LoadDay(int day)
            {
                var dayDate = DateForDay(admission.AdmissionDateTime, day);
                marByDay[day] = _dailyNotesRepo.GetOrCreateMARForDate(ipdId, dayDate);
                ordersByDay[day] = allOrders.Where(g => g.CreatedDate.Date == dayDate).ToList();
                medsByDay[day] = allMeds.Where(m => m.CreatedDate.Date == dayDate).ToList();
                investigationsByDay[day] = allInvestigations.Where(i => i.OrderedDateTime.Date == dayDate).ToList();
            }

            if (dayNumber.HasValue)
            {
                int day = Math.Min(Math.Max(1, dayNumber.Value), vm.TotalDays);
                LoadDay(day);
                vm.ActiveDay = day;
            }
            else
            {
                for (int day = 1; day <= vm.TotalDays; day++) LoadDay(day);
            }

            ViewBag.MarByDay = marByDay;
            ViewBag.OrdersByDay = ordersByDay;
            ViewBag.MedsByDay = medsByDay;
            ViewBag.InvestigationsByDay = investigationsByDay;
            ViewBag.SingleDay = dayNumber;

            return View(vm);
        }
    }
}
