using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace WebApplicationSampleTest2.Repository
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly string _connectionString;

        public DashboardRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        /// <summary>
        /// Returns ALL dashboard counts using inline SQL with multiple
        /// result sets — eliminates the 3 massive in-memory loads that
        /// were previously done in DashBordController.GetDashboardCounts().
        ///
        /// Uses a single DB round-trip with multiple SELECT queries
        /// separated by semicolons (MySqlDataReader supports this) to
        /// avoid creating a permanent stored procedure.
        /// </summary>
        public DashboardCountsVM GetDashboardCounts(int hospitalId, int? subHospitalId)
        {
            var vm = new DashboardCountsVM();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // Build the sub-hospital filter fragment (prevents SQL injection
                // — only two possible values: IS NULL or = value).
                string subFilter = subHospitalId.HasValue
                    ? "AND a.SubHospitalId = @SubHospitalId"
                    : "AND (a.SubHospitalId IS NULL OR a.SubHospitalId = 0)";

                string subFilterPat = subHospitalId.HasValue
                    ? "AND p.SubHospital_Id = @SubHospitalId"
                    : "AND (p.SubHospital_Id IS NULL OR p.SubHospital_Id = 0)";

                string sql = @"
-- ===== RS1: Top-level aggregated counts =====
SELECT
    -- Counts
    (SELECT COUNT(*) FROM tbl_patient p WHERE p.Hospital_Id = @HospitalId " + subFilterPat.Replace("a.", "") + @") AS TotalPatients,
    (SELECT COUNT(*) FROM doctor d WHERE d.Hospital_Id = @HospitalId AND d.IsActive = 1) AS TotalDoctors,
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1) AS TotalAppointments,
    -- Status counts
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1 AND a.Status = 'OPD Completed') AS CompletedAppointments,
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1 AND a.Status = 'Pending') AS PendingAppointments,
    -- Today
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1 AND DATE(a.AppointmentDate) = CURDATE()) AS TodayAppointments,
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1 AND DATE(a.AppointmentDate) = CURDATE() AND a.Status = 'OPD Completed') AS TodayCompleted,
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1 AND DATE(a.AppointmentDate) = CURDATE() AND a.Status = 'Pending') AS TodayPending,
    -- Yesterday
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1 AND DATE(a.AppointmentDate) = DATE_SUB(CURDATE(), INTERVAL 1 DAY)) AS YesterdayAppointments,
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1 AND DATE(a.AppointmentDate) = DATE_SUB(CURDATE(), INTERVAL 1 DAY) AND a.Status = 'OPD Completed') AS YesterdayCompleted,
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1 AND DATE(a.AppointmentDate) = DATE_SUB(CURDATE(), INTERVAL 1 DAY) AND a.Status = 'Pending') AS YesterdayPending,
    -- Tomorrow
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1 AND DATE(a.AppointmentDate) = DATE_ADD(CURDATE(), INTERVAL 1 DAY)) AS TomorrowAppointments,
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1 AND DATE(a.AppointmentDate) = DATE_ADD(CURDATE(), INTERVAL 1 DAY) AND a.Status = 'OPD Completed') AS TomorrowCompleted,
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1 AND DATE(a.AppointmentDate) = DATE_ADD(CURDATE(), INTERVAL 1 DAY) AND a.Status = 'Pending') AS TomorrowPending,
    -- Available doctors (total doctors minus those booked each day)
    (SELECT COUNT(*) FROM doctor d WHERE d.Hospital_Id = @HospitalId AND d.IsActive = 1 AND d.Doctor_Id NOT IN (SELECT a2.DoctorId FROM opdappointment a2 WHERE a2.HospitalId = @HospitalId " + subFilter.Replace("a.", "a2.") + @" AND DATE(a2.AppointmentDate) = CURDATE() AND a2.IsActive = 1)) AS TodayAvailableDoctors,
    (SELECT COUNT(*) FROM doctor d WHERE d.Hospital_Id = @HospitalId AND d.IsActive = 1 AND d.Doctor_Id NOT IN (SELECT a2.DoctorId FROM opdappointment a2 WHERE a2.HospitalId = @HospitalId " + subFilter.Replace("a.", "a2.") + @" AND DATE(a2.AppointmentDate) = DATE_SUB(CURDATE(), INTERVAL 1 DAY) AND a2.IsActive = 1)) AS YesterdayAvailableDoctors,
    (SELECT COUNT(*) FROM doctor d WHERE d.Hospital_Id = @HospitalId AND d.IsActive = 1 AND d.Doctor_Id NOT IN (SELECT a2.DoctorId FROM opdappointment a2 WHERE a2.HospitalId = @HospitalId " + subFilter.Replace("a.", "a2.") + @" AND DATE(a2.AppointmentDate) = DATE_ADD(CURDATE(), INTERVAL 1 DAY) AND a2.IsActive = 1)) AS TomorrowAvailableDoctors,
    -- Weekly / Monthly / Yearly
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1 AND a.AppointmentDate >= DATE_SUB(CURDATE(), INTERVAL WEEKDAY(CURDATE()) DAY)) AS WeeklyAppointments,
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1 AND YEAR(a.AppointmentDate) = YEAR(CURDATE()) AND MONTH(a.AppointmentDate) = MONTH(CURDATE())) AS MonthlyAppointments,
    (SELECT COUNT(*) FROM opdappointment a WHERE a.HospitalId = @HospitalId " + subFilter + @" AND a.IsActive = 1 AND YEAR(a.AppointmentDate) = YEAR(CURDATE())) AS YearlyAppointments;

-- ===== RS2: Monthly chart data (12 months) =====
SELECT
    MONTH(a.AppointmentDate) AS MonthNum,
    COUNT(*) AS MonthTotal,
    SUM(CASE WHEN a.Status = 'OPD Completed' THEN 1 ELSE 0 END) AS MonthCompleted,
    SUM(CASE WHEN a.Status = 'Pending' THEN 1 ELSE 0 END) AS MonthPending
FROM opdappointment a
WHERE a.HospitalId = @HospitalId " + subFilter + @"
  AND a.IsActive = 1
  AND YEAR(a.AppointmentDate) = YEAR(CURDATE())
GROUP BY MONTH(a.AppointmentDate)
ORDER BY MonthNum;

-- ===== RS3: Today's appointment list =====
SELECT Id, PatientId, DoctorId, AppointmentDate, Status
FROM opdappointment a
WHERE a.HospitalId = @HospitalId " + subFilter + @"
  AND a.IsActive = 1
  AND DATE(a.AppointmentDate) = CURDATE()
ORDER BY a.AppointmentTime;

-- ===== RS4: Yesterday's appointment list =====
SELECT Id, PatientId, DoctorId, AppointmentDate, Status
FROM opdappointment a
WHERE a.HospitalId = @HospitalId " + subFilter + @"
  AND a.IsActive = 1
  AND DATE(a.AppointmentDate) = DATE_SUB(CURDATE(), INTERVAL 1 DAY)
ORDER BY a.AppointmentTime;

-- ===== RS5: Tomorrow's appointment list =====
SELECT Id, PatientId, DoctorId, AppointmentDate, Status
FROM opdappointment a
WHERE a.HospitalId = @HospitalId " + subFilter + @"
  AND a.IsActive = 1
  AND DATE(a.AppointmentDate) = DATE_ADD(CURDATE(), INTERVAL 1 DAY)
ORDER BY a.AppointmentTime;
";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);
                if (subHospitalId.HasValue)
                    cmd.Parameters.AddWithValue("@SubHospitalId", subHospitalId.Value);

                using var reader = cmd.ExecuteReader();

                // ============ RS1: Top-level counts ============
                if (reader.Read())
                {
                    vm.TotalPatients = SafeInt(reader, "TotalPatients");
                    vm.TotalDoctors = SafeInt(reader, "TotalDoctors");
                    vm.TotalAppointments = SafeInt(reader, "TotalAppointments");
                    vm.CompletedAppointments = SafeInt(reader, "CompletedAppointments");
                    vm.PendingAppointments = SafeInt(reader, "PendingAppointments");

                    vm.TodayAppointments = SafeInt(reader, "TodayAppointments");
                    vm.TodayCompleted = SafeInt(reader, "TodayCompleted");
                    vm.TodayPending = SafeInt(reader, "TodayPending");

                    vm.YesterdayAppointments = SafeInt(reader, "YesterdayAppointments");
                    vm.YesterdayCompleted = SafeInt(reader, "YesterdayCompleted");
                    vm.YesterdayPending = SafeInt(reader, "YesterdayPending");

                    vm.TomorrowAppointments = SafeInt(reader, "TomorrowAppointments");
                    vm.TomorrowCompleted = SafeInt(reader, "TomorrowCompleted");
                    vm.TomorrowPending = SafeInt(reader, "TomorrowPending");

                    vm.TodayAvailableDoctors = SafeInt(reader, "TodayAvailableDoctors");
                    vm.YesterdayAvailableDoctors = SafeInt(reader, "YesterdayAvailableDoctors");
                    vm.TomorrowAvailableDoctors = SafeInt(reader, "TomorrowAvailableDoctors");

                    vm.WeeklyAppointments = SafeInt(reader, "WeeklyAppointments");
                    vm.MonthlyAppointments = SafeInt(reader, "MonthlyAppointments");
                    vm.YearlyAppointments = SafeInt(reader, "YearlyAppointments");
                }

                // ============ RS2: Monthly chart data (12 rows) ============
                if (reader.NextResult())
                {
                    while (reader.Read())
                    {
                        int month = SafeInt(reader, "MonthNum");       // 1..12
                        int total = SafeInt(reader, "MonthTotal");
                        int comp = SafeInt(reader, "MonthCompleted");
                        int pend = SafeInt(reader, "MonthPending");

                        if (month >= 1 && month <= 12)
                        {
                            // 0-indexed list
                            vm.MonthlyTotal[month - 1] = total;
                            vm.MonthlyCompleted[month - 1] = comp;
                            vm.MonthlyPending[month - 1] = pend;
                        }
                    }
                }

                // ============ RS3: Today's appointment list ============
                if (reader.NextResult())
                {
                    while (reader.Read())
                    {
                        vm.TodayAppointmentList.Add(new
                        {
                            Id = SafeInt(reader, "Id"),
                            PatientId = SafeInt(reader, "PatientId"),
                            DoctorId = SafeInt(reader, "DoctorId"),
                            AppointmentDate = SafeDateTime(reader, "AppointmentDate"),
                            Status = SafeString(reader, "Status")
                        });
                    }
                }

                // ============ RS4: Yesterday's appointment list ============
                if (reader.NextResult())
                {
                    while (reader.Read())
                    {
                        vm.YesterdayAppointmentList.Add(new
                        {
                            Id = SafeInt(reader, "Id"),
                            PatientId = SafeInt(reader, "PatientId"),
                            DoctorId = SafeInt(reader, "DoctorId"),
                            AppointmentDate = SafeDateTime(reader, "AppointmentDate"),
                            Status = SafeString(reader, "Status")
                        });
                    }
                }

                // ============ RS5: Tomorrow's appointment list ============
                if (reader.NextResult())
                {
                    while (reader.Read())
                    {
                        vm.TomorrowAppointmentList.Add(new
                        {
                            Id = SafeInt(reader, "Id"),
                            PatientId = SafeInt(reader, "PatientId"),
                            DoctorId = SafeInt(reader, "DoctorId"),
                            AppointmentDate = SafeDateTime(reader, "AppointmentDate"),
                            Status = SafeString(reader, "Status")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log and return zeros — never crash the dashboard
                System.Diagnostics.Debug.WriteLine("DashboardRepo ERROR: " + ex.Message);
            }

            return vm;
        }

        // ── Safe helpers ──────────────────────────────────────────
        private static int SafeInt(IDataRecord r, string col)
        {
            try { return r[col] == DBNull.Value ? 0 : Convert.ToInt32(r[col]); }
            catch { return 0; }
        }

        private static string SafeString(IDataRecord r, string col)
        {
            try { return r[col] == DBNull.Value ? "" : r[col].ToString(); }
            catch { return ""; }
        }

        private static DateTime SafeDateTime(IDataRecord r, string col)
        {
            try { return r[col] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r[col]); }
            catch { return DateTime.MinValue; }
        }
    }
}

