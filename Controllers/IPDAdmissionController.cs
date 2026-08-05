using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class IPDAdmissionController : Controller
    {
        private readonly IIPDAdmission _repository;
        private readonly ILogger<IPDAdmissionController> _logger;
        private readonly IDoctor _iDoctor;
        private readonly Ipatient _ipatient;
        private readonly IOPDAppointment _iAppointment;
        private readonly IOPD _IOPD;
        private readonly IReferenceDoctor _referenceDoctor;
        private readonly IIPDNurseVitals _vitalsRepository;
        private readonly IDoctorRound _roundRepository;
        private readonly IIPDNursingCharge _nursingRepo;
        private readonly IAdmissionNotes _admissionNoteRepo;
        private readonly IDiagnosis _diagnosisRepository;
        private readonly IProcedure _procedureRepository;
        private readonly IDailyNotes _dailyNotesRepository;
        private readonly IInpatientDocument _documentRepository;

        public IPDAdmissionController(
            IAdmissionNotes admissionNoteRepo,
            IReferenceDoctor referenceDoctor,
            IDailyNotes dailyNotes,
            IOPD ipd,
            Ipatient ipatient,
            IOPDAppointment iAppointment,
            IDoctor doctor,
            IIPDAdmission repository,
            IIPDNurseVitals vitalsRepository,
            IDoctorRound roundRepository,
            IDiagnosis diagnosisRepository,
            IIPDNursingCharge nursingRepo,
            IProcedure procedure,
            IInpatientDocument documentRepository,
            ILogger<IPDAdmissionController> logger)
        {
            _repository = repository;
            _logger = logger;
            _ipatient = ipatient;
            _iDoctor = doctor;
            _iAppointment = iAppointment;
            _IOPD = ipd;
            _referenceDoctor = referenceDoctor;
            _vitalsRepository = vitalsRepository;
            _roundRepository = roundRepository;
            _nursingRepo = nursingRepo;
            _admissionNoteRepo = admissionNoteRepo;
            _diagnosisRepository = diagnosisRepository;
            _procedureRepository = procedure;
            _dailyNotesRepository = dailyNotes;
            _documentRepository = documentRepository;
        }

        // ===================== INDEX (LIST) =====================
        public IActionResult Index(string search, int page = 1)
        {
            try
            {
                int pageSize = 6;
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var data = _repository.GetAllIPDAdmissions(hospitalId, subHospitalId).AsQueryable();

                foreach (var item in data)
                {
                    if (item.PatientId > 0)
                    {
                        var patient = _ipatient.GetPatientById(item.PatientId, hospitalId, subHospitalId);
                        if (patient != null)
                            item.PatientName = (patient.FirstName ?? "") + " " + (patient.LastName ?? "");
                    }

                    if (item.PrimaryDoctorId > 0)
                    {
                        var doctor = _iDoctor.GetDoctorById(item.PrimaryDoctorId, hospitalId, subHospitalId);
                        if (doctor != null)
                            item.DoctorName = (doctor.FirstName ?? "") + " " + (doctor.LastName ?? "");
                    }
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    data = data.Where(x =>
                        (!string.IsNullOrEmpty(x.PatientName) && x.PatientName.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.DoctorName) && x.DoctorName.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.AdmissionNumber) && x.AdmissionNumber.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(x.Status) && x.Status.ToLower().Contains(search))
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
                _logger.LogError(ex, "Error loading IPD admissions");
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
                ViewBag.Search = search;
                ViewBag.Error = "Something went wrong while loading IPD admissions.";
                return View(new List<IPDAdmissionModel>());
            }
        }

        // ===================== CREATE (GET) =====================
        [HttpGet]
        public IActionResult Create(int? Id)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            IPDAdmissionModel model = new IPDAdmissionModel
            {
                AdmissionDateTime = DateTime.Now,
                IsActiveAdmission = true
            };

            if (Id.HasValue)
            {
                var appointment = _iAppointment.GetAppointmentById(Id.Value, hospitalId, subHospitalId);

                if (appointment != null)
                {
                    model.PatientId = appointment.PatientId;
                    model.PrimaryDoctorId = appointment.DoctorId;

                    var opdVisitId = _IOPD.GetOPDIdByAppointmentId(Id.Value, hospitalId, subHospitalId);
                    model.OPDVisitId = opdVisitId;

                    var patient = _ipatient.GetPatientById(appointment.PatientId, hospitalId, subHospitalId);
                    if (patient != null)
                        model.PatientName = (patient.FirstName ?? "") + " " + (patient.LastName ?? "");

                    var doctor = _iDoctor.GetDoctorById(appointment.DoctorId, hospitalId, subHospitalId);
                    if (doctor != null)
                        model.DoctorName = (doctor.FirstName ?? "") + " " + (doctor.LastName ?? "");

                    model.IsFromAppointment = true;
                }
                else
                {
                    _logger.LogWarning("Appointment not found for Id {Id}", Id.Value);
                }
            }

            ViewBag.IsUpdate = false;
            return View(model);
        }

        // ===================== CREATE / UPDATE (POST) =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(IPDAdmissionModel model)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                if (model.PatientId <= 0)
                    ModelState.AddModelError("", "Patient selection required");

                if (model.PrimaryDoctorId <= 0)
                    ModelState.AddModelError("", "Doctor selection required");

                if (!ModelState.IsValid)
                {
                    ViewBag.IsUpdate = model.IPDId > 0;
                    return View("Create", model);
                }

                if (model.IPDId > 0)  // UPDATE
                {
                    int rows = _repository.UpdateIPDAdmission(model, hospitalId, subHospitalId);
                    TempData[rows > 0 ? "Success" : "Error"] = rows > 0
                        ? "IPD Admission updated successfully"
                        : "No record updated!";
                    return RedirectToAction("Index");
                }
                else  // INSERT
                {
                    model.AdmissionNumber = "IPD" + DateTime.Now.ToString("yyyyMMddHHmmss");

                    int newIpdId = _repository.AddIPDAdmission(model, hospitalId, subHospitalId);
                    TempData["Success"] = "IPD Admission added successfully";
                    return RedirectToAction("Create", "IPDBedAllocation", new { ipdId = newIpdId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving IPD Admission");
                TempData["Error"] = "Error while saving IPD Admission data.";
                ViewBag.IsUpdate = model.IPDId > 0;
                return View("Create", model);
            }
        }

        // ===================== EDIT (GET) =====================
        // ⚠️  KEY FIX: Populates ViewBag.CurrentDoctorId so the dropdown pre-selects
        //              the existing doctor. Also loads reference doctors for the dropdown.
        [HttpGet]
        public IActionResult Edit(int id)
        {
            try
            {
                if (id <= 0) return NotFound();

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var data = _repository.GetIPDAdmissionById(id, hospitalId, subHospitalId);
                if (data == null) return NotFound();

                // In edit mode patient and admission date are LOCKED — cannot be changed
                data.IsFromAppointment = false;

                // ✅ Pass current doctor ID so JS can pre-select it after AJAX load
                ViewBag.CurrentDoctorId = data.PrimaryDoctorId;

                // ✅ Pass current referring doctor ID for pre-selection
                ViewBag.CurrentReferringDoctorId = data.ReferringDoctorId;

                // ✅ Populate patient name for display (read-only in edit)
                var patient = _ipatient.GetPatientById(data.PatientId, hospitalId, subHospitalId);
                if (patient != null)
                    data.PatientName = (patient.FirstName ?? "") + " " + (patient.LastName ?? "");

                ViewBag.IsUpdate = true;
                return View("Create", data);   // reuse Create view — IsUpdate = true locks readonly fields
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading IPD Admission for edit");
                TempData["Error"] = "Error while loading IPD Admission data.";
                return RedirectToAction("Index");
            }
        }

        // ===================== DELETE (SOFT DELETE) =====================
        // ⚠️  Uses sp_DeleteIPDAdmission which:
        //      1. Sets Status = 'Deleted'  (soft delete — record stays in DB)
        //      2. Closes current bed allocation  (IsCurrent = 0)
        //      3. Frees the bed  (OperationalStatus = 'Active')
        //
        // Only allow delete if Status = 'Admitted'.
        // Discharged records should NOT be deleted — they are medical history.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                if (id <= 0) return NotFound();

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                // ✅ Safety check: only allow cancellation of 'Admitted' records
                var admission = _repository.GetIPDAdmissionById(id, hospitalId, subHospitalId);
                if (admission == null)
                {
                    TempData["Error"] = "IPD Admission not found.";
                    return RedirectToAction("Index");
                }

                if (admission.Status == "Discharged")
                {
                    TempData["Error"] = "Discharged records cannot be deleted. They are permanent medical history.";
                    return RedirectToAction("Index");
                }

                _repository.DeleteIPDAdmission(id, hospitalId, subHospitalId);
                TempData["Success"] = "IPD Admission cancelled and removed successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting IPD Admission ID: {id}", id);
                TempData["Error"] = "Error while deleting IPD Admission.";
                return RedirectToAction("Index");
            }
        }

        // ===================== DETAILS =====================
        [HttpGet]
        public IActionResult Details(int id)
        {
            try
            {
                if (id <= 0) return NotFound();

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var admission = _repository.GetIPDAdmissionById(id, hospitalId, subHospitalId);
                if (admission == null) return NotFound();

                var patient = _ipatient.GetPatientById(admission.PatientId, hospitalId, subHospitalId);
                var vitals = _vitalsRepository.GetVitalsByIPDId(id, hospitalId, subHospitalId);
                var rounds = _roundRepository.GetRoundsByIPD(id, hospitalId);
                var nursingCharges = _nursingRepo.GetByIPDId(id);

                var model = new IPDDetailsViewModel
                {
                    Admission = admission,
                    Patient = patient,
                    VitalsList = vitals,
                    RoundsList = rounds,
                    NursingCharges = nursingCharges ?? new List<IPDNursingCharge>()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading IPD Details");
                TempData["Error"] = "Unable to load IPD details.";
                return RedirectToAction("Index");
            }
        }

        // ===================== AJAX: GET DOCTORS =====================
        [HttpGet]
        public JsonResult GetDoctors()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var doctors = _iDoctor.GetAllDoctor(hospitalId, subHospitalId) ?? new List<Doctor>();

            var result = doctors.Select(d => new
            {
                id = d.Doctor_Id,
                name = (d.FirstName ?? "") + " " + (d.LastName ?? "")
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        public JsonResult SearchPatientByMobile(string search)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var patients = _ipatient.SearchPatientByMobile(search, hospitalId, subHospitalId)
                           ?? new List<Patient>();

            var result = patients.Select(p => new
            {
                id = p.Id,
                name = (p.FirstName ?? "") + " " + (p.LastName ?? ""),
                mobile = p.PhoneNumber,
                gender = p.Gender
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        public JsonResult GetReferenceDoctors()
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var data = _referenceDoctor
                    .GetAllReferenceDoctor(hospitalId, subHospitalId)
                    .Select(x => new { id = x.ReferenceDoctorId, name = x.DoctorName })
                    .ToList();

                return Json(data);
            }
            catch (Exception)
            {
                return Json(null);
            }
        }

      

    }
}





