using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;
using Microsoft.Extensions.Logging;

namespace WebApplicationSampleTest2.Controllers
{
    public class DashBordController : Controller
    {
        private readonly ILogger<DashBordController> _logger;
        private readonly Ipatient _patientRepo;
        private readonly IOPDAppointment _appointmentRepo;
        private readonly IDoctor _docotrRepo;
        private readonly IHospital _hospitalRepo;
        private readonly IUser _userRepo;
        private readonly IDashboardRepository _dashboardRepo;
        public DashBordController(Ipatient ipatient, IOPDAppointment oPDAppointment, IDoctor docotrRepo, IHospital hospitalRepo, IUser userRepo, IDashboardRepository dashboardRepo, ILogger<DashBordController> logger)
        {
            _patientRepo = ipatient;
            _logger = logger;
            _appointmentRepo = oPDAppointment;
            _docotrRepo = docotrRepo;
            _hospitalRepo = hospitalRepo;
            _userRepo = userRepo;
            _dashboardRepo = dashboardRepo;

        }

        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return View();
        }

        //public IActionResult Dash()
        //{
        //    var role = HttpContext.Session.GetString("UserRole");
        //    return View();
        //}

        //    [HttpGet]
        //    public IActionResult GetDashboardCounts()
        //    {
        //        var hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //        var subHospitalId = HttpContext.Session.GetInt32("SubHospitalId") ?? 0;

        //        var totalDoct = _docotrRepo.GetAllDoctor(hospitalId, subHospitalId);

        //        var allPatients = _patientRepo.GetAllPatients(hospitalId, subHospitalId);
        //        var allAppointments = _appointmentRepo.GetAllAppointments(hospitalId, subHospitalId);

        //        var today = DateTime.Today;
        //        var yesterday = today.AddDays(-1);
        //        var tomorrow = today.AddDays(1);
        //        var todayStart = DateTime.Today;
        //        var todayEnd = todayStart.AddDays(1);

        //        var yesterdayStart = todayStart.AddDays(-1);
        //        var yesterdayEnd = todayStart;

        //        var tomorrowStart = todayStart.AddDays(1);
        //        var tomorrowEnd = todayStart.AddDays(2);

        //        // Today booked doctors
        //        var todayDoctorIds = allAppointments
        //            .Where(a => a.AppointmentDate >= todayStart &&
        //                        a.AppointmentDate < todayEnd)
        //            .Select(a => a.DoctorId)
        //            .Distinct()
        //            .ToList();

        //        // Yesterday booked doctors
        //        var yesterdayDoctorIds = allAppointments
        //            .Where(a => a.AppointmentDate >= yesterdayStart &&
        //                        a.AppointmentDate < yesterdayEnd)
        //            .Select(a => a.DoctorId)
        //            .Distinct()
        //            .ToList();

        //        // Tomorrow booked doctors
        //        var tomorrowDoctorIds = allAppointments
        //            .Where(a => a.AppointmentDate >= tomorrowStart &&
        //                        a.AppointmentDate < tomorrowEnd)
        //            .Select(a => a.DoctorId)
        //            .Distinct()
        //            .ToList();




        //        //added by me
        //        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        //        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        //        var startOfYear = new DateTime(today.Year, 1, 1);


        //        var result = new
        //        {

        //            TotalPatients = allPatients.Count,
        //            TotalAppointments = allAppointments.Count,
        //            TotalDoctors = totalDoct.Count,

        //            TodayAvailableDoctors = totalDoct
        //.Count(d => !todayDoctorIds.Contains(d.Doctor_Id)),

        //            YesterdayAvailableDoctors = totalDoct
        //.Count(d => !yesterdayDoctorIds.Contains(d.Doctor_Id)),

        //            TomorrowAvailableDoctors = totalDoct
        //.Count(d => !tomorrowDoctorIds.Contains(d.Doctor_Id)),


        //            CompletedAppointments = allAppointments.Count(a => a.Status == "OPD Completed"),
        //            PendingAppointments = allAppointments.Count(a => a.Status == "Pending"),

        //            //today 
        //            TodayCompleted = allAppointments .Count(a => a.AppointmentDate.Date == today && a.Status == "OPD Completed"),

        //            TodayPending = allAppointments .Count(a => a.AppointmentDate.Date == today  && a.Status == "Pending"),
        //            //ye

        //            TodayAppointments = allAppointments.Count(a => a.AppointmentDate.Date == today),
        //            YesterdayAppointments = allAppointments.Count(a => a.AppointmentDate.Date == yesterday),
        //            TomorrowAppointments = allAppointments.Count(a => a.AppointmentDate.Date == tomorrow),

        //            //added by me
        //            WeeklyAppointments = allAppointments.Count(a => a.AppointmentDate.Date >= startOfWeek),
        //            MonthlyAppointments = allAppointments.Count(a => a.AppointmentDate.Date >= startOfMonth),
        //            YearlyAppointments = allAppointments.Count(a => a.AppointmentDate.Date >= startOfYear),





        //            TodayAppointmentList = allAppointments.Where(a => a.AppointmentDate.Date == today)
        //                                                  .Select(a => new { a.Id, a.PatientId, a.DoctorId, a.AppointmentDate, a.Status })
        //                                                  .ToList(),
        //            YesterdayAppointmentList = allAppointments.Where(a => a.AppointmentDate.Date == yesterday)
        //                                                      .Select(a => new { a.Id, a.PatientId, a.DoctorId, a.AppointmentDate, a.Status })
        //                                                      .ToList(),
        //            YesterdayCompleted = allAppointments .Count(a => a.AppointmentDate.Date == yesterday  && a.Status == "OPD Completed"),

        //            YesterdayPending = allAppointments  .Count(a => a.AppointmentDate.Date == yesterday && a.Status == "Pending"),

        //            TomorrowAppointmentList = allAppointments.Where(a => a.AppointmentDate.Date == tomorrow)
        //                                                     .Select(a => new { a.Id, a.PatientId, a.DoctorId, a.AppointmentDate, a.Status })
        //                                                     .ToList(),
        //            TomorrowCompleted = allAppointments
        //.Count(a => a.AppointmentDate.Date == tomorrow
        //         && a.Status == "OPD Completed"),

        //            TomorrowPending = allAppointments
        //.Count(a => a.AppointmentDate.Date == tomorrow
        //         && a.Status == "Pending")

        //        };

        //        return Json(result);
        //    }

        [HttpGet]
        public IActionResult GetDashboardCounts()
        {
            var hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            var subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            // ═══ PERFORMANCE FIX ═══════════════════════════════════════
            // Previously this loaded EVERY patient, EVERY appointment and
            // EVERY doctor into memory and counted them in C# LINQ (dozens
            // of full-table scans + object allocations per request). Now a
            // single aggregated SQL query returns all the counts, month
            // chart data and appointment lists in ONE DB round-trip.
            // ══════════════════════════════════════════════════════════
            var vm = _dashboardRepo.GetDashboardCounts(hospitalId, subHospitalId);

            var result = new
            {
                // TOP COUNTS
                TotalPatients = vm.TotalPatients,
                TotalAppointments = vm.TotalAppointments,
                TotalDoctors = vm.TotalDoctors,

                CompletedAppointments = vm.CompletedAppointments,
                PendingAppointments = vm.PendingAppointments,

                // AVAILABLE DOCTORS
                TodayAvailableDoctors = vm.TodayAvailableDoctors,
                YesterdayAvailableDoctors = vm.YesterdayAvailableDoctors,
                TomorrowAvailableDoctors = vm.TomorrowAvailableDoctors,

                // TODAY
                TodayAppointments = vm.TodayAppointments,
                TodayCompleted = vm.TodayCompleted,
                TodayPending = vm.TodayPending,

                // YESTERDAY
                YesterdayAppointments = vm.YesterdayAppointments,
                YesterdayCompleted = vm.YesterdayCompleted,
                YesterdayPending = vm.YesterdayPending,

                // TOMORROW
                TomorrowAppointments = vm.TomorrowAppointments,
                TomorrowCompleted = vm.TomorrowCompleted,
                TomorrowPending = vm.TomorrowPending,

                // WEEK / MONTH / YEAR
                WeeklyAppointments = vm.WeeklyAppointments,
                MonthlyAppointments = vm.MonthlyAppointments,
                YearlyAppointments = vm.YearlyAppointments,

                // LISTS
                TodayAppointmentList = vm.TodayAppointmentList,
                YesterdayAppointmentList = vm.YesterdayAppointmentList,
                TomorrowAppointmentList = vm.TomorrowAppointmentList,

                MonthlyTotal = vm.MonthlyTotal,
                MonthlyCompleted = vm.MonthlyCompleted,
                MonthlyPending = vm.MonthlyPending,
            };

            return Json(result);
        }
        public IActionResult Dash()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "SuperAdmin")
                return Unauthorized();

            var mainDt = _hospitalRepo.GetMainHospitalDashboard();
            var subDt = _hospitalRepo.GetSubHospitalDashboard();

            var vm = new SuperAdminDashboardVM();

            vm.TotalHospitals = mainDt.Rows.Count;
            vm.TotalSubHospitals = subDt.Rows.Count;
            vm.TotalUsers = 0;

            foreach (DataRow row in mainDt.Rows)
            {
                var hospital = new HospitalInfo
                {
                    HospitalId = Convert.ToInt32(row["HospitalId"]),
                    HospitalName = row["HospitalName"].ToString(),
                    UserCount = Convert.ToInt32(row["UserCount"]),
                    SubHospitalCount = 0
                };

                vm.TotalUsers += hospital.UserCount;

                var relatedSubs = subDt.AsEnumerable()
                    .Where(r => Convert.ToInt32(r["ParentHospitalId"]) == hospital.HospitalId);

                foreach (var subRow in relatedSubs)
                {
                    hospital.SubHospitals.Add(new SubHospitalInfo
                    {
                        SubHospitalId = Convert.ToInt32(subRow["SubHospitalId"]),
                        SubHospitalName = subRow["SubHospitalName"].ToString(),
                        UserCount = Convert.ToInt32(subRow["UserCount"])
                    });

                    hospital.SubHospitalCount++;
                }

                vm.Hospitals.Add(hospital);
            }

            return View(vm);
        }


    }
}


//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;
//using System.Threading.Tasks;
//using WebApplicationSampleTest2.Models;
//using WebApplicationSampleTest2.Repository;

//namespace WebApplicationSampleTest2.Controllers
//{
//    public class DashBordController : Controller
//    {
//        private readonly Ipatient _patientRepo;
//        private readonly IOPDAppointment _appointmentRepo;
//        private readonly IDoctor _docotrRepo;
//        private readonly IHospital _hospitalRepo;
//        private readonly IUser _userRepo;
//        public DashBordController(Ipatient ipatient, IOPDAppointment oPDAppointment, IDoctor docotrRepo, IHospital hospitalRepo, IUser userRepo)
//        {
//            _patientRepo = ipatient;
//            _appointmentRepo = oPDAppointment;
//            _docotrRepo = docotrRepo;
//            _hospitalRepo = hospitalRepo;
//            _userRepo = userRepo;

//        }

//        public IActionResult Index()
//        {
//            var role = HttpContext.Session.GetString("UserRole");
//            return View();
//        }

//        //public IActionResult Dash()
//        //{
//        //    var role = HttpContext.Session.GetString("UserRole");
//        //    return View();
//        //}

//        //    [HttpGet]
//        //    public IActionResult GetDashboardCounts()
//        //    {
//        //        var hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//        //        var subHospitalId = HttpContext.Session.GetInt32("SubHospitalId") ?? 0;

//        //        var totalDoct = _docotrRepo.GetAllDoctor(hospitalId, subHospitalId);

//        //        var allPatients = _patientRepo.GetAllPatients(hospitalId, subHospitalId);
//        //        var allAppointments = _appointmentRepo.GetAllAppointments(hospitalId, subHospitalId);

//        //        var today = DateTime.Today;
//        //        var yesterday = today.AddDays(-1);
//        //        var tomorrow = today.AddDays(1);
//        //        var todayStart = DateTime.Today;
//        //        var todayEnd = todayStart.AddDays(1);

//        //        var yesterdayStart = todayStart.AddDays(-1);
//        //        var yesterdayEnd = todayStart;

//        //        var tomorrowStart = todayStart.AddDays(1);
//        //        var tomorrowEnd = todayStart.AddDays(2);

//        //        // Today booked doctors
//        //        var todayDoctorIds = allAppointments
//        //            .Where(a => a.AppointmentDate >= todayStart &&
//        //                        a.AppointmentDate < todayEnd)
//        //            .Select(a => a.DoctorId)
//        //            .Distinct()
//        //            .ToList();

//        //        // Yesterday booked doctors
//        //        var yesterdayDoctorIds = allAppointments
//        //            .Where(a => a.AppointmentDate >= yesterdayStart &&
//        //                        a.AppointmentDate < yesterdayEnd)
//        //            .Select(a => a.DoctorId)
//        //            .Distinct()
//        //            .ToList();

//        //        // Tomorrow booked doctors
//        //        var tomorrowDoctorIds = allAppointments
//        //            .Where(a => a.AppointmentDate >= tomorrowStart &&
//        //                        a.AppointmentDate < tomorrowEnd)
//        //            .Select(a => a.DoctorId)
//        //            .Distinct()
//        //            .ToList();




//        //        //added by me
//        //        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
//        //        var startOfMonth = new DateTime(today.Year, today.Month, 1);
//        //        var startOfYear = new DateTime(today.Year, 1, 1);


//        //        var result = new
//        //        {

//        //            TotalPatients = allPatients.Count,
//        //            TotalAppointments = allAppointments.Count,
//        //            TotalDoctors = totalDoct.Count,

//        //            TodayAvailableDoctors = totalDoct
//        //.Count(d => !todayDoctorIds.Contains(d.Doctor_Id)),

//        //            YesterdayAvailableDoctors = totalDoct
//        //.Count(d => !yesterdayDoctorIds.Contains(d.Doctor_Id)),

//        //            TomorrowAvailableDoctors = totalDoct
//        //.Count(d => !tomorrowDoctorIds.Contains(d.Doctor_Id)),


//        //            CompletedAppointments = allAppointments.Count(a => a.Status == "OPD Completed"),
//        //            PendingAppointments = allAppointments.Count(a => a.Status == "Pending"),

//        //            //today 
//        //            TodayCompleted = allAppointments .Count(a => a.AppointmentDate.Date == today && a.Status == "OPD Completed"),

//        //            TodayPending = allAppointments .Count(a => a.AppointmentDate.Date == today  && a.Status == "Pending"),
//        //            //ye

//        //            TodayAppointments = allAppointments.Count(a => a.AppointmentDate.Date == today),
//        //            YesterdayAppointments = allAppointments.Count(a => a.AppointmentDate.Date == yesterday),
//        //            TomorrowAppointments = allAppointments.Count(a => a.AppointmentDate.Date == tomorrow),

//        //            //added by me
//        //            WeeklyAppointments = allAppointments.Count(a => a.AppointmentDate.Date >= startOfWeek),
//        //            MonthlyAppointments = allAppointments.Count(a => a.AppointmentDate.Date >= startOfMonth),
//        //            YearlyAppointments = allAppointments.Count(a => a.AppointmentDate.Date >= startOfYear),





//        //            TodayAppointmentList = allAppointments.Where(a => a.AppointmentDate.Date == today)
//        //                                                  .Select(a => new { a.Id, a.PatientId, a.DoctorId, a.AppointmentDate, a.Status })
//        //                                                  .ToList(),
//        //            YesterdayAppointmentList = allAppointments.Where(a => a.AppointmentDate.Date == yesterday)
//        //                                                      .Select(a => new { a.Id, a.PatientId, a.DoctorId, a.AppointmentDate, a.Status })
//        //                                                      .ToList(),
//        //            YesterdayCompleted = allAppointments .Count(a => a.AppointmentDate.Date == yesterday  && a.Status == "OPD Completed"),

//        //            YesterdayPending = allAppointments  .Count(a => a.AppointmentDate.Date == yesterday && a.Status == "Pending"),

//        //            TomorrowAppointmentList = allAppointments.Where(a => a.AppointmentDate.Date == tomorrow)
//        //                                                     .Select(a => new { a.Id, a.PatientId, a.DoctorId, a.AppointmentDate, a.Status })
//        //                                                     .ToList(),
//        //            TomorrowCompleted = allAppointments
//        //.Count(a => a.AppointmentDate.Date == tomorrow
//        //         && a.Status == "OPD Completed"),

//        //            TomorrowPending = allAppointments
//        //.Count(a => a.AppointmentDate.Date == tomorrow
//        //         && a.Status == "Pending")

//        //        };

//        //        return Json(result);
//        //    }

//        [HttpGet]
//        public IActionResult GetDashboardCounts()
//        {
//            var hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
//            var subHospitalId = HttpContext.Session.GetInt32("SubHospitalId") ;

//            var totalDoct = _docotrRepo.GetAllDoctor(hospitalId, subHospitalId);
//            var allPatients = _patientRepo.GetAllPatients(hospitalId, subHospitalId);
//            var allAppointments = _appointmentRepo.GetAllAppointments(hospitalId, subHospitalId);

//            // ===== DATE RANGES =====
//            var todayStart = DateTime.Today;
//            var todayEnd = todayStart.AddDays(1);

//            var yesterdayStart = todayStart.AddDays(-1);
//            var yesterdayEnd = todayStart;

//            var tomorrowStart = todayStart.AddDays(1);
//            var tomorrowEnd = todayStart.AddDays(2);

//            // ===== BOOKED DOCTORS =====
//            var todayDoctorIds = allAppointments
//                .Where(a => a.AppointmentDate >= todayStart && a.AppointmentDate < todayEnd)
//                .Select(a => a.DoctorId)
//                .Distinct()
//                .ToList();

//            var yesterdayDoctorIds = allAppointments
//                .Where(a => a.AppointmentDate >= yesterdayStart && a.AppointmentDate < yesterdayEnd)
//                .Select(a => a.DoctorId)
//                .Distinct()
//                .ToList();

//            var tomorrowDoctorIds = allAppointments
//                .Where(a => a.AppointmentDate >= tomorrowStart && a.AppointmentDate < tomorrowEnd)
//                .Select(a => a.DoctorId)
//                .Distinct()
//                .ToList();

//            // ===== WEEK / MONTH / YEAR =====
//            var startOfWeek = todayStart.AddDays(-(int)todayStart.DayOfWeek);
//            var startOfMonth = new DateTime(todayStart.Year, todayStart.Month, 1);
//            var startOfYear = new DateTime(todayStart.Year, 1, 1);

//            var result = new
//            {
//                // TOP COUNTS
//                TotalPatients = allPatients.Count,
//                TotalAppointments = allAppointments.Count,
//                TotalDoctors = totalDoct.Count,

//                CompletedAppointments = allAppointments.Count(a => a.Status == "OPD Completed"),
//                PendingAppointments = allAppointments.Count(a => a.Status == "Pending"),

//                // AVAILABLE DOCTORS
//                TodayAvailableDoctors = totalDoct.Count(d => !todayDoctorIds.Contains(d.Doctor_Id)),
//                YesterdayAvailableDoctors = totalDoct.Count(d => !yesterdayDoctorIds.Contains(d.Doctor_Id)),
//                TomorrowAvailableDoctors = totalDoct.Count(d => !tomorrowDoctorIds.Contains(d.Doctor_Id)),

//                // TODAY
//                TodayAppointments = allAppointments.Count(a => a.AppointmentDate >= todayStart && a.AppointmentDate < todayEnd),
//                TodayCompleted = allAppointments.Count(a => a.AppointmentDate >= todayStart && a.AppointmentDate < todayEnd && a.Status == "OPD Completed"),
//                TodayPending = allAppointments.Count(a => a.AppointmentDate >= todayStart && a.AppointmentDate < todayEnd && a.Status == "Pending"),

//                // YESTERDAY
//                YesterdayAppointments = allAppointments.Count(a => a.AppointmentDate >= yesterdayStart && a.AppointmentDate < yesterdayEnd),
//                YesterdayCompleted = allAppointments.Count(a => a.AppointmentDate >= yesterdayStart && a.AppointmentDate < yesterdayEnd && a.Status == "OPD Completed"),
//                YesterdayPending = allAppointments.Count(a => a.AppointmentDate >= yesterdayStart && a.AppointmentDate < yesterdayEnd && a.Status == "Pending"),

//                // TOMORROW
//                TomorrowAppointments = allAppointments.Count(a => a.AppointmentDate >= tomorrowStart && a.AppointmentDate < tomorrowEnd),
//                TomorrowCompleted = allAppointments.Count(a => a.AppointmentDate >= tomorrowStart && a.AppointmentDate < tomorrowEnd && a.Status == "OPD Completed"),
//                TomorrowPending = allAppointments.Count(a => a.AppointmentDate >= tomorrowStart && a.AppointmentDate < tomorrowEnd && a.Status == "Pending"),

//                // WEEK / MONTH / YEAR
//                WeeklyAppointments = allAppointments.Count(a => a.AppointmentDate >= startOfWeek),
//                MonthlyAppointments = allAppointments.Count(a => a.AppointmentDate >= startOfMonth),
//                YearlyAppointments = allAppointments.Count(a => a.AppointmentDate >= startOfYear),

//                // LISTS
//                TodayAppointmentList = allAppointments
//                    .Where(a => a.AppointmentDate >= todayStart && a.AppointmentDate < todayEnd)
//                    .Select(a => new { a.Id, a.PatientId, a.DoctorId, a.AppointmentDate, a.Status })
//                    .ToList(),

//                YesterdayAppointmentList = allAppointments
//                    .Where(a => a.AppointmentDate >= yesterdayStart && a.AppointmentDate < yesterdayEnd)
//                    .Select(a => new { a.Id, a.PatientId, a.DoctorId, a.AppointmentDate, a.Status })
//                    .ToList(),

//                TomorrowAppointmentList = allAppointments
//                    .Where(a => a.AppointmentDate >= tomorrowStart && a.AppointmentDate < tomorrowEnd)
//                    .Select(a => new { a.Id, a.PatientId, a.DoctorId, a.AppointmentDate, a.Status })
//                    .ToList()
//            };

//            return Json(result);
//        }
//        public IActionResult Dash()
//        {
//            var role = HttpContext.Session.GetString("UserRole");
//            if (role != "SuperAdmin")
//                return Unauthorized();

//            var mainDt = _hospitalRepo.GetMainHospitalDashboard();
//            var subDt = _hospitalRepo.GetSubHospitalDashboard();

//            var vm = new SuperAdminDashboardVM();

//            vm.TotalHospitals = mainDt.Rows.Count;
//            vm.TotalSubHospitals = subDt.Rows.Count;
//            vm.TotalUsers = 0;

//            foreach (DataRow row in mainDt.Rows)
//            {
//                var hospital = new HospitalInfo
//                {
//                    HospitalId = Convert.ToInt32(row["HospitalId"]),
//                    HospitalName = row["HospitalName"].ToString(),
//                    UserCount = Convert.ToInt32(row["UserCount"]),
//                    SubHospitalCount = 0
//                };

//                vm.TotalUsers += hospital.UserCount;

//                var relatedSubs = subDt.AsEnumerable()
//                    .Where(r => Convert.ToInt32(r["ParentHospitalId"]) == hospital.HospitalId);

//                foreach (var subRow in relatedSubs)
//                {
//                    hospital.SubHospitals.Add(new SubHospitalInfo
//                    {
//                        SubHospitalId = Convert.ToInt32(subRow["SubHospitalId"]),
//                        SubHospitalName = subRow["SubHospitalName"].ToString(),
//                        UserCount = Convert.ToInt32(subRow["UserCount"])
//                    });

//                    hospital.SubHospitalCount++;
//                }

//                vm.Hospitals.Add(hospital);
//            }

//            return View(vm);
//        }


//    }
//}
