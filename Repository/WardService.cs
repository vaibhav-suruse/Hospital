using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class WardService:IWard
    {
        private readonly string _connectionString;

        public WardService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        public int CreateWard(Ward ward)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_CreateWard", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_HospitalId", ward.HospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", ward.SubHospitalId.HasValue ? ward.SubHospitalId.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_WardName", ward.WardName);
                cmd.Parameters.AddWithValue("p_WardType", ward.WardType);
                cmd.Parameters.AddWithValue("p_FloorNumber", ward.FloorNumber);
                cmd.Parameters.AddWithValue("p_Description", ward.Description);

                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating ward: " + ex.Message);
            }
        }

        public int UpdateWard(Ward ward)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_UpdateWard", con);
                cmd.CommandType = CommandType.StoredProcedure;

                // Correct order and all 8 parameters including IsActive
                cmd.Parameters.AddWithValue("p_WardId", ward.WardId);
                cmd.Parameters.AddWithValue("p_HospitalId", ward.HospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", ward.SubHospitalId.HasValue ? ward.SubHospitalId.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_WardName", ward.WardName);
                cmd.Parameters.AddWithValue("p_WardType", ward.WardType);
                cmd.Parameters.AddWithValue("p_FloorNumber", ward.FloorNumber);
                cmd.Parameters.AddWithValue("p_IsActive", ward.IsActive ? 1 : 0);
                cmd.Parameters.AddWithValue("p_Description", ward.Description);

                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating ward: " + ex.Message);
            }
        }

        public List<Ward> GetAllWards(int hospitalId, int? subHospitalId)
        {
            List<Ward> list = new List<Ward>();

            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_GetAllWards", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                con.Open();
                using var dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new Ward
                    {
                        WardId = Convert.ToInt32(dr["WardId"]),
                        WardName = dr["WardName"].ToString(),
                        WardType = dr["WardType"].ToString(),
                        FloorNumber = Convert.ToInt32(dr["FloorNumber"]),
                        Description = dr["Description"]?.ToString(),
                        IsActive = Convert.ToBoolean(dr["IsActive"]) // <-- fix here
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching wards: " + ex.Message);
            }

            return list;
        }

        public Ward GetWardById(int wardId, int hospitalId, int? subHospitalId)
        {
            Ward ward = null;

            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_GetWardById", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_WardId", wardId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                con.Open();
                using var dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    ward = new Ward
                    {
                        WardId = Convert.ToInt32(dr["WardId"]),
                        WardName = dr["WardName"].ToString(),
                        WardType = dr["WardType"].ToString(),
                        FloorNumber = Convert.ToInt32(dr["FloorNumber"]),
                        Description = dr["Description"]?.ToString(),
                        IsActive = Convert.ToBoolean(dr["IsActive"]) // <-- fix here
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching ward: " + ex.Message);
            }

            return ward;
        }

        public int DeleteWard(int wardId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_SoftDeleteWard", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_WardId", wardId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting ward: " + ex.Message);
            }
        }

        public List<WardListVM> GetWardsWithCounts(int hospitalId, int? subHospitalId)
        {
            var wards = new List<WardListVM>();

            using (var conn = new MySqlConnection(_connectionString))
            {
                using (var cmd = new MySqlCommand("sp_GetWardsWithCounts", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            wards.Add(new WardListVM
                            {
                                WardId = Convert.ToInt32(reader["WardId"]),
                                WardName = reader["WardName"].ToString(),
                                WardType = reader["WardType"].ToString(),
                                FloorNumber = Convert.ToInt32(reader["FloorNumber"]),
                                Description = reader["Description"].ToString(),
                                IsActive = Convert.ToBoolean(reader["IsActive"]),
                                TotalRooms = Convert.ToInt32(reader["TotalRooms"]),
                                TotalBeds = Convert.ToInt32(reader["TotalBeds"])
                            });
                        }
                    }
                }
            }

            return wards;
        }

    }
}
