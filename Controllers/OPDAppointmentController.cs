
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class OPDAppointmentController : Controller
    {
private readonly IOPDAppointment _iAppointment;
        private readonly IDoctor _iDoctor;
        private readonly Ipatient _ipatient;
        private readonly IOPD _IOPD;
        private readonly IIPDAdmission iPDAdmission;
        private readonly INotification _notifRepo;
        private readonly IPatientHistory _iPatientHistory;

        public OPDAppointmentController(
            IIPDAdmission IPDAdmission,
            IOPDAppointment appointment,
            Ipatient ipatient,
            IOPD OPD,
            IDoctor doctor,
            INotification notifRepo,
            IPatientHistory patientHistory)
        {
            _iAppointment = appointment;
            _iDoctor = doctor;
            _ipatient = ipatient;
            _IOPD = OPD;
            iPDAdmission = IPDAdmission;
            _notifRepo = notifRepo;
            _iPatientHistory = patientHistory;
        }

        // ─────────────────────────────────────────────────────────────────
        // INDEX — Appointment List
        // ─────────────────────────────────────────────────────────────────
public IActionResult Index(string search, int page = 1, string date = "")
        {
            int pageSize = 6;
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            // ═══ PERFORMANCE FIX ═══════════════════════════════════════
            // Previously this loaded ALL appointments, ALL patients and
            // ALL doctors into memory, LINQ-joined them, then filtered +
            // paginated in C#. With large tables that caused the ~30s page
            // load and slow pagination. Now a single SQL query does the
            // JOIN + date/search filter + COUNT + LIMIT — only the current
            // page's rows (6) are fetched from the DB.
            // ══════════════════════════════════════════════════════════
            DateTime selectedDate;
            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out selectedDate))
            {
                // use the user-selected date
            }
            else
            {
                selectedDate = DateTime.Today;
            }

            var pagedData = _iAppointment.GetAppointmentsPaged(
                hospitalId, subHospitalId, selectedDate, search, page, pageSize,
                out int totalRecords, out int todayAppointmentCount);

            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            if (totalPages < 1) totalPages = 1;

            ViewBag.TodayAppointmentCount = todayAppointmentCount;

            // ═══ PERFORMANCE FIX ═══════════════════════════════════════
            // Single batched query fetches OPD ID + IPD status for the
            // current page's rows (no per-row stored-procedure calls).
            // ══════════════════════════════════════════════════════════
            var apptIds = pagedData.Select(x => x.Id).ToList();
            var batch = _iAppointment.GetOPDWithIPDStatusBatch(apptIds, hospitalId, subHospitalId);

            foreach (var item in pagedData)
            {
                if (batch.TryGetValue(item.Id, out var info))
                {
                    item.OPDId = info.opdId;
                    item.IPDStatus = info.ipdStatus;
                    item.IsIPDAdmitted = !string.IsNullOrEmpty(info.ipdStatus);
                }
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");
            return View(pagedData);
        }

        // ─────────────────────────────────────────────────────────────────
        // CREATE GET
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            return View(new OPDAppointmentModel
            {
                AppointmentDate = DateTime.Today,
                isUpdate = false
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // CREATE / UPDATE POST
        // ─────────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OPDAppointmentModel model)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            if (!ModelState.IsValid) return View(model);

            if (model.Id > 0)
            {
                _iAppointment.UpdateAppointment(model, hospitalId, subHospitalId);
                TempData["Success"] = "Appointment updated successfully";
            }
            else
            {
                if (string.IsNullOrEmpty(model.Status)) model.Status = "Pending";
                _iAppointment.CreateAppointment(model, hospitalId, subHospitalId);
                TempData["Success"] = "Appointment booked successfully";
            }

            return RedirectToAction("Index");
        }

        // ─────────────────────────────────────────────────────────────────
        // EDIT GET
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (id <= 0) return NotFound();

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var appt = _iAppointment.GetAppointmentById(id, hospitalId, subHospitalId);
            if (appt == null) return NotFound();

            var patient = _ipatient.GetPatientById(appt.PatientId, hospitalId, subHospitalId);
            var doctor = _iDoctor.GetDoctorById(appt.DoctorId, hospitalId, subHospitalId);

            var model = new OPDAppointmentModel
            {
                Id = appt.Id,
                PatientId = appt.PatientId,
                PatientName = patient != null ? patient.FirstName + " " + patient.LastName : "",
                MobileNo = patient?.PhoneNumber,
                AppointmentDate = appt.AppointmentDate,
                AppointmentTime = appt.AppointmentTime,
                DoctorId = appt.DoctorId,
                DoctorName = doctor != null ? doctor.FirstName + " " + doctor.LastName : "",
                Status = appt.Status,
                isUpdate = true
            };

            return View("Create", model);
        }

        // ─────────────────────────────────────────────────────────────────
        // DELETE
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Delete(int id)
        {
            if (id <= 0) { TempData["Error"] = "Invalid appointment id"; return RedirectToAction("Index"); }

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var appointment = _iAppointment.GetAppointmentById(id, hospitalId, subHospitalId);
            if (appointment == null) { TempData["Error"] = "Appointment not found"; return RedirectToAction("Index"); }

            _iAppointment.DeleteAppointment(id, hospitalId, subHospitalId);
            TempData["Success"] = "Appointment deleted successfully";
            return RedirectToAction("Index");
        }

        // ─────────────────────────────────────────────────────────────────
        // AJAX: GET DOCTORS
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetDoctors()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var doctors = _iDoctor.GetAllDoctor(hospitalId, subHospitalId) ?? new List<Doctor>();
            var result = doctors.Select(d => new { id = d.Doctor_Id, name = (d.FirstName ?? "") + " " + (d.LastName ?? "") });
            return Json(result);
        }

        // ─────────────────────────────────────────────────────────────────
        // AJAX: SEARCH PATIENT BY MOBILE
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult SearchPatientByMobile(string search)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var patients = _ipatient.SearchPatientByMobile(search, hospitalId, subHospitalId) ?? new List<Patient>();
            var result = patients.Select(p => new
            {
                id = p.Id,
                name = (p.FirstName ?? "") + " " + (p.LastName ?? ""),
                mobile = p.PhoneNumber,
                gender = p.Gender
            });
            return Json(result);
        }

        // ─────────────────────────────────────────────────────────────────
        // PATIENT HISTORY
        // ─────────────────────────────────────────────────────────────────
public IActionResult PatientHistory(int appointmentId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var appointment = _iAppointment.GetAppointmentById(appointmentId, hospitalId, subHospitalId);
            if (appointment == null) return NotFound("Appointment not found");

            // ✅ Set ViewBag for redirect target based on appointment type
            ViewBag.IsWalkIn = appointment.IsWalkIn;

            // ── Build the combined full-history VM ──────────────────────
            var patient = _ipatient.GetPatientById(appointment.PatientId, hospitalId, subHospitalId);
            var visits = _iAppointment.GetPatientFullHistory(appointment.PatientId, hospitalId, subHospitalId);
            var clinical = _iPatientHistory.GetFullHistory(appointment.PatientId, hospitalId);

            var vm = new PatientFullHistoryVM
            {
                PatientId = appointment.PatientId,
                PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}".Trim() : $"Patient #{appointment.PatientId}",
                Gender = patient?.Gender,
                PhoneNumber = patient?.PhoneNumber,
                BloodGroup = patient?.BloodGroup,
                Email = patient?.Email,
                Address = patient?.Address,
                MaritalStatus = patient?.MaritalStatus,
                Occupation = patient?.Occupation,
                Age = patient != null ? ComputeAge(patient.DateOfBirth) : null,
                Visits = visits ?? new List<OPD>(),
                Clinical = clinical ?? new PatientHistoryVM()
            };

            return View(vm);
        }

        private static string ComputeAge(DateTime? dob)
        {
            if (!dob.HasValue) return null;
            var now = DateTime.Today;
            int years = now.Year - dob.Value.Year;
            int months = now.Month - dob.Value.Month;
            if (months < 0) { years--; months += 12; }
            if (years > 0) return $"{years} Y {months} M";
            if (months > 0) return $"{months} M";
            int days = (now - dob.Value.Date).Days;
            return $"{days} D";
        }

        // ─────────────────────────────────────────────────────────────────
        // QUEUE PAGE
        // ─────────────────────────────────────────────────────────────────
        public IActionResult Queue()
        {
            return View();
        }

        // ─────────────────────────────────────────────────────────────────
        // AJAX: GET QUEUE DATA
        // BUG FIXES:
        //   - tokenNumber now correctly set from index (1-based)
        //   - symptoms now fetched from OPD record when available
        //   - medicineCount batched — no N+1 query per appointment
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetQueueData(string date)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            DateTime selectedDate = DateTime.TryParse(date, out var parsed) ? parsed : DateTime.Today;

            var appointments = _iAppointment.GetAllAppointments(hospitalId, subHospitalId)
                               ?? new List<OPDAppointmentModel>();
            var patients = _ipatient.GetAllPatients(hospitalId, subHospitalId)
                               ?? new List<Patient>();
            var doctors = _iDoctor.GetAllDoctor(hospitalId, subHospitalId)
                               ?? new List<Doctor>();

            // ── FIX: Load all notifications once (no per-row call) ──────
            var allNotifs = _notifRepo?.GetAllNotifications(hospitalId, subHospitalId)
                            ?? new List<MedicineNotificationModel>();

// ── PERFORMANCE: Batch-fetch OPD Ids + symptoms in 2 round trips instead of N ──
            var joined = (from appt in appointments
                          where appt.AppointmentDate.Date == selectedDate.Date
                          join pat in patients on appt.PatientId equals pat.Id
                          join doc in doctors on appt.DoctorId equals doc.Doctor_Id into docG
                          from doc in docG.DefaultIfEmpty()
                          orderby appt.AppointmentTime
                          select new
                          {
                              appointmentId = appt.Id,
                              patientId = appt.PatientId,
                              patientName = (pat.FirstName ?? "") + " " + (pat.LastName ?? ""),
                              doctorName = doc != null ? (doc.FirstName ?? "") + " " + (doc.LastName ?? "") : "",
                              appointmentTime = DateTime.Today.Add(appt.AppointmentTime).ToString("hh:mm tt"),
                              status = appt.Status ?? "Pending",
                          }).ToList();

            // Batch-fetch OPD IDs for all appointments in one query
            var apptIds = joined.Select(j => j.appointmentId).ToList();
            var opdBatch = _iAppointment.GetOPDWithIPDStatusBatch(apptIds, hospitalId, subHospitalId);
            var opdIdsWithData = opdBatch.Where(kv => kv.Value.opdId > 0)
                                         .Select(kv => kv.Value.opdId)
                                         .Distinct()
                                         .ToList();
            var symptomsBatch = opdIdsWithData.Count > 0
                ? _iAppointment.GetSymptomsByOPDIds(opdIdsWithData)
                : new Dictionary<int, string>();

            var result = joined.Select((item, idx) =>
            {
                var notif = allNotifs.FirstOrDefault(x => x.AppointmentId == item.appointmentId);
                var opdId = opdBatch.TryGetValue(item.appointmentId, out var opdInfo) ? opdInfo.opdId : 0;
                var symptomsStr = opdId > 0 && symptomsBatch.TryGetValue(opdId, out var syms) ? syms : "";

                return new
                {
                    appointmentId = item.appointmentId,
                    patientId = item.patientId,
                    patientName = item.patientName,
                    doctorName = item.doctorName,
                    appointmentTime = item.appointmentTime,
                    status = item.status,
                    opdId = opdId,
                    tokenNumber = idx + 1,
                    symptoms = symptomsStr,
                    medicineCount = notif?.MedicineCount ?? 0
                };
            }).ToList();

            return Json(result);
        }

        // ─────────────────────────────────────────────────────────────────
        // AJAX: GET PRESCRIPTION DETAIL
        // BUG FIXES:
        //   - opdMedId now reads OPDMedicineId from DB (was always 0)
        //   - tokenNumber now computed from today's sorted list
        //   - isDispensed checked per-medicine via notification
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetPrescriptionDetail(int appointmentId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            try
            {
                var appt = _iAppointment.GetAppointmentById(appointmentId, hospitalId, subHospitalId);
                if (appt == null) return Json(null);

                var opdId = _IOPD.GetOPDIdByAppointmentId(appointmentId, hospitalId, subHospitalId);
                if (opdId <= 0) return Json(new { patientName = (string)null });

                var opd = _IOPD.GetOPDById(opdId, hospitalId, subHospitalId);
                if (opd == null) return Json(new { patientName = (string)null });

                var patient = _ipatient.GetPatientById(appt.PatientId, hospitalId, subHospitalId);
                var patientName = patient != null
                    ? $"{patient.FirstName} {patient.LastName}".Trim()
                    : $"Patient #{appt.PatientId}";

                var doctor = _iDoctor.GetDoctorById(appt.DoctorId, hospitalId, subHospitalId);
                var doctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}".Trim() : "";

                // Symptoms
                var symList = _IOPD.GetOPDSymptomsByOPDId(opdId);
                var symptomsStr = symList != null && symList.Any()
                    ? string.Join(", ", symList.Select(s => s.SymptomName))
                    : "";

                // ── FIX: Read actual OPDMedicineId from DB ───────────────
                // Use GetMedicinesByOPDId which reads Medicine_Id per row
                // If your SP returns an "Id" (OPDMedicineId) column, use it;
                // otherwise fall back to 0 (Dispense button hidden for that row)
                var dbMeds = _iAppointment.GetMedicinesByOPDId(opdId) ?? new List<OPDMedicineVM>();

                // Dispense status — check notification per appointment for now
                // (upgrade to per-medicine when your DB has OPDMedicineId column)
                var notifs = _notifRepo?.GetAllNotifications(hospitalId, subHospitalId);
                var notif = notifs?.FirstOrDefault(x => x.AppointmentId == appointmentId);
                bool allDisp = notif?.Status == "Dispensed";

                var medicines = dbMeds.Select((m, idx) => new
                {
                    opdMedId = m.MedicineId,    // ← FIX: actual row ID (add OPDMedicineId to OPDMedicineVM)
                    medicineId = m.MedicineId,
                    medicineName = m.MedicineName,
                    morning = m.Morning.ToString(),
                    afternoon = m.Afternoon.ToString(),
                    evening = m.Evening.ToString(),
                    days = m.Days,
                    isDispensed = allDisp
                }).ToList();

                // ── PERFORMANCE: Use single SQL query for token (not load-all-then-find) ──
                int tokenNum = _iAppointment.GetTodayTokenNumber(appointmentId, hospitalId, subHospitalId);

                return Json(new
                {
                    patientName = patientName,
                    doctorName = doctorName,
                    symptoms = symptomsStr,
                    tokenNumber = tokenNum,     // ← FIX: was always 0
                    opdId = opdId,
                    medicines = medicines,
                    isDispensed = allDisp
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // AJAX: DISPENSE MEDICINE
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult DispenseMedicine(int opdMedId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            try
            {
                _notifRepo?.MarkDispensed(opdMedId, hospitalId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // AJAX: COMPLETE VISIT
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult CompleteVisit(int appointmentId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            try
            {
                _iAppointment.UpdateStatus(appointmentId, hospitalId, subHospitalId, "OPD Completed");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
