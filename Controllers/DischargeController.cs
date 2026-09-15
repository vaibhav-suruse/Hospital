// DischargeController.cs
// Full file — replace your existing DischargeController.cs with this

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;
using Microsoft.Extensions.Logging;

namespace WebApplicationSampleTest2.Controllers
{
    public class DischargeController : Controller
    {
        private readonly ILogger<DischargeController> _logger;
        private readonly IDischarge _dischargeRepo;
        private readonly IDoctor _doctorRepo;
        private readonly IHospital _IHospital;
        private readonly IMedicine _medicineRepo;
        private readonly IDoctorRound _roundRepo;
        private readonly IIPDNurseVitals _vitalsRepo;
        private readonly IDiagnosis _diagnosisRepo;
        private readonly IIPDOperation _operationRepo;
        private readonly ILabReports _labReportsRepo;
        private readonly IAdmissionNotes _admissionNotesRepo;
        private readonly IRadiology _radiologyRepo;

        public DischargeController(
            IDischarge dischargeRepo,
            IDoctor doctorRepo,
            IHospital iHospital,
            IMedicine medicineRepo,
            IDoctorRound roundRepo,
            IIPDNurseVitals vitalsRepo,
            IDiagnosis diagnosisRepo,
            IIPDOperation operationRepo,
            ILabReports labReportsRepo,
            IAdmissionNotes admissionNotesRepo,
            IRadiology radiologyRepo, ILogger<DischargeController> logger)
        {
            _dischargeRepo = dischargeRepo;
            _logger = logger;
            _doctorRepo = doctorRepo;
            _IHospital = iHospital;
            _medicineRepo = medicineRepo;
            _roundRepo = roundRepo;
            _vitalsRepo = vitalsRepo;
            _diagnosisRepo = diagnosisRepo;
            _operationRepo = operationRepo;
            _labReportsRepo = labReportsRepo;
            _admissionNotesRepo = admissionNotesRepo;
            _radiologyRepo = radiologyRepo;
        }

        private int HospitalId => HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        private int? SubHospitalId => HttpContext.Session.GetInt32("SubHospitalId");

        // Pulls Allergies, Medical History, Chief Complaints, Admission Diagnosis,
        // Discharge Diagnosis, Procedures/Operations, Lab Reports, and Radiology
        // Reports for this admission into the ViewBag so the discharge form and
        // the final summary/print can show them - no re-typing data that already
        // lives in those modules. ipdId/patientId are assumed already
        // hospital-verified by the caller (every action below checks ownership
        // via GetAdmissionForDischarge / GetDischargeSummary before this runs).
        private async Task LoadClinicalReferenceData(int ipdId, int patientId)
        {
            // Diagnosis - admission and discharge shown separately, never merged.
            // What was suspected on admission vs. what was confirmed by discharge
            // is itself part of the clinical record, not just a fallback chain.
            ViewBag.AdmissionDiagnoses = await _diagnosisRepo.GetAdmissionDiagnosis(ipdId)
                ?? new List<DiagnosisModel>();
            ViewBag.DischargeDiagnoses = await _diagnosisRepo.GetDischargeDiagnosis(ipdId)
                ?? new List<DiagnosisModel>();

            // Allergies - patient safety, shown prominently regardless of section order
            ViewBag.Allergies = await _admissionNotesRepo.GetAllergiesAsync(patientId, ipdId)
                ?? new List<PatientAllergyModel>();

            // Medical history / comorbidities
            ViewBag.MedicalHistory = await _admissionNotesRepo.GetMedicalHistoryAsync(patientId)
                ?? new List<PatientMedicalHistoryModel>();

            // Chief complaints (why the patient came in)
            ViewBag.ChiefComplaints = await _admissionNotesRepo.GetChiefComplaintsAsync(ipdId)
                ?? new List<ChiefComplaintModel>();

            ViewBag.Procedures = _operationRepo.GetByIPDId(ipdId) ?? new List<IPDOperationModel>();

            ViewBag.LabResults = _labReportsRepo.GetLabValues(ipdId) ?? new List<LabReportValueVM>();

            ViewBag.RadiologyReports = _radiologyRepo.GetReportsByIPD(ipdId) ?? new List<RadiologyReportModel>();
        }

        [HttpGet]
        public async Task<IActionResult> Discharge(int ipdId)
        {
            try
            {
                var model = _dischargeRepo.GetAdmissionForDischarge(ipdId, HospitalId, SubHospitalId);
                if (model == null) return NotFound();

                if (model.Status == "Discharged")
                {
                    return RedirectToAction("Summary", new { ipdId = ipdId });
                }

                model.DischargeMedicines = _dischargeRepo.GetDischargeMedicines(ipdId, HospitalId, SubHospitalId);
                LoadDropdowns(ipdId);

                await LoadClinicalReferenceData(ipdId, model.PatientId);

                // Pre-fill Final Diagnosis from the discharge-tagged diagnosis records
                // (falling back to admission-tagged if none exist yet) so the doctor
                // isn't retyping something already recorded - still fully editable.
                var dischargeDx = ViewBag.DischargeDiagnoses as List<DiagnosisModel>;
                var admissionDx = ViewBag.AdmissionDiagnoses as List<DiagnosisModel>;
                var dxForPrefill = (dischargeDx != null && dischargeDx.Count > 0) ? dischargeDx : admissionDx;
                if (string.IsNullOrWhiteSpace(model.FinalDiagnosis) && dxForPrefill != null && dxForPrefill.Count > 0)
                {
                    model.FinalDiagnosis = string.Join("; ", dxForPrefill.Select(d => d.DiagnosisName));
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Action");
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", "IPDAdmission", new { id = ipdId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Discharge(DischargeModel model)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            if (!ModelState.IsValid)
            {
                LoadDropdowns(model.IPDId);
                var existing = _dischargeRepo.GetAdmissionForDischarge(model.IPDId, HospitalId, SubHospitalId);
                model.PatientName = existing.PatientName;
                model.AdmissionNumber = existing.AdmissionNumber;
                model.AdmissionDateTime = existing.AdmissionDateTime;
                model.PrimaryDoctorName = existing.PrimaryDoctorName;
                model.BedNumber = existing.BedNumber;
                model.WardName = existing.WardName;
                model.TotalDaysStayed = existing.TotalDaysStayed;
                return View(model);
            }

            try
            {
                // Step 1: Discharge the patient
                _dischargeRepo.DischargePatient(model, userId, HospitalId, SubHospitalId);

                // Step 2: Save medicines submitted with the form
                if (model.DischargeMedicines != null && model.DischargeMedicines.Count > 0)
                {
                    var existingMeds = _dischargeRepo.GetDischargeMedicines(model.IPDId, HospitalId, SubHospitalId);
                    foreach (var existing2 in existingMeds)
                        _dischargeRepo.DeleteDischargeMedicine(existing2.Id, HospitalId, SubHospitalId);

                    foreach (var med in model.DischargeMedicines)
                    {
                        if (med.MedicineId <= 0) continue;
                        med.IPDId = model.IPDId;
                        _dischargeRepo.SaveDischargeMedicine(med, userId, HospitalId, SubHospitalId);
                    }
                }

                TempData["Success"] = "Patient discharged successfully.";
                return RedirectToAction("Summary", new { ipdId = model.IPDId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Discharge");
                TempData["Error"] = ex.Message;
                LoadDropdowns(model.IPDId);
                var existingOnError = _dischargeRepo.GetAdmissionForDischarge(model.IPDId, HospitalId, SubHospitalId);
                model.PatientName = existingOnError.PatientName;
                model.AdmissionNumber = existingOnError.AdmissionNumber;
                model.AdmissionDateTime = existingOnError.AdmissionDateTime;
                model.PrimaryDoctorName = existingOnError.PrimaryDoctorName;
                model.BedNumber = existingOnError.BedNumber;
                model.WardName = existingOnError.WardName;
                model.TotalDaysStayed = existingOnError.TotalDaysStayed;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Summary(int ipdId)
        {
            try
            {
                var model = _dischargeRepo.GetDischargeSummary(ipdId, HospitalId, SubHospitalId);
                if (model == null) return NotFound();

                model.DischargeMedicines = _dischargeRepo.GetDischargeMedicines(ipdId, HospitalId, SubHospitalId);

                // Load rounds
                var allRounds = _roundRepo.GetAllRoundsPrescriptionPrint(ipdId);
                ViewBag.Rounds = allRounds?.Rounds ?? new List<IPDRoundPrintVM>();

                // ✅ Load vitals
                ViewBag.Vitals = _vitalsRepo.GetVitalsByIPDId(ipdId, HospitalId, SubHospitalId)
                                 ?? new List<IPDNurseVitals>();

                await LoadClinicalReferenceData(ipdId, model.PatientId);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Discharge");
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "IPDAdmission");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PrintSummary(int ipdId)
        {
            try
            {
                var model = _dischargeRepo.GetDischargeSummary(ipdId, HospitalId, SubHospitalId);
                if (model == null) return NotFound();

                var hospital = _IHospital.GetsubandMainHospitalById(HospitalId, SubHospitalId);
                ViewBag.HospitalName = hospital?.Name;
                ViewBag.HospitalAddress = hospital?.Address;
                ViewBag.HospitalPhone = hospital?.PhoneNumber;
                ViewBag.HospitalEmail = hospital?.EmailId;
                ViewBag.HospitalLogo = hospital?.Logo;
                ViewBag.HospitalRegNo = hospital?.RegistrationNumber;

                ViewBag.DischargeMedicines = _dischargeRepo.GetDischargeMedicines(ipdId, HospitalId, SubHospitalId);

                // Load rounds
                var allRounds = _roundRepo.GetAllRoundsPrescriptionPrint(ipdId);
                ViewBag.Rounds = allRounds?.Rounds ?? new List<IPDRoundPrintVM>();

                // ✅ Load vitals
                ViewBag.Vitals = _vitalsRepo.GetVitalsByIPDId(ipdId, HospitalId, SubHospitalId)
                                 ?? new List<IPDNurseVitals>();

                await LoadClinicalReferenceData(ipdId, model.PatientId);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Discharge");
                TempData["Error"] = ex.Message;
                return RedirectToAction("Summary", new { ipdId = ipdId });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult AddMedicine([FromForm] DischargeMedicineModel model)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                _dischargeRepo.SaveDischargeMedicine(model, userId, HospitalId, SubHospitalId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddMedicine");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult DeleteMedicine(int id, int ipdId)
        {
            try
            {
                _dischargeRepo.DeleteDischargeMedicine(id, HospitalId, SubHospitalId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteMedicine");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private void LoadDropdowns(int ipdId)
        {
            var doctors = _doctorRepo.GetAllDoctor(HospitalId, SubHospitalId)
                .Select(d => new SelectListItem
                {
                    Value = d.Doctor_Id.ToString(),
                    Text = $"Dr. {d.FirstName} {d.LastName} - {d.Specialization}"
                }).ToList();

            var medicines = _medicineRepo.GetAllMedicine(HospitalId, SubHospitalId)
                .Select(m => new SelectListItem
                {
                    Value = m.MedicineId.ToString(),
                    Text = $"{m.MedicineName} ({m.Type})"
                }).ToList();

            ViewBag.Doctors = doctors;
            ViewBag.Medicines = medicines;
            ViewBag.DischargeTypes = new SelectList(new[]
                { "Regular", "LAMA", "Death", "Referral", "Absconded" });
            ViewBag.DischargeConditions = new SelectList(new[]
                { "Good", "Fair", "Poor", "Expired" });
        }
    }
}