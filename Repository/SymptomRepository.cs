using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class SymptomRepository : ISymptom
    {
        private readonly string _connectionString;

        public SymptomRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }


        public int CreateSymptom(Symptom model, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_Symptom_Create", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_SymptomName", model.SymptomName);
                        cmd.Parameters.AddWithValue("@p_SubName", model.SubName);
                        cmd.Parameters.AddWithValue("@p_Description", model.Description);
                        cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        con.Open();
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
        }

        public int DeleteSymptom(int symptomId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_Symptom_Delete", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_SymptomId", symptomId);
                        cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        con.Open();
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
           
        }

        public List<Symptom> GetAllSymptoms(int hospitalId, int? subHospitalId)
        {
            List<Symptom> list = new List<Symptom>();
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_Symptom_GetAll", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        con.Open();
                        using (MySqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                list.Add(new Symptom
                                {
                                    SymptomId = Convert.ToInt32(dr["SymptomId"]),
                                    SymptomName = dr["SymptomName"].ToString(),
                                    SubName = dr["SubName"].ToString(),
                                    Description = dr["Description"].ToString()
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

        public Symptom GetSymptomById(int symptomId, int hospitalId, int? subHospitalId)
        {
            Symptom symptom = null;

            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_Symptom_GetById", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_SymptomId", symptomId);
                        cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        con.Open();
                        using (MySqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                symptom = new Symptom
                                {
                                    SymptomId = Convert.ToInt32(dr["SymptomId"]),
                                    SymptomName = dr["SymptomName"].ToString(),
                                    SubName = dr["SubName"].ToString(),
                                    Description = dr["Description"].ToString()
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
            return symptom;
        }

        public int UpdateSymptom(Symptom model, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_Symptom_Update", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_SymptomId", model.SymptomId);
                        cmd.Parameters.AddWithValue("@p_SymptomName", model.SymptomName);
                        cmd.Parameters.AddWithValue("@p_SubName", model.SubName);
                        cmd.Parameters.AddWithValue("@p_Description", model.Description);
                        cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        con.Open();
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
        }
    }
}
