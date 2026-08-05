// Controllers/DoctorRoundController.cs
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
    public class DoctorRoundController : Controller
    {
        private readonly IDoctorRound _roundRepo;
        private readonly IDoctor _doctorRepo;
        private readonly ISymptom _symptomRepo;
        private readonly IMedicine _medicineRepo;
        private readonly IIPDAdmission _ipdRepo;      // to look up PatientId from IPDId

        public DoctorRoundController(
            IDoctorRound roundRepo,
            IDoctor doctorRepo,
            ISymptom symptomRepo,
            IMedicine medicineRepo,
            IIPDAdmission ipdRepo)
        {
            _roundRepo = roundRepo;
            _doctorRepo = doctorRepo;
            _symptomRepo = symptomRepo;
            _medicineRepo = medicineRepo;
            _ipdRepo = ipdRepo;
        }

        // ─────────────────────────────────────────
        // DROPDOWNS helper
        // ─────────────────────────────────────────
        private void LoadDropdowns()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var doctors = _doctorRepo.GetAllDoctor(hospitalId, subHospitalId)
                          ?? new List<Doctor>();
            ViewBag.Doctors = new SelectList(
                doctors.Select(d => new { Id = d.Doctor_Id, Name = d.FirstName + " " + d.LastName }),
                "Id", "Name");

            var symptoms = _symptomRepo.GetAllSymptoms(hospitalId, subHospitalId)
                           ?? new List<Symptom>();
            ViewBag.Symptoms = symptoms;

            var medicines = _medicineRepo.GetAllMedicine(hospitalId, subHospitalId)
                            ?? new List<Medicine>();
            ViewBag.Medicines = new SelectList(
                medicines.Select(m => new { Id = m.MedicineId, Name = m.MedicineName }),
                "Id", "Name");

            ViewBag.RoundTypes = new SelectList(new[]
                { "Morning", "Afternoon", "Evening", "Emergency", "ICU", "Follow-up" });

            ViewBag.Routes = new SelectList(new[]
                { "Oral", "IV", "IM", "SC", "Topical", "Inhalation", "Other" });

            ViewBag.InvestigationTypes = new SelectList(new[]
                { "Lab", "Xray", "MRI", "CT", "ECG", "USG", "Endoscopy", "Other" });

            ViewBag.Priorities = new SelectList(new[]
                { "Routine", "Urgent", "Emergency" });
        }

        // ─────────────────────────────────────────
        // CREATE (GET)
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Create(int ipdId)
        {
            try
            {
                LoadDropdowns();

                var model = new IPDDoctorRound
                {
                    IPDId = ipdId,
                    RoundDateTime = DateTime.Now,
                    RoundType = "Morning"
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", "IPDAdmission", new { id = ipdId });
            }
        }

        // ─────────────────────────────────────────
        // CREATE (POST)
        // ─────────────────────────────────────────

        [HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Create(
    IPDDoctorRound model,
    List<int> SymptomIds,
    List<IPDRoundPrescription> Prescriptions,
    List<IPDRoundInvestigation> Investigations)
{
    int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
    int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
    try
    {
        if (hospitalId <= 0)
            throw new Exception("Invalid hospital session.");

        model.ParentHospitalId = hospitalId;
        model.SubHospitalId = subHospitalId;
        model.CreatedDate = DateTime.Now;
        model.IsActive = true;

        // Step 1: Insert Round → get RoundId
        int roundId = _roundRepo.CreateRound(model);

        // Step 2: Insert Symptoms
        if (SymptomIds != null && SymptomIds.Count > 0)
        {
            foreach (var symptomId in SymptomIds)
            {
                _roundRepo.InsertSymptom(new IPDRoundSymptom
                {
                    RoundId          = roundId,
                    IPDId            = model.IPDId,
                    ParentHospitalId = hospitalId,
                    SubHospitalId    = subHospitalId,
                    SymptomId        = symptomId
                });
            }
        }

        // Step 3: Insert Prescriptions
        if (Prescriptions != null && Prescriptions.Count > 0)
        {
            foreach (var presc in Prescriptions)
            {
                presc.RoundId          = roundId;
                presc.IPDId            = model.IPDId;
                presc.ParentHospitalId = hospitalId;
                presc.SubHospitalId    = subHospitalId;
                _roundRepo.InsertPrescription(presc);
            }
        }

        // Step 4: Insert Investigations
        if (Investigations != null && Investigations.Count > 0)
        {
            foreach (var invest in Investigations)
            {
                invest.RoundId          = roundId;
                invest.IPDId            = model.IPDId;
                invest.ParentHospitalId = hospitalId;
                invest.SubHospitalId    = subHospitalId;
                _roundRepo.InsertInvestigation(invest);
            }
        }

        // Step 5: Fire IPD Pharmacy Queue notification (non-blocking)
        if (Prescriptions != null && Prescriptions.Count > 0)
        {
            // Get patient info from IPD admission
            var admission = _ipdRepo.GetIPDAdmissionById(model.IPDId, hospitalId, subHospitalId);

            // Fix 3 — fetch doctor name from repo instead of relying on empty hidden field
            var doctor = _doctorRepo.GetAllDoctor(hospitalId, subHospitalId)
                                    ?.FirstOrDefault(d => d.Doctor_Id == model.DoctorId);
            string doctorName = doctor != null
                ? $"{doctor.FirstName} {doctor.LastName}".Trim()
                : "";

            // Fix 2 — look up medicine names from repo since form only posts MedicineId
            var allMeds = _medicineRepo.GetAllMedicine(hospitalId, subHospitalId);
            var medNames = Prescriptions
                .Select(p => allMeds?.FirstOrDefault(m => m.MedicineId == p.MedicineId)?.MedicineName)
                .Where(n => !string.IsNullOrEmpty(n))
                .Take(3)
                .ToList();
            string summary = string.Join(", ", medNames)
                             + (Prescriptions.Count > 3 ? "..." : "");

            _roundRepo.InsertIPDPharmacyNotification(new MedicineNotificationModel
            {
                PatientId        = admission?.PatientId ?? 0,
                PatientName      = admission?.PatientName ?? "",
                IPDId            = model.IPDId,
                RoundId          = roundId,
                DoctorName       = doctorName,  // ✅ Fix 3
                MedicineCount    = Prescriptions.Count,
                MedicinesSummary = summary,     // ✅ Fix 2
                Type             = "IPD",
                HospitalId       = hospitalId,
                SubHospitalId    = subHospitalId
            });
        }

        TempData["Success"] = "Doctor round saved successfully.";
        return RedirectToAction("Details", "IPDAdmission", new { id = model.IPDId });
    }
    catch (Exception ex)
    {
        LoadDropdowns();
        TempData["Error"] = ex.Message;
        return View(model);
    }
}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Create(
        //    IPDDoctorRound model,
        //    List<int> SymptomIds,
        //    List<IPDRoundPrescription> Prescriptions,
        //    List<IPDRoundInvestigation> Investigations)
        //{
        //    int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //    int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

        //    try
        //    {
        //        if (hospitalId <= 0)
        //            throw new Exception("Invalid hospital session.");

        //        model.ParentHospitalId = hospitalId;
        //        model.SubHospitalId = subHospitalId;
        //        model.CreatedDate = DateTime.Now;
        //        model.IsActive = true;

        //        // Step 1: Insert Round → get RoundId
        //        int roundId = _roundRepo.CreateRound(model);

        //        // Step 2: Insert Symptoms
        //        if (SymptomIds != null && SymptomIds.Count > 0)
        //        {
        //            foreach (var symptomId in SymptomIds)
        //            {
        //                _roundRepo.InsertSymptom(new IPDRoundSymptom
        //                {
        //                    RoundId = roundId,
        //                    IPDId = model.IPDId,
        //                    ParentHospitalId = hospitalId,
        //                    SubHospitalId = subHospitalId,
        //                    SymptomId = symptomId
        //                });
        //            }
        //        }

        //        // Step 3: Insert Prescriptions
        //        if (Prescriptions != null && Prescriptions.Count > 0)
        //        {
        //            foreach (var presc in Prescriptions)
        //            {
        //                presc.RoundId = roundId;
        //                presc.IPDId = model.IPDId;
        //                presc.ParentHospitalId = hospitalId;
        //                presc.SubHospitalId = subHospitalId;
        //                _roundRepo.InsertPrescription(presc);
        //            }
        //        }

        //        // Step 4: Insert Investigations
        //        if (Investigations != null && Investigations.Count > 0)
        //        {
        //            foreach (var invest in Investigations)
        //            {
        //                invest.RoundId = roundId;
        //                invest.IPDId = model.IPDId;
        //                invest.ParentHospitalId = hospitalId;
        //                invest.SubHospitalId = subHospitalId;
        //                _roundRepo.InsertInvestigation(invest);
        //            }
        //        }

        //        // Step 5: Fire IPD Pharmacy Queue notification (non-blocking)
        //        // Only fire if there are medicines prescribed
        //        if (Prescriptions != null && Prescriptions.Count > 0)
        //        {
        //            // Get patient info from IPD admission
        //            var admission = _ipdRepo.GetIPDAdmissionById(model.IPDId, hospitalId, subHospitalId);

        //            // Build a short summary of medicine names (first 3 + "..." if more)
        //            var medNames = Prescriptions
        //                .Where(p => !string.IsNullOrEmpty(p.MedicineName))
        //                .Select(p => p.MedicineName)
        //                .Take(3)
        //                .ToList();
        //            string summary = string.Join(", ", medNames)
        //                             + (Prescriptions.Count > 3 ? "..." : "");

        //            _roundRepo.InsertIPDPharmacyNotification(new MedicineNotificationModel
        //            {
        //                PatientId = admission?.PatientId ?? 0,
        //                PatientName = admission?.PatientName ?? "",
        //                IPDId = model.IPDId,
        //                RoundId = roundId,
        //                DoctorName = model.DoctorName ?? "",   // populated from form hidden field
        //                MedicineCount = Prescriptions.Count,
        //                MedicinesSummary = summary,
        //                Type = "IPD",
        //                HospitalId = hospitalId,
        //                SubHospitalId = subHospitalId
        //            });
        //        }

        //        TempData["Success"] = "Doctor round saved successfully.";
        //        return RedirectToAction("Details", "IPDAdmission", new { id = model.IPDId });
        //    }
        //    catch (Exception ex)
        //    {
        //        LoadDropdowns();
        //        TempData["Error"] = ex.Message;
        //        return View(model);
        //    }
        //}

        // ─────────────────────────────────────────
        // DETAIL (GET)
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Detail(int roundId, int ipdId)
        {
            try
            {
                var round = _roundRepo.GetRoundDetail(roundId);

                if (round == null)
                    return NotFound();

                ViewBag.IPDId = ipdId;
                return View(round);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", "IPDAdmission", new { id = ipdId });
            }
        }

        // ─────────────────────────────────────────
        // DELETE (POST)
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int roundId, int ipdId)
        {
            try
            {
                _roundRepo.DeleteRound(roundId);
                TempData["Success"] = "Round deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Details", "IPDAdmission", new { id = ipdId });
        }

        // ─────────────────────────────────────────
        // PRINT — single round
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult PrintPrescription(int roundId, int ipdId)
        {
            try
            {
                var vm = _roundRepo.GetRoundPrescriptionPrint(roundId);
                if (vm == null)
                    return NotFound();

                ViewBag.IPDId = ipdId;
                ViewBag.PrintMode = "single";
                return View("PrintPrescription", vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Detail", new { roundId = roundId, ipdId = ipdId });
            }
        }

        // ─────────────────────────────────────────
        // PRINT — all rounds
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult PrintAllPrescriptions(int ipdId)
        {
            try
            {
                var vm = _roundRepo.GetAllRoundsPrescriptionPrint(ipdId);
                if (vm == null)
                    return NotFound();

                ViewBag.IPDId = ipdId;
                ViewBag.PrintMode = "all";
                return View("PrintPrescription", vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", "IPDAdmission", new { id = ipdId });
            }
        }
    }
}









//// Controllers/DoctorRoundController.cs
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using WebApplicationSampleTest2.Models;
//using WebApplicationSampleTest2.Repository;

//namespace WebApplicationSampleTest2.Controllers
//{
//    public class DoctorRoundController : Controller
//    {
//        private readonly IDoctorRound _roundRepo;
//        private readonly IDoctor _doctorRepo;
//        private readonly ISymptom _symptomRepo;
//        private readonly IMedicine _medicineRepo;

//        public DoctorRoundController(
//            IDoctorRound roundRepo,
//            IDoctor doctorRepo,
//            ISymptom symptomRepo,
//            IMedicine medicineRepo)
//        {
//            _roundRepo = roundRepo;
//            _doctorRepo = doctorRepo;
//            _symptomRepo = symptomRepo;
//            _medicineRepo = medicineRepo;
//        }


//        private void LoadDropdowns()
//        {
//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//            // Doctors
//            var doctors = _doctorRepo.GetAllDoctor(hospitalId, subHospitalId)
//                          ?? new List<Doctor>();
//            ViewBag.Doctors = new SelectList(
//                doctors.Select(d => new
//                {
//                    Id = d.Doctor_Id,
//                    Name = d.FirstName + " " + d.LastName
//                }), "Id", "Name");

//            // Symptoms
//            var symptoms = _symptomRepo.GetAllSymptoms(hospitalId, subHospitalId)
//                           ?? new List<Symptom>();
//            ViewBag.Symptoms = symptoms; // passing full list for checkbox/multiselect

//            // Medicines
//            var medicines = _medicineRepo.GetAllMedicine(hospitalId, subHospitalId)
//                            ?? new List<Medicine>();
//            ViewBag.Medicines = new SelectList(
//                medicines.Select(m => new
//                {
//                    Id = m.MedicineId,
//                    Name = m.MedicineName
//                }), "Id", "Name");

//            // Static dropdowns
//            ViewBag.RoundTypes = new SelectList(new[]
//                { "Morning","Afternoon","Evening","Emergency", "ICU", "Follow-up" });

//            ViewBag.Routes = new SelectList(new[]
//                { "Oral", "IV", "IM", "SC", "Topical", "Inhalation", "Other" });

//            ViewBag.InvestigationTypes = new SelectList(new[]
//                { "Lab", "Xray", "MRI", "CT", "ECG", "USG", "Endoscopy", "Other" });

//            ViewBag.Priorities = new SelectList(new[]
//                { "Routine", "Urgent", "Emergency" });
//        }

//        // ===============================
//        // CREATE (GET)
//        // ===============================
//        [HttpGet]
//        public IActionResult Create(int ipdId)
//        {
//            try
//            {
//                LoadDropdowns();

//                var model = new IPDDoctorRound
//                {
//                    IPDId = ipdId,
//                    RoundDateTime = DateTime.Now,
//                    RoundType = "Morning"
//                };

//                return View(model);
//            }
//            catch (Exception ex)
//            {
//                TempData["Error"] = ex.Message;
//                return RedirectToAction("Details", "IPDAdmission", new { id = ipdId });
//            }
//        }


//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public IActionResult Create(
//            IPDDoctorRound model,
//            List<int> SymptomIds,
//            List<IPDRoundPrescription> Prescriptions,
//            List<IPDRoundInvestigation> Investigations)
//        {
//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//            try
//            {
//                if (hospitalId <= 0)
//                    throw new Exception("Invalid hospital session.");

//                model.ParentHospitalId = hospitalId;
//                model.SubHospitalId = subHospitalId;
//                model.CreatedDate = DateTime.Now;
//                model.IsActive = true;

//                //  Insert Round → get RoundId
//                int roundId = _roundRepo.CreateRound(model);

//                // Insert Symptoms
//                if (SymptomIds != null && SymptomIds.Count > 0)
//                {
//                    foreach (var symptomId in SymptomIds)
//                    {
//                        _roundRepo.InsertSymptom(new IPDRoundSymptom
//                        {
//                            RoundId = roundId,
//                            IPDId = model.IPDId,
//                            ParentHospitalId = hospitalId,
//                            SubHospitalId = subHospitalId,
//                            SymptomId = symptomId
//                        });
//                    }
//                }


//                if (Prescriptions != null && Prescriptions.Count > 0)
//                {
//                    foreach (var presc in Prescriptions)
//                    {
//                        presc.RoundId = roundId;
//                        presc.IPDId = model.IPDId;
//                        presc.ParentHospitalId = hospitalId;
//                        presc.SubHospitalId = subHospitalId;
//                        _roundRepo.InsertPrescription(presc);
//                    }
//                }

//                // Step 4: Insert Investigations
//                if (Investigations != null && Investigations.Count > 0)
//                {
//                    foreach (var invest in Investigations)
//                    {
//                        invest.RoundId = roundId;
//                        invest.IPDId = model.IPDId;
//                        invest.ParentHospitalId = hospitalId;
//                        invest.SubHospitalId = subHospitalId;
//                        _roundRepo.InsertInvestigation(invest);
//                    }
//                }

//                TempData["Success"] = "Doctor round saved successfully.";
//                return RedirectToAction("Details", "IPDAdmission", new { id = model.IPDId });
//            }
//            catch (Exception ex)
//            {
//                LoadDropdowns();
//                TempData["Error"] = ex.Message;
//                return View(model);
//            }
//        }

//        // ===============================
//        // VIEW DETAIL (GET)
//        // ===============================
//        [HttpGet]
//        public IActionResult Detail(int roundId, int ipdId)
//        {
//            try
//            {
//                var round = _roundRepo.GetRoundDetail(roundId);

//                if (round == null)
//                    return NotFound();

//                ViewBag.IPDId = ipdId;
//                return View(round);
//            }
//            catch (Exception ex)
//            {
//                TempData["Error"] = ex.Message;
//                return RedirectToAction("Details", "IPDAdmission", new { id = ipdId });
//            }
//        }

//        // ===============================
//        // DELETE (POST)
//        // ===============================
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public IActionResult Delete(int roundId, int ipdId)
//        {
//            try
//            {
//                _roundRepo.DeleteRound(roundId);
//                TempData["Success"] = "Round deleted successfully.";
//            }
//            catch (Exception ex)
//            {
//                TempData["Error"] = ex.Message;
//            }

//            return RedirectToAction("Details", "IPDAdmission", new { id = ipdId });
//        }






//        [HttpGet]
//        public IActionResult PrintPrescription(int roundId, int ipdId)
//        {
//            try
//            {
//                var vm = _roundRepo.GetRoundPrescriptionPrint(roundId);
//                if (vm == null)
//                    return NotFound();

//                ViewBag.IPDId = ipdId;
//                ViewBag.PrintMode = "single";
//                return View("PrintPrescription", vm);
//            }
//            catch (Exception ex)
//            {
//                TempData["Error"] = ex.Message;
//                return RedirectToAction("Detail", new { roundId = roundId, ipdId = ipdId });
//            }
//        }


//        [HttpGet]
//        public IActionResult PrintAllPrescriptions(int ipdId)
//        {
//            try
//            {
//                var vm = _roundRepo.GetAllRoundsPrescriptionPrint(ipdId);
//                if (vm == null)
//                    return NotFound();

//                ViewBag.IPDId = ipdId;
//                ViewBag.PrintMode = "all";
//                return View("PrintPrescription", vm);
//            }
//            catch (Exception ex)
//            {
//                TempData["Error"] = ex.Message;
//                return RedirectToAction("Details", "IPDAdmission", new { id = ipdId });
//            }
//        }
//    }
//}