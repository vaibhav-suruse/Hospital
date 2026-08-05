using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
public class WalkInConsultationController : Controller
    {
        private readonly Ipatient _patientService;
        private readonly IIPDAdmission _ipdRepo;
        private readonly string _connectionString;

        public WalkInConsultationController(Ipatient patientService, IIPDAdmission ipdRepo, IConfiguration configuration)
        {
            _patientService = patientService;
            _ipdRepo = ipdRepo;
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // ── Multi-hospital IPD data isolation helper ──────────────
        // When the Registration tab is opened from an IPD record, the URL
        // carries ipdId. This verifies the IPD admission actually belongs to
        // the current session's hospital/sub-hospital so a user can never
        // open/see another hospital's patient by typing a foreign ipdId.
        private bool IsIpdAuthorized(int ipdId)
        {
            int hospitalId = GetHospitalId();
            int? subHospitalId = GetSubHospitalId();
            return _ipdRepo.GetIPDAdmissionById(ipdId, hospitalId, subHospitalId) != null;
        }

        // ── Session helpers ───────────────────────────────────────
        // FIX: use "MainHospitalId" to match the rest of the app
        private int GetHospitalId() =>
            HttpContext.Session.GetInt32("MainHospitalId")
            ?? HttpContext.Session.GetInt32("PatientHospitalId")
            ?? 0;

        private int? GetSubHospitalId()
        {
            var val = HttpContext.Session.GetInt32("SubHospitalId")
                   ?? HttpContext.Session.GetInt32("PatientSubHospitalId");
            return (val == null || val == 0) ? (int?)null : val;
        }

// ══════════════════════════════════════════════════════════
        //  GET: /WalkInConsultation/Index
        //  If opened from an IPD record (ipdId > 0), verify the admission
        //  belongs to the current session's hospital/sub-hospital. A user
        //  typing another hospital's ipdId directly into the URL gets a
        //  "Not Found" instead of seeing that hospital's data.
        //  Also supports a date filter (mirrors OPD Appointment Index):
        //  the page always shows WALK-IN consults only (IsWalkIn = 1); the
        //  date filter just changes which day's walk-ins are shown.
        // ══════════════════════════════════════════════════════════
public IActionResult Index(int ipdId = 0, string date = "")
        {
            if (ipdId > 0 && !IsIpdAuthorized(ipdId))
                return NotFound();

            int hospitalId = GetHospitalId();
            int? subHospitalId = GetSubHospitalId();

            // ── Date filter ────────────────────────────────────
            DateTime selectedDate;
            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out selectedDate))
            {
                // use the user-selected date
            }
            else
            {
                selectedDate = DateTime.Today;
            }

            var todayConsults = GetWalkInConsults(hospitalId, subHospitalId, DateTime.Today);
            var selectedConsults = GetWalkInConsults(hospitalId, subHospitalId, selectedDate);

            var vm = new WalkInConsultationVM
            {
                TodayConsults = todayConsults,
                TodayCount = todayConsults.Count,
                SelectedDate = selectedDate,
                SelectedDateConsults = selectedConsults,
                SelectedDateCount = selectedConsults.Count
            };
            return View(vm);
        }

        // ══════════════════════════════════════════════════════════
        //  GET: /WalkInConsultation/SearchPatient?query=...
        //  Returns patients from tbl_patient for this hospital.
        //  FIX: column names are Hospital_Id / SubHospital_Id (underscore)
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult SearchPatient(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Json(new List<object>());

            int hospitalId = GetHospitalId();
            int? subHospitalId = GetSubHospitalId();

            var results = new List<object>();
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
                    SELECT Id,
                           TRIM(CONCAT(COALESCE(FirstName,''), ' ', COALESCE(LastName,''))) AS FullName,
                           COALESCE(PhoneNumber,'') AS PhoneNumber
                    FROM tbl_patient
                    WHERE Hospital_Id = @HospitalId
                      AND (
                            @SubHospitalId IS NULL
                            OR SubHospital_Id = @SubHospitalId
                          )
                      AND (
                            CONCAT(COALESCE(FirstName,''), ' ', COALESCE(LastName,'')) LIKE @q
                         OR PhoneNumber LIKE @q
                         OR CAST(Id AS CHAR) LIKE @q
                          )
                    ORDER BY FirstName
                    LIMIT 10", con);

                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("@SubHospitalId",
                    subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@q", "%" + query + "%");

                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    results.Add(new
                    {
                        id = Convert.ToInt32(dr["Id"]),
                        name = dr["FullName"].ToString(),
                        mobile = dr["PhoneNumber"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }

            return Json(results);
        }

        // ══════════════════════════════════════════════════════════
        //  POST: /WalkInConsultation/AddPatient
        //  Adds patient + creates walk-in appointment
        // ══════════════════════════════════════════════════════════
[HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddPatient(Patient model)
        {
            int hospitalId = GetHospitalId();
            int? subHospitalId = GetSubHospitalId();

            try
            {
                int patientId = _patientService.AddPatient(model, hospitalId, subHospitalId);
                if (patientId <= 0)
                    return Json(new { success = false, message = "Failed to add patient." });

                // NOTE: The walk-in appointment is NOT created here.
                // It is only created when the user actually clicks "Consult"
                // (via CreateWalkInAppointment). This prevents orphaned
                // "Pending" appointments from appearing in the list when the
                // user closes/cancels the consult popup without consulting.
                return Json(new
                {
                    success = true,
                    patientId = patientId,
                    patientName = model.FullName?.Trim(),
                    mobile = model.PhoneNumber
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════
        //  POST: /WalkInConsultation/AssignDoctor
        //  Updates DoctorId on the walk-in appointment before
        //  the receptionist opens the OPD form.
        //  Called from the doctor selection popup on the walk-in page.
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignDoctor(int appointmentId, int doctorId)
        {
            if (appointmentId <= 0 || doctorId <= 0)
                return Json(new { success = false, message = "Invalid appointment or doctor." });

            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
                    UPDATE opdappointment
                       SET DoctorId = @DoctorId
                     WHERE Id = @AppointmentId", con);

                cmd.Parameters.AddWithValue("@DoctorId", doctorId);
                cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

                con.Open();
                int rows = cmd.ExecuteNonQuery();

                if (rows > 0)
                    return Json(new { success = true });
                else
                    return Json(new { success = false, message = "Appointment not found." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        //  For existing patient clicking Consult — creates appointment
        //  then redirects directly to OPD form.
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateWalkInAppointment(int patientId)
        {
            int hospitalId = GetHospitalId();
            int? subHospitalId = GetSubHospitalId();

            try
            {
                int appointmentId = CreateWalkInAppointmentInDb(patientId, hospitalId, subHospitalId);
                if (appointmentId <= 0)
                    return Json(new { success = false, message = "Failed to create walk-in appointment." });

                return Json(new { success = true, appointmentId = appointmentId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════
        //  POST: /WalkInConsultation/CancelWalkInAppointment
        //  Soft-deletes a walk-in appointment that the user did not
        //  proceed with (e.g. closed the doctor-selection popup without
        //  starting the consultation). This prevents orphaned "Pending"
        //  rows from staying in Today's Walk-In Consults.
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelWalkInAppointment(int appointmentId)
        {
            if (appointmentId <= 0)
                return Json(new { success = false, message = "Invalid appointment." });

            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
                    UPDATE opdappointment
                       SET IsActive = 0,
                           Status = 'Cancelled'
                     WHERE Id = @AppointmentId", con);

                cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

                con.Open();
                int rows = cmd.ExecuteNonQuery();

                if (rows > 0)
                    return Json(new { success = true });
                else
                    return Json(new { success = false, message = "Appointment not found." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PRIVATE — insert walk-in into opdappointment
        //  opdappointment uses camelCase: HospitalId, SubHospitalId
        // ══════════════════════════════════════════════════════════
        private int CreateWalkInAppointmentInDb(int patientId, int hospitalId, int? subHospitalId)
        {
            using var con = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand(@"
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

// ══════════════════════════════════════════════════════════
        //  PRIVATE — walk-in consult list for a given date
        //  ══════════════════════════════════════════════════════════
        private List<WalkInConsultRow> GetWalkInConsults(int hospitalId, int? subHospitalId, DateTime date)
        {
            var list = new List<WalkInConsultRow>();
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
                    SELECT
                        a.Id                                                            AS AppointmentId,
                        a.PatientId,
                        TRIM(CONCAT(COALESCE(p.FirstName,''), ' ', COALESCE(p.LastName,''))) AS PatientName,
                        COALESCE(p.PhoneNumber,'')                                      AS PhoneNumber,
                        a.AppointmentTime,
                        a.Status,
                        COALESCE(o.Id, 0)                                               AS OPDId
                    FROM opdappointment a
                    INNER JOIN tbl_patient p
                        ON p.Id = a.PatientId
                    LEFT JOIN opdmaster o
                        ON o.AppointmentId = a.Id
                    WHERE a.HospitalId = @HospitalId
                      AND (
                            @SubHospitalId IS NULL
                            OR a.SubHospitalId = @SubHospitalId
                          )
AND DATE(a.AppointmentDate) = @AppointmentDate
                      AND a.IsWalkIn = 1
                      AND a.IsActive = 1
                    ORDER BY a.AppointmentTime ASC", con);

                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("@SubHospitalId",
                    subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@AppointmentDate", date.Date);

                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new WalkInConsultRow
                    {
                        AppointmentId = Convert.ToInt32(dr["AppointmentId"]),
                        PatientId = Convert.ToInt32(dr["PatientId"]),
                        PatientName = dr["PatientName"].ToString(),
                        MobileNo = dr["PhoneNumber"].ToString(),
                        AppointmentTime = dr["AppointmentTime"] == DBNull.Value
                                            ? TimeSpan.Zero
                                            : TimeSpan.Parse(dr["AppointmentTime"].ToString()),
                        Status = dr["Status"].ToString(),
                        OPDId = Convert.ToInt32(dr["OPDId"])
                    });
                }
            }
            catch { /* return empty list — page won't crash */ }

            return list;
        }
    }
}

//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Configuration;
//using MySql.Data.MySqlClient;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using WebApplicationSampleTest2.Models;
//using WebApplicationSampleTest2.Repository;

//namespace WebApplicationSampleTest2.Controllers
//{
//    public class WalkInConsultationController : Controller
//    {
//        private readonly Ipatient _patientService;
//        private readonly string _connectionString;

//        public WalkInConsultationController(Ipatient patientService, IConfiguration configuration)
//        {
//            _patientService = patientService;
//            _connectionString = configuration.GetConnectionString("MySqlConnection");
//        }

//        // ── Session helpers ───────────────────────────────────────
//        // FIX: use "MainHospitalId" to match the rest of the app
//        private int GetHospitalId() =>
//            HttpContext.Session.GetInt32("MainHospitalId")
//            ?? HttpContext.Session.GetInt32("PatientHospitalId")
//            ?? 0;

//        private int? GetSubHospitalId()
//        {
//            var val = HttpContext.Session.GetInt32("SubHospitalId")
//                   ?? HttpContext.Session.GetInt32("PatientSubHospitalId");
//            return (val == null || val == 0) ? (int?)null : val;
//        }

//        // ══════════════════════════════════════════════════════════
//        //  GET: /WalkInConsultation/Index
//        // ══════════════════════════════════════════════════════════
//        public IActionResult Index()
//        {
//            var vm = new WalkInConsultationVM
//            {
//                TodayConsults = GetTodayWalkInConsults(GetHospitalId(), GetSubHospitalId())
//            };
//            return View(vm);
//        }

//        // ══════════════════════════════════════════════════════════
//        //  GET: /WalkInConsultation/SearchPatient?query=...
//        //  Returns patients from tbl_patient for this hospital.
//        //  FIX: column names are Hospital_Id / SubHospital_Id (underscore)
//        // ══════════════════════════════════════════════════════════
//        [HttpGet]
//        public IActionResult SearchPatient(string query)
//        {
//            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
//                return Json(new List<object>());

//            int hospitalId = GetHospitalId();
//            int? subHospitalId = GetSubHospitalId();

//            var results = new List<object>();
//            try
//            {
//                using var con = new MySqlConnection(_connectionString);
//                using var cmd = new MySqlCommand(@"
//                    SELECT Id,
//                           TRIM(CONCAT(COALESCE(FirstName,''), ' ', COALESCE(LastName,''))) AS FullName,
//                           COALESCE(PhoneNumber,'') AS PhoneNumber
//                    FROM tbl_patient
//                    WHERE Hospital_Id = @HospitalId
//                      AND (
//                            @SubHospitalId IS NULL
//                            OR SubHospital_Id = @SubHospitalId
//                          )
//                      AND (
//                            CONCAT(COALESCE(FirstName,''), ' ', COALESCE(LastName,'')) LIKE @q
//                         OR PhoneNumber LIKE @q
//                         OR CAST(Id AS CHAR) LIKE @q
//                          )
//                    ORDER BY FirstName
//                    LIMIT 10", con);

//                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);
//                cmd.Parameters.AddWithValue("@SubHospitalId",
//                    subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
//                cmd.Parameters.AddWithValue("@q", "%" + query + "%");

//                con.Open();
//                using var dr = cmd.ExecuteReader();
//                while (dr.Read())
//                {
//                    results.Add(new
//                    {
//                        id = Convert.ToInt32(dr["Id"]),
//                        name = dr["FullName"].ToString(),
//                        mobile = dr["PhoneNumber"].ToString()
//                    });
//                }
//            }
//            catch (Exception ex)
//            {
//                return Json(new { error = ex.Message });
//            }

//            return Json(results);
//        }

//        // ══════════════════════════════════════════════════════════
//        //  POST: /WalkInConsultation/AddPatient
//        //  Adds patient + creates walk-in appointment
//        // ══════════════════════════════════════════════════════════
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public IActionResult AddPatient(Patient model)
//        {
//            int hospitalId = GetHospitalId();
//            int? subHospitalId = GetSubHospitalId();

//            try
//            {
//                int patientId = _patientService.AddPatient(model, hospitalId, subHospitalId);
//                if (patientId <= 0)
//                    return Json(new { success = false, message = "Failed to add patient." });

//                int appointmentId = CreateWalkInAppointmentInDb(patientId, hospitalId, subHospitalId);
//                if (appointmentId <= 0)
//                    return Json(new { success = false, message = "Patient added but failed to create appointment." });

//                return Json(new
//                {
//                    success = true,
//                    patientId = patientId,
//                    appointmentId = appointmentId,
//                    patientName = model.FullName?.Trim(),
//                    mobile = model.PhoneNumber
//                });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        // ══════════════════════════════════════════════════════════
//        //  POST: /WalkInConsultation/CreateWalkInAppointment
//        //  For existing patient clicking Consult — creates appointment
//        //  then redirects directly to OPD form.
//        // ══════════════════════════════════════════════════════════
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public IActionResult CreateWalkInAppointment(int patientId)
//        {
//            int hospitalId = GetHospitalId();
//            int? subHospitalId = GetSubHospitalId();

//            try
//            {
//                int appointmentId = CreateWalkInAppointmentInDb(patientId, hospitalId, subHospitalId);
//                if (appointmentId <= 0)
//                    return Json(new { success = false, message = "Failed to create walk-in appointment." });

//                return Json(new { success = true, appointmentId = appointmentId });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        // ══════════════════════════════════════════════════════════
//        //  PRIVATE — insert walk-in into opdappointment
//        //  opdappointment uses camelCase: HospitalId, SubHospitalId
//        // ══════════════════════════════════════════════════════════
//        private int CreateWalkInAppointmentInDb(int patientId, int hospitalId, int? subHospitalId)
//        {
//            using var con = new MySqlConnection(_connectionString);
//            using var cmd = new MySqlCommand(@"
//                INSERT INTO opdappointment
//                    (PatientId, HospitalId, SubHospitalId, DoctorId,
//                     AppointmentDate, AppointmentTime, Status, IsActive, IsWalkIn)
//                VALUES
//                    (@PatientId, @HospitalId, @SubHospitalId, 0,
//                     @AppointmentDate, @AppointmentTime, 'Pending', 1, 1);
//                SELECT LAST_INSERT_ID();", con);

//            cmd.Parameters.AddWithValue("@PatientId", patientId);
//            cmd.Parameters.AddWithValue("@HospitalId", hospitalId);
//            cmd.Parameters.AddWithValue("@SubHospitalId",
//                subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
//            cmd.Parameters.AddWithValue("@AppointmentDate", DateTime.Today);
//            cmd.Parameters.AddWithValue("@AppointmentTime", DateTime.Now.ToString("HH:mm:ss"));

//            con.Open();
//            var result = cmd.ExecuteScalar();
//            return result != null ? Convert.ToInt32(result) : 0;
//        }

//        // ══════════════════════════════════════════════════════════
//        //  PRIVATE — today's walk-in consult list
//        // ══════════════════════════════════════════════════════════
//        private List<WalkInConsultRow> GetTodayWalkInConsults(int hospitalId, int? subHospitalId)
//        {
//            var list = new List<WalkInConsultRow>();
//            try
//            {
//                using var con = new MySqlConnection(_connectionString);
//                using var cmd = new MySqlCommand(@"
//                    SELECT
//                        a.Id                                                            AS AppointmentId,
//                        a.PatientId,
//                        TRIM(CONCAT(COALESCE(p.FirstName,''), ' ', COALESCE(p.LastName,''))) AS PatientName,
//                        COALESCE(p.PhoneNumber,'')                                      AS PhoneNumber,
//                        a.AppointmentTime,
//                        a.Status,
//                        COALESCE(o.Id, 0)                                               AS OPDId
//                    FROM opdappointment a
//                    INNER JOIN tbl_patient p
//                        ON p.Id = a.PatientId
//                    LEFT JOIN opdmaster o
//                        ON o.AppointmentId = a.Id
//                    WHERE a.HospitalId = @HospitalId
//                      AND (
//                            @SubHospitalId IS NULL
//                            OR a.SubHospitalId = @SubHospitalId
//                          )
//                      AND DATE(a.AppointmentDate) = CURDATE()
//                      AND a.IsWalkIn = 1
//                      AND a.IsActive = 1
//                    ORDER BY a.AppointmentTime ASC", con);

//                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);
//                cmd.Parameters.AddWithValue("@SubHospitalId",
//                    subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);

//                con.Open();
//                using var dr = cmd.ExecuteReader();
//                while (dr.Read())
//                {
//                    list.Add(new WalkInConsultRow
//                    {
//                        AppointmentId = Convert.ToInt32(dr["AppointmentId"]),
//                        PatientId = Convert.ToInt32(dr["PatientId"]),
//                        PatientName = dr["PatientName"].ToString(),
//                        MobileNo = dr["PhoneNumber"].ToString(),
//                        AppointmentTime = dr["AppointmentTime"] == DBNull.Value
//                                            ? TimeSpan.Zero
//                                            : TimeSpan.Parse(dr["AppointmentTime"].ToString()),
//                        Status = dr["Status"].ToString(),
//                        OPDId = Convert.ToInt32(dr["OPDId"])
//                    });
//                }
//            }
//            catch { /* return empty list — page won't crash */ }

//            return list;
//        }
//    }
//}
