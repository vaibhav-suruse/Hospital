using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class MedicineRepository : IMedicine
    {

        private readonly string _connectionString;

        public MedicineRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }


        public int AddMedicine(Medicine model, int hospitalId, int? subHospitalId)
        {
            int result = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_create_medicine", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_MedicineName", model.MedicineName);
                        cmd.Parameters.AddWithValue("@p_Quantity", model.Quantity);
                        cmd.Parameters.AddWithValue("@p_Description", model.Description);
                        cmd.Parameters.AddWithValue("@p_Type", model.Type);
                        cmd.Parameters.AddWithValue("@p_Morning", model.Morning);
                        cmd.Parameters.AddWithValue("@p_Afternoon", model.Afternoon);
                        cmd.Parameters.AddWithValue("@p_Evening", model.Evening);
                        cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        con.Open();
                        result = cmd.ExecuteNonQuery();
                        con.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }

           
            return result;
        }

        public List<Medicine> GetAllMedicine(int hospitalId, int? subHospitalId)
        {
            List<Medicine> list = new List<Medicine>();

            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_get_all_medicine", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        con.Open();
                        using (MySqlDataReader dr = cmd.ExecuteReader())
                        {
                           
                            while (dr.Read())
                            {
                                list.Add(new Medicine
                                {
                                    MedicineId = dr["MedicineId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["MedicineId"]),
                                    MedicineName = dr["MedicineName"] == DBNull.Value ? "" : dr["MedicineName"].ToString(),
                                    Quantity = dr["Quantity"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Quantity"]),
                                    Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString(),
                                    Type = dr["Type"] == DBNull.Value ? "" : dr["Type"].ToString(),
                                    Morning = dr["Morning"] != DBNull.Value && Convert.ToBoolean(dr["Morning"]),
                                    Afternoon = dr["Afternoon"] != DBNull.Value && Convert.ToBoolean(dr["Afternoon"]),
                                    Evening = dr["Evening"] != DBNull.Value && Convert.ToBoolean(dr["Evening"])
                                });
                            }
                        }
                        con.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
            
            return list;
        }

        public Medicine GetMedicineById(int medicineId, int hospitalId, int? subHospitalId)
        {
            Medicine medicine = null;
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_get_medicine_by_id", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_MedicineId", medicineId);
                        cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        con.Open();
                        using (MySqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                // ✅ NEW
                                medicine = new Medicine
                                {
                                    MedicineId = dr["MedicineId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["MedicineId"]),
                                    MedicineName = dr["MedicineName"] == DBNull.Value ? "" : dr["MedicineName"].ToString(),
                                    Quantity = dr["Quantity"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Quantity"]),
                                    Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString(),
                                    Type = dr["Type"] == DBNull.Value ? "" : dr["Type"].ToString(),
                                    Morning = dr["Morning"] != DBNull.Value && Convert.ToBoolean(dr["Morning"]),
                                    Afternoon = dr["Afternoon"] != DBNull.Value && Convert.ToBoolean(dr["Afternoon"]),
                                    Evening = dr["Evening"] != DBNull.Value && Convert.ToBoolean(dr["Evening"])
                                };
                            }
                        }
                        con.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }

            return medicine;
        }

        public int UpdateMedicine(Medicine model, int hospitalId, int? subHospitalId)
        {
            int result = 0;

            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_update_medicine", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_MedicineId", model.MedicineId);
                        cmd.Parameters.AddWithValue("@p_MedicineName", model.MedicineName);
                        cmd.Parameters.AddWithValue("@p_Quantity", model.Quantity);
                        cmd.Parameters.AddWithValue("@p_Description", model.Description);
                        cmd.Parameters.AddWithValue("@p_Type", model.Type);
                        cmd.Parameters.AddWithValue("@p_Morning", model.Morning);
                        cmd.Parameters.AddWithValue("@p_Afternoon", model.Afternoon);
                        cmd.Parameters.AddWithValue("@p_Evening", model.Evening);
                        cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);


                        con.Open();
                        result = cmd.ExecuteNonQuery();
                        con.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
           
            return result;
        }

        public int DeleteMedicine(int medicineId, int hospitalId, int? subHospitalId)
        {
            int result = 0;
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    // ✅ Soft delete — just set IsActive = 0
                    using (MySqlCommand cmd = new MySqlCommand("sp_delete_medicine", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_MedicineId", medicineId);
                        cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        con.Open();
                        result = cmd.ExecuteNonQuery();
                        con.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while deleting medicine", ex);
            }
            return result;
        }
    }
}
