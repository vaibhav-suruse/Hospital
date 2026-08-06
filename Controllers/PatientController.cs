using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class PatientController : Controller
    {
        public static List<Patient> lstPatient = new List<Patient>();
        public static List<Symptom> _PreAuthSymTomsList = new List<Symptom> { };
        public static List<tablet> _MedicinesList = new List<tablet> { };
        public static Dictionary<string, string> _BillingDetails = new Dictionary<string, string>();
        private static List<BillingDetailModel> _BillingList = new List<BillingDetailModel>();
        private readonly Ipatient _patientRepo;
        private readonly IMedicine _medicineRepo;
        private readonly ISymptom _symptomRepo;
        private readonly IDoctor _doctorService;
        private readonly IOPDAppointment _appointmentRepo;
        private readonly IBillingMaster _IBillingMaster;
        private readonly IOPD _IOPD;
        private readonly IHospital _IHospital;
        private readonly INotification _notifRepo;

        public PatientController(IHospital Hospital, IBillingMaster billingMaster, IOPD OPD, IOPDAppointment appointmentRepo, Ipatient patientRepo, ISymptom symptom, IMedicine medicine, IDoctor doctor, INotification notifRepo)
        {
            _patientRepo = patientRepo;
            _medicineRepo = medicine;
            _symptomRepo = symptom;
            _doctorService = doctor;
            _appointmentRepo = appointmentRepo;
            _IOPD = OPD;
            _IBillingMaster = billingMaster;
            _IHospital = Hospital;
            _notifRepo = notifRepo;
        }

        [HttpGet]
        public IActionResult PrintPriscription(int appointmentId)
        {
            if (appointmentId <= 0)
                return BadRequest("Appointment identifier missing");

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId")
                 ?? HttpContext.Session.GetInt32("PatientHospitalId")
                 ?? 0;

            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId")
                                 ?? HttpContext.Session.GetInt32("PatientSubHospitalId");

            var appointment = _appointmentRepo
                .GetAppointmentById(appointmentId, hospitalId, subHospitalId);

            if (appointment == null)
                return NotFound("Appointment not found");

            // Track origin so the action panel's "Edit Prescription" / back
            // navigation can return to WalkInConsultation for walk-ins.
            ViewBag.IsWalkIn = appointment.IsWalkIn;
            ViewBag.AppointmentId = appointmentId;

            int opdId = _IOPD
                .GetOPDIdByAppointmentId(appointmentId, hospitalId, subHospitalId);
            Console.WriteLine("OPD ID in Controller: " + opdId);

            if (opdId == null)
            {
                TempData["ToastMessage"] = "Record not found.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Index", "Appointment");
            }

            var opdData = _IOPD
                .GetOPDById(opdId, hospitalId, subHospitalId);
            var medicines = _appointmentRepo.GetMedicinesByOPDId(opdId);

            var symptomVMList = _IOPD.GetOPDSymptomsByOPDId(opdId);
            var symptomNames = symptomVMList.Select(s => s.SymptomName).ToList();

            var patient = _patientRepo.GetPatientById(appointment.PatientId, hospitalId, subHospitalId);
            var doctor = _doctorService.GetDoctorById(appointment.DoctorId, hospitalId, subHospitalId);
            var hospital = _IHospital.GetsubandMainHospitalById(hospitalId, subHospitalId);

            var doctorFullName = doctor != null
                     ? $"{doctor.FirstName} {doctor.LastName}"
                     : string.Empty;

            var patientFullName = patient != null
                                  ? $"{patient.FirstName} {patient.LastName}"
                                  : string.Empty;

            var model = new PrescriptionVM
            {
                HospitalName = hospital?.Name,
                HospitalAddress = hospital?.Address,
                HospitalLogo = hospital?.Logo,
                HospitalRegistrationNo = hospital?.RegistrationNumber,
                HospitalEmail = hospital?.EmailId,
                PatientName = patientFullName,
                Age = 0,
                Gender = patient?.Gender,

                DoctorName = doctorFullName,
                Specialization = doctor?.Specialization,
                Education = doctor?.Education,
                DoctorRegNo = doctor?.DoctorCode,

                AppointmentDate = appointment.AppointmentDate,

                BP = opdData?.BP,
                Pulse = opdData?.Pulse,
                Investigation = opdData?.Investigation,

                Medicines = medicines,
                Symptoms = symptomVMList
            };
            if (!string.IsNullOrEmpty(patient?.Age))
            {
                int.TryParse(patient.Age, out int age);
                model.Age = age;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult TestMedicines(int appointmentId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            int opdId = _IOPD.GetOPDIdByAppointmentId(appointmentId, hospitalId, subHospitalId);
            var medicines = _appointmentRepo.GetMedicinesByOPDId(opdId);

            return Json(new { appointmentId, opdId, medicinesCount = medicines.Count, medicines });
        }

        public IActionResult Print([Bind] PatientReportModel _Patient)
        {
            string FirstName = _Patient.PatientName.Split(' ')[0];
            if (_Patient.tablate.Count == 0)
                _Patient.tablate = lstPatient.Where(w => w.FirstName == FirstName).Select(s => s.Medicineslist).FirstOrDefault();

            PriscriptionReport priscriptionReport = new PriscriptionReport();
            priscriptionReport.GenrateReport(_Patient);

            return RedirectToAction("Index");
        }

        public IActionResult BillingofPatient(int appointmentId)
        {
            if (appointmentId <= 0)
                return BadRequest("Appointment identifier missing");

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var appointment = _appointmentRepo.GetAppointmentById(appointmentId, hospitalId, subHospitalId);
            if (appointment == null)
                return NotFound("Appointment not found");

            int opdId = _IOPD.GetOPDIdByAppointmentId(appointmentId, hospitalId, subHospitalId);
            return View(appointment);
        }

        [HttpGet]
        public JsonResult GetBillingTypes()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var list = _IBillingMaster.GetAllBillings(hospitalId, subHospitalId);
            var dropdownList = list.Select(x => new { x.Id, x.Name }).ToList();
            return Json(dropdownList);
        }

        // ======================== ✅ EDITASOPD FIXED =========================
        public IActionResult EditAsOPD(int appointmentId)
        {
            _PreAuthSymTomsList.Clear();
            _MedicinesList.Clear();

            if (appointmentId <= 0)
                return BadRequest("Appointment identifier missing");

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var appointment = _appointmentRepo.GetAppointmentById(appointmentId, hospitalId, subHospitalId);
            if (appointment == null)
                return NotFound("Appointment not found");

            // ✅ Set ViewBag for redirect target based on appointment type
            ViewBag.IsWalkIn = appointment.IsWalkIn;

            var patient = _patientRepo.GetPatientById(appointment.PatientId, hospitalId, subHospitalId);

            int opdId = _IOPD.GetOPDIdByAppointmentId(appointmentId, hospitalId, subHospitalId);

            // UPDATE FLOW
            if (opdId > 0)
            {
                var opdDetail = _IOPD.GetOPDById(opdId, hospitalId, subHospitalId);

                if (opdDetail == null)
                    return NotFound("OPD not found");

                // Always reload the persisted report path for edit mode.
                opdDetail.ReportFilePath = opdDetail.ReportFilePath?.Trim();

                // ✅ FIX 1: Set PatientId manually
                opdDetail.PatientId = appointment.PatientId;

                return View(opdDetail);
            }

            // CREATE FLOW
            var opd = new OPD
            {
                AppointmentId = appointmentId,
                PatientId = appointment.PatientId,   // ✅ FIX 2
                BP = "",
                Pulse = "",
                Investigation = "",
                ReportDetail = "",
                Symptoms = new List<int>(),
                Medicines = new List<OPDMedicine>()
            };

            return View(opd);
        }


        // ======================== ✅ ADDOPD WITH DIAGNOSIS =========================
        [HttpPost]
        public IActionResult AddOPD(OPD model, string SymptomsJson, string MedicinesJson, string DiagnosesJson)
        {
            if (!ModelState.IsValid)
            {
                return View("EditAsOPD", model);
            }

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            // ✅ FIX BUG 4: Use SymptomsJson parameter instead of static _PreAuthSymTomsList
            if (!string.IsNullOrEmpty(SymptomsJson))
            {
                try
                {
                    var tempSymptoms = JsonSerializer.Deserialize<List<int>>(SymptomsJson);
                    model.Symptoms = tempSymptoms?.Where(s => s > 0).ToList() ?? new List<int>();
                }
                catch
                {
                    model.Symptoms = new List<int>();
                }
            }
            else
            {
                model.Symptoms = new List<int>();
            }

            // ── Medicines ────────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(MedicinesJson))
            {
                try
                {
                    var tempList = JsonSerializer.Deserialize<List<OPDMedicine>>(MedicinesJson);
                    model.Medicines = tempList?
                        .Where(m => m != null && m.MedicineId > 0)
                        .Select(m => new OPDMedicine
                        {
                            MedicineId = m.MedicineId,
                            Morning = m.Morning ?? "0",
                            Afternoon = m.Afternoon ?? "0",
                            Evening = m.Evening ?? "0",
                            Days = m.Days > 0 ? m.Days : 1,
                            Frequency = m.Frequency ?? "TDS",
                            WhenToTake = m.WhenToTake ?? "After Food",
                            Dose = m.Dose ?? "1",
                            Unit = m.Unit ?? "Tablet(s)",
                            Quantity = m.Quantity
                        })
                        .ToList() ?? new List<OPDMedicine>();
                }
                catch
                {
                    model.Medicines = new List<OPDMedicine>();
                }
            }
            else
            {
                model.Medicines = new List<OPDMedicine>();
            }

            // ── Diagnoses (NEW) ──────────────────────────────────────────────
            if (!string.IsNullOrEmpty(DiagnosesJson))
            {
                try
                {
                    var tempDiagnoses = JsonSerializer.Deserialize<List<OPDDiagnosis>>(DiagnosesJson);
                    model.Diagnoses = tempDiagnoses?
                        .Where(d => d != null && !string.IsNullOrEmpty(d.DiagnosisName))
                        .Select(d => new OPDDiagnosis
                        {
                            DiagnosisName = d.DiagnosisName.Trim(),
                            Type = d.Type ?? "Final",
                            Notes = d.Notes ?? ""
                        })
                        .ToList() ?? new List<OPDDiagnosis>();
                }
                catch
                {
                    model.Diagnoses = new List<OPDDiagnosis>();
                }
            }
            else
            {
                model.Diagnoses = new List<OPDDiagnosis>();
            }

            // ── Report File Upload ───────────────────────────────────────────
            if (model.ReportFile != null && model.ReportFile.Length > 0)
            {
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/OPDReports");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = Guid.NewGuid() + Path.GetExtension(model.ReportFile.FileName);
                string filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.ReportFile.CopyTo(stream);
                }

                model.ReportFilePath = "/OPDReports/" + fileName;
            }

            if (model.Id > 0)
            {
                var existingOpd = _IOPD.GetOPDById(model.Id, hospitalId, subHospitalId);
                if (existingOpd != null)
                {
                    model.HospitalId = existingOpd.HospitalId;
                    model.SubHospitalId = existingOpd.SubHospitalId;
                }
                if (model.ReportFile == null || model.ReportFile.Length == 0)
                {
                    model.ReportFilePath = existingOpd?.ReportFilePath;
                }
            }

            // ── Save OPD ─────────────────────────────────────────────────────
            int opdId;
            if (model.Id > 0)
                opdId = _IOPD.UpdateOPD(model);
            else
                opdId = _IOPD.AddOPD(model, hospitalId, subHospitalId);

            if (opdId <= 0)
            {
                ModelState.AddModelError("", "OPD save failed");
                return View("EditAsOPD", model);
            }

            // ── Update Appointment Status ────────────────────────────────────
            var appointment = _appointmentRepo.GetAppointmentById(model.AppointmentId, hospitalId, subHospitalId);
            if (appointment != null)
            {
                _appointmentRepo.UpdateStatus(model.AppointmentId, hospitalId, subHospitalId, "OPD Completed");
            }

            // ── Medicine Notification ────────────────────────────────────────
            if (model.Medicines != null && model.Medicines.Count > 0)
            {
                try
                {
                    string summary;
                    if (model.Medicines.Count == 1)
                        summary = model.Medicines[0].MedicineName ?? "1 medicine";
                    else if (model.Medicines.Count == 2)
                        summary = string.Join(", ", model.Medicines.Take(2).Select(m => m.MedicineName ?? "Medicine"));
                    else
                        summary = string.Join(", ", model.Medicines.Take(2).Select(m => m.MedicineName ?? "Medicine"))
                                  + $" +{model.Medicines.Count - 2} more";

                    var patient = _patientRepo.GetPatientById(model.PatientId, hospitalId, subHospitalId);
                    string patientName = patient != null
                        ? $"{patient.FirstName} {patient.LastName}".Trim()
                        : $"Patient #{model.PatientId}";

                    string doctorName = "";
                    if (appointment != null)
                    {
                        var doc = _doctorService?.GetDoctorById(appointment.DoctorId, hospitalId, subHospitalId);
                        if (doc != null)
                            doctorName = $"{doc.FirstName} {doc.LastName}".Trim();
                    }

                    _notifRepo.InsertNotification(new MedicineNotificationModel
                    {
                        PatientId = model.PatientId,
                        PatientName = patientName,
                        OPDId = opdId,
                        AppointmentId = model.AppointmentId,
                        DoctorName = doctorName,
                        MedicineCount = model.Medicines.Count,
                        MedicinesSummary = summary,
                        Type = "OPD",
                        HospitalId = hospitalId,
                        SubHospitalId = subHospitalId
                    });
                }
                catch { }
            }

            TempData["SuccessMessage"] = "OPD saved successfully";

            // ✅ Redirect walk-in appointments back to WalkInConsultation
            if (appointment != null && appointment.IsWalkIn)
            {
                return RedirectToAction("Index", "WalkInConsultation");
            }

            return RedirectToAction("Index", "OPDAppointment");
        }



        //// ======================== ✅ ADDOPD FIXED =========================
        //[HttpPost]
        //public IActionResult AddOPD(OPD model, string SymptomsJson, string MedicinesJson)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View("EditAsOPD", model);
        //    }

        //    int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //    int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

        //    model.Symptoms = _PreAuthSymTomsList.Select(s => s.SymptomId).ToList();
        //    if (!string.IsNullOrEmpty(MedicinesJson))
        //    {
        //        try
        //        {
        //            var tempList = JsonSerializer.Deserialize<List<OPDMedicine>>(MedicinesJson);
        //            model.Medicines = tempList?
        //                .Where(m => m != null && m.MedicineId > 0)
        //                .Select(m => new OPDMedicine
        //                {
        //                    MedicineId = m.MedicineId,
        //                    Morning = m.Morning ?? "0",
        //                    Afternoon = m.Afternoon ?? "0",
        //                    Evening = m.Evening ?? "0",
        //                    Days = m.Days > 0 ? m.Days : 1,
        //                    Frequency = m.Frequency ?? "TDS",
        //                    WhenToTake = m.WhenToTake ?? "After Food",
        //                    Dose = m.Dose ?? "1",
        //                    Unit = m.Unit ?? "Tablet(s)",
        //                    Quantity = m.Quantity
        //                })
        //                .ToList() ?? new List<OPDMedicine>();
        //        }
        //        catch
        //        {
        //            model.Medicines = new List<OPDMedicine>();
        //        }
        //    }
        //    else
        //    {
        //        model.Medicines = new List<OPDMedicine>();
        //    }

        //    if (model.ReportFile != null && model.ReportFile.Length > 0)
        //    {
        //        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/OPDReports");

        //        if (!Directory.Exists(folderPath))
        //            Directory.CreateDirectory(folderPath);

        //        string fileName = Guid.NewGuid() + Path.GetExtension(model.ReportFile.FileName);
        //        string filePath = Path.Combine(folderPath, fileName);

        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            model.ReportFile.CopyTo(stream);
        //        }

        //        model.ReportFilePath = "/OPDReports/" + fileName;
        //    }

        //    if (model.Id > 0)
        //    {
        //        var existingOpd = _IOPD.GetOPDById(model.Id, hospitalId, subHospitalId);

        //        if (model.ReportFile == null || model.ReportFile.Length == 0)
        //        {
        //            model.ReportFilePath = existingOpd?.ReportFilePath;
        //        }
        //    }

        //    int opdId;
        //    if (model.Id > 0)
        //    {
        //        opdId = _IOPD.UpdateOPD(model);
        //    }
        //    else
        //    {
        //        // ✅ FIX 3: Get real OPDId from DB
        //        opdId = _IOPD.AddOPD(model, hospitalId, subHospitalId);
        //    }

        //    if (opdId <= 0)
        //    {
        //        ModelState.AddModelError("", "OPD save failed");
        //        return View("EditAsOPD", model);
        //    }

        //    var appointment = _appointmentRepo.GetAppointmentById(model.AppointmentId, hospitalId, subHospitalId);
        //    if (appointment != null)
        //    {
        //        _appointmentRepo.UpdateStatus(model.AppointmentId, hospitalId, subHospitalId, "OPD Completed");
        //    }

        //    if (model.Medicines != null && model.Medicines.Count > 0)
        //    {
        //        try
        //        {
        //            string summary = "";
        //            if (model.Medicines.Count == 1)
        //                summary = model.Medicines[0].MedicineName ?? "1 medicine";
        //            else if (model.Medicines.Count == 2)
        //                summary = string.Join(", ", model.Medicines.Take(2).Select(m => m.MedicineName ?? "Medicine"));
        //            else
        //                summary = string.Join(", ", model.Medicines.Take(2).Select(m => m.MedicineName ?? "Medicine"))
        //                          + $" +{model.Medicines.Count - 2} more";

        //            var patient = _patientRepo.GetPatientById(model.PatientId, hospitalId, subHospitalId);
        //            string patientName = patient != null
        //                ? $"{patient.FirstName} {patient.LastName}".Trim()
        //                : $"Patient #{model.PatientId}";

        //            string doctorName = "";
        //            if (appointment != null)
        //            {
        //                var doc = _doctorService?.GetDoctorById(appointment.DoctorId, hospitalId, subHospitalId);
        //                if (doc != null)
        //                    doctorName = $"{doc.FirstName} {doc.LastName}".Trim();
        //            }

        //            _notifRepo.InsertNotification(new MedicineNotificationModel
        //            {
        //                PatientId = model.PatientId,
        //                PatientName = patientName,
        //                OPDId = opdId,
        //                AppointmentId = model.AppointmentId,
        //                DoctorName = doctorName,
        //                MedicineCount = model.Medicines.Count,
        //                MedicinesSummary = summary,
        //                Type = "OPD",
        //                HospitalId = hospitalId,
        //                SubHospitalId = subHospitalId
        //            });
        //        }
        //        catch { }
        //    }

        //    TempData["SuccessMessage"] = "OPD saved successfully";
        //    return RedirectToAction("Index", "OPDAppointment");
        //}

        public IActionResult EditAsIPD(string Id)
        {
            var sdsdsd = Id;
            Patient patient = lstPatient.Where(w => w.FirstName.ToLower() == Id.ToLower()).Select(s => s).FirstOrDefault();
            var dsdsw = patient;
            return RedirectToAction("Index");
        }
        public IActionResult Index(string search = "", int page = 1)
        {
            int pageSize = 6;
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var data = _appointmentRepo.GetAllAppointments(hospitalId, subHospitalId).AsQueryable();

            // Server-side search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                data = data.Where(x =>
                    x.PatientId.ToString().Contains(search) ||
                    x.DoctorId.ToString().Contains(search) ||
                    x.AppointmentDate.ToString().Contains(search)
                );
            }

            // Pagination
            int totalRecords = data.Count();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            var pagedData = data.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;

            return View(pagedData);
        }

        public IActionResult Create()
        {
            Patient patient = new Patient();
            return View(patient);
        }

        public IActionResult SavePatientDetails([Bind] Patient _Patient)
        {

            lstPatient.Add(_Patient);

            return RedirectToAction("Index", "Patient");
        }

        public IActionResult UpdateBilling([Bind] Patient _Patient)
        {
            _Patient.BillingDetails = _BillingDetails;
            _Patient.Status = "Billing Completed";
            var _newpatianet = lstPatient.Where(w => w.FirstName.ToLower() == _Patient.FirstName.ToLower()).Select(s => s).FirstOrDefault();
            lstPatient.Remove(_newpatianet);
            lstPatient.Add(_Patient);

            return RedirectToAction("Index", "Patient");
        }
        public IActionResult UpdatePatientDetails([Bind] Patient _Patient)
        {
            if (string.IsNullOrEmpty(_Patient.ReportDetail))
            {
                _Patient.Status = "Visiting Progress";
            }
            else
                _Patient.Status = "Completed";

            //_Patient.symptoms = _PreAuthSymTomsList;
            //_Patient.symptomsmain = _PreAuthSymTomsList;
            _Patient.Medicineslist = _MedicinesList;
            var _newpatianet = lstPatient.Where(w => w.FirstName.ToLower() == _Patient.FirstName.ToLower()).Select(s => s).FirstOrDefault();
            lstPatient.Remove(_newpatianet);
            lstPatient.Add(_Patient);

            return RedirectToAction("Index", "Patient");
        }

        public IActionResult SaveSymtons(int Command)
        {
            if (Command > 0)
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var selectedSymptom = _symptomRepo.GetSymptomById(Command, hospitalId, subHospitalId);

                if (selectedSymptom != null)
                {
                    // prevent duplicates
                    if (!_PreAuthSymTomsList.Any(s => s.SymptomId == selectedSymptom.SymptomId))
                    {
                        _PreAuthSymTomsList.Add(new Symptom
                        {
                            SymptomId = selectedSymptom.SymptomId,
                            SymptomName = selectedSymptom.SymptomName,
                            SubName = selectedSymptom.SubName,
                            Description = selectedSymptom.Description
                        });
                    }
                }
            }


            return PartialView("_PreAuth", _PreAuthSymTomsList);
        }

        [HttpGet]
        public IActionResult DeleteSymtons(int Command) // Command = SymptomId
        {
            _PreAuthSymTomsList.RemoveAll(s => s.SymptomId == Command);
            return PartialView("_PreAuth", _PreAuthSymTomsList);
        }


        public IActionResult SaveMedicines(int Command, int _Morning, int _Afternoon, int _Evening, int _Days)
        {
            if (Command > 0)
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var selectedMedicine = _medicineRepo.GetMedicineById(Command, hospitalId, subHospitalId);

                if (selectedMedicine != null)
                {
                    tablet objtablet = new tablet();
                    objtablet.Id = _MedicinesList.Count > 0 ? _MedicinesList.Max(m => m.Id) + 1 : 1; // unique ID
                    objtablet.TablateName = selectedMedicine.MedicineName; // map correctly
                    objtablet.Morning = _Morning == 1 ? "Y" : "N";
                    objtablet.Afternoon = _Afternoon == 1 ? "Y" : "N";
                    objtablet.Evening = _Evening == 1 ? "Y" : "N";
                    objtablet.Days = _Days > 0 ? _Days : 1;

                    _MedicinesList.Add(objtablet);
                }
            }

            return PartialView("_Medicine", _MedicinesList);
        }




        [HttpGet]
        public IActionResult DeleteMedicines(int Command) // Command is the medicine ID now
        {
            _MedicinesList.RemoveAll(m => m.Id == Command);
            return PartialView("_Medicine", _MedicinesList);
        }

        [HttpGet]
        public IActionResult SaveBilling(int Command) // Command = BillingId
        {
            if (Command > 0)
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var selectedBilling = _IBillingMaster.GetBillingById(Command, hospitalId, subHospitalId);

                if (selectedBilling != null)
                {
                    // prevent duplicates
                    if (!_BillingList.Any(b => b.BillingId == selectedBilling.Id))
                    {
                        _BillingList.Add(new BillingDetailModel
                        {
                            BillingId = selectedBilling.Id,
                            BillName = selectedBilling.Name,
                            Amount = selectedBilling.Amount
                        });
                    }
                }
            }

            return PartialView("_TotalBill", _BillingList);
        }
        [HttpGet]
        public IActionResult DeleteBilling(int Command) // BillingId
        {
            _BillingList.RemoveAll(b => b.BillingId == Command);
            return PartialView("_TotalBill", _BillingList);
        }





        [HttpPost]
        public IActionResult CreatePatientDetails(Patient model)
        {
            if (!ModelState.IsValid)
                return View("Create", model);

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            // Handle profile photo upload
            var photoFile = Request.Form.Files["ProfilePhotoFile"];
            if (photoFile != null && photoFile.Length > 0)
            {
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/PatientPhotos");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = Guid.NewGuid() + Path.GetExtension(photoFile.FileName);
                string filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    photoFile.CopyTo(stream);

                model.ProfilePhoto = "/PatientPhotos/" + fileName;
            }

            if (model.Id == 0)
            {
                // ── NEW PATIENT: handled by CreatePatientDetailsAjax now ──
                // This branch only runs if someone submits the form without JS.
                // Safe fallback — save and go to PatientList.
                _patientRepo.AddPatient(model, hospitalId, subHospitalId);
                TempData["SuccessMessage"] = "Patient created successfully!";
                return RedirectToAction("PatientList");
            }
            else
            {
                // ── EDIT PATIENT: save then redirect back to origin ──
                _patientRepo.UpdatePatient(model, hospitalId, subHospitalId);
                TempData["SuccessMessage"] = "Patient updated successfully!";

                // Read the return URL — try form field first (survives validation errors),
                // then fall back to TempData (set when Edit was first opened)
                string returnUrl = Request.Form["ReturnUrl"].ToString();
                if (string.IsNullOrWhiteSpace(returnUrl))
                    returnUrl = TempData["EditReturnUrl"] as string;

                // Safety check: only redirect to local URLs to prevent open-redirect attacks
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("PatientList");
            }
        }


        //[HttpPost]
        //public IActionResult CreatePatientDetails(Patient model)
        //{
        //    if (!ModelState.IsValid)
        //        return View("Create", model);

        //    int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //    int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

        //    // Handle profile photo upload
        //    var photoFile = Request.Form.Files["ProfilePhotoFile"];
        //    if (photoFile != null && photoFile.Length > 0)
        //    {
        //        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/PatientPhotos");
        //        if (!Directory.Exists(folderPath))
        //            Directory.CreateDirectory(folderPath);

        //        string fileName = Guid.NewGuid() + Path.GetExtension(photoFile.FileName);
        //        string filePath = Path.Combine(folderPath, fileName);

        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //            photoFile.CopyTo(stream);

        //        model.ProfilePhoto = "/PatientPhotos/" + fileName;
        //    }

        //    if (model.Id == 0)
        //    {
        //        _patientRepo.AddPatient(model, hospitalId, subHospitalId);
        //        TempData["SuccessMessage"] = "Patient created successfully!";
        //    }
        //    else
        //    {
        //        _patientRepo.UpdatePatient(model, hospitalId, subHospitalId);
        //        TempData["SuccessMessage"] = "Patient updated successfully!";
        //    }

        //    return RedirectToAction("PatientList");
        //}


        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                _patientRepo.DeletePatient(id, hospitalId, subHospitalId);
                TempData["SuccessMessage"] = "Patient deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("PatientList");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            _PreAuthSymTomsList.Clear();
            _MedicinesList.Clear();

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            Patient patient = _patientRepo.GetPatientById(id, hospitalId, subHospitalId);

            if (patient == null)
                return NotFound();

            patient.FullName = $"{patient.FirstName} {patient.LastName}".Trim();
            patient.isUpdate = true;

            // Capture where the user came from so we can redirect back after save.
            // Uses the Referer header; falls back to PatientList.
            string returnUrl = Request.Headers["Referer"].ToString();
            if (string.IsNullOrWhiteSpace(returnUrl))
                returnUrl = Url.Action("PatientList", "Patient");

            // Store in TempData so it survives the POST round-trip
            TempData["EditReturnUrl"] = returnUrl;

            return View("Create", patient);
        }

        //[HttpGet]
        //public IActionResult Edit(int id)
        //{
        //    _PreAuthSymTomsList.Clear();
        //    _MedicinesList.Clear();

        //    int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //    int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

        //    Patient patient = _patientRepo.GetPatientById(id, hospitalId, subHospitalId);

        //    if (patient == null)
        //        return NotFound();

        //    // Build FullName so the single input in the form is pre-filled
        //    patient.FullName = $"{patient.FirstName} {patient.LastName}".Trim();

        //    patient.isUpdate = true;
        //    return View("Create", patient);
        //}


public IActionResult PatientList(string search = "", int page = 1)
        {
            int pageSize = 5;

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            // ═══ PERFORMANCE FIX ═══════════════════════════════════════
            // Previously this loaded ALL patients (sp_get_all_patient) into
            // memory, then searched + paginated in C#. With a large patient
            // table that caused the ~30s page load and slow pagination.
            // Now the COUNT and the current page's rows are fetched with a
            // single SQL query (WHERE + COUNT + LIMIT) — only 5 rows return.
            // ══════════════════════════════════════════════════════════
            List<Patient> pagedPatients = _patientRepo.GetPatientsPaged(
                hospitalId, subHospitalId, search, page, pageSize, out int totalRecords);

            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            if (totalPages < 1) totalPages = 1;

            // ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;

            return View(pagedPatients);
        }

        [HttpGet]
        public JsonResult GetAllSymptoms()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var symptoms = _symptomRepo.GetAllSymptoms(hospitalId, subHospitalId) ?? new List<Symptom>();
            var result = symptoms.Select(s => new { id = s.SymptomId, name = s.SymptomName }).ToList();

            return Json(result);
        }

        [HttpGet]
        public JsonResult GetAllMedicines()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var medicines = _medicineRepo.GetAllMedicine(hospitalId, subHospitalId) ?? new List<Medicine>();
            var result = medicines.Select(m => new { id = m.MedicineId, name = m.MedicineName }).ToList();

            return Json(result);
        }

        [HttpGet]
        public IActionResult LoadExistingSymptoms(int opdId)
        {
            var symptomsVM = _IOPD.GetOPDSymptomsByOPDId(opdId);

            var symptoms = symptomsVM.Select(s => new Symptom
            {
                SymptomId = s.Symptom_Id,
                SymptomName = s.SymptomName
            }).ToList();
            _PreAuthSymTomsList = symptoms;
            return PartialView("_PreAuth", symptoms);
        }

        [HttpGet]
        public IActionResult LoadExistingMedicines(int opdId)
        {
            var medList = _appointmentRepo.GetMedicinesByOPDId(opdId);

            var tablets = medList.Select(m => new tablet
            {
                Id = m.MedicineId,
                TablateName = m.MedicineName,
                Morning = m.Morning.ToString(),    // Convert int to string
                Afternoon = m.Afternoon.ToString(),// Convert int to string
                Evening = m.Evening.ToString(),    // Convert int to string
                Days = m.Days
            }).ToList();
            _MedicinesList = tablets;

            return PartialView("_Medicine", tablets);
        }





        [HttpPost]
        public IActionResult CreatePatientDetailsAjax(Patient model)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId")
                                 ?? HttpContext.Session.GetInt32("PatientHospitalId")
                                 ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId")
                                  ?? HttpContext.Session.GetInt32("PatientSubHospitalId");

                // Handle profile photo upload (same logic as CreatePatientDetails)
                var photoFile = Request.Form.Files["ProfilePhotoFile"];
                if (photoFile != null && photoFile.Length > 0)
                {
                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/PatientPhotos");
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string fileName = Guid.NewGuid() + Path.GetExtension(photoFile.FileName);
                    string filePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        photoFile.CopyTo(stream);

                    model.ProfilePhoto = "/PatientPhotos/" + fileName;
                }

                // Save patient to DB
                int patientId = _patientRepo.AddPatient(model, hospitalId, subHospitalId);
                if (patientId <= 0)
                    return Json(new { success = false, message = "Failed to save patient." });

                // Create a walk-in appointment for this patient
                int appointmentId = CreateWalkInAppointment(patientId, hospitalId, subHospitalId);
                if (appointmentId <= 0)
                    return Json(new { success = false, message = "Patient saved but could not create appointment." });

                return Json(new { success = true, appointmentId = appointmentId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── Private helper: inserts a walk-in row into opdappointment ──
        // (same logic as WalkInConsultationController.CreateWalkInAppointmentInDb)
        private int CreateWalkInAppointment(int patientId, int hospitalId, int? subHospitalId)
        {
            try
            {
                var config = HttpContext.RequestServices
                                .GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration))
                                as Microsoft.Extensions.Configuration.IConfiguration;

                string connStr = config.GetConnectionString("MySqlConnection");

                using var con = new MySql.Data.MySqlClient.MySqlConnection(connStr);
                using var cmd = new MySql.Data.MySqlClient.MySqlCommand(@"
            INSERT INTO opdappointment
                (PatientId, HospitalId, SubHospitalId, DoctorId,
                 AppointmentDate, AppointmentTime, Status, IsActive, IsWalkIn)
            VALUES
                (@PatientId, @HospitalId, @SubHospitalId, 0,
                 @AppointmentDate, @AppointmentTime, 'Pending', 1, 1);
            SELECT LAST_INSERT_ID();", con);

                cmd.Parameters.AddWithValue("@PatientId", patientId);
                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("@SubHospitalId",
                    subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@AppointmentDate", DateTime.Today);
                cmd.Parameters.AddWithValue("@AppointmentTime", DateTime.Now.ToString("HH:mm:ss"));

                con.Open();
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0;
            }
        }

        [HttpGet]
        public JsonResult GetPatientInfo(int patientId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var p = _patientRepo.GetPatientById(patientId, hospitalId, subHospitalId);
            if (p == null) return Json(null);

            return Json(new
            {
                id = p.Id,
                firstName = p.FirstName,
                lastName = p.LastName,
                gender = p.Gender,
                age = p.Age,
                phoneNumber = p.PhoneNumber,
                bloodGroup = p.BloodGroup,
                address = p.Address
            });
        }

        // ── 2. Existing symptoms as JSON (for edit-mode chip restore) ─────────────
        [HttpGet]
        public JsonResult LoadExistingSymptomsJson(int opdId)
        {
            var list = _IOPD.GetOPDSymptomsByOPDId(opdId);
            var result = list.Select(s => new
            {
                symptomId = s.Symptom_Id,
                symptomName = s.SymptomName
            }).ToList();
            return Json(result);
        }


        // ═══════════════════════════════════════════════════════════════════════════
        //  NEW ENDPOINTS — Add these methods to OPDAppointmentController.cs
        //  (paste inside the OPDAppointmentController class, before the closing brace)
        // ═══════════════════════════════════════════════════════════════════════════

        // ── 3. Medicines by OPD ID as JSON (for edit-mode Rx table restore) ───────
        [HttpGet]
        public JsonResult GetMedicinesByOPDIdJson(int opdId)
        {
            var list = _appointmentRepo.GetMedicinesByOPDId(opdId);
            var result = list.Select(m => new
            {
                medicineId = m.MedicineId,
                medicineName = m.MedicineName,
                morning = m.Morning,
                afternoon = m.Afternoon,
                evening = m.Evening,
                days = m.Days,
                quantity = m.Quantity,  // ✅ FIX BUG 3: Include Quantity in edit response
                when = "After Food"   // default; extend SP/model if you store this
            }).ToList();
            return Json(result);
        }

// ── 4. Patient full history as JSON (for left panel history timeline) ─────
        [HttpGet]
        public JsonResult GetPatientHistory(int patientId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId")
                             ?? HttpContext.Session.GetInt32("PatientHospitalId")
                             ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId")
                              ?? HttpContext.Session.GetInt32("PatientSubHospitalId");

            var history = _appointmentRepo.GetPatientFullHistory(patientId, hospitalId, subHospitalId);
            var result = history.Select(v => new
            {
                id = v.Id,
                appointmentId = v.AppointmentId,
                appointmentDate = v.AppointmentDate,
                bp = v.BP,
                pulse = v.Pulse,
                investigation = v.Investigation,
                reportDetail = v.ReportDetail,
                reportFilePath = v.ReportFilePath,
                nextAppointmentDate = v.NextAppointmentDate,
                symptom = v.Symptom,
                medicines = v.Medicines.Select(m => new
                {
                    medicineName = m.MedicineName,
                    morning = m.Morning,
                    afternoon = m.Afternoon,
                    evening = m.Evening,
                    days = m.Days
                }).ToList()
            }).ToList();
            return Json(result);
        }


        [HttpGet]
        public JsonResult GetAllDiagnoses()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            var list = _IOPD.GetAllDiagnoses(hospitalId);
            return Json(list.Select(d => new { id = d.Id, name = d.Name, icdCode = d.IcdCode, category = d.Category }));
        }


    }
}
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.Extensions.Configuration;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Security.Cryptography;
//using System.Text.Json;
//using System.Threading.Tasks;
//using WebApplicationSampleTest2.Models;
//using WebApplicationSampleTest2.Repository;

//namespace WebApplicationSampleTest2.Controllers
//{
//    public class PatientController : Controller
//    {
//        public static List<Patient> lstPatient = new List<Patient>();
//        public static List<Symptom> _PreAuthSymTomsList = new List<Symptom> { };
//        public static List<tablet> _MedicinesList = new List<tablet> { };
//        public static Dictionary<string, string> _BillingDetails = new Dictionary<string, string>();
//        private static List<BillingDetailModel> _BillingList = new List<BillingDetailModel>();
//        private readonly Ipatient _patientRepo;
//        private readonly IMedicine _medicineRepo;
//        private readonly ISymptom _symptomRepo;
//        private readonly IDoctor _doctorService;
//        private readonly IOPDAppointment _appointmentRepo;
//        private readonly IBillingMaster _IBillingMaster;
//        private readonly IOPD _IOPD;
//        private readonly IHospital _IHospital;
//        private readonly INotification _notifRepo;

//        public PatientController(IHospital Hospital, IBillingMaster billingMaster, IOPD OPD, IOPDAppointment appointmentRepo, Ipatient patientRepo, ISymptom symptom, IMedicine medicine, IDoctor doctor, INotification notifRepo)
//        {
//            _patientRepo = patientRepo;
//            _medicineRepo = medicine;
//            _symptomRepo = symptom;
//            _doctorService = doctor;
//            _appointmentRepo = appointmentRepo;
//            _IOPD = OPD;
//            _IBillingMaster = billingMaster;
//            _IHospital = Hospital;
//            _notifRepo = notifRepo;
//        }

//        [HttpGet]
//        public IActionResult PrintPriscription(int appointmentId)
//        {
//            if (appointmentId <= 0)
//                return BadRequest("Appointment identifier missing");

//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId")
//                 ?? HttpContext.Session.GetInt32("PatientHospitalId")
//                 ?? 0;

//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId")
//                                 ?? HttpContext.Session.GetInt32("PatientSubHospitalId");

//            var appointment = _appointmentRepo
//                .GetAppointmentById(appointmentId, hospitalId, subHospitalId);

//            if (appointment == null)
//                return NotFound("Appointment not found");

//            int opdId = _IOPD
//                .GetOPDIdByAppointmentId(appointmentId, hospitalId, subHospitalId);
//            Console.WriteLine("OPD ID in Controller: " + opdId);

//            if (opdId == null)
//            {
//                TempData["ToastMessage"] = "Record not found.";
//                TempData["ToastType"] = "warning";
//                return RedirectToAction("Index", "Appointment");
//            }

//            var opdData = _IOPD
//                .GetOPDById(opdId, hospitalId, subHospitalId);
//            var medicines = _appointmentRepo.GetMedicinesByOPDId(opdId);

//            var symptomVMList = _IOPD.GetOPDSymptomsByOPDId(opdId);
//            var symptomNames = symptomVMList.Select(s => s.SymptomName).ToList();

//            var patient = _patientRepo.GetPatientById(appointment.PatientId, hospitalId, subHospitalId);
//            var doctor = _doctorService.GetDoctorById(appointment.DoctorId, hospitalId, subHospitalId);
//            var hospital = _IHospital.GetsubandMainHospitalById(hospitalId, subHospitalId);

//            var doctorFullName = doctor != null
//                     ? $"{doctor.FirstName} {doctor.LastName}"
//                     : string.Empty;

//            var patientFullName = patient != null
//                                  ? $"{patient.FirstName} {patient.LastName}"
//                                  : string.Empty;

//            var model = new PrescriptionVM
//            {
//                HospitalName = hospital?.Name,
//                HospitalAddress = hospital?.Address,
//                HospitalLogo = hospital?.Logo,
//                HospitalRegistrationNo = hospital?.RegistrationNumber,
//                HospitalEmail = hospital?.EmailId,
//                PatientName = patientFullName,
//                Age = 0,
//                Gender = patient?.Gender,

//                DoctorName = doctorFullName,
//                Specialization = doctor?.Specialization,
//                Education = doctor?.Education,

//                AppointmentDate = appointment.AppointmentDate,

//                BP = opdData?.BP,
//                Pulse = opdData?.Pulse,
//                Investigation = opdData?.Investigation,

//                Medicines = medicines,
//                Symptoms = symptomVMList
//            };
//            if (!string.IsNullOrEmpty(patient?.Age))
//            {
//                int.TryParse(patient.Age, out int age);
//                model.Age = age;
//            }

//            return View(model);
//        }

//        [HttpGet]
//        public IActionResult TestMedicines(int appointmentId)
//        {
//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//            int opdId = _IOPD.GetOPDIdByAppointmentId(appointmentId, hospitalId, subHospitalId);
//            var medicines = _appointmentRepo.GetMedicinesByOPDId(opdId);

//            return Json(new { appointmentId, opdId, medicinesCount = medicines.Count, medicines });
//        }

//        public IActionResult Print([Bind] PatientReportModel _Patient)
//        {
//            string FirstName = _Patient.PatientName.Split(' ')[0];
//            if (_Patient.tablate.Count == 0)
//                _Patient.tablate = lstPatient.Where(w => w.FirstName == FirstName).Select(s => s.Medicineslist).FirstOrDefault();

//            PriscriptionReport priscriptionReport = new PriscriptionReport();
//            priscriptionReport.GenrateReport(_Patient);

//            return RedirectToAction("Index");
//        }

//        public IActionResult BillingofPatient(int appointmentId)
//        {
//            if (appointmentId <= 0)
//                return BadRequest("Appointment identifier missing");

//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//            var appointment = _appointmentRepo.GetAppointmentById(appointmentId, hospitalId, subHospitalId);
//            if (appointment == null)
//                return NotFound("Appointment not found");

//            int opdId = _IOPD.GetOPDIdByAppointmentId(appointmentId, hospitalId, subHospitalId);
//            return View(appointment);
//        }

//        [HttpGet]
//        public JsonResult GetBillingTypes()
//        {
//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//            var list = _IBillingMaster.GetAllBillings(hospitalId, subHospitalId);
//            var dropdownList = list.Select(x => new { x.Id, x.Name }).ToList();
//            return Json(dropdownList);
//        }

//        // ======================== ✅ EDITASOPD FIXED =========================
//        public IActionResult EditAsOPD(int appointmentId)
//        {
//            _PreAuthSymTomsList.Clear();
//            _MedicinesList.Clear();

//            if (appointmentId <= 0)
//                return BadRequest("Appointment identifier missing");

//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//            var appointment = _appointmentRepo.GetAppointmentById(appointmentId, hospitalId, subHospitalId);
//            if (appointment == null)
//                return NotFound("Appointment not found");

//            var patient = _patientRepo.GetPatientById(appointment.PatientId, hospitalId, subHospitalId);

//            int opdId = _IOPD.GetOPDIdByAppointmentId(appointmentId, hospitalId, subHospitalId);

//            // UPDATE FLOW
//            if (opdId > 0)
//            {
//                var opdDetail = _IOPD.GetOPDById(opdId, hospitalId, subHospitalId);

//                if (opdDetail == null)
//                    return NotFound("OPD not found");

//                // ✅ FIX 1: Set PatientId manually
//                opdDetail.PatientId = appointment.PatientId;

//                return View(opdDetail);
//            }

//            // CREATE FLOW
//            var opd = new OPD
//            {
//                AppointmentId = appointmentId,
//                PatientId = appointment.PatientId,   // ✅ FIX 2
//                BP = "",
//                Pulse = "",
//                Investigation = "",
//                ReportDetail = "",
//                Symptoms = new List<int>(),
//                Medicines = new List<OPDMedicine>()
//            };

//            return View(opd);
//        }


//        // ======================== ✅ ADDOPD WITH DIAGNOSIS =========================
//        [HttpPost]
//        public IActionResult AddOPD(OPD model, string SymptomsJson, string MedicinesJson, string DiagnosesJson)
//        {
//            if (!ModelState.IsValid)
//            {
//                return View("EditAsOPD", model);
//            }

//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//            model.Symptoms = _PreAuthSymTomsList.Select(s => s.SymptomId).ToList();

//            // ── Medicines ────────────────────────────────────────────────────
//            if (!string.IsNullOrEmpty(MedicinesJson))
//            {
//                try
//                {
//                    var tempList = JsonSerializer.Deserialize<List<OPDMedicine>>(MedicinesJson);
//                    model.Medicines = tempList?
//                        .Where(m => m != null && m.MedicineId > 0)
//                        .Select(m => new OPDMedicine
//                        {
//                            MedicineId = m.MedicineId,
//                            Morning = m.Morning ?? "0",
//                            Afternoon = m.Afternoon ?? "0",
//                            Evening = m.Evening ?? "0",
//                            Days = m.Days > 0 ? m.Days : 1,
//                            Frequency = m.Frequency ?? "TDS",
//                            WhenToTake = m.WhenToTake ?? "After Food",
//                            Dose = m.Dose ?? "1",
//                            Unit = m.Unit ?? "Tablet(s)",
//                            Quantity = m.Quantity
//                        })
//                        .ToList() ?? new List<OPDMedicine>();
//                }
//                catch
//                {
//                    model.Medicines = new List<OPDMedicine>();
//                }
//            }
//            else
//            {
//                model.Medicines = new List<OPDMedicine>();
//            }

//            // ── Diagnoses (NEW) ──────────────────────────────────────────────
//            if (!string.IsNullOrEmpty(DiagnosesJson))
//            {
//                try
//                {
//                    var tempDiagnoses = JsonSerializer.Deserialize<List<OPDDiagnosis>>(DiagnosesJson);
//                    model.Diagnoses = tempDiagnoses?
//                        .Where(d => d != null && !string.IsNullOrEmpty(d.DiagnosisName))
//                        .Select(d => new OPDDiagnosis
//                        {
//                            DiagnosisName = d.DiagnosisName.Trim(),
//                            Type = d.Type ?? "Final",
//                            Notes = d.Notes ?? ""
//                        })
//                        .ToList() ?? new List<OPDDiagnosis>();
//                }
//                catch
//                {
//                    model.Diagnoses = new List<OPDDiagnosis>();
//                }
//            }
//            else
//            {
//                model.Diagnoses = new List<OPDDiagnosis>();
//            }

//            // ── Report File Upload ───────────────────────────────────────────
//            if (model.ReportFile != null && model.ReportFile.Length > 0)
//            {
//                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/OPDReports");

//                if (!Directory.Exists(folderPath))
//                    Directory.CreateDirectory(folderPath);

//                string fileName = Guid.NewGuid() + Path.GetExtension(model.ReportFile.FileName);
//                string filePath = Path.Combine(folderPath, fileName);

//                using (var stream = new FileStream(filePath, FileMode.Create))
//                {
//                    model.ReportFile.CopyTo(stream);
//                }

//                model.ReportFilePath = "/OPDReports/" + fileName;
//            }

//            if (model.Id > 0)
//            {
//                var existingOpd = _IOPD.GetOPDById(model.Id, hospitalId, subHospitalId);
//                if (model.ReportFile == null || model.ReportFile.Length == 0)
//                {
//                    model.ReportFilePath = existingOpd?.ReportFilePath;
//                }
//            }

//            // ── Save OPD ─────────────────────────────────────────────────────
//            int opdId;
//            if (model.Id > 0)
//                opdId = _IOPD.UpdateOPD(model);
//            else
//                opdId = _IOPD.AddOPD(model, hospitalId, subHospitalId);

//            if (opdId <= 0)
//            {
//                ModelState.AddModelError("", "OPD save failed");
//                return View("EditAsOPD", model);
//            }

//            // ── Update Appointment Status ────────────────────────────────────
//            var appointment = _appointmentRepo.GetAppointmentById(model.AppointmentId, hospitalId, subHospitalId);
//            if (appointment != null)
//            {
//                _appointmentRepo.UpdateStatus(model.AppointmentId, hospitalId, subHospitalId, "OPD Completed");
//            }

//            // ── Medicine Notification ────────────────────────────────────────
//            if (model.Medicines != null && model.Medicines.Count > 0)
//            {
//                try
//                {
//                    string summary;
//                    if (model.Medicines.Count == 1)
//                        summary = model.Medicines[0].MedicineName ?? "1 medicine";
//                    else if (model.Medicines.Count == 2)
//                        summary = string.Join(", ", model.Medicines.Take(2).Select(m => m.MedicineName ?? "Medicine"));
//                    else
//                        summary = string.Join(", ", model.Medicines.Take(2).Select(m => m.MedicineName ?? "Medicine"))
//                                  + $" +{model.Medicines.Count - 2} more";

//                    var patient = _patientRepo.GetPatientById(model.PatientId, hospitalId, subHospitalId);
//                    string patientName = patient != null
//                        ? $"{patient.FirstName} {patient.LastName}".Trim()
//                        : $"Patient #{model.PatientId}";

//                    string doctorName = "";
//                    if (appointment != null)
//                    {
//                        var doc = _doctorService?.GetDoctorById(appointment.DoctorId, hospitalId, subHospitalId);
//                        if (doc != null)
//                            doctorName = $"{doc.FirstName} {doc.LastName}".Trim();
//                    }

//                    _notifRepo.InsertNotification(new MedicineNotificationModel
//                    {
//                        PatientId = model.PatientId,
//                        PatientName = patientName,
//                        OPDId = opdId,
//                        AppointmentId = model.AppointmentId,
//                        DoctorName = doctorName,
//                        MedicineCount = model.Medicines.Count,
//                        MedicinesSummary = summary,
//                        Type = "OPD",
//                        HospitalId = hospitalId,
//                        SubHospitalId = subHospitalId
//                    });
//                }
//                catch { }
//            }

//            TempData["SuccessMessage"] = "OPD saved successfully";
//            return RedirectToAction("Index", "OPDAppointment");
//        }



//        //// ======================== ✅ ADDOPD FIXED =========================
//        //[HttpPost]
//        //public IActionResult AddOPD(OPD model, string SymptomsJson, string MedicinesJson)
//        //{
//        //    if (!ModelState.IsValid)
//        //    {
//        //        return View("EditAsOPD", model);
//        //    }

//        //    int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//        //    int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//        //    model.Symptoms = _PreAuthSymTomsList.Select(s => s.SymptomId).ToList();
//        //    if (!string.IsNullOrEmpty(MedicinesJson))
//        //    {
//        //        try
//        //        {
//        //            var tempList = JsonSerializer.Deserialize<List<OPDMedicine>>(MedicinesJson);
//        //            model.Medicines = tempList?
//        //                .Where(m => m != null && m.MedicineId > 0)
//        //                .Select(m => new OPDMedicine
//        //                {
//        //                    MedicineId = m.MedicineId,
//        //                    Morning = m.Morning ?? "0",
//        //                    Afternoon = m.Afternoon ?? "0",
//        //                    Evening = m.Evening ?? "0",
//        //                    Days = m.Days > 0 ? m.Days : 1,
//        //                    Frequency = m.Frequency ?? "TDS",
//        //                    WhenToTake = m.WhenToTake ?? "After Food",
//        //                    Dose = m.Dose ?? "1",
//        //                    Unit = m.Unit ?? "Tablet(s)",
//        //                    Quantity = m.Quantity
//        //                })
//        //                .ToList() ?? new List<OPDMedicine>();
//        //        }
//        //        catch
//        //        {
//        //            model.Medicines = new List<OPDMedicine>();
//        //        }
//        //    }
//        //    else
//        //    {
//        //        model.Medicines = new List<OPDMedicine>();
//        //    }

//        //    if (model.ReportFile != null && model.ReportFile.Length > 0)
//        //    {
//        //        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/OPDReports");

//        //        if (!Directory.Exists(folderPath))
//        //            Directory.CreateDirectory(folderPath);

//        //        string fileName = Guid.NewGuid() + Path.GetExtension(model.ReportFile.FileName);
//        //        string filePath = Path.Combine(folderPath, fileName);

//        //        using (var stream = new FileStream(filePath, FileMode.Create))
//        //        {
//        //            model.ReportFile.CopyTo(stream);
//        //        }

//        //        model.ReportFilePath = "/OPDReports/" + fileName;
//        //    }

//        //    if (model.Id > 0)
//        //    {
//        //        var existingOpd = _IOPD.GetOPDById(model.Id, hospitalId, subHospitalId);

//        //        if (model.ReportFile == null || model.ReportFile.Length == 0)
//        //        {
//        //            model.ReportFilePath = existingOpd?.ReportFilePath;
//        //        }
//        //    }

//        //    int opdId;
//        //    if (model.Id > 0)
//        //    {
//        //        opdId = _IOPD.UpdateOPD(model);
//        //    }
//        //    else
//        //    {
//        //        // ✅ FIX 3: Get real OPDId from DB
//        //        opdId = _IOPD.AddOPD(model, hospitalId, subHospitalId);
//        //    }

//        //    if (opdId <= 0)
//        //    {
//        //        ModelState.AddModelError("", "OPD save failed");
//        //        return View("EditAsOPD", model);
//        //    }

//        //    var appointment = _appointmentRepo.GetAppointmentById(model.AppointmentId, hospitalId, subHospitalId);
//        //    if (appointment != null)
//        //    {
//        //        _appointmentRepo.UpdateStatus(model.AppointmentId, hospitalId, subHospitalId, "OPD Completed");
//        //    }

//        //    if (model.Medicines != null && model.Medicines.Count > 0)
//        //    {
//        //        try
//        //        {
//        //            string summary = "";
//        //            if (model.Medicines.Count == 1)
//        //                summary = model.Medicines[0].MedicineName ?? "1 medicine";
//        //            else if (model.Medicines.Count == 2)
//        //                summary = string.Join(", ", model.Medicines.Take(2).Select(m => m.MedicineName ?? "Medicine"));
//        //            else
//        //                summary = string.Join(", ", model.Medicines.Take(2).Select(m => m.MedicineName ?? "Medicine"))
//        //                          + $" +{model.Medicines.Count - 2} more";

//        //            var patient = _patientRepo.GetPatientById(model.PatientId, hospitalId, subHospitalId);
//        //            string patientName = patient != null
//        //                ? $"{patient.FirstName} {patient.LastName}".Trim()
//        //                : $"Patient #{model.PatientId}";

//        //            string doctorName = "";
//        //            if (appointment != null)
//        //            {
//        //                var doc = _doctorService?.GetDoctorById(appointment.DoctorId, hospitalId, subHospitalId);
//        //                if (doc != null)
//        //                    doctorName = $"{doc.FirstName} {doc.LastName}".Trim();
//        //            }

//        //            _notifRepo.InsertNotification(new MedicineNotificationModel
//        //            {
//        //                PatientId = model.PatientId,
//        //                PatientName = patientName,
//        //                OPDId = opdId,
//        //                AppointmentId = model.AppointmentId,
//        //                DoctorName = doctorName,
//        //                MedicineCount = model.Medicines.Count,
//        //                MedicinesSummary = summary,
//        //                Type = "OPD",
//        //                HospitalId = hospitalId,
//        //                SubHospitalId = subHospitalId
//        //            });
//        //        }
//        //        catch { }
//        //    }

//        //    TempData["SuccessMessage"] = "OPD saved successfully";
//        //    return RedirectToAction("Index", "OPDAppointment");
//        //}

//        public IActionResult EditAsIPD(string Id)
//        {
//            var sdsdsd = Id;
//            Patient patient = lstPatient.Where(w => w.FirstName.ToLower() == Id.ToLower()).Select(s => s).FirstOrDefault();
//            var dsdsw = patient;
//            return RedirectToAction("Index");
//        }
//        public IActionResult Index(string search = "", int page = 1)
//        {
//            int pageSize = 6;
//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//            var data = _appointmentRepo.GetAllAppointments(hospitalId, subHospitalId).AsQueryable();

//            // Server-side search
//            if (!string.IsNullOrWhiteSpace(search))
//            {
//                search = search.ToLower();
//                data = data.Where(x =>
//                    x.PatientId.ToString().Contains(search) ||
//                    x.DoctorId.ToString().Contains(search) ||
//                    x.AppointmentDate.ToString().Contains(search)
//                );
//            }

//            // Pagination
//            int totalRecords = data.Count();
//            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
//            var pagedData = data.Skip((page - 1) * pageSize).Take(pageSize).ToList();

//            ViewBag.CurrentPage = page;
//            ViewBag.TotalPages = totalPages;
//            ViewBag.Search = search;

//            return View(pagedData);
//        }

//        public IActionResult Create()
//        {
//            Patient patient = new Patient();
//            return View(patient);
//        }

//        public IActionResult SavePatientDetails([Bind] Patient _Patient)
//        {

//            lstPatient.Add(_Patient);

//            return RedirectToAction("Index", "Patient");
//        }

//        public IActionResult UpdateBilling([Bind] Patient _Patient)
//        {
//            _Patient.BillingDetails = _BillingDetails;
//            _Patient.Status = "Billing Completed";
//            var _newpatianet = lstPatient.Where(w => w.FirstName.ToLower() == _Patient.FirstName.ToLower()).Select(s => s).FirstOrDefault();
//            lstPatient.Remove(_newpatianet);
//            lstPatient.Add(_Patient);

//            return RedirectToAction("Index", "Patient");
//        }
//        public IActionResult UpdatePatientDetails([Bind] Patient _Patient)
//        {
//            if (string.IsNullOrEmpty(_Patient.ReportDetail))
//            {
//                _Patient.Status = "Visiting Progress";
//            }
//            else
//                _Patient.Status = "Completed";

//            //_Patient.symptoms = _PreAuthSymTomsList;
//            //_Patient.symptomsmain = _PreAuthSymTomsList;
//            _Patient.Medicineslist = _MedicinesList;
//            var _newpatianet = lstPatient.Where(w => w.FirstName.ToLower() == _Patient.FirstName.ToLower()).Select(s => s).FirstOrDefault();
//            lstPatient.Remove(_newpatianet);
//            lstPatient.Add(_Patient);

//            return RedirectToAction("Index", "Patient");
//        }

//        public IActionResult SaveSymtons(int Command)
//        {
//            if (Command > 0)
//            {
//                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
//                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//                var selectedSymptom = _symptomRepo.GetSymptomById(Command, hospitalId, subHospitalId);

//                if (selectedSymptom != null)
//                {
//                    // prevent duplicates
//                    if (!_PreAuthSymTomsList.Any(s => s.SymptomId == selectedSymptom.SymptomId))
//                    {
//                        _PreAuthSymTomsList.Add(new Symptom
//                        {
//                            SymptomId = selectedSymptom.SymptomId,
//                            SymptomName = selectedSymptom.SymptomName,
//                            SubName = selectedSymptom.SubName,
//                            Description = selectedSymptom.Description
//                        });
//                    }
//                }
//            }


//            return PartialView("_PreAuth", _PreAuthSymTomsList);
//        }

//        [HttpGet]
//        public IActionResult DeleteSymtons(int Command) // Command = SymptomId
//        {
//            _PreAuthSymTomsList.RemoveAll(s => s.SymptomId == Command);
//            return PartialView("_PreAuth", _PreAuthSymTomsList);
//        }


//        public IActionResult SaveMedicines(int Command, int _Morning, int _Afternoon, int _Evening, int _Days)
//        {
//            if (Command > 0)
//            {
//                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
//                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//                var selectedMedicine = _medicineRepo.GetMedicineById(Command, hospitalId, subHospitalId);

//                if (selectedMedicine != null)
//                {
//                    tablet objtablet = new tablet();
//                    objtablet.Id = _MedicinesList.Count > 0 ? _MedicinesList.Max(m => m.Id) + 1 : 1; // unique ID
//                    objtablet.TablateName = selectedMedicine.MedicineName; // map correctly
//                    objtablet.Morning = _Morning == 1 ? "Y" : "N";
//                    objtablet.Afternoon = _Afternoon == 1 ? "Y" : "N";
//                    objtablet.Evening = _Evening == 1 ? "Y" : "N";
//                    objtablet.Days = _Days > 0 ? _Days : 1;

//                    _MedicinesList.Add(objtablet);
//                }
//            }

//            return PartialView("_Medicine", _MedicinesList);
//        }




//        [HttpGet]
//        public IActionResult DeleteMedicines(int Command) // Command is the medicine ID now
//        {
//            _MedicinesList.RemoveAll(m => m.Id == Command);
//            return PartialView("_Medicine", _MedicinesList);
//        }

//        [HttpGet]
//        public IActionResult SaveBilling(int Command) // Command = BillingId
//        {
//            if (Command > 0)
//            {
//                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
//                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//                var selectedBilling = _IBillingMaster.GetBillingById(Command, hospitalId, subHospitalId);

//                if (selectedBilling != null)
//                {
//                    // prevent duplicates
//                    if (!_BillingList.Any(b => b.BillingId == selectedBilling.Id))
//                    {
//                        _BillingList.Add(new BillingDetailModel
//                        {
//                            BillingId = selectedBilling.Id,
//                            BillName = selectedBilling.Name,
//                            Amount = selectedBilling.Amount
//                        });
//                    }
//                }
//            }

//            return PartialView("_TotalBill", _BillingList);
//        }
//        [HttpGet]
//        public IActionResult DeleteBilling(int Command) // BillingId
//        {
//            _BillingList.RemoveAll(b => b.BillingId == Command);
//            return PartialView("_TotalBill", _BillingList);
//        }





//        [HttpPost]
//        public IActionResult CreatePatientDetails(Patient model)
//        {
//            if (!ModelState.IsValid)
//                return View("Create", model);

//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//            // Handle profile photo upload
//            var photoFile = Request.Form.Files["ProfilePhotoFile"];
//            if (photoFile != null && photoFile.Length > 0)
//            {
//                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/PatientPhotos");
//                if (!Directory.Exists(folderPath))
//                    Directory.CreateDirectory(folderPath);

//                string fileName = Guid.NewGuid() + Path.GetExtension(photoFile.FileName);
//                string filePath = Path.Combine(folderPath, fileName);

//                using (var stream = new FileStream(filePath, FileMode.Create))
//                    photoFile.CopyTo(stream);

//                model.ProfilePhoto = "/PatientPhotos/" + fileName;
//            }

//            if (model.Id == 0)
//            {
//                // ── NEW PATIENT: handled by CreatePatientDetailsAjax now ──
//                // This branch only runs if someone submits the form without JS.
//                // Safe fallback — save and go to PatientList.
//                _patientRepo.AddPatient(model, hospitalId, subHospitalId);
//                TempData["SuccessMessage"] = "Patient created successfully!";
//                return RedirectToAction("PatientList");
//            }
//            else
//            {
//                // ── EDIT PATIENT: save then redirect back to origin ──
//                _patientRepo.UpdatePatient(model, hospitalId, subHospitalId);
//                TempData["SuccessMessage"] = "Patient updated successfully!";

//                // Read the return URL — try form field first (survives validation errors),
//                // then fall back to TempData (set when Edit was first opened)
//                string returnUrl = Request.Form["ReturnUrl"].ToString();
//                if (string.IsNullOrWhiteSpace(returnUrl))
//                    returnUrl = TempData["EditReturnUrl"] as string;

//                // Safety check: only redirect to local URLs to prevent open-redirect attacks
//                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
//                    return Redirect(returnUrl);

//                return RedirectToAction("PatientList");
//            }
//        }


//        //[HttpPost]
//        //public IActionResult CreatePatientDetails(Patient model)
//        //{
//        //    if (!ModelState.IsValid)
//        //        return View("Create", model);

//        //    int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//        //    int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//        //    // Handle profile photo upload
//        //    var photoFile = Request.Form.Files["ProfilePhotoFile"];
//        //    if (photoFile != null && photoFile.Length > 0)
//        //    {
//        //        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/PatientPhotos");
//        //        if (!Directory.Exists(folderPath))
//        //            Directory.CreateDirectory(folderPath);

//        //        string fileName = Guid.NewGuid() + Path.GetExtension(photoFile.FileName);
//        //        string filePath = Path.Combine(folderPath, fileName);

//        //        using (var stream = new FileStream(filePath, FileMode.Create))
//        //            photoFile.CopyTo(stream);

//        //        model.ProfilePhoto = "/PatientPhotos/" + fileName;
//        //    }

//        //    if (model.Id == 0)
//        //    {
//        //        _patientRepo.AddPatient(model, hospitalId, subHospitalId);
//        //        TempData["SuccessMessage"] = "Patient created successfully!";
//        //    }
//        //    else
//        //    {
//        //        _patientRepo.UpdatePatient(model, hospitalId, subHospitalId);
//        //        TempData["SuccessMessage"] = "Patient updated successfully!";
//        //    }

//        //    return RedirectToAction("PatientList");
//        //}


//        [HttpPost]
//        public IActionResult Delete(int id)
//        {
//            try
//            {
//                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
//                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//                _patientRepo.DeletePatient(id, hospitalId, subHospitalId);
//                TempData["SuccessMessage"] = "Patient deleted successfully!";
//            }
//            catch (Exception ex)
//            {
//                TempData["ErrorMessage"] = ex.Message;
//            }

//            return RedirectToAction("PatientList");
//        }

//        [HttpGet]
//        public IActionResult Edit(int id)
//        {
//            _PreAuthSymTomsList.Clear();
//            _MedicinesList.Clear();

//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//            Patient patient = _patientRepo.GetPatientById(id, hospitalId, subHospitalId);

//            if (patient == null)
//                return NotFound();

//            patient.FullName = $"{patient.FirstName} {patient.LastName}".Trim();
//            patient.isUpdate = true;

//            // Capture where the user came from so we can redirect back after save.
//            // Uses the Referer header; falls back to PatientList.
//            string returnUrl = Request.Headers["Referer"].ToString();
//            if (string.IsNullOrWhiteSpace(returnUrl))
//                returnUrl = Url.Action("PatientList", "Patient");

//            // Store in TempData so it survives the POST round-trip
//            TempData["EditReturnUrl"] = returnUrl;

//            return View("Create", patient);
//        }

//        //[HttpGet]
//        //public IActionResult Edit(int id)
//        //{
//        //    _PreAuthSymTomsList.Clear();
//        //    _MedicinesList.Clear();

//        //    int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//        //    int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//        //    Patient patient = _patientRepo.GetPatientById(id, hospitalId, subHospitalId);

//        //    if (patient == null)
//        //        return NotFound();

//        //    // Build FullName so the single input in the form is pre-filled
//        //    patient.FullName = $"{patient.FirstName} {patient.LastName}".Trim();

//        //    patient.isUpdate = true;
//        //    return View("Create", patient);
//        //}


//        public IActionResult PatientList(string search = "", int page = 1)
//        {
//            int pageSize = 5;

//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//            // 1️⃣ Get all patients for current hospital
//            List<Patient> patients = _patientRepo
//                                        .GetAllPatients(hospitalId, subHospitalId);

//            // 2️⃣ SEARCH
//            if (!string.IsNullOrEmpty(search))
//            {
//                search = search.ToLower();

//                patients = patients.Where(p =>
//                    (!string.IsNullOrEmpty(p.FirstName) && p.FirstName.ToLower().Contains(search)) ||
//                    (!string.IsNullOrEmpty(p.LastName) && p.LastName.ToLower().Contains(search)) ||
//                    (!string.IsNullOrEmpty(p.Gender) && p.Gender.ToLower().Contains(search)) ||
//                    (!string.IsNullOrEmpty(p.PhoneNumber) && p.PhoneNumber.ToLower().Contains(search)) ||
//                    (!string.IsNullOrEmpty(p.Address) && p.Address.ToLower().Contains(search))
//                ).ToList();
//            }

//            // 3️⃣ PAGINATION
//            int totalRecords = patients.Count;
//            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

//            List<Patient> pagedPatients = patients
//                .Skip((page - 1) * pageSize)
//                .Take(pageSize)
//                .ToList();

//            // 4️⃣ ViewBag
//            ViewBag.CurrentPage = page;
//            ViewBag.TotalPages = totalPages;
//            ViewBag.Search = search;

//            return View(pagedPatients);
//        }

//        [HttpGet]
//        public JsonResult GetAllSymptoms()
//        {
//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//            var symptoms = _symptomRepo.GetAllSymptoms(hospitalId, subHospitalId) ?? new List<Symptom>();
//            var result = symptoms.Select(s => new { id = s.SymptomId, name = s.SymptomName }).ToList();

//            return Json(result);
//        }

//        [HttpGet]
//        public JsonResult GetAllMedicines()
//        {
//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0; // default 0
//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//            var medicines = _medicineRepo.GetAllMedicine(hospitalId, subHospitalId) ?? new List<Medicine>();
//            var result = medicines.Select(m => new { id = m.MedicineId, name = m.MedicineName }).ToList();

//            return Json(result);
//        }

//        [HttpGet]
//        public IActionResult LoadExistingSymptoms(int opdId)
//        {
//            var symptomsVM = _IOPD.GetOPDSymptomsByOPDId(opdId);

//            var symptoms = symptomsVM.Select(s => new Symptom
//            {
//                SymptomId = s.Symptom_Id,
//                SymptomName = s.SymptomName
//            }).ToList();
//            _PreAuthSymTomsList = symptoms;
//            return PartialView("_PreAuth", symptoms);
//        }

//        [HttpGet]
//        public IActionResult LoadExistingMedicines(int opdId)
//        {
//            var medList = _appointmentRepo.GetMedicinesByOPDId(opdId);

//            var tablets = medList.Select(m => new tablet
//            {
//                Id = m.MedicineId,
//                TablateName = m.MedicineName,
//                Morning = m.Morning.ToString(),    // Convert int to string
//                Afternoon = m.Afternoon.ToString(),// Convert int to string
//                Evening = m.Evening.ToString(),    // Convert int to string
//                Days = m.Days
//            }).ToList();
//            _MedicinesList = tablets;

//            return PartialView("_Medicine", tablets);
//        }





//        [HttpPost]
//        public IActionResult CreatePatientDetailsAjax(Patient model)
//        {
//            try
//            {
//                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId")
//                                 ?? HttpContext.Session.GetInt32("PatientHospitalId")
//                                 ?? 0;
//                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId")
//                                  ?? HttpContext.Session.GetInt32("PatientSubHospitalId");

//                // Handle profile photo upload (same logic as CreatePatientDetails)
//                var photoFile = Request.Form.Files["ProfilePhotoFile"];
//                if (photoFile != null && photoFile.Length > 0)
//                {
//                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/PatientPhotos");
//                    if (!Directory.Exists(folderPath))
//                        Directory.CreateDirectory(folderPath);

//                    string fileName = Guid.NewGuid() + Path.GetExtension(photoFile.FileName);
//                    string filePath = Path.Combine(folderPath, fileName);

//                    using (var stream = new FileStream(filePath, FileMode.Create))
//                        photoFile.CopyTo(stream);

//                    model.ProfilePhoto = "/PatientPhotos/" + fileName;
//                }

//                // Save patient to DB
//                int patientId = _patientRepo.AddPatient(model, hospitalId, subHospitalId);
//                if (patientId <= 0)
//                    return Json(new { success = false, message = "Failed to save patient." });

//                // Create a walk-in appointment for this patient
//                int appointmentId = CreateWalkInAppointment(patientId, hospitalId, subHospitalId);
//                if (appointmentId <= 0)
//                    return Json(new { success = false, message = "Patient saved but could not create appointment." });

//                return Json(new { success = true, appointmentId = appointmentId });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        // ── Private helper: inserts a walk-in row into opdappointment ──
//        // (same logic as WalkInConsultationController.CreateWalkInAppointmentInDb)
//        private int CreateWalkInAppointment(int patientId, int hospitalId, int? subHospitalId)
//        {
//            try
//            {
//                var config = HttpContext.RequestServices
//                                .GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration))
//                                as Microsoft.Extensions.Configuration.IConfiguration;

//                string connStr = config.GetConnectionString("MySqlConnection");

//                using var con = new MySql.Data.MySqlClient.MySqlConnection(connStr);
//                using var cmd = new MySql.Data.MySqlClient.MySqlCommand(@"
//            INSERT INTO opdappointment
//                (PatientId, HospitalId, SubHospitalId, DoctorId,
//                 AppointmentDate, AppointmentTime, Status, IsActive, IsWalkIn)
//            VALUES
//                (@PatientId, @HospitalId, @SubHospitalId, 0,
//                 @AppointmentDate, @AppointmentTime, 'Pending', 1, 1);
//            SELECT LAST_INSERT_ID();", con);

//                cmd.Parameters.AddWithValue("@PatientId", patientId);
//                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);
//                cmd.Parameters.AddWithValue("@SubHospitalId",
//                    subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
//                cmd.Parameters.AddWithValue("@AppointmentDate", DateTime.Today);
//                cmd.Parameters.AddWithValue("@AppointmentTime", DateTime.Now.ToString("HH:mm:ss"));

//                con.Open();
//                var result = cmd.ExecuteScalar();
//                return result != null ? Convert.ToInt32(result) : 0;
//            }
//            catch
//            {
//                return 0;
//            }
//        }

//        [HttpGet]
//        public JsonResult GetPatientInfo(int patientId)
//        {
//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

//            var p = _patientRepo.GetPatientById(patientId, hospitalId, subHospitalId);
//            if (p == null) return Json(null);

//            return Json(new
//            {
//                id = p.Id,
//                firstName = p.FirstName,
//                lastName = p.LastName,
//                gender = p.Gender,
//                age = p.Age,
//                phoneNumber = p.PhoneNumber,
//                bloodGroup = p.BloodGroup,
//                address = p.Address
//            });
//        }

//        // ── 2. Existing symptoms as JSON (for edit-mode chip restore) ─────────────
//        [HttpGet]
//        public JsonResult LoadExistingSymptomsJson(int opdId)
//        {
//            var list = _IOPD.GetOPDSymptomsByOPDId(opdId);
//            var result = list.Select(s => new
//            {
//                symptomId = s.Symptom_Id,
//                symptomName = s.SymptomName
//            }).ToList();
//            return Json(result);
//        }


//        // ═══════════════════════════════════════════════════════════════════════════
//        //  NEW ENDPOINTS — Add these methods to OPDAppointmentController.cs
//        //  (paste inside the OPDAppointmentController class, before the closing brace)
//        // ═══════════════════════════════════════════════════════════════════════════

//        // ── 3. Medicines by OPD ID as JSON (for edit-mode Rx table restore) ───────
//        [HttpGet]
//        public JsonResult GetMedicinesByOPDIdJson(int opdId)
//        {
//            var list = _appointmentRepo.GetMedicinesByOPDId(opdId);
//            var result = list.Select(m => new
//            {
//                medicineId = m.MedicineId,
//                medicineName = m.MedicineName,
//                morning = m.Morning,
//                afternoon = m.Afternoon,
//                evening = m.Evening,
//                days = m.Days,
//                when = "After Food"   // default; extend SP/model if you store this
//            }).ToList();
//            return Json(result);
//        }

//        // ── 4. Patient full history as JSON (for left panel history timeline) ─────
//        [HttpGet]
//        public JsonResult GetPatientHistory(int patientId)
//        {
//            var history = _appointmentRepo.GetPatientFullHistory(patientId);
//            var result = history.Select(v => new
//            {
//                id = v.Id,
//                appointmentId = v.AppointmentId,
//                appointmentDate = v.AppointmentDate,
//                bp = v.BP,
//                pulse = v.Pulse,
//                investigation = v.Investigation,
//                reportDetail = v.ReportDetail,
//                reportFilePath = v.ReportFilePath,
//                nextAppointmentDate = v.NextAppointmentDate,
//                symptom = v.Symptom,
//                medicines = v.Medicines.Select(m => new
//                {
//                    medicineName = m.MedicineName,
//                    morning = m.Morning,
//                    afternoon = m.Afternoon,
//                    evening = m.Evening,
//                    days = m.Days
//                }).ToList()
//            }).ToList();
//            return Json(result);
//        }


//        [HttpGet]
//        public JsonResult GetAllDiagnoses()
//        {
//            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//            var list = _IOPD.GetAllDiagnoses(hospitalId);
//            return Json(list.Select(d => new { id = d.Id, name = d.Name, icdCode = d.IcdCode, category = d.Category }));
//        }


//    }
//}
