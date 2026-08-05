using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;


namespace WebApplicationSampleTest2.Repository
{
    public class NurseRepository:INurse
    {
        private readonly string _connectionString;
        private readonly ILogger<NurseRepository> _logger;

        public NurseRepository(IConfiguration configuration, ILogger<NurseRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        public List<NurseModel> GetAll(int hospitalId, int? subHospitalId = null)
        {
            var nurses = new List<NurseModel>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("GetAllNurses", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);

                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    nurses.Add(new NurseModel
                    {
                        NurseId = Convert.ToInt32(reader["NurseId"]),
                        ParentHospitalId = Convert.ToInt32(reader["ParentHospitalId"]),
                        SubHospitalId = reader["SubHospitalId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["SubHospitalId"]),
                        FirstName = reader["FirstName"].ToString(),
                        LastName = reader["LastName"].ToString(),
                        Gender = reader["Gender"].ToString(),
                        PhoneNumber = reader["PhoneNumber"].ToString(),
                        Email = reader["Email"].ToString(),
                        Qualification = reader["Qualification"].ToString(),
                        Department = reader["Department"].ToString(),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll nurses for HospitalId: {HospitalId}, SubHospitalId: {SubHospitalId}", hospitalId, subHospitalId);
            }
            return nurses;
        }

        public NurseModel GetById(int nurseId, int hospitalId, int? subHospitalId = null)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("GetNurseById", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("p_NurseId", nurseId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);

                conn.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new NurseModel
                    {
                        NurseId = Convert.ToInt32(reader["NurseId"]),
                        ParentHospitalId = Convert.ToInt32(reader["ParentHospitalId"]),
                        SubHospitalId = reader["SubHospitalId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["SubHospitalId"]),
                        FirstName = reader["FirstName"].ToString(),
                        LastName = reader["LastName"].ToString(),
                        Gender = reader["Gender"].ToString(),
                        PhoneNumber = reader["PhoneNumber"].ToString(),
                        Email = reader["Email"].ToString(),
                        Qualification = reader["Qualification"].ToString(),
                        Department = reader["Department"].ToString(),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetById nurseId: {NurseId} for HospitalId: {HospitalId}, SubHospitalId: {SubHospitalId}", nurseId, hospitalId, subHospitalId);
            }
            return null;
        }

        public void Insert(NurseModel nurse)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("AddNurse", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("p_ParentHospitalId", nurse.ParentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", nurse.SubHospitalId.HasValue ? (object)nurse.SubHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_FirstName", nurse.FirstName);
                cmd.Parameters.AddWithValue("p_LastName", nurse.LastName);
                cmd.Parameters.AddWithValue("p_Gender", nurse.Gender);
                cmd.Parameters.AddWithValue("p_PhoneNumber", nurse.PhoneNumber);
                cmd.Parameters.AddWithValue("p_Email", nurse.Email);
                cmd.Parameters.AddWithValue("p_Qualification", nurse.Qualification);
                cmd.Parameters.AddWithValue("p_Department", nurse.Department);
                cmd.Parameters.AddWithValue("p_IsActive", nurse.IsActive);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting nurse for HospitalId: {HospitalId}, SubHospitalId: {SubHospitalId}", nurse.ParentHospitalId, nurse.SubHospitalId);
            }
        }

        public void Update(NurseModel nurse)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("UpdateNurse", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("p_NurseId", nurse.NurseId);
                cmd.Parameters.AddWithValue("p_HospitalId", nurse.ParentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", nurse.SubHospitalId.HasValue ? (object)nurse.SubHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_FirstName", nurse.FirstName);
                cmd.Parameters.AddWithValue("p_LastName", nurse.LastName);
                cmd.Parameters.AddWithValue("p_Gender", nurse.Gender);
                cmd.Parameters.AddWithValue("p_PhoneNumber", nurse.PhoneNumber);
                cmd.Parameters.AddWithValue("p_Email", nurse.Email);
                cmd.Parameters.AddWithValue("p_Qualification", nurse.Qualification);
                cmd.Parameters.AddWithValue("p_Department", nurse.Department);
                cmd.Parameters.AddWithValue("p_IsActive", nurse.IsActive);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating nurseId: {NurseId} for HospitalId: {HospitalId}, SubHospitalId: {SubHospitalId}", nurse.NurseId, nurse.ParentHospitalId, nurse.SubHospitalId);
            }
        }

        public void Delete(int nurseId, int hospitalId, int? subHospitalId = null)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("DeleteNurse", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("p_NurseId", nurseId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting nurseId: {NurseId} for HospitalId: {HospitalId}, SubHospitalId: {SubHospitalId}", nurseId, hospitalId, subHospitalId);
            }
        }
    }
}
