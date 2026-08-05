using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class NotificationRepository : INotification
    {
        private readonly string _connectionString;
        private readonly ILogger<NotificationRepository> _logger;

        public NotificationRepository(IConfiguration configuration, ILogger<NotificationRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        // ─────────────────────────────────────────
        // INSERT — called right after AddOPD saves
        // ─────────────────────────────────────────
        public void InsertNotification(MedicineNotificationModel model)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("InsertMedicineNotification", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_PatientId", model.PatientId);
                    cmd.Parameters.AddWithValue("p_PatientName", model.PatientName);
                    cmd.Parameters.AddWithValue("p_OPDId", (object)model.OPDId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("p_AppointmentId", (object)model.AppointmentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("p_DoctorName", model.DoctorName ?? "");
                    cmd.Parameters.AddWithValue("p_MedicineCount", model.MedicineCount);
                    cmd.Parameters.AddWithValue("p_MedicinesSummary", model.MedicinesSummary ?? "");
                    cmd.Parameters.AddWithValue("p_Type", model.Type ?? "OPD");
                    cmd.Parameters.AddWithValue("p_HospitalId", model.HospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);

                    con.Open();
                    cmd.ExecuteNonQuery();

                    _logger.LogInformation(
                        "Notification inserted for Patient: {PatientName}, Medicines: {Count}",
                        model.PatientName, model.MedicineCount);
                }
            }
            catch (Exception ex)
            {
                // Non-blocking — OPD save must not fail because of notification
                _logger.LogError(ex, "Error inserting medicine notification. Patient: {PatientId}", model.PatientId);
            }
        }

        // ─────────────────────────────────────────
        // GET ALL (last 50, ordered newest first)
        // ─────────────────────────────────────────
        public List<MedicineNotificationModel> GetAllNotifications(int hospitalId, int? subHospitalId)
        {
            var list = new List<MedicineNotificationModel>();
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("GetMedicineNotifications", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);

                    con.Open();
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(Map(r));
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error in GetAllNotifications"); }
            return list;
        }

        // ─────────────────────────────────────────
        // GET NEW SINCE TIMESTAMP (for polling)
        // ─────────────────────────────────────────
        public List<MedicineNotificationModel> GetNewNotificationsSince(int hospitalId, int? subHospitalId, DateTime since)
        {
            var list = new List<MedicineNotificationModel>();
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("GetNewNotificationsSince", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                    cmd.Parameters.AddWithValue("p_Since", since);

                    con.Open();
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(Map(r));
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error in GetNewNotificationsSince"); }
            return list;
        }

        // ─────────────────────────────────────────
        // GET PENDING COUNT
        // ─────────────────────────────────────────
        public int GetPendingCount(int hospitalId, int? subHospitalId)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("GetPendingNotificationCount", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error in GetPendingCount"); return 0; }
        }

        // ─────────────────────────────────────────
        // MARK DISPENSED
        // ─────────────────────────────────────────
        public void MarkDispensed(int notificationId, int hospitalId)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("MarkNotificationDispensed", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_NotificationId", notificationId);
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error in MarkDispensed"); }
        }

        // ─────────────────────────────────────────
        // MARK CANCELLED
        // ─────────────────────────────────────────
        public void MarkCancelled(int notificationId, int hospitalId)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("MarkNotificationCancelled", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_NotificationId", notificationId);
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error in MarkCancelled"); }
        }

        // ─────────────────────────────────────────
        // PRIVATE MAPPER
        // ─────────────────────────────────────────
        private MedicineNotificationModel Map(IDataReader r)
        {
            return new MedicineNotificationModel
            {
                NotificationId = Convert.ToInt32(r["NotificationId"]),
                PatientId = Convert.ToInt32(r["PatientId"]),
                PatientName = r["PatientName"]?.ToString(),
                OPDId = r["OPDId"] != DBNull.Value ? Convert.ToInt32(r["OPDId"]) : (int?)null,
                AppointmentId = r["AppointmentId"] != DBNull.Value ? Convert.ToInt32(r["AppointmentId"]) : (int?)null,
                DoctorName = r["DoctorName"]?.ToString(),
                MedicineCount = r["MedicineCount"] != DBNull.Value ? Convert.ToInt32(r["MedicineCount"]) : 0,
                MedicinesSummary = r["MedicinesSummary"]?.ToString(),
                Type = r["Type"]?.ToString(),
                Status = r["Status"]?.ToString(),
                CreatedAt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now,
                DispensedAt = r["DispensedAt"] != DBNull.Value ? Convert.ToDateTime(r["DispensedAt"]) : (DateTime?)null
            };
        }
    }
}



//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using MySql.Data.MySqlClient;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using WebApplicationSampleTest2.Models;

//namespace WebApplicationSampleTest2.Repository
//{
//    public class NotificationRepository : INotification
//    {
//        private readonly string _connectionString;
//        private readonly ILogger<NotificationRepository> _logger;

//        public NotificationRepository(IConfiguration configuration, ILogger<NotificationRepository> logger)
//        {
//            _connectionString = configuration.GetConnectionString("MySqlConnection");
//            _logger = logger;
//        }

//        // ─────────────────────────────────────────
//        // INSERT — called right after AddOPD saves
//        // ─────────────────────────────────────────
//        public void InsertNotification(MedicineNotificationModel model)
//        {
//            try
//            {
//                using (var con = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("InsertMedicineNotification", con))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("p_PatientId", model.PatientId);
//                    cmd.Parameters.AddWithValue("p_PatientName", model.PatientName);
//                    cmd.Parameters.AddWithValue("p_OPDId", (object)model.OPDId ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("p_AppointmentId", (object)model.AppointmentId ?? DBNull.Value);
//                    cmd.Parameters.AddWithValue("p_DoctorName", model.DoctorName ?? "");
//                    cmd.Parameters.AddWithValue("p_MedicineCount", model.MedicineCount);
//                    cmd.Parameters.AddWithValue("p_MedicinesSummary", model.MedicinesSummary ?? "");
//                    cmd.Parameters.AddWithValue("p_Type", model.Type ?? "OPD");
//                    cmd.Parameters.AddWithValue("p_HospitalId", model.HospitalId);
//                    cmd.Parameters.AddWithValue("p_SubHospitalId",
//                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);

//                    con.Open();
//                    cmd.ExecuteNonQuery();

//                    _logger.LogInformation(
//                        "Notification inserted for Patient: {PatientName}, Medicines: {Count}",
//                        model.PatientName, model.MedicineCount);
//                }
//            }
//            catch (Exception ex)
//            {
//                // Non-blocking — OPD save must not fail because of notification
//                _logger.LogError(ex, "Error inserting medicine notification. Patient: {PatientId}", model.PatientId);
//            }
//        }

//        // ─────────────────────────────────────────
//        // GET ALL (last 50, ordered newest first)
//        // ─────────────────────────────────────────
//        public List<MedicineNotificationModel> GetAllNotifications(int hospitalId, int? subHospitalId)
//        {
//            var list = new List<MedicineNotificationModel>();
//            try
//            {
//                using (var con = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("GetMedicineNotifications", con))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
//                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);

//                    con.Open();
//                    using (var r = cmd.ExecuteReader())
//                        while (r.Read()) list.Add(Map(r));
//                }
//            }
//            catch (Exception ex) { _logger.LogError(ex, "Error in GetAllNotifications"); }
//            return list;
//        }

//        // ─────────────────────────────────────────
//        // GET NEW SINCE TIMESTAMP (for polling)
//        // ─────────────────────────────────────────
//        public List<MedicineNotificationModel> GetNewNotificationsSince(int hospitalId, int? subHospitalId, DateTime since)
//        {
//            var list = new List<MedicineNotificationModel>();
//            try
//            {
//                using (var con = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("GetNewNotificationsSince", con))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
//                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
//                    cmd.Parameters.AddWithValue("p_Since", since);

//                    con.Open();
//                    using (var r = cmd.ExecuteReader())
//                        while (r.Read()) list.Add(Map(r));
//                }
//            }
//            catch (Exception ex) { _logger.LogError(ex, "Error in GetNewNotificationsSince"); }
//            return list;
//        }

//        // ─────────────────────────────────────────
//        // GET PENDING COUNT
//        // ─────────────────────────────────────────
//        public int GetPendingCount(int hospitalId, int? subHospitalId)
//        {
//            try
//            {
//                using (var con = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("GetPendingNotificationCount", con))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
//                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);

//                    con.Open();
//                    var result = cmd.ExecuteScalar();
//                    return result != null ? Convert.ToInt32(result) : 0;
//                }
//            }
//            catch (Exception ex) { _logger.LogError(ex, "Error in GetPendingCount"); return 0; }
//        }

//        // ─────────────────────────────────────────
//        // MARK DISPENSED
//        // ─────────────────────────────────────────
//        public void MarkDispensed(int notificationId, int hospitalId)
//        {
//            try
//            {
//                using (var con = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("MarkNotificationDispensed", con))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("p_NotificationId", notificationId);
//                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
//                    con.Open();
//                    cmd.ExecuteNonQuery();
//                }
//            }
//            catch (Exception ex) { _logger.LogError(ex, "Error in MarkDispensed"); }
//        }

//        // ─────────────────────────────────────────
//        // MARK CANCELLED
//        // ─────────────────────────────────────────
//        public void MarkCancelled(int notificationId, int hospitalId)
//        {
//            try
//            {
//                using (var con = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("MarkNotificationCancelled", con))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("p_NotificationId", notificationId);
//                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
//                    con.Open();
//                    cmd.ExecuteNonQuery();
//                }
//            }
//            catch (Exception ex) { _logger.LogError(ex, "Error in MarkCancelled"); }
//        }

//        // ─────────────────────────────────────────
//        // PRIVATE MAPPER
//        // ─────────────────────────────────────────
//        private MedicineNotificationModel Map(IDataReader r)
//        {
//            return new MedicineNotificationModel
//            {
//                NotificationId = Convert.ToInt32(r["NotificationId"]),
//                PatientId = Convert.ToInt32(r["PatientId"]),
//                PatientName = r["PatientName"]?.ToString(),
//                OPDId = r["OPDId"] != DBNull.Value ? Convert.ToInt32(r["OPDId"]) : (int?)null,
//                AppointmentId = r["AppointmentId"] != DBNull.Value ? Convert.ToInt32(r["AppointmentId"]) : (int?)null,
//                DoctorName = r["DoctorName"]?.ToString(),
//                MedicineCount = r["MedicineCount"] != DBNull.Value ? Convert.ToInt32(r["MedicineCount"]) : 0,
//                MedicinesSummary = r["MedicinesSummary"]?.ToString(),
//                Type = r["Type"]?.ToString(),
//                Status = r["Status"]?.ToString(),
//                CreatedAt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now,
//                DispensedAt = r["DispensedAt"] != DBNull.Value ? Convert.ToDateTime(r["DispensedAt"]) : (DateTime?)null
//            };
//        }
//    }
//}
