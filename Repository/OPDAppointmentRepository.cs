using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class OPDAppointmentRepository:IOPDAppointment
    {
        private readonly string _connectionString;

        public OPDAppointmentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }
        // Create appointment
        public void CreateAppointment(OPDAppointmentModel appointment, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPDAppointment_Insert", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_PatientId", appointment.PatientId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                //cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object?)subHospitalId ?? DBNull.Value);

                cmd.Parameters.AddWithValue("p_DoctorId", appointment.DoctorId);
                cmd.Parameters.AddWithValue("p_AppointmentDate", appointment.AppointmentDate);
                cmd.Parameters.AddWithValue("p_AppointmentTime", appointment.AppointmentTime);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
           
        }

        // Update appointment
        public int UpdateAppointment(OPDAppointmentModel appointment, int hospitalId, int? subHospitalId)
        {

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPDAppointment_Update", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_Id", appointment.Id);
                cmd.Parameters.AddWithValue("p_PatientId", appointment.PatientId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object?)subHospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_DoctorId", appointment.DoctorId);
                cmd.Parameters.AddWithValue("p_AppointmentDate", appointment.AppointmentDate);
                cmd.Parameters.AddWithValue("p_AppointmentTime", appointment.AppointmentTime);
                cmd.Parameters.AddWithValue("p_IsActive", appointment.IsActive);

                conn.Open();
                return cmd.ExecuteNonQuery(); // returns number of rows affected
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
           
        }

        // Soft delete appointment
        public void DeleteAppointment(int appointmentId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPDAppointment_Delete", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_Id", appointmentId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                //cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object?)subHospitalId ?? DBNull.Value);



                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
           
        }

        // Get appointment by Id
        public OPDAppointmentModel? GetAppointmentById(int appointmentId, int hospitalId, int? subHospitalId)
        {
            OPDAppointmentModel? appointment = null;
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPDAppointment_GetById", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_Id", appointmentId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                //cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object?)subHospitalId ?? DBNull.Value);


                conn.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    appointment = new OPDAppointmentModel
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        PatientId = Convert.ToInt32(reader["PatientId"]),
                        DoctorId = Convert.ToInt32(reader["DoctorId"]),
                        HospitalId = Convert.ToInt32(reader["HospitalId"]),
                        //SubHospitalId = Convert.ToInt32(reader["SubHospitalId"]),
                        SubHospitalId = reader["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SubHospitalId"]),
                        AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]),
                        AppointmentTime = TimeSpan.Parse(reader["AppointmentTime"].ToString()),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        // ✅ FIX: Read IsWalkIn so walk-in saves redirect back to
                        // WalkInConsultation/Index instead of OPDAppointment/Index
                        IsWalkIn = ColumnExists(reader, "IsWalkIn")
                                   && reader["IsWalkIn"] != DBNull.Value
                                   && Convert.ToBoolean(reader["IsWalkIn"])
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
            return appointment;
        }

        // Get all appointments
        public List<OPDAppointmentModel> GetAllAppointments(int hospitalId, int? subHospitalId)
        {
            var list = new List<OPDAppointmentModel>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPDAppointment_GetAll", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                //cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object?)subHospitalId ?? DBNull.Value);


                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new OPDAppointmentModel
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        PatientId = Convert.ToInt32(reader["PatientId"]),
                        DoctorId = Convert.ToInt32(reader["DoctorId"]),
                        HospitalId = Convert.ToInt32(reader["HospitalId"]),
                        //SubHospitalId = Convert.ToInt32(reader["SubHospitalId"]),
                        SubHospitalId = reader["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SubHospitalId"]),

                        AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]),
                        AppointmentTime = TimeSpan.Parse(reader["AppointmentTime"].ToString()),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        Status = reader["Status"].ToString(),
                        IsWalkIn = reader["IsWalkIn"] != DBNull.Value && Convert.ToBoolean(reader["IsWalkIn"])
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
            return list;
        }

// ══════════════════════════════════════════════════════════
        //  GET APPOINTMENTS PAGED — single JOIN query + COUNT + LIMIT.
        //  Replaces the old Index flow that loaded EVERY appointment,
        //  EVERY patient and EVERY doctor into memory, joined them in
        //  LINQ, then filtered/paginated in C#. That caused the 30s
        //  page loads and slow pagination. Now only the current page's
        //  rows are fetched from the DB.
        // ══════════════════════════════════════════════════════════
        public List<OPDAppointmentModel> GetAppointmentsPaged(
            int hospitalId, int? subHospitalId, DateTime date, string search,
            int page, int pageSize, out int totalRecords, out int todayAppointmentCount)
        {
            var list = new List<OPDAppointmentModel>();
            totalRecords = 0;
            todayAppointmentCount = 0;

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            int offset = (page - 1) * pageSize;

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // Sub-hospital filter — only two safe forms, no injection risk.
                string subFilter = subHospitalId.HasValue
                    ? "AND a.SubHospitalId = @SubHospitalId"
                    : "AND (a.SubHospitalId IS NULL OR a.SubHospitalId = 0)";
                string subFilterC = subHospitalId.HasValue
                    ? "AND a.SubHospitalId = @SubHospitalId"
                    : "AND (a.SubHospitalId IS NULL OR a.SubHospitalId = 0)";

                string searchSql = "";
                if (!string.IsNullOrWhiteSpace(search))
                {
                    searchSql = @" AND (
                            LOWER(CONCAT(COALESCE(p.FirstName,''), ' ', COALESCE(p.LastName,''))) LIKE @Search OR
                            LOWER(COALESCE(p.PhoneNumber,'')) LIKE @Search OR
                            LOWER(CONCAT(COALESCE(d.FirstName,''), ' ', COALESCE(d.LastName,''))) LIKE @Search
                        )";
                }

                // 1) Today's total count (for the header badge).
                using (var todayCmd = new MySqlCommand(
                    @"SELECT COUNT(*) FROM opdappointment a
                      WHERE a.HospitalId = @HospitalId " + subFilterC + @"
                        AND a.IsActive = 1 AND DATE(a.AppointmentDate) = CURDATE()", conn))
                {
                    todayCmd.Parameters.AddWithValue("@HospitalId", hospitalId);
                    todayCmd.Parameters.AddWithValue("@SubHospitalId",
                        subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);
                    todayAppointmentCount = Convert.ToInt32(todayCmd.ExecuteScalar());
                }

                // 2) Total matching records for the selected date (fast COUNT).
                using (var countCmd = new MySqlCommand(
                    @"SELECT COUNT(*) FROM opdappointment a
                      INNER JOIN tbl_patient p ON p.Id = a.PatientId
                      LEFT JOIN doctor d ON d.Doctor_Id = a.DoctorId
                      WHERE a.HospitalId = @HospitalId " + subFilter + @"
                        AND a.IsActive = 1 AND a.IsWalkIn = 0
                        AND DATE(a.AppointmentDate) = @AppointmentDate" + searchSql, conn))
                {
                    countCmd.Parameters.AddWithValue("@HospitalId", hospitalId);
                    countCmd.Parameters.AddWithValue("@SubHospitalId",
                        subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);
                    countCmd.Parameters.AddWithValue("@AppointmentDate", date.Date);
                    if (!string.IsNullOrWhiteSpace(search))
                        countCmd.Parameters.AddWithValue("@Search", "%" + search.Trim().ToLower() + "%");

                    totalRecords = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                // 3) Current page rows only — JOIN + WHERE + LIMIT/OFFSET.
                string sql =
                    @"SELECT a.Id, a.PatientId, a.DoctorId, a.HospitalId, a.SubHospitalId,
                             a.AppointmentDate, a.AppointmentTime, a.IsActive, a.Status, a.IsWalkIn,
                             CONCAT(COALESCE(p.FirstName,''), ' ', COALESCE(p.LastName,'')) AS PatientName,
                             COALESCE(p.PhoneNumber,'') AS MobileNo,
                             CONCAT(COALESCE(d.FirstName,''), ' ', COALESCE(d.LastName,'')) AS DoctorName
                      FROM opdappointment a
                      INNER JOIN tbl_patient p ON p.Id = a.PatientId
                      LEFT JOIN doctor d ON d.Doctor_Id = a.DoctorId
                      WHERE a.HospitalId = @HospitalId " + subFilter + @"
                        AND a.IsActive = 1 AND a.IsWalkIn = 0
                        AND DATE(a.AppointmentDate) = @AppointmentDate" + searchSql + @"
                      ORDER BY a.AppointmentTime, a.Id
                      LIMIT @PageSize OFFSET @Offset";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("@SubHospitalId",
                    subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@AppointmentDate", date.Date);
                cmd.Parameters.AddWithValue("@Search",
                    string.IsNullOrWhiteSpace(search) ? "%" : "%" + search.Trim().ToLower() + "%");
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new OPDAppointmentModel
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        PatientId = Convert.ToInt32(reader["PatientId"]),
                        DoctorId = Convert.ToInt32(reader["DoctorId"]),
                        HospitalId = Convert.ToInt32(reader["HospitalId"]),
                        SubHospitalId = reader["SubHospitalId"] == DBNull.Value
                            ? (int?)null : Convert.ToInt32(reader["SubHospitalId"]),
                        AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]),
                        AppointmentTime = TimeSpan.Parse(reader["AppointmentTime"].ToString()),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        Status = reader["Status"]?.ToString(),
                        IsWalkIn = reader["IsWalkIn"] != DBNull.Value && Convert.ToBoolean(reader["IsWalkIn"]),
                        PatientName = reader["PatientName"]?.ToString(),
                        MobileNo = reader["MobileNo"]?.ToString(),
                        DoctorName = reader["DoctorName"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting paged appointments", ex);
            }

            return list;
        }

        public void UpdateStatus(int appointmentId, int hospitalId, int? subHospitalId, string status)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPDAppointment_UpdateStatus", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                // 🔹 Add parameters
                cmd.Parameters.AddWithValue("p_AppointmentId", appointmentId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object?)subHospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_Status", status);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
            
        }

        public List<OPDMedicineVM> GetMedicinesByOPDId(int opdId)
        {
            List<OPDMedicineVM> list = new List<OPDMedicineVM>();
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_GetMedicinesByOPDId", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_OPDId", opdId);

                        con.Open();

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new OPDMedicineVM
                                {
                                    MedicineId = Convert.ToInt32(reader["Medicine_Id"]),
                                    MedicineName = reader["MedicineName"].ToString(),
                                    Morning = Convert.ToInt32(reader["Morning"]),
                                    Afternoon = Convert.ToInt32(reader["Afternoon"]),
                                    Evening = Convert.ToInt32(reader["Evening"]),
                                    Days = Convert.ToInt32(reader["Days"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
            return list;
        }

public List<OPD> GetPatientFullHistory(int patientId, int hospitalId, int? subHospitalId)
        {
            var opdList = new List<OPD>();
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("GetPatientFullHistory", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PatientId", patientId);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        // 1️⃣ Read OPD visits
                        while (reader.Read())
                        {
                            var opd = new OPD
                            {
                                Id = Convert.ToInt32(reader["OPDId"]),
                                AppointmentId = Convert.ToInt32(reader["AppointmentId"]),
                                AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]),
                                BP = reader["BP"]?.ToString(),
                                Pulse = reader["Pulse"]?.ToString(),
                                Investigation = reader["Investigation"]?.ToString(),
                                ReportDetail = reader["ReportDetail"]?.ToString(),
                                ReportFilePath = reader["ReportFilePath"]?.ToString(),
                                NextAppointmentDate = reader["NextAppointmentDate"] != DBNull.Value
                                                      ? Convert.ToDateTime(reader["NextAppointmentDate"])
                                                      : (DateTime?)null
                            };
                            opdList.Add(opd);
                        }

                        // 2️⃣ Read Symptoms
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                var opd = opdList.Find(x => x.Id == Convert.ToInt32(reader["OPD_Id"]));
                                if (opd != null)
                                {
                                    opd.Symptom.Add(reader["SymptomName"]?.ToString());
                                }
                            }
                        }

                        // 3️⃣ Read Medicines
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                var opd = opdList.Find(x => x.Id == Convert.ToInt32(reader["OPD_Id"]));
                                if (opd != null)
                                {
                                    opd.Medicines.Add(new OPDMedicine
                                    {
                                        MedicineName = reader["MedicineName"]?.ToString(),
                                        MedicineId = 0,
                                        Morning = reader["Morning"]?.ToString(),
                                        Afternoon = reader["Afternoon"]?.ToString(),
                                        Evening = reader["Evening"]?.ToString(),
                                        Days = reader["Days"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Days"])
                                    });
                                }
                            }
                        }
                    }
                }

                // ── ENRICH: doctor name + appointment time/status + diagnoses ──
                if (opdList.Count > 0)
                {
                    var apptIds = opdList.Select(x => x.AppointmentId).Distinct().ToList();
                    var opdIds = opdList.Select(x => x.Id).Distinct().ToList();

                    // Doctor / time / status via appointment table (single query)
                    var apptInfo = new Dictionary<int, (string doctor, TimeSpan time, string status)>();
                    if (apptIds.Count > 0)
                    {
                        string idList = string.Join(",", apptIds);
                        string subFilter = subHospitalId.HasValue
                            ? "AND a.SubHospitalId = @SubHospitalId"
                            : "AND (a.SubHospitalId IS NULL OR a.SubHospitalId = 0)";
                        string sql = "SELECT a.Id, COALESCE(CONCAT(COALESCE(d.FirstName,''), ' ', COALESCE(d.LastName,'')),'') AS DoctorName, " +
                                     "a.AppointmentTime, COALESCE(a.Status,'') AS Status " +
                                     "FROM opdappointment a LEFT JOIN doctor d ON d.Doctor_Id = a.DoctorId " +
                                     "WHERE a.Id IN (" + idList + ") AND a.HospitalId = @HospitalId " + subFilter;
                        using (var conn2 = new MySqlConnection(_connectionString))
                        using (var cmd2 = new MySqlCommand(sql, conn2))
                        {
                            cmd2.Parameters.AddWithValue("@HospitalId", hospitalId);
                            if (subHospitalId.HasValue)
                                cmd2.Parameters.AddWithValue("@SubHospitalId", subHospitalId.Value);
                            conn2.Open();
                            using var r2 = cmd2.ExecuteReader();
                            while (r2.Read())
                            {
                                var time = r2["AppointmentTime"] == DBNull.Value
                                    ? TimeSpan.Zero
                                    : TimeSpan.TryParse(r2["AppointmentTime"]?.ToString(), out var t) ? t : TimeSpan.Zero;
                                apptInfo[Convert.ToInt32(r2["Id"])] = (
                                    r2["DoctorName"]?.ToString() ?? "",
                                    time,
                                    r2["Status"]?.ToString() ?? "");
                            }
                        }
                    }

                    // Diagnoses per OPD (single query)
                    var diagByOpd = new Dictionary<int, List<OPDDiagnosis>>();
                    if (opdIds.Count > 0)
                    {
                        string idList = string.Join(",", opdIds);
                        string sql = "SELECT OPD_Id, Id AS DiagnId, DiagnosisName, Type, Notes FROM opd_diagnosis " +
                                     "WHERE OPD_Id IN (" + idList + ") AND IsActive = 1";
                        try
                        {
                            using var conn3 = new MySqlConnection(_connectionString);
                            using var cmd3 = new MySqlCommand(sql, conn3);
                            conn3.Open();
                            using var r3 = cmd3.ExecuteReader();
                            while (r3.Read())
                            {
                                int opdId = Convert.ToInt32(r3["OPD_Id"]);
                                if (!diagByOpd.ContainsKey(opdId))
                                    diagByOpd[opdId] = new List<OPDDiagnosis>();
                                diagByOpd[opdId].Add(new OPDDiagnosis
                                {
                                    Id = Convert.ToInt32(r3["DiagnId"]),
                                    OPDId = opdId,
                                    DiagnosisName = r3["DiagnosisName"]?.ToString(),
                                    Type = r3["Type"]?.ToString() ?? "Final",
                                    Notes = r3["Notes"]?.ToString()
                                });
                            }
                        }
                        catch
                        {
                            // diagnosis table may not exist in some deployments — ignore
                        }
                    }

                    foreach (var opd in opdList)
                    {
                        if (apptInfo.TryGetValue(opd.AppointmentId, out var ai))
                        {
                            opd.DoctorName = ai.doctor;
                            opd.AppointmentTime = ai.time;
                            opd.AppointmentStatus = ai.status;
                        }
                        if (diagByOpd.TryGetValue(opd.Id, out var diags))
                            opd.Diagnoses = diags;
                    }

                    // Latest first
                    opdList = opdList.OrderByDescending(v => v.AppointmentDate).ThenByDescending(v => v.Id).ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting patient full history", ex);
            }

            return opdList;
        }

        // Helper: safely check whether a column exists in a data reader.
        // Prevents IndexOutOfRange/KeyNotFound crashes when a stored procedure
        // does not return a particular column.
        private static bool ColumnExists(IDataRecord reader, string columnName)
        {
            try
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch
            {
                // If anything goes wrong while inspecting fields, treat as missing.
            }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  PERFORMANCE: BATCH QUERIES
        //  Replace N+1 round trips (per-appointment SP calls) with a single
        //  SQL query using a temp table / JOIN. This is the single biggest
        //  fix for the 4–5 second page/button delays when the list grows.
        // ═══════════════════════════════════════════════════════════════════

        public Dictionary<int, (int opdId, string ipdStatus)> GetOPDWithIPDStatusBatch(
            List<int> appointmentIds, int hospitalId, int? subHospitalId)
        {
            var result = new Dictionary<int, (int, string)>();
            if (appointmentIds == null || appointmentIds.Count == 0) return result;

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                var idList = string.Join(",", appointmentIds);
                string subFilter = subHospitalId.HasValue
                    ? "AND SubHospital_Id = @SubHospitalId"
                    : "AND SubHospital_Id IS NULL";

                string sql = "SELECT o.Id AS OpdId, o.AppointmentId, a.Id AS ApptId " +
                    "FROM opdmaster o " +
                    "INNER JOIN (SELECT AppointmentId, MAX(Id) AS MaxId FROM opdmaster " +
                    "WHERE AppointmentId IN (" + idList + ") AND Hospital_Id = @HospitalId " +
                    subFilter + " GROUP BY AppointmentId) t ON o.Id = t.MaxId " +
                    "INNER JOIN opdappointment a ON a.Id = o.AppointmentId ORDER BY o.AppointmentId";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);
                if (subHospitalId.HasValue)
                    cmd.Parameters.AddWithValue("@SubHospitalId", subHospitalId.Value);

                var opdByAppt = new Dictionary<int, int>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        opdByAppt[Convert.ToInt32(reader["ApptId"])] = Convert.ToInt32(reader["OpdId"]);
                    }
                }

                if (opdByAppt.Count == 0) return result;

                var opdIds = opdByAppt.Values.Distinct().ToList();
                var opdIdList = string.Join(",", opdIds);
                string subFilter2 = subHospitalId.HasValue
                    ? "AND SubHospitalId = @SubHospitalId2"
                    : "AND (SubHospitalId IS NULL OR SubHospitalId IS NULL)";

                string sql2 = "SELECT OPDVisitId, Status FROM ipdadmission " +
                    "WHERE OPDVisitId IN (" + opdIdList + ") " +
                    "AND ParentHospitalId = @HospitalId2 " +
                    subFilter2 + " AND IsActiveAdmission = 1 ORDER BY IPDId DESC";

                var statusByOpd = new Dictionary<int, string>();
                using (var cmd2 = new MySqlCommand(sql2, conn))
                {
                    cmd2.Parameters.AddWithValue("@HospitalId2", hospitalId);
                    if (subHospitalId.HasValue)
                        cmd2.Parameters.AddWithValue("@SubHospitalId2", subHospitalId.Value);

                    using var reader = cmd2.ExecuteReader();
                    while (reader.Read())
                    {
                        int opd = reader.IsDBNull(reader.GetOrdinal("OPDVisitId")) ? 0 : Convert.ToInt32(reader["OPDVisitId"]);
                        string st = reader["Status"]?.ToString() ?? "";
                        if (opd > 0 && !statusByOpd.ContainsKey(opd)) statusByOpd[opd] = st;
                    }
                }

                foreach (var kvp in opdByAppt)
                {
                    statusByOpd.TryGetValue(kvp.Value, out var st);
                    result[kvp.Key] = (kvp.Value, st);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BATCH_FAIL: " + ex.Message);
            }
            return result;
        }

        public Dictionary<int, string> GetSymptomsByOPDIds(List<int> opdIds)
        {
            var result = new Dictionary<int, string>();
            if (opdIds == null || opdIds.Count == 0) return result;

            try
            {
                var idList = string.Join(",", opdIds);
                string sql = "SELECT os.OPD_Id, GROUP_CONCAT(s.SymptomName SEPARATOR ', ') AS SymptomNames " +
                    "FROM opdsymptom os INNER JOIN symptoms s ON s.SymptomId = os.Symptom_Id " +
                    "WHERE os.OPD_Id IN (" + idList + ") AND os.IsActive = 1 GROUP BY os.OPD_Id";

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result[Convert.ToInt32(reader["OPD_Id"])] = reader["SymptomNames"]?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SYMPTOMS_BATCH_FAIL: " + ex.Message);
            }
            return result;
        }

        public int GetTodayTokenNumber(int appointmentId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                string subFilter = subHospitalId.HasValue
                    ? "AND SubHospitalId = @SubHospitalId"
                    : "AND (SubHospitalId IS NULL OR SubHospitalId = 0)";

                string sql = "SELECT Token FROM ( " +
                    "SELECT Id, ROW_NUMBER() OVER ( " +
                    "PARTITION BY AppointmentDate ORDER BY AppointmentDate, AppointmentTime, Id " +
                    ") AS Token FROM opdappointment " +
                    "WHERE AppointmentDate = CURDATE() AND HospitalId = @HospitalId " +
                    subFilter + " AND IsActive = 1 " +
                    ") t WHERE t.Id = @AppointmentId";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
                if (subHospitalId.HasValue)
                    cmd.Parameters.AddWithValue("@SubHospitalId", subHospitalId.Value);

                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TOKEN_FAIL: " + ex.Message);
                return 0;
            }
        }
    }
}
