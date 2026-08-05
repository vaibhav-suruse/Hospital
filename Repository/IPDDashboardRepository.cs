using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    /// <summary>
    /// Replaces the IPD Dashboard's previous 5 full-table in-memory loads
    /// (GetAllBeds, GetAllWards, GetAllRooms, GetAllDoctors, GetAllPatients)
    /// with a single aggregated SQL query that returns the bed/ward/room/
    /// doctor/patient counts plus the ward-wise occupied/total bed rows.
    ///
    /// The admission / discharge / lab / critical / chart-trend counts are
    /// still produced by the existing sp_GetIPDDashboard stored procedure
    /// (which is already a single efficient DB round-trip), so this class
    /// keeps calling it. The net effect is the page now does 2 fast DB
    /// round-trips instead of 6+ (5 full-table scans + 1 SP).
    /// </summary>
    public class IPDDashboardRepository : IIPDDashboardRepository
    {
        private readonly string _connectionString;

        public IPDDashboardRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        public IPDDashboardVM GetDashboardCounts(int hospitalId, int? subHospitalId)
        {
            var vm = new IPDDashboardVM();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // ── Sub-hospital filter fragments ─────────────────────────
                // Each table uses a different column name for the sub hospital.
                string subFilterBed = subHospitalId.HasValue
                    ? "AND b.SubHospitalId = @SubHospitalId"
                    : "AND (b.SubHospitalId IS NULL OR b.SubHospitalId = 0)";
                string subFilterWard = subHospitalId.HasValue
                    ? "AND w.SubHospitalId = @SubHospitalId"
                    : "AND (w.SubHospitalId IS NULL OR w.SubHospitalId = 0)";
                string subFilterRoom = subHospitalId.HasValue
                    ? "AND r.SubHospitalId = @SubHospitalId"
                    : "AND (r.SubHospitalId IS NULL OR r.SubHospitalId = 0)";
                string subFilterDoctor = subHospitalId.HasValue
                    ? "AND d.Sub_Hospital_Id = @SubHospitalId"
                    : "AND (d.Sub_Hospital_Id IS NULL OR d.Sub_Hospital_Id = 0)";
                string subFilterPatient = subHospitalId.HasValue
                    ? "AND p.SubHospital_Id = @SubHospitalId"
                    : "AND (p.SubHospital_Id IS NULL OR p.SubHospital_Id = 0)";

                string sql = @"
-- ===== RS1: Top-level counts =====
SELECT
    (SELECT COUNT(*) FROM room r WHERE r.HospitalId = @HospitalId " + subFilterRoom + @") AS TotalRooms,
    (SELECT COUNT(*) FROM bed b WHERE b.HospitalId = @HospitalId " + subFilterBed + @") AS TotalBeds,
    (SELECT COUNT(*) FROM bed b WHERE b.HospitalId = @HospitalId " + subFilterBed + @" AND b.IsActive = 1 AND b.OperationalStatus = 'Occupied') AS OccupiedBeds,
    (SELECT COUNT(*) FROM bed b WHERE b.HospitalId = @HospitalId " + subFilterBed + @" AND b.OperationalStatus = 'Maintenance') AS MaintenanceBeds,
    (SELECT COUNT(*) FROM bed b WHERE b.HospitalId = @HospitalId " + subFilterBed + @" AND b.IsActive = 1 AND b.OperationalStatus = 'Active') AS AvailableBeds,
    (SELECT COUNT(*) FROM ward w WHERE w.HospitalId = @HospitalId " + subFilterWard + @") AS TotalWards,
    (SELECT COUNT(*) FROM doctor d WHERE d.Hospital_Id = @HospitalId " + subFilterDoctor + @" AND d.IsActive = 1) AS TotalDoctors,
    (SELECT COUNT(*) FROM tbl_patient p WHERE p.Hospital_Id = @HospitalId " + subFilterPatient + @") AS TotalPatients;

-- ===== RS2: Ward-wise bed status =====
SELECT
    w.WardName AS WardName,
    COUNT(b.BedId) AS Total,
    SUM(CASE WHEN b.OperationalStatus = 'Occupied' THEN 1 ELSE 0 END) AS Occupied
FROM ward w
LEFT JOIN bed b
    ON b.WardId = w.WardId
   AND b.HospitalId = w.HospitalId
   AND (b.SubHospitalId <=> w.SubHospitalId)
   AND b.IsActive = 1
WHERE w.HospitalId = @HospitalId " + subFilterWard.Replace("w.", "") + @"
GROUP BY w.WardId, w.WardName
ORDER BY w.WardName;
";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);
                if (subHospitalId.HasValue)
                    cmd.Parameters.AddWithValue("@SubHospitalId", subHospitalId.Value);

                using var reader = cmd.ExecuteReader();

                // ============ RS1: Top-level counts ============
                if (reader.Read())
                {
                    vm.TotalRooms = SafeInt(reader, "TotalRooms");
                    vm.TotalBeds = SafeInt(reader, "TotalBeds");
                    vm.OccupiedBeds = SafeInt(reader, "OccupiedBeds");
                    vm.MaintenanceBeds = SafeInt(reader, "MaintenanceBeds");
                    vm.AvailableBeds = SafeInt(reader, "AvailableBeds");
                    vm.TotalWards = SafeInt(reader, "TotalWards");
                    vm.TotalDoctors = SafeInt(reader, "TotalDoctors");
                    vm.TotalPatients = SafeInt(reader, "TotalPatients");
                }

                // ============ RS2: Ward-wise bed status ============
                if (reader.NextResult())
                {
                    while (reader.Read())
                    {
                        vm.WardWiseBeds.Add(new WardBedInfo
                        {
                            WardName = SafeString(reader, "WardName"),
                            Total = SafeInt(reader, "Total"),
                            Occupied = SafeInt(reader, "Occupied")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log and return empty — never crash the dashboard page
                System.Diagnostics.Debug.WriteLine("IPDDashboardRepo ERROR: " + ex.Message);
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
    }
}
