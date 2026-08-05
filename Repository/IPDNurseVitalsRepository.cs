using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class IPDNurseVitalsRepository : IIPDNurseVitals
    {
        private readonly string _connectionString;

        public IPDNurseVitalsRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        public List<IPDNurseVitals> GetVitalsByIPDId(int ipdId, int hospitalId, int? subHospitalId)
        {
            var vitalsList = new List<IPDNurseVitals>();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("SP_GetVitalsByIPDId", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                    cmd.Parameters.AddWithValue("p_ParentHospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vitalsList.Add(MapVitals(reader));
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error occurred while fetching vitals.", ex);
            }

            return vitalsList;
        }

        public IPDNurseVitals GetVitalsById(int vitalsId)
        {
            IPDNurseVitals vitals = null;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("SP_GetVitalsById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_VitalsId", vitalsId);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            vitals = MapVitals(reader);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error occurred while fetching vitals by id.", ex);
            }

            return vitals;
        }

        public void CreateVitals(IPDNurseVitals model)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("SP_InsertIPDNurseVitals", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_ParentHospitalId", model.ParentHospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
                    cmd.Parameters.AddWithValue("p_RecordedByRole", model.RecordedByRole ?? "Nurse");
                    cmd.Parameters.AddWithValue("p_NurseId",
                        model.NurseId.HasValue ? (object)model.NurseId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_DoctorId",
                        model.DoctorId.HasValue ? (object)model.DoctorId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_Shift",
                        string.IsNullOrEmpty(model.Shift) ? (object)DBNull.Value : model.Shift);
                    cmd.Parameters.AddWithValue("p_RecordedDateTime", model.RecordedDateTime);
                    cmd.Parameters.AddWithValue("p_Temperature", model.Temperature);
                    cmd.Parameters.AddWithValue("p_Pulse", model.Pulse);
                    cmd.Parameters.AddWithValue("p_Systolic", model.Systolic);
                    cmd.Parameters.AddWithValue("p_Diastolic", model.Diastolic);
                    cmd.Parameters.AddWithValue("p_RespirationRate", model.RespirationRate);
                    cmd.Parameters.AddWithValue("p_OxygenSaturation", model.OxygenSaturation);
                    cmd.Parameters.AddWithValue("p_Notes", model.Notes);
                    cmd.Parameters.AddWithValue("p_IsAbnormal", model.IsAbnormal);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error occurred while inserting vitals.", ex);
            }
        }

        public void UpdateVitals(IPDNurseVitals model)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("SP_UpdateIPDNurseVitals", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_VitalsId", model.VitalsId);
                    cmd.Parameters.AddWithValue("p_RecordedByRole", model.RecordedByRole ?? "Nurse");
                    cmd.Parameters.AddWithValue("p_NurseId",
                        model.NurseId.HasValue ? (object)model.NurseId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_DoctorId",
                        model.DoctorId.HasValue ? (object)model.DoctorId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_Shift",
                        string.IsNullOrEmpty(model.Shift) ? (object)DBNull.Value : model.Shift);
                    cmd.Parameters.AddWithValue("p_Temperature", model.Temperature);
                    cmd.Parameters.AddWithValue("p_Pulse", model.Pulse);
                    cmd.Parameters.AddWithValue("p_Systolic", model.Systolic);
                    cmd.Parameters.AddWithValue("p_Diastolic", model.Diastolic);
                    cmd.Parameters.AddWithValue("p_RespirationRate", model.RespirationRate);
                    cmd.Parameters.AddWithValue("p_OxygenSaturation", model.OxygenSaturation);
                    cmd.Parameters.AddWithValue("p_Notes", model.Notes);
                    cmd.Parameters.AddWithValue("p_IsAbnormal", model.IsAbnormal);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error occurred while updating vitals.", ex);
            }
        }

        public void DeleteVitals(int vitalsId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("SP_DeleteIPDNurseVitals", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_VitalsId", vitalsId);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error occurred while deleting vitals.", ex);
            }
        }

        private IPDNurseVitals MapVitals(MySqlDataReader reader)
        {
            return new IPDNurseVitals
            {
                VitalsId = Convert.ToInt32(reader["VitalsId"]),
                ParentHospitalId = Convert.ToInt32(reader["ParentHospitalId"]),
                SubHospitalId = reader["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SubHospitalId"]),
                IPDId = Convert.ToInt32(reader["IPDId"]),
                NurseId = HasColumn(reader, "NurseId") && reader["NurseId"] != DBNull.Value ? Convert.ToInt32(reader["NurseId"]) : (int?)null,
                DoctorId = HasColumn(reader, "DoctorId") && reader["DoctorId"] != DBNull.Value ? Convert.ToInt32(reader["DoctorId"]) : (int?)null,
                RecordedByRole = HasColumn(reader, "RecordedByRole") && reader["RecordedByRole"] != DBNull.Value ? reader["RecordedByRole"].ToString() : "Nurse",
                RecordedByName = HasColumn(reader, "RecordedByName") && reader["RecordedByName"] != DBNull.Value ? reader["RecordedByName"].ToString() : null,
                Shift = HasColumn(reader, "Shift") && reader["Shift"] != DBNull.Value ? reader["Shift"].ToString() : null,
                RecordedDateTime = Convert.ToDateTime(reader["RecordedDateTime"]),
                Temperature = reader["Temperature"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Temperature"]),
                Pulse = reader["Pulse"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Pulse"]),
                Systolic = reader["Systolic"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Systolic"]),
                Diastolic = reader["Diastolic"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Diastolic"]),
                RespirationRate = reader["RespirationRate"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["RespirationRate"]),
                OxygenSaturation = reader["OxygenSaturation"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["OxygenSaturation"]),
                Notes = reader["Notes"]?.ToString(),
                IsAbnormal = Convert.ToBoolean(reader["IsAbnormal"]),
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                UpdatedDate = reader["UpdatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["UpdatedDate"])
            };
        }

        // Helper - lets MapVitals safely handle result sets from procs that may
        // or may not include the newer columns, without throwing.
        private static bool HasColumn(MySqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // ===============================
        // Latest vitals for an IPD - powers the "Copy Previous" quick-entry
        // button on the Create screen.
        // ===============================
        public IPDNurseVitals GetLastVitalsByIPDId(int ipdId)
        {
            IPDNurseVitals vitals = null;

            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(
                "SELECT * FROM ipd_nurse_vitals WHERE IPDId = @IPDId AND IsActive = 1 ORDER BY RecordedDateTime DESC LIMIT 1",
                conn))
            {
                cmd.Parameters.AddWithValue("@IPDId", ipdId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        vitals = MapVitals(reader);
                    }
                }
            }

            return vitals;
        }

        public List<IPDNurseVitals> GetVitalsByHospital(int parentHospitalId, int? subHospitalId)
        {
            List<IPDNurseVitals> list = new List<IPDNurseVitals>();

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("SP_GetIPDNurseVitalsByHospital", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId",
                    subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);

                conn.Open();

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapVitals(reader));
                    }
                }
            }

            return list;
        }
    }
}