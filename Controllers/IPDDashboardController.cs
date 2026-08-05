using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using WebApplicationSampleTest2.Repository;
using WebApplicationSampleTest2.Models;


public class IPDDashboardController : Controller
{
    private readonly IIPDDashboardRepository _dashboardRepo;
    private readonly string _connectionString;

    public IPDDashboardController(
        IIPDDashboardRepository dashboardRepo,
        IConfiguration configuration)
    {
        _dashboardRepo = dashboardRepo;
        _connectionString = configuration.GetConnectionString("MySqlConnection");
    }

    public IActionResult Index()
    {
        try
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            // ═══ PERFORMANCE FIX ═══════════════════════════════════════
            // Previously this loaded ALL beds, wards, rooms, doctors and
            // patients into memory (5 full-table scans) then counted them
            // in C# LINQ. Now a single aggregated SQL query returns the
            // bed/ward/room/doctor/patient counts AND the ward-wise bed
            // status in one DB round-trip.
            // ══════════════════════════════════════════════════════════
            var vm = _dashboardRepo.GetDashboardCounts(hospitalId, subHospitalId);

            // Admission / discharge / lab / critical / chart-trend counts
            // are still produced by the existing sp_GetIPDDashboard SP
            // (already a single efficient DB round-trip).
            // SP Data
            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_GetIPDDashboard", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId",
                    subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    // RS1 - Today Admissions
                    if (reader.Read())
                        vm.TodayAdmissions = Convert.ToInt32(reader["TodayAdmissions"]);

                    // RS2 - Today Discharges
                    reader.NextResult();
                    if (reader.Read())
                        vm.TodayDischarges = Convert.ToInt32(reader["TodayDischarges"]);

                    // RS3 - Currently Admitted
                    reader.NextResult();
                    if (reader.Read())
                        vm.CurrentlyAdmitted = Convert.ToInt32(reader["CurrentlyAdmitted"]);

                    // RS4 - Weekly
                    reader.NextResult();
                    if (reader.Read())
                        vm.WeeklyAdmissions = Convert.ToInt32(reader["WeeklyAdmissions"]);

                    // RS5 - Monthly
                    reader.NextResult();
                    if (reader.Read())
                        vm.MonthlyAdmissions = Convert.ToInt32(reader["MonthlyAdmissions"]);

                    // RS6 - Yearly
                    reader.NextResult();
                    if (reader.Read())
                        vm.YearlyAdmissions = Convert.ToInt32(reader["YearlyAdmissions"]);

                    // RS7 - Pending Lab
                    reader.NextResult();
                    if (reader.Read())
                        vm.PendingLab = Convert.ToInt32(reader["PendingLab"]);

                    // RS8 - Critical
                    reader.NextResult();
                    if (reader.Read())
                        vm.CriticalPatients = Convert.ToInt32(reader["CriticalPatients"]);

                    // RS9 - Chart trend
                    reader.NextResult();
                    while (reader.Read())
                    {
                        vm.ChartDates.Add(
                            Convert.ToDateTime(reader["AdmissionDate"]).ToString("dd MMM"));
                        vm.ChartCounts.Add(Convert.ToInt32(reader["Count"]));
                    }
                }
            }

            return View(vm);
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return View(new IPDDashboardVM());
        }
    }
}