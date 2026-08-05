using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class UserRepository : IUser
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }
        public void DeleteUser(int id)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("DeleteUser", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
        }

        public List<User> GetAllUsers()
        {
            List<User> users = new List<User>();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    using (MySqlCommand cmd = new MySqlCommand("sp_GetAllUsers", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                users.Add(new User
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    LoginName = reader["LoginName"].ToString(),
                                    FirstName = reader["FirstName"].ToString(),
                                    LastName = reader["LastName"].ToString(),
                                    EmailId = reader["EmailId"].ToString(),
                                    PhoneNo = reader["PhoneNo"].ToString(),
                                    Role = reader["Role"].ToString(),

                                    // 🔹 Hospital Name from HospitalId
                                    HospitalName = reader["HospitalName"] == DBNull.Value
                                                    ? ""
                                                    : reader["HospitalName"].ToString()
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
            return users;
        }
        public User GetUserById(int id)
        {
            User user = null;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("GetUserById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_Id", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user = new User
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    LoginName = reader["LoginName"].ToString(),
                                    FirstName = reader["FirstName"].ToString(),
                                    LastName = reader["LastName"].ToString(),
                                    Gender = reader["Gender"].ToString(),
                                    PhoneNo = reader["PhoneNo"].ToString(),
                                    EmailId = reader["EmailId"].ToString(),
                                    Password = reader["Password"].ToString(),
                                    HospitalId = Convert.ToInt32(reader["HospitalId"]),
                                    SubHospitalId = reader["SubHospitalId"] != DBNull.Value
                                        ? Convert.ToInt32(reader["SubHospitalId"])
                                        : (int?)null,
                                    Type = reader["Type"].ToString(),
                                    Role = reader["Role"].ToString(),
                                    IsActive = Convert.ToBoolean(reader["IsActive"]),
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

            
            return user;
        }


        public void InsertUser(User user)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("InsertUser", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_LoginName", user.LoginName);
                    cmd.Parameters.AddWithValue("p_FirstName", user.FirstName);
                    cmd.Parameters.AddWithValue("p_LastName", user.LastName);
                    cmd.Parameters.AddWithValue("p_Gender", user.Gender);
                    cmd.Parameters.AddWithValue("p_PhoneNo", user.PhoneNo);
                    cmd.Parameters.AddWithValue("p_EmailId", user.EmailId);
                    cmd.Parameters.AddWithValue("p_Password", user.Password);
                    cmd.Parameters.AddWithValue("p_HospitalId", user.HospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", user.SubHospitalId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p_Type", user.Type);
                    cmd.Parameters.AddWithValue("p_Role", user.Role);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        

        public void UpdateUser(User user)
        {
            try
            {

                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("UpdateUser", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_Id", user.Id);
                        cmd.Parameters.AddWithValue("p_LoginName", user.LoginName);
                        cmd.Parameters.AddWithValue("p_FirstName", user.FirstName);
                        cmd.Parameters.AddWithValue("p_LastName", user.LastName);
                        cmd.Parameters.AddWithValue("p_Gender", user.Gender);
                        cmd.Parameters.AddWithValue("p_PhoneNo", user.PhoneNo);
                        cmd.Parameters.AddWithValue("p_EmailId", user.EmailId);
                        cmd.Parameters.AddWithValue("p_Type", user.Type);
                        cmd.Parameters.AddWithValue("p_Role", user.Role);
                        cmd.Parameters.AddWithValue("p_IsActive", user.IsActive);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
           
        }

        // 🔹 1. Identify User by Username
        public string GetUserRoleByUsername(string loginName)
        {
            string role = null;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("IdentifyUserByUsername", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_LoginName", loginName);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                role = reader["Role"].ToString();
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
            return role;
        }

        //public User LoginUser(string loginName, string password, int? mainHospitalId, int? subHospitalId)
        //{
        //    User user = null;
        //    try
        //    {
        //        using (var conn = new MySqlConnection(_connectionString))
        //        {
        //            conn.Open();

        //            using (var cmd = new MySqlCommand("LoginUser", conn))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.AddWithValue("p_LoginName", loginName);
        //                cmd.Parameters.AddWithValue("p_Password", password);
        //                cmd.Parameters.AddWithValue(
        //                    "p_MainHospitalId",
        //                    mainHospitalId.HasValue ? (object)mainHospitalId.Value : DBNull.Value
        //                );
        //                cmd.Parameters.AddWithValue(
        //                    "p_SubHospitalId",
        //                    subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value
        //                );

        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    if (reader.Read())
        //                    {
        //                        user = new User
        //                        {
        //                            Id = Convert.ToInt32(reader["UserId"]),
        //                            LoginName = reader["LoginName"].ToString(),
        //                            Role = reader["Role"].ToString(),
        //                            HospitalId = Convert.ToInt32(reader["HospitalId"]),
        //                            SubHospitalId = reader["SubHospitalId"] == DBNull.Value
        //                                            ? (int?)null
        //                                            : Convert.ToInt32(reader["SubHospitalId"]),
        //                            IsActive = Convert.ToBoolean(reader["IsActive"])
        //                        };
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Error while inserting hospital", ex);
        //    }



        //    return user;
        //}





        public User LoginUser(string loginName, string password, int? mainHospitalId, int? subHospitalId)
        {
            User user = null;
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("LoginUser", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_LoginName", loginName);
                        cmd.Parameters.AddWithValue("p_Password", password);
                        cmd.Parameters.AddWithValue(
                            "p_MainHospitalId",
                            mainHospitalId.HasValue ? (object)mainHospitalId.Value : DBNull.Value
                        );
                        cmd.Parameters.AddWithValue(
                            "p_SubHospitalId",
                            subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value
                        );

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string message = reader["Message"].ToString();

                                if (message == "Login Successful")
                                {
                                    user = new User
                                    {
                                        Id = Convert.ToInt32(reader["UserId"]),
                                        LoginName = reader["LoginName"].ToString(),
                                        Role = reader["Role"].ToString(),
                                        HospitalId = Convert.ToInt32(reader["HospitalId"]),
                                        SubHospitalId = reader["SubHospitalId"] == DBNull.Value
                                                        ? (int?)null
                                                        : Convert.ToInt32(reader["SubHospitalId"]),
                                        IsActive = Convert.ToBoolean(reader["IsActive"])
                                    };
                                }
                                else
                                {
                                    throw new Exception(message);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

            return user;
        }







        // 🔹 3. Get Sub Hospitals
        public DataTable GetSubHospitals(int mainHospitalId)
        {
            DataTable dt = new DataTable();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("GetSubHospitalsByMainHospital", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_MainHospitalId", mainHospitalId);

                        using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
            return dt;
        }

    }
}
