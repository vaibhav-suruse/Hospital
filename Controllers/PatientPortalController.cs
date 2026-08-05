using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class PatientPortalController : Controller
    {
        private readonly IPatientPortal _portalRepo;
        private readonly Ipatient _patientRepo;
        private readonly IDoctor _doctorRepo;
        private readonly IOPDAppointment _appointmentRepo;
        private readonly IHospital _IHospital;
        private readonly string _connectionString;

        public PatientPortalController(
            IPatientPortal portalRepo,
            Ipatient patientRepo,
            IDoctor doctorRepo,
            IOPDAppointment appointmentRepo,
            IHospital hospitalRepo,
            IConfiguration configuration)
        {
            _portalRepo = portalRepo;
            _patientRepo = patientRepo;
            _doctorRepo = doctorRepo;
            _appointmentRepo = appointmentRepo;
            _IHospital = hospitalRepo;
            _connectionString = configuration
                .GetConnectionString("MySqlConnection");
        }

        // ── SECURITY HELPERS ─────────────────────────────
        private int GetLoggedInPatientId()
        {
            return HttpContext.Session
                   .GetInt32("PatientId") ?? 0;
        }

        private IActionResult RedirectIfNotLoggedIn()
        {
            int? accountId = HttpContext.Session
                             .GetInt32("AccountId");
            if (!accountId.HasValue ||
                 accountId.Value == 0)
                return RedirectToAction(
                    "PatientLogin", "Login");
            return null;
        }

        // ── DASHBOARD ────────────────────────────────────
        public IActionResult Dashboard()
        {
            int? accountId = HttpContext.Session
                             .GetInt32("AccountId");
            int? patientId = HttpContext.Session
                             .GetInt32("PatientId");

            if (!accountId.HasValue ||
                 accountId.Value == 0)
                return RedirectToAction(
                    "PatientLogin", "Login");

            try
            {
                if (!patientId.HasValue ||
                     patientId.Value == 0)
                {
                    var emptyVm = new PatientDashboardVM
                    {
                        PatientName =
                            HttpContext.Session
                            .GetString("AccountName")
                            ?? "Patient",
                        UpcomingAppointmentsCount = 0,
                        TotalOPDVisits = 0,
                        TotalIPDAdmissions = 0,
                        TotalDueAmount = 0,
                        RecentAppointments =
                            new List<PatientAppointmentVM>()
                    };
                    return View(emptyVm);
                }

                var vm = _portalRepo
                    .GetDashboardSummary(patientId.Value);
                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Dashboard error: " + ex.Message;
                return View(new PatientDashboardVM
                {
                    PatientName =
                        HttpContext.Session
                        .GetString("AccountName")
                        ?? "Patient",
                    RecentAppointments =
                        new List<PatientAppointmentVM>()
                });
            }
        }

        // ── APPOINTMENTS ─────────────────────────────────
        public IActionResult Appointments()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            int patientId = GetLoggedInPatientId();
            try
            {
                var appointments = patientId > 0
                    ? _portalRepo
                      .GetAppointmentsByPatientId(patientId)
                    : new List<PatientAppointmentVM>();

                ViewBag.Upcoming = appointments
                    .Where(a =>
                        a.AppointmentDate.Date >=
                        DateTime.Today &&
                        a.Status == "Pending")
                    .OrderBy(a => a.AppointmentDate)
                    .ToList();

                ViewBag.Past = appointments
                    .Where(a =>
                        a.AppointmentDate.Date <
                        DateTime.Today ||
                        a.Status != "Pending")
                    .OrderByDescending(
                        a => a.AppointmentDate)
                    .ToList();

                return View(appointments);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load appointments. " +
                    ex.Message;
                return View(
                    new List<PatientAppointmentVM>());
            }
        }

        // ── CANCEL APPOINTMENT ───────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelAppointment(
            int appointmentId)
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            int patientId = GetLoggedInPatientId();
            try
            {
                bool success = _portalRepo
                    .CancelAppointment(
                        appointmentId, patientId);
                TempData[success ? "Success" : "Error"] =
                    success
                    ? "Appointment cancelled successfully."
                    : "Unable to cancel appointment.";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Cancellation failed: " + ex.Message;
            }
            return RedirectToAction("Appointments");
        }

        // ── BOOK APPOINTMENT GET ─────────────────────────
        [HttpGet]
        public IActionResult BookAppointment()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            int patientId = GetLoggedInPatientId();

            var vm = new PatientBookAppointmentVM
            {
                PatientId = patientId,
                HospitalId = 0,
                AvailableDoctors = new List<Doctor>()
            };

            var allHospitals = _IHospital
                .GetAllHospitals()
                .Where(h => h.IsActive)
                .ToList();

            ViewBag.MainHospitals = allHospitals
                .Where(h => !h.IsSubHospital)
                .ToList();

            ViewBag.SubHospitals = allHospitals
                .Where(h => h.IsSubHospital)
                .ToList();

            return View(vm);
        }

        // ── BOOK APPOINTMENT POST ────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BookAppointment(
            PatientBookAppointmentVM model)
        {

          
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            int patientId = GetLoggedInPatientId();
            int accountId = HttpContext.Session
                            .GetInt32("AccountId") ?? 0;

            model.PatientId = patientId;

            // ── Validation ───────────────────────────
            if (model.HospitalId <= 0)
            {
                TempData["Error"] =
                    "Please select a hospital.";
                return ReloadBookView(model);
            }

            if (model.DoctorId <= 0)
            {
                TempData["Error"] =
                    "Please select a doctor.";
                return ReloadBookView(model);
            }

            if (model.AppointmentDate.Date <
                DateTime.Today)
            {
                TempData["Error"] =
                    "Please select a future date.";
                return ReloadBookView(model);
            }

            try
            {
                // ── Auto create patient if no record ─────────
                if (patientId == 0 && accountId > 0)
                {
                    string accountName = HttpContext.Session
                        .GetString("AccountName") ?? "";

                    string[] nameParts = accountName.Split(' ');

                    string firstName = nameParts.Length > 0
                        ? nameParts[0] : accountName;

                    string lastName = nameParts.Length > 1
                        ? string.Join(" ", nameParts,
                            1, nameParts.Length - 1)
                        : "";

                    string patientEmail = HttpContext.Session
                        .GetString("AccountEmail") ?? "";

                    using var con =
                        new MySqlConnection(_connectionString);

                    using var cmd = new MySqlCommand(@"
            INSERT INTO tbl_patient
                (FirstName, LastName, Email,
                 Hospital_Id, SubHospital_Id,
                 AccountId, Relation)
            VALUES
                (@fn, @ln, @email,
                 @hid, @shid,
                 @aid, 'Self');
            SELECT LAST_INSERT_ID();", con);

                    cmd.Parameters.AddWithValue("@fn", firstName);
                    cmd.Parameters.AddWithValue("@ln", lastName);
                    cmd.Parameters.AddWithValue("@email", patientEmail);
                    cmd.Parameters.AddWithValue("@hid", model.HospitalId);
                    cmd.Parameters.AddWithValue("@shid",
                        model.SubHospitalId.HasValue
                        ? (object)model.SubHospitalId.Value
                        : DBNull.Value);
                    cmd.Parameters.AddWithValue("@aid", accountId);

                    con.Open();
                    patientId = Convert.ToInt32(
                        cmd.ExecuteScalar());

                    HttpContext.Session.SetInt32(
                        "PatientId", patientId);
                }

                // ── Always update Hospital on patient record ──
                if (patientId > 0)
                {
                    using var conUpd =
                        new MySqlConnection(_connectionString);
                    using var cmdUpd = new MySqlCommand(@"
            UPDATE tbl_patient
            SET    Hospital_Id    = @hid,
                   SubHospital_Id = @shid
            WHERE  Id = @pid",
                        conUpd);

                    cmdUpd.Parameters.AddWithValue(
                        "@hid", model.HospitalId);
                    cmdUpd.Parameters.AddWithValue(
                        "@shid",
                        model.SubHospitalId.HasValue
                        ? (object)model.SubHospitalId.Value
                        : DBNull.Value);
                    cmdUpd.Parameters.AddWithValue(
                        "@pid", patientId);

                    conUpd.Open();
                    cmdUpd.ExecuteNonQuery();
                }

                // ── Update session ────────────────────────────
                HttpContext.Session.SetInt32(
                    "PatientHospitalId", model.HospitalId);

                if (model.SubHospitalId.HasValue)
                    HttpContext.Session.SetInt32(
                        "PatientSubHospitalId",
                        model.SubHospitalId.Value);
                else
                    HttpContext.Session.Remove(
                        "PatientSubHospitalId");

                // ── Create appointment ────────────────────────
                var appointment = new OPDAppointmentModel
                {
                    PatientId = patientId,
                    DoctorId = model.DoctorId,
                    AppointmentDate = model.AppointmentDate,
                    AppointmentTime = model.AppointmentTime,
                    Status = "Pending"
                };

                _appointmentRepo.CreateAppointment(
                    appointment,
                    model.HospitalId,
                    model.SubHospitalId);

                TempData["Success"] =
                    "Appointment booked successfully!";
                return RedirectToAction("Appointments");
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Booking failed: " + ex.Message;
                return ReloadBookView(model);
            }
        }

        // ── RELOAD BOOK VIEW HELPER ───────────────────────
        private IActionResult ReloadBookView(
            PatientBookAppointmentVM model)
        {
            var allHospitals = _IHospital
                .GetAllHospitals()
                .Where(h => h.IsActive)
                .ToList();

            ViewBag.MainHospitals = allHospitals
                .Where(h => !h.IsSubHospital)
                .ToList();

            ViewBag.SubHospitals = allHospitals
                .Where(h => h.IsSubHospital)
                .ToList();

            return View(model);
        }

        // ── AJAX: GET DOCTORS BY HOSPITAL ────────────────
        [HttpGet]
        public JsonResult GetDoctorsByHospital(
            int hospitalId, int? subHospitalId)
        {
            var doctors = _doctorRepo
                .GetAllDoctor(hospitalId, subHospitalId)
                ?? new List<Doctor>();

            var result = doctors.Select(d => new
            {
                id = d.Doctor_Id,
                name = (d.FirstName ?? "") + " " +
                       (d.LastName ?? "") +
                       (string.IsNullOrEmpty(
                           d.Specialization)
                        ? ""
                        : $" ({d.Specialization})")
            }).ToList();

            return Json(result);
        }

        // ── OPD HISTORY ──────────────────────────────────
        public IActionResult OPDHistory()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            int patientId = GetLoggedInPatientId();
            try
            {
                var history = patientId > 0
                    ? _portalRepo
                      .GetOPDHistoryByPatientId(patientId)
                    : new List<PatientOPDHistoryVM>();
                return View(history);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load OPD history. " +
                    ex.Message;
                return View(
                    new List<PatientOPDHistoryVM>());
            }
        }

        // ── IPD HISTORY ──────────────────────────────────
        public IActionResult IPDHistory()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            int patientId = GetLoggedInPatientId();

            int hospitalId = HttpContext.Session
                .GetInt32("PatientHospitalId") ?? 0;

            int? subHospitalId = HttpContext.Session
                .GetInt32("PatientSubHospitalId");

            try
            {
                var ipdList = patientId > 0
                    ? _portalRepo.GetIPDHistoryByPatientId(patientId)
                    : new List<PatientIPDHistoryVM>();

                var detailList = new List<PatientIPDDetailVM>();

                foreach (var ipd in ipdList)
                {
                    var detail = new PatientIPDDetailVM
                    {
                        IPDId = ipd.IPDId,
                        AdmissionNumber = ipd.AdmissionNumber,
                        AdmissionDateTime = ipd.AdmissionDateTime,
                        DischargeDateTime = ipd.DischargeDateTime,
                        Status = ipd.Status,
                        ReasonForAdmission = ipd.ReasonForAdmission,
                        DoctorName = ipd.DoctorName,
                        WardName = ipd.WardName,
                        BedNumber = ipd.BedNumber,
                        TotalDays = ipd.TotalDays,
                        HasBill = ipd.HasBill,
                        PaymentStatus = ipd.PaymentStatus,
                        TotalAmount = ipd.TotalAmount,
                        PaidAmount = ipd.PaidAmount,
                        DueAmount = ipd.DueAmount
                    };

                    if (patientId > 0 && hospitalId > 0)
                    {
                        detail.DoctorRounds = _portalRepo
                            .GetIPDRoundsByIPDId(
                                ipd.IPDId,
                                patientId,
                                hospitalId);
                    }

                    if (patientId > 0)
                    {
                        detail.Vitals = _portalRepo
                            .GetVitalsByIPDId(
                                ipd.IPDId,
                                patientId,
                                hospitalId,
                                subHospitalId);
                    }

                    detailList.Add(detail);
                }

                return View(detailList);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load IPD history. " + ex.Message;

                return View(new List<PatientIPDDetailVM>());
            }
        }

        // ── BILLING ──────────────────────────────────────
        public IActionResult Billing()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            int patientId = GetLoggedInPatientId();
            try
            {
                var vm = new PatientBillingVM();

                if (patientId > 0)
                {
                    vm.OPDBills = _portalRepo
                        .GetOPDBillsByPatientId(patientId);
                    vm.IPDBills = _portalRepo
                        .GetIPDHistoryByPatientId(patientId)
                        .Where(i => i.HasBill)
                        .ToList();
                    vm.TotalOPDAmount =
                        vm.OPDBills.Sum(b => b.TotalAmount);
                    vm.TotalIPDAmount =
                        vm.IPDBills.Sum(b => b.TotalAmount);
                    vm.TotalDue =
                        vm.OPDBills.Sum(b => b.DueAmount) +
                        vm.IPDBills.Sum(b => b.DueAmount);
                }

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load billing. " +
                    ex.Message;
                return View(new PatientBillingVM());
            }
        }

        // ── LOGOUT ───────────────────────────────────────
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("PatientId");
            HttpContext.Session.Remove("AccountId");
            HttpContext.Session.Remove("AccountName");
            HttpContext.Session.Remove("AccountEmail");
            HttpContext.Session.Remove(
                "PatientHospitalId");
            HttpContext.Session.Remove(
                "PatientSubHospitalId");
            HttpContext.Session.Remove("PatientName");
            HttpContext.Session.Remove(
                "PatientRelation");
            HttpContext.Session.Remove(
                "PatientHospitalName");
            return RedirectToAction(
                "PatientLogin", "Login");
        }
    }
}
