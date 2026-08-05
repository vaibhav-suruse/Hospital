using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class DoctorRepository:IDoctor
    {
        private readonly string _connectionString;

        public DoctorRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // ---------------- ADD ----------------
        public int AddDoctor(Doctor model, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    MySqlCommand cmd = new MySqlCommand("sp_Doctor_Insert", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@p_FirstName", model.FirstName);
                    cmd.Parameters.AddWithValue("@p_LastName", model.LastName);
                    cmd.Parameters.AddWithValue("@p_Gender", model.Gender);
                    cmd.Parameters.AddWithValue("@p_Education", model.Education);
                    cmd.Parameters.AddWithValue("@p_Specialization", model.Specialization);
                    cmd.Parameters.AddWithValue("@p_ExperienceYears", model.ExperienceYears);
                    cmd.Parameters.AddWithValue("@p_MobileNo", model.MobileNo);
                    cmd.Parameters.AddWithValue("@p_Email", model.Email);
                    cmd.Parameters.AddWithValue("@p_Address", model.Address);
                    cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                    cmd.Parameters.AddWithValue("@p_Sub_Hospital_Id", subHospitalId ?? (object)DBNull.Value);

                    con.Open();
                    return cmd.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
           
        }

        // ---------------- GET ALL ----------------
        public List<Doctor> GetAllDoctor(int hospitalId, int? subHospitalId)
        {
            List<Doctor> list = new List<Doctor>();
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    MySqlCommand cmd = new MySqlCommand("sp_Doctor_GetAll", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                    cmd.Parameters.AddWithValue("@p_Sub_Hospital_Id", subHospitalId ?? (object)DBNull.Value);

                    con.Open();
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new Doctor
                            {
                                Doctor_Id = Convert.ToInt32(dr["Doctor_Id"]),
                                FirstName = dr["FirstName"].ToString(),
                                LastName = dr["LastName"].ToString(),
                                Gender = dr["Gender"].ToString(),
                                Education = dr["Education"].ToString(),
                                Specialization = dr["Specialization"].ToString(),
                                ExperienceYears = Convert.ToInt32(dr["ExperienceYears"]),
                                MobileNo = dr["MobileNo"].ToString(),
                                Email = dr["Email"].ToString(),
                                Address = dr["Address"].ToString()
                            });
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

        // ---------------- GET BY ID ----------------
        public Doctor GetDoctorById(int doctorId, int hospitalId, int? subHospitalId)
        {
            Doctor model = null;
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    MySqlCommand cmd = new MySqlCommand("sp_Doctor_GetById", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@p_Doctor_Id", doctorId);
                    cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                    cmd.Parameters.AddWithValue("@p_Sub_Hospital_Id", subHospitalId ?? (object)DBNull.Value);

                    con.Open();
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            model = new Doctor
                            {
                                Doctor_Id = Convert.ToInt32(dr["Doctor_Id"]),
                                FirstName = dr["FirstName"].ToString(),
                                LastName = dr["LastName"].ToString(),
                                Gender = dr["Gender"].ToString(),
                                Education = dr["Education"].ToString(),
                                Specialization = dr["Specialization"].ToString(),
                                ExperienceYears = Convert.ToInt32(dr["ExperienceYears"]),
                                MobileNo = dr["MobileNo"].ToString(),
                                Email = dr["Email"].ToString(),
                                Address = dr["Address"].ToString()
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }

           
            return model;
        }

        // ---------------- UPDATE ----------------
        public int UpdateDoctor(Doctor model, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    MySqlCommand cmd = new MySqlCommand("sp_Doctor_Update", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@p_Doctor_Id", model.Doctor_Id);
                    cmd.Parameters.AddWithValue("@p_FirstName", model.FirstName);
                    cmd.Parameters.AddWithValue("@p_LastName", model.LastName);
                    cmd.Parameters.AddWithValue("@p_Gender", model.Gender);
                    cmd.Parameters.AddWithValue("@p_Education", model.Education);
                    cmd.Parameters.AddWithValue("@p_Specialization", model.Specialization);
                    cmd.Parameters.AddWithValue("@p_ExperienceYears", model.ExperienceYears);
                    cmd.Parameters.AddWithValue("@p_MobileNo", model.MobileNo);
                    cmd.Parameters.AddWithValue("@p_Email", model.Email);
                    cmd.Parameters.AddWithValue("@p_Address", model.Address);
                    cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                    cmd.Parameters.AddWithValue("@p_Sub_Hospital_Id", subHospitalId ?? (object)DBNull.Value);

                    con.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
            
        }

        // ---------------- DELETE (Hard) ----------------
        public int DeleteDoctor(int doctorId, int hospitalId, int? subHospitalId)
        {
            try
            {

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    MySqlCommand cmd = new MySqlCommand("sp_Doctor_HardDelete", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@p_Doctor_Id", doctorId);
                    cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                    cmd.Parameters.AddWithValue("@p_Sub_Hospital_Id", subHospitalId ?? (object)DBNull.Value);

                    con.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
           
        }
    }
}
