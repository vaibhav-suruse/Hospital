// Repository/LabInvestigationRepository.cs
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class LabInvestigationRepository : ILabInvestigation
    {
        private readonly string _connectionString;

        public LabInvestigationRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // ===============================
        // GET ALL WITH FILTERS
        // ===============================
        public List<LabInvestigationModel> GetAllInvestigations(
            int parentHospitalId,
            int? subHospitalId,
            string status,
            string priority)
        {
            var list = new List<LabInvestigationModel>();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetAllInvestigations", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_Status",
                        string.IsNullOrEmpty(status) ? DBNull.Value : (object)status);
                    cmd.Parameters.AddWithValue("p_Priority",
                        string.IsNullOrEmpty(priority) ? DBNull.Value : (object)priority);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(MapInvestigation(reader));
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while fetching investigations.", ex);
            }

            return list;
        }

        // ===============================
        // GET BY ID
        // ===============================
        public LabInvestigationModel GetInvestigationById(int id)
        {
            LabInvestigationModel model = null;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetInvestigationById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_InvestigationId", id);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model = MapInvestigation(reader);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while fetching investigation by id.", ex);
            }

            return model;
        }

        // ===============================
        // UPDATE INVESTIGATION
        // ===============================
        public void UpdateInvestigation(
            int investigationId,
            string status,
            string result,
            string resultFilePath,
            int updatedBy,
            bool isAbnormal = false)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_UpdateInvestigation", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_InvestigationId", investigationId);
                    cmd.Parameters.AddWithValue("p_Status", status);
                    cmd.Parameters.AddWithValue("p_Result",
                        string.IsNullOrEmpty(result) ? DBNull.Value : (object)result);
                    cmd.Parameters.AddWithValue("p_ResultFilePath",
                        string.IsNullOrEmpty(resultFilePath) ? DBNull.Value : (object)resultFilePath);
                    cmd.Parameters.AddWithValue("p_CollectedDateTime", DBNull.Value);
                    cmd.Parameters.AddWithValue("p_CompletedDateTime", DBNull.Value);
                    cmd.Parameters.AddWithValue("p_UpdatedBy", updatedBy);

                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte)
                    { Direction = ParameterDirection.Output };
                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255)
                    { Direction = ParameterDirection.Output };

                    cmd.Parameters.Add(successParam);
                    cmd.Parameters.Add(messageParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    bool success = Convert.ToBoolean(successParam.Value);
                    if (!success)
                        throw new Exception(messageParam.Value?.ToString());

                    // BUGFIX: IsAbnormal wasn't part of the original stored
                    // proc's signature - rather than changing that proc's
                    // contract, set the new flag with a simple follow-up
                    // update on the same row.
                    using (var flagCmd = new MySqlCommand(
                        "UPDATE ipd_round_investigation SET IsAbnormal = @abn WHERE Id = @id", conn))
                    {
                        flagCmd.Parameters.AddWithValue("@abn", isAbnormal);
                        flagCmd.Parameters.AddWithValue("@id", investigationId);
                        flagCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while updating investigation.", ex);
            }
        }

        // ===============================
        // MAPPER
        // ===============================
        private LabInvestigationModel MapInvestigation(MySqlDataReader reader)
        {
            return new LabInvestigationModel
            {
                Id = Convert.ToInt32(reader["Id"]),
                RoundId = Convert.ToInt32(reader["RoundId"]),
                IPDId = Convert.ToInt32(reader["IPDId"]),
                InvestigationType = reader["InvestigationType"]?.ToString(),
                TestName = reader["TestName"]?.ToString(),
                Priority = reader["Priority"]?.ToString(),
                Status = reader["Status"]?.ToString(),
                Instructions = reader["Instructions"]?.ToString(),
                OrderedDateTime = Convert.ToDateTime(reader["OrderedDateTime"]),
                CollectedDateTime = reader["CollectedDateTime"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(reader["CollectedDateTime"]),
                CompletedDateTime = reader["CompletedDateTime"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(reader["CompletedDateTime"]),
                Result = reader["Result"]?.ToString(),
                ResultFilePath = reader["ResultFilePath"]?.ToString(),
                PatientName = reader["PatientName"]?.ToString(),
                AdmissionNumber = reader["AdmissionNumber"]?.ToString(),
                DoctorName = reader["DoctorName"]?.ToString()
            };
        }
    }
}