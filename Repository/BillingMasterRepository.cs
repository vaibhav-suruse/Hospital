using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class BillingMasterRepository : IBillingMaster
    {
        private readonly string _connectionString;

        public BillingMasterRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }
        // ✅ Create Billing
        public void CreateBilling(BillingMaster billing, int hospitalId, int? subHospitalId)
        {

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    using (var cmd = new MySqlCommand("sp_create_BillingMaster", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("p_Name", billing.Name);
                        cmd.Parameters.AddWithValue("p_Description", billing.Description);
                        cmd.Parameters.AddWithValue("p_BillingType", billing.BillingType);
                        cmd.Parameters.AddWithValue("p_Amount", billing.Amount);

                        // 🔥 FIX HERE
                        cmd.Parameters.AddWithValue("p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }

        }



        // ✅ Update Billing
        // ✅ Update BillingMaster
        public int UpdateBilling(BillingMaster billing, int hospitalId, int? subHospitalId)
        {

            try
            {
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(_connectionString))
                {
                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand("sp_update_BillingMaster", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_BillingId", billing.Id);
                        cmd.Parameters.AddWithValue("p_Name", billing.Name);
                        cmd.Parameters.AddWithValue("p_Description", billing.Description);
                        cmd.Parameters.AddWithValue("p_BillingType", billing.BillingType);
                        cmd.Parameters.AddWithValue("p_Amount", billing.Amount);
                        cmd.Parameters.AddWithValue("p_IsActive", billing.IsActive);
                        cmd.Parameters.AddWithValue("p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected; // return number of rows updated
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }

        }


        // ✅ Delete Billing
        public void DeleteBilling(int billingId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(_connectionString))
                {
                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand("sp_delete_BillingMaster", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_BillingId", billingId);
                        cmd.Parameters.AddWithValue("p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }

        }

        // ✅ Get All Billings
        public List<BillingMaster> GetAllBillings(int hospitalId, int? subHospitalId)
        {
            var list = new List<BillingMaster>();

            try
            {
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(_connectionString))
                {
                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand("sp_get_all_BillingMaster", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new BillingMaster
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Name = reader["Name"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    BillingType = reader["BillingType"].ToString(),
                                    Category = reader["Category"]?.ToString(),
                                    Amount = Convert.ToDecimal(reader["Amount"]),
                                    GstPercent = reader["GstPercent"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["GstPercent"]),
                                    DefaultDiscountPercent = reader["DefaultDiscountPercent"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DefaultDiscountPercent"]),
                                    Hospital_Id = Convert.ToInt32(reader["Hospital_Id"]),
                                    SubHospital_Id = reader["SubHospital_Id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SubHospital_Id"]),
                                    IsActive = Convert.ToInt32(reader["IsActive"]),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
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

        // ✅ Get Billing By Id
        public BillingMaster GetBillingById(int billingId, int hospitalId, int? subHospitalId)
        {
            BillingMaster billing = null;

            try
            {
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(_connectionString))
                {
                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand("sp_get_BillingMaster_by_id", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_BillingId", billingId);
                        cmd.Parameters.AddWithValue("p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                billing = new BillingMaster
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Name = reader["Name"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    BillingType = reader["BillingType"].ToString(),
                                    Category = reader["Category"]?.ToString(),
                                    Amount = Convert.ToDecimal(reader["Amount"]),
                                    GstPercent = reader["GstPercent"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["GstPercent"]),
                                    DefaultDiscountPercent = reader["DefaultDiscountPercent"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DefaultDiscountPercent"]),
                                    Hospital_Id = Convert.ToInt32(reader["Hospital_Id"]),
                                    SubHospital_Id = reader["SubHospital_Id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SubHospital_Id"]),
                                    IsActive = Convert.ToInt32(reader["IsActive"]),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }



            return billing;
        }
    }
}
