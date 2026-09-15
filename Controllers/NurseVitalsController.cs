using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;
using Microsoft.Extensions.Logging;

namespace WebApplicationSampleTest2.Controllers
{
    public class NurseVitalsController : Controller
    {
        private readonly ILogger<NurseVitalsController> _logger;
        private readonly IIPDNurseVitals _vitalsRepo;
        private readonly INurse _nurseRepo;
        private readonly IDoctor _doctorRepo;
        private readonly IIPDNursingCharge _nursingChargeRepo;
        private readonly INursingChargesMaster _nursingMasterRepo;
private readonly IIPDAdmission _admissionRepo; // used only to resolve AdmissionDateTime for day-wise grouping
        private readonly IIPDClinicalExtras _extrasRepo; // Examination, Input/Output, Daily Wellbeing, Body Composition
        private readonly Ipatient _patientRepo; // used only to resolve age/gender for BMR
        private readonly IHospital _hospitalRepo; // used to render the professional hospital letterhead on the print page


        public NurseVitalsController(IIPDNurseVitals vitalsRepo, INurse nurseRepo, IDoctor doctorRepo,
    IIPDNursingCharge nursingChargeRepo, INursingChargesMaster nursingMasterRepo, IIPDAdmission admissionRepo,
    IIPDClinicalExtras extrasRepo, Ipatient patientRepo, IHospital hospitalRepo, ILogger<NurseVitalsController> logger)
        {
            _vitalsRepo = vitalsRepo;
            _logger = logger;
            _nurseRepo = nurseRepo;
            _doctorRepo = doctorRepo;
            _nursingChargeRepo = nursingChargeRepo;
            _nursingMasterRepo = nursingMasterRepo;
            _admissionRepo = admissionRepo;
            _extrasRepo = extrasRepo;
            _patientRepo = patientRepo;
            _hospitalRepo = hospitalRepo;
        }

// ===============================
        // Day-wise helper - Day 1 = admission date. Used by Timeline, DayVitals
        // and the "Copy Previous Day" quick-entry feature. No DB change needed:
        // computed purely from AdmissionDateTime + RecordedDateTime.
        // ===============================
        private static int ComputeDayNumber(DateTime admissionDateTime, DateTime recordedDateTime)
        {
            var days = (recordedDateTime.Date - admissionDateTime.Date).Days + 1;
            return days < 1 ? 1 : days;
        }

// ===============================
        // Effective end date for day-counting.
        // For an ACTIVE admission we count up to today (DateTime.Now).
        // For a DISCHARGED admission we stop at the actual discharge date,
        // so the day tabs never show "additional days" beyond the stay.
        // ===============================
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

        // ===============================
        // Private Method - Load Nurses
        // ===============================
        private void LoadNurses()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var nurses = _nurseRepo.GetAll(hospitalId, subHospitalId) ?? new List<NurseModel>();

            ViewBag.Nurses = new SelectList(
                nurses.Select(n => new
                {
                    Id = n.NurseId,
                    Name = n.FirstName + " " + n.LastName
                }),
                "Id",
                "Name"
            );
        }

        // ===============================
        // Private Method - Load Doctors
        // ===============================
        private void LoadDoctors()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var doctors = _doctorRepo.GetAllDoctor(hospitalId, subHospitalId) ?? new List<Doctor>();

            ViewBag.Doctors = new SelectList(
                doctors.Select(d => new
                {
                    Id = d.Doctor_Id,
                    Name = "Dr. " + d.FirstName + " " + d.LastName
                }),
                "Id",
                "Name"
            );
        }
        // ===============================
        // INDEX - List Vitals By IPD
        // ===============================
        public IActionResult Index(int hospitalId, int? subHospitalId)
        {
            var vitalsList = _vitalsRepo.GetVitalsByHospital(hospitalId, subHospitalId);


            ViewBag.HospitalId = hospitalId;
            ViewBag.SubHospitalId = subHospitalId;

            return View(vitalsList);
        }


        // ===============================
        // TIMELINE - "Vital Parameters" (Clinical Details menu, item 1)
        // Day-wise view: Day 1, Day 2, ... tabs, newest day active by default.
        // ===============================
        [HttpGet]
        public IActionResult Timeline(int ipdId, int? activeDay = null)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, hospitalId, subHospitalId);
            if (admission == null)
                return NotFound(); // multi-tenant safe: admission lookup is already hospital-scoped

            var vitals = _vitalsRepo.GetVitalsByIPDId(ipdId, hospitalId, subHospitalId) ?? new List<IPDNurseVitals>();

            var vm = new NurseVitalsTimelineVM { Admission = admission };

            // For a discharged patient the day tabs must stop at the discharge
            // date; for an admitted patient they continue up to today.
            DateTime endDate = EffectiveEndDate(admission);
            int daysSinceAdmission = ComputeDayNumber(admission.AdmissionDateTime, endDate);
            int highestDayWithData = vitals.Count > 0
                ? vitals.Max(v => ComputeDayNumber(admission.AdmissionDateTime, v.RecordedDateTime))
                : 1;

            vm.TotalDays = Math.Max(daysSinceAdmission, highestDayWithData);
            vm.ActiveDay = activeDay.HasValue
                ? Math.Min(Math.Max(1, activeDay.Value), vm.TotalDays)
                : daysSinceAdmission; // default to "today" so the ward's current shift lands here first

            foreach (var v in vitals)
            {
                int day = ComputeDayNumber(admission.AdmissionDateTime, v.RecordedDateTime);
                if (!vm.VitalsByDay.ContainsKey(day))
                    vm.VitalsByDay[day] = new List<IPDNurseVitals>();
                vm.VitalsByDay[day].Add(v);
            }

            foreach (var day in vm.VitalsByDay.Keys.ToList())
                vm.VitalsByDay[day] = vm.VitalsByDay[day].OrderByDescending(v => v.RecordedDateTime).ToList();

            return View(vm);
        }

        // ===============================
        // AJAX - vitals for a single day (powers tab switching without a
        // full page reload).
        // ===============================
        [HttpGet]
        public IActionResult DayVitals(int ipdId, int dayNumber)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, hospitalId, subHospitalId);
            if (admission == null)
                return NotFound();

            var vitals = _vitalsRepo.GetVitalsByIPDId(ipdId, hospitalId, subHospitalId) ?? new List<IPDNurseVitals>();

            var dayVitals = vitals
                .Where(v => ComputeDayNumber(admission.AdmissionDateTime, v.RecordedDateTime) == dayNumber)
                .OrderByDescending(v => v.RecordedDateTime)
                .ToList();

            ViewBag.IPDId = ipdId;
            ViewBag.DayNumber = dayNumber;
            ViewBag.DayDate = admission.AdmissionDateTime.Date.AddDays(dayNumber - 1);
            ViewBag.HasPreviousDay = dayNumber > 1 && vitals.Any(v =>
                ComputeDayNumber(admission.AdmissionDateTime, v.RecordedDateTime) == dayNumber - 1);

            ViewBag.Examinations = FilterByDay(_extrasRepo.GetExaminations(ipdId, hospitalId, subHospitalId), admission, dayNumber);
            ViewBag.InputOutputs = FilterByDay(_extrasRepo.GetInputOutputs(ipdId, hospitalId, subHospitalId), admission, dayNumber);
            ViewBag.Wellbeings = FilterByDay(_extrasRepo.GetDailyWellbeings(ipdId, hospitalId, subHospitalId), admission, dayNumber);
            ViewBag.BodyCompositions = FilterByDay(_extrasRepo.GetBodyCompositions(ipdId, hospitalId, subHospitalId), admission, dayNumber);

            LoadNurses();
            LoadDoctors();

            return PartialView("_DayVitalsPartial", dayVitals);
        }

        private List<T> FilterByDay<T>(List<T> source, IPDAdmissionModel admission, int dayNumber)
            where T : class
        {
            // All 4 extra types share a RecordedDateTime property, but there's
            // no common interface for it - reflection keeps this generic
            // helper tiny instead of writing four near-identical overloads.
            var prop = typeof(T).GetProperty("RecordedDateTime");
            return source
                .Where(x => ComputeDayNumber(admission.AdmissionDateTime, (DateTime)prop.GetValue(x)) == dayNumber)
                .ToList();
        }

        // ===============================
        // AJAX DELETE - used only by the Timeline's day view, so it can stay
        // on the same day/tab after deleting instead of bouncing to Index
        // (the existing Delete action's redirect target). Does not touch or
        // replace the existing Delete action used elsewhere.
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteVitalAjax(int vitalsId)
        {
            try
            {
                _vitalsRepo.DeleteVitals(vitalsId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteVitalAjax");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================
        // EXAMINATION
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddExamination(int ipdId, int dayNumber, string CNS, string CVS, string RS, string PA,
            string RecordedByRole, int? NurseId, int? DoctorId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, hospitalId, subHospitalId);

            var recordedDate = admission != null
                ? admission.AdmissionDateTime.Date.AddDays(dayNumber - 1).Add(DateTime.Now.TimeOfDay)
                : DateTime.Now;

            if (string.Equals(RecordedByRole, "Doctor", StringComparison.OrdinalIgnoreCase))
                NurseId = null;
            else
                DoctorId = null;

            _extrasRepo.AddExamination(new IPDExamination
            {
                ParentHospitalId = hospitalId,
                SubHospitalId = subHospitalId,
                IPDId = ipdId,
                RecordedDateTime = recordedDate,
                CNS = CNS,
                CVS = CVS,
                RS = RS,
                PA = PA,
                RecordedByRole = string.IsNullOrEmpty(RecordedByRole) ? "Nurse" : RecordedByRole,
                NurseId = NurseId,
                DoctorId = DoctorId
            });

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteExamination(int id)
        {
            _extrasRepo.DeleteExamination(id);
            return Json(new { success = true });
        }

        // ===============================
        // INPUT / OUTPUT CHARTING
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddInputOutput(int ipdId, int dayNumber, decimal? Input, decimal? Output, decimal? DrainOutput,
            string RecordedByRole, int? NurseId, int? DoctorId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, hospitalId, subHospitalId);

            var recordedDate = admission != null
                ? admission.AdmissionDateTime.Date.AddDays(dayNumber - 1).Add(DateTime.Now.TimeOfDay)
                : DateTime.Now;

            if (string.Equals(RecordedByRole, "Doctor", StringComparison.OrdinalIgnoreCase))
                NurseId = null;
            else
                DoctorId = null;

            _extrasRepo.AddInputOutput(new IPDInputOutput
            {
                ParentHospitalId = hospitalId,
                SubHospitalId = subHospitalId,
                IPDId = ipdId,
                RecordedDateTime = recordedDate,
                Input = Input,
                Output = Output,
                DrainOutput = DrainOutput,
                RecordedByRole = string.IsNullOrEmpty(RecordedByRole) ? "Nurse" : RecordedByRole,
                NurseId = NurseId,
                DoctorId = DoctorId
            });

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteInputOutput(int id)
        {
            _extrasRepo.DeleteInputOutput(id);
            return Json(new { success = true });
        }

        // ===============================
        // DAILY WELLBEING
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddDailyWellbeing(int ipdId, int dayNumber, string Sleep, string Bowel,
            string Bladder, string Appetite, string Ambulation, string Eating,
            string RecordedByRole, int? NurseId, int? DoctorId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, hospitalId, subHospitalId);

            var recordedDate = admission != null
                ? admission.AdmissionDateTime.Date.AddDays(dayNumber - 1).Add(DateTime.Now.TimeOfDay)
                : DateTime.Now;

            if (string.Equals(RecordedByRole, "Doctor", StringComparison.OrdinalIgnoreCase))
                NurseId = null;
            else
                DoctorId = null;

            _extrasRepo.AddDailyWellbeing(new IPDDailyWellbeing
            {
                ParentHospitalId = hospitalId,
                SubHospitalId = subHospitalId,
                IPDId = ipdId,
                RecordedDateTime = recordedDate,
                Sleep = Sleep,
                Bowel = Bowel,
                Bladder = Bladder,
                Appetite = Appetite,
                Ambulation = Ambulation,
                Eating = Eating,
                RecordedByRole = string.IsNullOrEmpty(RecordedByRole) ? "Nurse" : RecordedByRole,
                NurseId = NurseId,
                DoctorId = DoctorId
            });

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteDailyWellbeing(int id)
        {
            _extrasRepo.DeleteDailyWellbeing(id);
            return Json(new { success = true });
        }

        // ===============================
        // BODY COMPOSITION - BMI/BMR/BSA are ALWAYS computed here, server-side.
        // The client sends only Height + Weight; any BMI/BMR/BSA a client
        // might try to send is ignored.
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddBodyComposition(int ipdId, int dayNumber, decimal? HeightCm, decimal? WeightKg,
            string RecordedByRole, int? NurseId, int? DoctorId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, hospitalId, subHospitalId);
            int? age = null;
            string gender = null;
            if (admission != null)
            {
                var patient = _patientRepo.GetPatientById(admission.PatientId, hospitalId, subHospitalId);
                if (patient != null)
                {
                    gender = patient.Gender;
                    if (int.TryParse(patient.Age, out int parsedAge)) age = parsedAge;
                }
            }

            var recordedDate = admission != null
                ? admission.AdmissionDateTime.Date.AddDays(dayNumber - 1).Add(DateTime.Now.TimeOfDay)
                : DateTime.Now;

            if (string.Equals(RecordedByRole, "Doctor", StringComparison.OrdinalIgnoreCase))
                NurseId = null;
            else
                DoctorId = null;

            var (bmi, bmr, bsa) = BodyCompositionCalculator.Calculate(HeightCm, WeightKg, age, gender);

            _extrasRepo.AddBodyComposition(new IPDBodyComposition
            {
                ParentHospitalId = hospitalId,
                SubHospitalId = subHospitalId,
                IPDId = ipdId,
                RecordedDateTime = recordedDate,
                HeightCm = HeightCm,
                WeightKg = WeightKg,
                BMI = bmi,
                BMR = bmr,
                BSA = bsa,
                RecordedByRole = string.IsNullOrEmpty(RecordedByRole) ? "Nurse" : RecordedByRole,
                NurseId = NurseId,
                DoctorId = DoctorId
            });

            return Json(new { success = true, bmi, bmr, bsa });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBodyComposition(int id)
        {
            _extrasRepo.DeleteBodyComposition(id);
            return Json(new { success = true });
        }

        // ===============================
        // PRINT - print-friendly single-day sheet
        // ===============================
        [HttpGet]
        public IActionResult PrintDay(int ipdId, int dayNumber)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, hospitalId, subHospitalId);
            if (admission == null)
                return NotFound();

var vitals = _vitalsRepo.GetVitalsByIPDId(ipdId, hospitalId, subHospitalId) ?? new List<IPDNurseVitals>();

            var dayVitals = vitals
                .Where(v => ComputeDayNumber(admission.AdmissionDateTime, v.RecordedDateTime) == dayNumber)
                .OrderBy(v => v.RecordedDateTime)
                .ToList();

            ViewBag.Admission = admission;
            ViewBag.DayNumber = dayNumber;
            ViewBag.DayDate = admission.AdmissionDateTime.Date.AddDays(dayNumber - 1);

            // ── Professional hospital letterhead + patient details for the print page ──
            var hospital = _hospitalRepo.GetsubandMainHospitalById(hospitalId, subHospitalId);
            ViewBag.HospitalName = hospital?.Name;
            ViewBag.HospitalAddress = hospital?.Address;
            ViewBag.HospitalPhone = hospital?.PhoneNumber;
            ViewBag.HospitalEmail = hospital?.EmailId;
            ViewBag.HospitalLogo = hospital?.Logo;
            ViewBag.HospitalRegNo = hospital?.RegistrationNumber;

            var patient = _patientRepo.GetPatientById(admission.PatientId, hospitalId, subHospitalId);
            ViewBag.PatientAge = patient?.Age;
            ViewBag.PatientGender = patient?.Gender;

            return View(dayVitals);
        }

        [HttpGet]
        public IActionResult Create(int ipdId, int? dayNumber, int? copyFromDay)
        {
            int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subid = HttpContext.Session.GetInt32("SubHospitalId");
            LoadNurses();
            LoadDoctors();

            var model = new IPDNurseVitals
            {
                IPDId = ipdId,
                RecordedByRole = "Nurse",
                RecordedDateTime = DateTime.Now
            };

            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, hid, subid);
            if (admission != null && dayNumber.HasValue && dayNumber.Value > 0)
            {
                var selectedDayDate = admission.AdmissionDateTime.Date.AddDays(dayNumber.Value - 1);
                model.RecordedDateTime = selectedDayDate.Add(DateTime.Now.TimeOfDay);
                model.Shift = SuggestShift(model.RecordedDateTime);
            }
            else
            {
                model.Shift = SuggestShift(DateTime.Now);
            }

            // "Copy Previous Day" - came from the Timeline's day tab. Prefill the
            // numeric readings from that day's latest entry; nurse still reviews
            // and saves as a brand-new record (never edits history in place).
            if (copyFromDay.HasValue && admission != null)
            {
                var vitals = _vitalsRepo.GetVitalsByIPDId(ipdId, hid, subid) ?? new List<IPDNurseVitals>();
                var source = vitals
                    .Where(v => ComputeDayNumber(admission.AdmissionDateTime, v.RecordedDateTime) == copyFromDay.Value)
                    .OrderByDescending(v => v.RecordedDateTime)
                    .FirstOrDefault();

                if (source != null)
                {
                    model.Temperature = source.Temperature;
                    model.Pulse = source.Pulse;
                    model.Systolic = source.Systolic;
                    model.Diastolic = source.Diastolic;
                    model.RespirationRate = source.RespirationRate;
                    model.OxygenSaturation = source.OxygenSaturation;
                    model.Notes = source.Notes;
                    TempData["Info"] = $"Prefilled from Day {copyFromDay.Value}'s last reading - please review before saving.";
                }
            }

            ViewBag.NursingMaster = _nursingMasterRepo.GetAll(hid, subid);

            return View(model);
        }

        // Quick-entry helper: guesses the current shift from the clock so the
        // nurse/doctor doesn't have to pick it manually every time.
        private static string SuggestShift(DateTime time)
        {
            var hour = time.Hour;
            if (hour >= 7 && hour < 15) return "Morning";
            if (hour >= 15 && hour < 23) return "Afternoon";
            return "Night";
        }

        // ===============================
        // Copy Previous Vitals (AJAX) - "Real Hospital" quick-entry feature.
        // Lets the recording nurse/doctor pull the last recorded reading for
        // this admission into the form as a starting point instead of typing
        // everything from scratch every round.
        // ===============================
        [HttpGet]
        public IActionResult GetLastVitals(int ipdId)
        {
            var last = _vitalsRepo.GetLastVitalsByIPDId(ipdId);

            if (last == null)
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                temperature = last.Temperature,
                pulse = last.Pulse,
                systolic = last.Systolic,
                diastolic = last.Diastolic,
                respirationRate = last.RespirationRate,
                oxygenSaturation = last.OxygenSaturation,
                notes = last.Notes
            });
        }

        // ===============================
        // CREATE (POST)
        // ===============================
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Create(IPDNurseVitals model, List<IPDNursingCharge> NursingProcedures)
        //{
        //    int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //    int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

        //    try
        //    {
        //        if (hospitalId <= 0)
        //            throw new Exception("Invalid hospital session.");

        //        if (!ModelState.IsValid)
        //        {
        //            LoadNurses();
        //            int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //            int? subid = HttpContext.Session.GetInt32("SubHospitalId");
        //            ViewBag.NursingMaster = _nursingMasterRepo.GetAll(hid, subid); // ← ADD
        //            return View(model);

        //        }

        //        model.ParentHospitalId = hospitalId;
        //        model.SubHospitalId = subHospitalId;
        //        model.CreatedDate = DateTime.Now;
        //        model.IsActive = true;

        //        _vitalsRepo.CreateVitals(model);

        //        TempData["Success"] = "Vitals added successfully.";
        //        if (NursingProcedures != null && NursingProcedures.Count > 0)
        //        {
        //            int hid =
        //                HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //            int? subid =
        //                HttpContext.Session.GetInt32("SubHospitalId");

        //            foreach (var c in NursingProcedures)
        //            {
        //                c.IPDId = model.IPDId;
        //                c.ParentHospitalId = hid;
        //                c.SubHospitalId = subid;
        //                c.ChargeDate = model.RecordedDateTime.Date;
        //                c.NurseId = model.NurseId;
        //            }
        //            _nursingChargeRepo.SaveCharges(NursingProcedures);
        //        }
        //        return RedirectToAction("Details", "IPDAdmission", new { id = model.IPDId });
        //    }
        //    // Same in catch block:
        //    catch (Exception ex)
        //    {
        //        LoadNurses();
        //        int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //        int? subid = HttpContext.Session.GetInt32("SubHospitalId");
        //        ViewBag.NursingMaster = _nursingMasterRepo.GetAll(hid, subid); // ← ADD
        //        TempData["Error"] = ex.Message;
        //        return View(model);
        //    }
        //}




        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Create(IPDNurseVitals model)
        //{
        //    int hospitalId =
        //        HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //    int? subHospitalId =
        //        HttpContext.Session.GetInt32("SubHospitalId");

        //    try
        //    {
        //        if (hospitalId <= 0)
        //            throw new Exception("Invalid hospital session.");

        //        if (!ModelState.IsValid)
        //        {
        //            LoadNurses();
        //            LoadNursingMaster();
        //            return View(model);
        //        }

        //        model.ParentHospitalId = hospitalId;
        //        model.SubHospitalId = subHospitalId;
        //        model.CreatedDate = DateTime.Now;
        //        model.IsActive = true;

        //        // ── Step 1: Save vitals ──────────────────────────────────────
        //        _vitalsRepo.CreateVitals(model);

        //        // ── Step 2: Save nursing procedures ─────────────────────────
        //        if (NursingProcedures != null
        //            && NursingProcedures.Count > 0)
        //        {
        //            // ✅ FIX — set ALL required fields here
        //            // never rely on model binding for session values
        //            foreach (var c in NursingProcedures)
        //            {
        //                c.IPDId = model.IPDId;
        //                c.ParentHospitalId = hospitalId;   // ← from session
        //                c.SubHospitalId = subHospitalId; // ← from session
        //                c.ChargeDate = model.RecordedDateTime.Date;
        //                c.NurseId = model.NurseId;

        //                // ✅ FIX — recalculate total in case JS didn't post it
        //                if (c.TotalCharge <= 0)
        //                    c.TotalCharge = c.Quantity * c.UnitCharge;

        //                // ✅ FIX — default quantity
        //                if (c.Quantity <= 0) c.Quantity = 1;
        //            }

        //            _nursingChargeRepo.SaveCharges(NursingProcedures);
        //        }

        //        TempData["Success"] = "Vitals added successfully.";
        //        return RedirectToAction("Details", "IPDAdmission",
        //            new { id = model.IPDId });
        //    }
        //    catch (Exception ex)
        //    {
        //        LoadNurses();
        //        LoadNursingMaster();
        //        TempData["Error"] = ex.Message;
        //        return View(model);
        //    }
        //}


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(IPDNurseVitals model)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            try
            {
                if (hospitalId <= 0)
                    throw new Exception("Invalid hospital session.");

                // Only the selected role's id is meaningful - clear the other
                // so we never save a stray NurseId/DoctorId from a previous submit.
                if (string.Equals(model.RecordedByRole, "Doctor", StringComparison.OrdinalIgnoreCase))
                    model.NurseId = null;
                else
                    model.DoctorId = null;

                if (!ModelState.IsValid)
                {
                    LoadNurses();
                    LoadDoctors();
                    LoadNursingMaster();
                    return View(model);
                }

                model.ParentHospitalId = hospitalId;
                model.SubHospitalId = subHospitalId;
                model.CreatedDate = DateTime.Now;
                model.IsActive = true;

                _vitalsRepo.CreateVitals(model);

                var admission = _admissionRepo.GetIPDAdmissionById(model.IPDId, hospitalId, subHospitalId);
                int activeDay = admission != null
                    ? ComputeDayNumber(admission.AdmissionDateTime, model.RecordedDateTime)
                    : 1;

                TempData["Success"] = "Vitals added successfully.";
                return RedirectToAction("Timeline", "NurseVitals", new { ipdId = model.IPDId, activeDay });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                LoadNurses();
                LoadDoctors();
                LoadNursingMaster();
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }



        private void LoadNursingMaster()
        {
            int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subid = HttpContext.Session.GetInt32("SubHospitalId");
            ViewBag.NursingMaster = _nursingMasterRepo.GetAll(hid, subid);
        }






        // ===============================
        // EDIT (GET)
        // ===============================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var vitals = _vitalsRepo.GetVitalsById(id);
            if (vitals == null)
                return NotFound();

            var admission = _admissionRepo.GetIPDAdmissionById(vitals.IPDId, hospitalId, subHospitalId);
            if (admission != null)
            {
                ViewBag.ActiveDay = ComputeDayNumber(admission.AdmissionDateTime, vitals.RecordedDateTime);
            }

            LoadNurses();
            LoadDoctors();

            return View(vitals);
        }

        // ===============================
        // EDIT (POST)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(IPDNurseVitals model)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            if (string.Equals(model.RecordedByRole, "Doctor", StringComparison.OrdinalIgnoreCase))
                model.NurseId = null;
            else
                model.DoctorId = null;

            if (!ModelState.IsValid)
            {
                LoadNurses();
                LoadDoctors();
                return View(model);
            }

            model.UpdatedDate = DateTime.Now;

            _vitalsRepo.UpdateVitals(model);

            var admission = _admissionRepo.GetIPDAdmissionById(model.IPDId, hospitalId, subHospitalId);
            int activeDay = admission != null
                ? ComputeDayNumber(admission.AdmissionDateTime, model.RecordedDateTime)
                : 1;

            TempData["SuccessMessage"] = "Vitals updated successfully.";
            return RedirectToAction("Timeline", "NurseVitals", new { ipdId = model.IPDId, activeDay });
        }

        // ===============================
        // DELETE (SOFT DELETE)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int vitalsId, int ipdId, int hospitalId, int? subHospitalId)
        {
            _vitalsRepo.DeleteVitals(vitalsId);

            TempData["SuccessMessage"] = "Vitals deleted successfully.";

            return RedirectToAction("Index", new
            {
                ipdId = ipdId,
                hospitalId = hospitalId,
                subHospitalId = subHospitalId
            });
        }




        [HttpGet]
        public IActionResult AddNursingProcedure(int ipdId)
        {
            int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subid = HttpContext.Session.GetInt32("SubHospitalId");
            LoadNurses();
            ViewBag.NursingMaster = _nursingMasterRepo.GetAll(hid, subid);
            ViewBag.IPDId = ipdId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveNursingProcedures(int IPDId, List<IPDNursingCharge> NursingProcedures)
        {
            int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subid = HttpContext.Session.GetInt32("SubHospitalId");
            try
            {
                if (NursingProcedures != null && NursingProcedures.Count > 0)
                {
                    foreach (var c in NursingProcedures)
                    {
                        c.IPDId = IPDId;
                        c.ParentHospitalId = hid;
                        c.SubHospitalId = subid;
                        c.ChargeDate = DateTime.Now.Date;
                        if (c.Quantity <= 0) c.Quantity = 1;
                        if (c.TotalCharge <= 0) c.TotalCharge = c.Quantity * c.UnitCharge;
                    }
                    _nursingChargeRepo.SaveCharges(NursingProcedures);
                }
                TempData["Success"] = "Nursing procedures saved successfully.";
                return RedirectToAction("Details", "IPDAdmission", new { id = IPDId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveNursingProcedures");
                LoadNurses();
                ViewBag.NursingMaster = _nursingMasterRepo.GetAll(hid, subid);
                ViewBag.IPDId = IPDId;
                TempData["Error"] = ex.Message;
                return View("AddNursingProcedure");
            }
        }





    }
}

