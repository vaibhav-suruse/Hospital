using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class ReferenceDoctorRepository:IReferenceDoctor
    {
        private readonly ILogger<ReferenceDoctorRepository> _logger;
        private readonly string _connectionString;

        public ReferenceDoctorRepository(IConfiguration configuration, ILogger<ReferenceDoctorRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;

        }

        // ================= ADD =================
        public int AddReferenceDoctor(ReferenceDoctorModel model, int hospitalId, int? subHospitalId)
        {
            try
            {
                using MySqlConnection con = new MySqlConnection(_connectionString);
                using MySqlCommand cmd = new MySqlCommand("sp_ReferenceDoctor_Add", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_ParentHospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId",
                    subHospitalId ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("p_DoctorName", model.DoctorName);
                cmd.Parameters.AddWithValue("p_ClinicName", model.ClinicName);
                cmd.Parameters.AddWithValue("p_MobileNumber", model.MobileNumber);
                cmd.Parameters.AddWithValue("p_Email", model.Email);
                cmd.Parameters.AddWithValue("p_Address", model.Address);
                cmd.Parameters.AddWithValue("p_City", model.City);
                cmd.Parameters.AddWithValue("p_Percentage", model.Percentage);
                cmd.Parameters.AddWithValue("p_IsActive", model.IsActive);

                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddReferenceDoctor");
                throw;
            }
        }

        // ================= UPDATE =================
        public int UpdateReferenceDoctor(ReferenceDoctorModel model, int hospitalId, int? subHospitalId)
        {
            try
            {
                using MySqlConnection con = new MySqlConnection(_connectionString);
                using MySqlCommand cmd = new MySqlCommand("sp_ReferenceDoctor_Update", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_ReferenceDoctorId", model.ReferenceDoctorId);
                cmd.Parameters.AddWithValue("p_ParentHospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId",
                    subHospitalId ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("p_DoctorName", model.DoctorName);
                cmd.Parameters.AddWithValue("p_ClinicName", model.ClinicName);
                cmd.Parameters.AddWithValue("p_MobileNumber", model.MobileNumber);
                cmd.Parameters.AddWithValue("p_Email", model.Email);
                cmd.Parameters.AddWithValue("p_Address", model.Address);
                cmd.Parameters.AddWithValue("p_City", model.City);
                cmd.Parameters.AddWithValue("p_Percentage", model.Percentage);
                cmd.Parameters.AddWithValue("p_IsActive", model.IsActive);

                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateReferenceDoctor");
                throw;
            }
        }

        // ================= DELETE =================
        public int DeleteReferenceDoctor(int referenceDoctorId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using MySqlConnection con = new MySqlConnection(_connectionString);
                using MySqlCommand cmd = new MySqlCommand("sp_ReferenceDoctor_Delete", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_ReferenceDoctorId", referenceDoctorId);
                cmd.Parameters.AddWithValue("p_ParentHospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId",
                    subHospitalId ?? (object)DBNull.Value);

                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteReferenceDoctor");
                throw;
            }
        }

        // ================= GET ALL =================
        public List<ReferenceDoctorModel> GetAllReferenceDoctor(int hospitalId, int? subHospitalId)
        {
            List<ReferenceDoctorModel> list = new List<ReferenceDoctorModel>();

            try
            {
                using MySqlConnection con = new MySqlConnection(_connectionString);
                using MySqlCommand cmd = new MySqlCommand("sp_ReferenceDoctor_GetAll", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_ParentHospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId",
                    subHospitalId ?? (object)DBNull.Value);

                con.Open();

                using MySqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new ReferenceDoctorModel
                    {
                        ReferenceDoctorId = Convert.ToInt32(dr["ReferenceDoctorId"]),
                        ParentHospitalId = Convert.ToInt32(dr["ParentHospitalId"]),
                        SubHospitalId = dr["SubHospitalId"] == DBNull.Value
                            ? (int?)null
                            : Convert.ToInt32(dr["SubHospitalId"]),
                        DoctorName = dr["DoctorName"].ToString(),
                        ClinicName = dr["ClinicName"]?.ToString(),
                        MobileNumber = dr["MobileNumber"]?.ToString(),
                        Email = dr["Email"]?.ToString(),
                        Address = dr["Address"]?.ToString(),
                        City = dr["City"]?.ToString(),
                        Percentage = Convert.ToDecimal(dr["Percentage"]),
                        IsActive = Convert.ToBoolean(dr["IsActive"])
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllReferenceDoctor");
                throw;
            }

            return list;
        }

        // ================= GET BY ID =================
        public ReferenceDoctorModel GetReferenceDoctorById(int referenceDoctorId, int hospitalId, int? subHospitalId)
        {
            ReferenceDoctorModel model = null;

            try
            {
                using MySqlConnection con = new MySqlConnection(_connectionString);
                using MySqlCommand cmd = new MySqlCommand("sp_ReferenceDoctor_GetById", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_ReferenceDoctorId", referenceDoctorId);
                cmd.Parameters.AddWithValue("p_ParentHospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId",
                    subHospitalId ?? (object)DBNull.Value);

                con.Open();

                using MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model = new ReferenceDoctorModel
                    {
                        ReferenceDoctorId = Convert.ToInt32(dr["ReferenceDoctorId"]),
                        ParentHospitalId = Convert.ToInt32(dr["ParentHospitalId"]),
                        SubHospitalId = dr["SubHospitalId"] == DBNull.Value
                            ? (int?)null
                            : Convert.ToInt32(dr["SubHospitalId"]),
                        DoctorName = dr["DoctorName"].ToString(),
                        ClinicName = dr["ClinicName"]?.ToString(),
                        MobileNumber = dr["MobileNumber"]?.ToString(),
                        Email = dr["Email"]?.ToString(),
                        Address = dr["Address"]?.ToString(),
                        City = dr["City"]?.ToString(),
                        Percentage = Convert.ToDecimal(dr["Percentage"]),
                        IsActive = Convert.ToBoolean(dr["IsActive"])
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReferenceDoctorById");
                throw;
            }

            return model;
        }

    }
}
