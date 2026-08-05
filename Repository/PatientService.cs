using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class PatientService : Ipatient
    {
        private readonly string _connectionString;

        public PatientService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // ══════════════════════════════════════════════════════════
        //  HELPER — splits FullName into FirstName / LastName
        // ══════════════════════════════════════════════════════════
        private static (string first, string last) SplitFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return ("", "");

            fullName = fullName.Trim();
            int spaceIndex = fullName.IndexOf(' ');

            if (spaceIndex < 0)
                return (fullName, "");   // only one word

            return (fullName.Substring(0, spaceIndex),
                    fullName.Substring(spaceIndex + 1).Trim());
        }

        // ══════════════════════════════════════════════════════════
        //  ADD PATIENT  — direct SQL (no stored procedure)
        // ══════════════════════════════════════════════════════════
        public int AddPatient(Patient model, int hospitalId, int? subHospitalId)
        {
            // Split FullName → FirstName / LastName
            if (!string.IsNullOrWhiteSpace(model.FullName))
            {
                var (first, last) = SplitFullName(model.FullName);
                model.FirstName = first;
                model.LastName = last;
            }

            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
                    INSERT INTO tbl_patient
                        (Salutation, FirstName, LastName, FatherName,
                         Gender, Age, AgeMonths, DateOfBirth,
                         ReferenceId, BloodGroup, MaritalStatus, Occupation,
                         PhoneNumber, Email, Address, ProfilePhoto,
                         Hospital_Id, SubHospital_Id)
                    VALUES
                        (@Salutation, @FirstName, @LastName, @FatherName,
                         @Gender, @Age, @AgeMonths, @DateOfBirth,
                         @ReferenceId, @BloodGroup, @MaritalStatus, @Occupation,
                         @PhoneNumber, @Email, @Address, @ProfilePhoto,
                         @Hospital_Id, @SubHospital_Id);
                    SELECT LAST_INSERT_ID();", con);

                cmd.Parameters.AddWithValue("@Salutation", model.Salutation ?? "");
                cmd.Parameters.AddWithValue("@FirstName", model.FirstName ?? "");
                cmd.Parameters.AddWithValue("@LastName", model.LastName ?? "");
                cmd.Parameters.AddWithValue("@FatherName", model.FatherName ?? "");
                cmd.Parameters.AddWithValue("@Gender", model.Gender ?? "");
                cmd.Parameters.AddWithValue("@Age", int.TryParse(model.Age, out int age) ? age : 0);
                cmd.Parameters.AddWithValue("@AgeMonths", model.AgeMonths);
                cmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth.HasValue ? (object)model.DateOfBirth.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@ReferenceId", model.ReferenceId ?? "");
                cmd.Parameters.AddWithValue("@BloodGroup", model.BloodGroup ?? "");
                cmd.Parameters.AddWithValue("@MaritalStatus", model.MaritalStatus ?? "");
                cmd.Parameters.AddWithValue("@Occupation", model.Occupation ?? "");
                cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber ?? "");
                cmd.Parameters.AddWithValue("@Email", model.Email ?? "");
                cmd.Parameters.AddWithValue("@Address", model.Address ?? "");
                cmd.Parameters.AddWithValue("@ProfilePhoto", model.ProfilePhoto ?? "");
                cmd.Parameters.AddWithValue("@Hospital_Id", hospitalId);
                cmd.Parameters.AddWithValue("@SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                throw new Exception("Error while adding patient", ex);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  UPDATE PATIENT — direct SQL (no stored procedure)
        // ══════════════════════════════════════════════════════════
        public int UpdatePatient(Patient model, int hospitalId, int? subHospitalId)
        {
            // Split FullName → FirstName / LastName
            if (!string.IsNullOrWhiteSpace(model.FullName))
            {
                var (first, last) = SplitFullName(model.FullName);
                model.FirstName = first;
                model.LastName = last;
            }

            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
                    UPDATE tbl_patient SET
                        Salutation    = @Salutation,
                        FirstName     = @FirstName,
                        LastName      = @LastName,
                        FatherName    = @FatherName,
                        Gender        = @Gender,
                        Age           = @Age,
                        AgeMonths     = @AgeMonths,
                        DateOfBirth   = @DateOfBirth,
                        ReferenceId   = @ReferenceId,
                        BloodGroup    = @BloodGroup,
                        MaritalStatus = @MaritalStatus,
                        Occupation    = @Occupation,
                        PhoneNumber   = @PhoneNumber,
                        Email         = @Email,
                        Address       = @Address,
                        ProfilePhoto  = @ProfilePhoto,
                        isUpdate      = 1
                    WHERE Id = @PatientId
                      AND Hospital_Id = @Hospital_Id
                      AND (
                          (SubHospital_Id IS NULL AND @SubHospital_Id IS NULL)
                          OR (SubHospital_Id = @SubHospital_Id)
                      );", con);

                cmd.Parameters.AddWithValue("@PatientId", model.Id);
                cmd.Parameters.AddWithValue("@Salutation", model.Salutation ?? "");
                cmd.Parameters.AddWithValue("@FirstName", model.FirstName ?? "");
                cmd.Parameters.AddWithValue("@LastName", model.LastName ?? "");
                cmd.Parameters.AddWithValue("@FatherName", model.FatherName ?? "");
                cmd.Parameters.AddWithValue("@Gender", model.Gender ?? "");
                cmd.Parameters.AddWithValue("@Age", int.TryParse(model.Age, out int age) ? age : 0);
                cmd.Parameters.AddWithValue("@AgeMonths", model.AgeMonths);
                cmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth.HasValue ? (object)model.DateOfBirth.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@ReferenceId", model.ReferenceId ?? "");
                cmd.Parameters.AddWithValue("@BloodGroup", model.BloodGroup ?? "");
                cmd.Parameters.AddWithValue("@MaritalStatus", model.MaritalStatus ?? "");
                cmd.Parameters.AddWithValue("@Occupation", model.Occupation ?? "");
                cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber ?? "");
                cmd.Parameters.AddWithValue("@Email", model.Email ?? "");
                cmd.Parameters.AddWithValue("@Address", model.Address ?? "");
                cmd.Parameters.AddWithValue("@ProfilePhoto", model.ProfilePhoto ?? "");
                cmd.Parameters.AddWithValue("@Hospital_Id", hospitalId);
                cmd.Parameters.AddWithValue("@SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while updating patient", ex);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  GET ALL PATIENTS — reads new fields too
        // ══════════════════════════════════════════════════════════
        public List<Patient> GetAllPatients(int hospitalId, int? subHospitalId)
        {
            var list = new List<Patient>();
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_get_all_patient", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(MapPatientFromReader(dr));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting all patients", ex);
            }
            return list;
        }

        // ══════════════════════════════════════════════════════════
        //  GET PATIENTS PAGED — single SQL query with COUNT + LIMIT.
        //  Replaces the "load ALL patients then Skip/Take in C#" pattern
        //  that made PatientList and every pagination click take ~30s.
        // ══════════════════════════════════════════════════════════
        public List<Patient> GetPatientsPaged(int hospitalId, int? subHospitalId, string search, int page, int pageSize, out int totalRecords)
        {
            var list = new List<Patient>();
            totalRecords = 0;

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            int offset = (page - 1) * pageSize;

            try
            {
                using var con = new MySqlConnection(_connectionString);
                con.Open();

                // Sub-hospital filter — only two possible forms, so no injection risk.
                string subFilter = subHospitalId.HasValue
                    ? "AND SubHospital_Id = @SubHospital_Id"
                    : "AND (SubHospital_Id IS NULL OR SubHospital_Id = 0)";

                string searchSql = "";
                if (!string.IsNullOrWhiteSpace(search))
                {
                    searchSql = @" AND (
                            LOWER(COALESCE(FirstName,'')) LIKE @Search OR
                            LOWER(COALESCE(LastName,'')) LIKE @Search OR
                            LOWER(COALESCE(Gender,'')) LIKE @Search OR
                            LOWER(COALESCE(PhoneNumber,'')) LIKE @Search OR
                            LOWER(COALESCE(Address,'')) LIKE @Search
                        )";
                }

                // 1) Total matching records (fast COUNT, no row data).
                using (var countCmd = new MySqlCommand(
                    @"SELECT COUNT(*) FROM tbl_patient
                      WHERE Hospital_Id = @Hospital_Id " + subFilter + searchSql, con))
                {
                    countCmd.Parameters.AddWithValue("@Hospital_Id", hospitalId);
                    countCmd.Parameters.AddWithValue("@SubHospital_Id",
                        subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);
                    if (!string.IsNullOrWhiteSpace(search))
                        countCmd.Parameters.AddWithValue("@Search", "%" + search.Trim().ToLower() + "%");

                    totalRecords = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                // 2) Current page rows only — LIMIT/OFFSET keeps the result tiny.
                string sql =
                    @"SELECT Id, Salutation, FirstName, LastName, FatherName, Gender, Age, AgeMonths,
                             DateOfBirth, ReferenceId, BloodGroup, MaritalStatus, Occupation,
                             PhoneNumber, Email, Address, ProfilePhoto
                      FROM tbl_patient
                      WHERE Hospital_Id = @Hospital_Id " + subFilter + searchSql + @"
                      ORDER BY Id DESC
                      LIMIT @PageSize OFFSET @Offset";

                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Hospital_Id", hospitalId);
                cmd.Parameters.AddWithValue("@SubHospital_Id",
                    subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Search",
                    string.IsNullOrWhiteSpace(search) ? "%" : "%" + search.Trim().ToLower() + "%");
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);

                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(MapPatientFromReader(dr));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting paged patients", ex);
            }

            return list;
        }

        // ══════════════════════════════════════════════════════════
        //  GET PATIENT BY ID — reads new fields too
        // ══════════════════════════════════════════════════════════
        public Patient GetPatientById(int patientId, int hospitalId, int? subHospitalId)
        {
            Patient patient = null;
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_get_patient_by_id", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_PatientId", patientId);
                cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                con.Open();
                using var dr = cmd.ExecuteReader();
                if (dr.Read())
                    patient = MapPatientFromReader(dr);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting patient by id", ex);
            }
            return patient;
        }

        // ══════════════════════════════════════════════════════════
        //  SHARED READER MAPPER
        // ══════════════════════════════════════════════════════════
        private static Patient MapPatientFromReader(MySqlDataReader dr)
        {
            var p = new Patient
            {
                Id = Convert.ToInt32(dr["Id"]),
                FirstName = dr["FirstName"].ToString(),
                LastName = dr["LastName"].ToString(),
                Gender = dr["Gender"].ToString(),
                Age = dr["Age"].ToString(),
                PhoneNumber = dr["PhoneNumber"] == DBNull.Value ? "" : dr["PhoneNumber"].ToString(),
                Address = dr["Address"] == DBNull.Value ? "" : dr["Address"].ToString(),
            };

            // Build FullName for the edit form
            p.FullName = $"{p.FirstName} {p.LastName}".Trim();

            // New fields — safe read (column may not exist in old SP result sets)
            p.Salutation = SafeString(dr, "Salutation");
            p.FatherName = SafeString(dr, "FatherName");
            p.AgeMonths = SafeInt(dr, "AgeMonths");
            p.DateOfBirth = SafeDate(dr, "DateOfBirth");
            p.ReferenceId = SafeString(dr, "ReferenceId");
            p.BloodGroup = SafeString(dr, "BloodGroup");
            p.MaritalStatus = SafeString(dr, "MaritalStatus");
            p.Occupation = SafeString(dr, "Occupation");
            p.Email = SafeString(dr, "Email");
            p.ProfilePhoto = SafeString(dr, "ProfilePhoto");

            return p;
        }

        // Safe helpers so old SP results that don't return new columns don't crash
        private static string SafeString(MySqlDataReader dr, string col)
        {
            try { return dr[col] == DBNull.Value ? "" : dr[col].ToString(); }
            catch { return ""; }
        }
        private static int SafeInt(MySqlDataReader dr, string col)
        {
            try { return dr[col] == DBNull.Value ? 0 : Convert.ToInt32(dr[col]); }
            catch { return 0; }
        }
        private static DateTime? SafeDate(MySqlDataReader dr, string col)
        {
            try { return dr[col] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr[col]); }
            catch { return null; }
        }

        // ══════════════════════════════════════════════════════════
        //  DELETE PATIENT — unchanged
        // ══════════════════════════════════════════════════════════
        public int DeletePatient(int patientId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_delete_patient", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_PatientId", patientId);
                cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);
                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while deleting patient", ex);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  SEARCH BY MOBILE — unchanged
        // ══════════════════════════════════════════════════════════
        public List<Patient> SearchPatientByMobile(string search, int hospitalId, int? subHospitalId)
        {
            var list = new List<Patient>();
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_search_patients", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Search", search);
                cmd.Parameters.AddWithValue("p_Hospital_Id", hospitalId);
                cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new Patient
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        FirstName = dr["FirstName"].ToString(),
                        LastName = dr["LastName"].ToString(),
                        PhoneNumber = dr["PhoneNumber"].ToString(),
                        Gender = dr["Gender"].ToString(),
                        FullName = $"{dr["FirstName"]} {dr["LastName"]}".Trim()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while searching patients", ex);
            }
            return list;
        }

        // ══════════════════════════════════════════════════════════
        //  ALL REMAINING METHODS — completely unchanged from original
        // ══════════════════════════════════════════════════════════

        public Patient Login(string email, string password)
        {
            Patient patient = null;
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_patient_login_old", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_email", email);
                cmd.Parameters.AddWithValue("@p_password", password);
                con.Open();
                using var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    patient = new Patient
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        FirstName = dr["FirstName"].ToString(),
                        LastName = dr["LastName"].ToString(),
                        Email = dr["Email"].ToString(),
                        Hospital_Id = dr["Hospital_Id"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Hospital_Id"]),
                        SubHospital_Id = dr["SubHospital_Id"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["SubHospital_Id"])
                    };
                    patient.FullName = $"{patient.FirstName} {patient.LastName}".Trim();
                }
            }
            catch (Exception ex) { throw new Exception("Error during login", ex); }
            return patient;
        }

        public bool CheckEmail(string email)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_check_email", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_email", email);
                con.Open();
                using var dr = cmd.ExecuteReader();
                return dr.HasRows;
            }
            catch (Exception ex) { throw new Exception("Error checking email", ex); }
        }

        public bool UpdatePassword(string email, string newPassword)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_update_password", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_email", email);
                cmd.Parameters.AddWithValue("@p_new_password", newPassword);
                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex) { throw new Exception("Error updating password", ex); }
        }

        public bool SignupPatient(Patient patient, out string message)
        {
            message = string.Empty;
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_patient_Signup", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_FirstName", patient.FirstName);
                cmd.Parameters.AddWithValue("@p_LastName", patient.LastName);
                cmd.Parameters.AddWithValue("@p_Gender", patient.Gender);
                cmd.Parameters.AddWithValue("@p_Age", patient.Age);
                cmd.Parameters.AddWithValue("@p_PhoneNumber", patient.PhoneNumber);
                cmd.Parameters.AddWithValue("@p_Email", patient.Email);
                cmd.Parameters.AddWithValue("@p_Password", patient.Password);
                cmd.Parameters.AddWithValue("@p_Address", patient.Address);
                con.Open();
                cmd.ExecuteNonQuery();
                message = "Patient registered successfully";
                return true;
            }
            catch (MySqlException ex) { message = ex.Message; return false; }
        }

        public PatientAccount LoginAccount(string email, string password)
        {
            PatientAccount account = null;
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_patient_login", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_email", email);
                cmd.Parameters.AddWithValue("@p_password", password);
                con.Open();
                using var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    account = new PatientAccount
                    {
                        AccountId = Convert.ToInt32(dr["AccountId"]),
                        Email = dr["Email"].ToString(),
                        FirstName = dr["FirstName"].ToString(),
                        LastName = dr["LastName"].ToString(),
                        PhoneNumber = dr["PhoneNumber"]?.ToString()
                    };
                }
            }
            catch (Exception ex) { throw new Exception("Error during account login", ex); }
            return account;
        }

        public List<PatientProfileVM> GetProfilesByAccountId(int accountId)
        {
            var list = new List<PatientProfileVM>();
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_get_profiles_by_account", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_AccountId", accountId);
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new PatientProfileVM
                    {
                        PatientId = dr["PatientId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["PatientId"]),
                        FirstName = dr["FirstName"] == DBNull.Value ? "" : dr["FirstName"].ToString(),
                        LastName = dr["LastName"] == DBNull.Value ? "" : dr["LastName"].ToString(),
                        Gender = dr["Gender"] == DBNull.Value ? "" : dr["Gender"].ToString(),
                        Age = dr["Age"] == DBNull.Value ? "" : dr["Age"].ToString(),
                        Relation = dr["Relation"] == DBNull.Value ? "Self" : dr["Relation"].ToString(),
                        Hospital_Id = dr["Hospital_Id"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Hospital_Id"]),
                        SubHospital_Id = dr["SubHospital_Id"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["SubHospital_Id"]),
                        HospitalName = dr["HospitalName"] == DBNull.Value ? "" : dr["HospitalName"].ToString()
                    });
                }
            }
            catch (Exception ex) { throw new Exception("Error fetching profiles", ex); }
            return list;
        }

        public int SignupAccount(PatientAccount account, out string message)
        {
            message = string.Empty;
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_patient_signup_new", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_FirstName", account.FirstName);
                cmd.Parameters.AddWithValue("@p_LastName", account.LastName);
                cmd.Parameters.AddWithValue("@p_Email", account.Email);
                cmd.Parameters.AddWithValue("@p_Password", account.Password);
                cmd.Parameters.AddWithValue("@p_PhoneNumber", account.PhoneNumber ?? "");
                var pAccountId = new MySqlParameter("@p_AccountId", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
                var pMessage = new MySqlParameter("@p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pAccountId);
                cmd.Parameters.Add(pMessage);
                con.Open();
                cmd.ExecuteNonQuery();
                message = pMessage.Value?.ToString();
                return Convert.ToInt32(pAccountId.Value);
            }
            catch (Exception ex) { throw new Exception("Error during signup", ex); }
        }

        public void GenerateOTP(string email, string purpose, string otp)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_generate_otp", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_Email", email);
                cmd.Parameters.AddWithValue("@p_Purpose", purpose);
                cmd.Parameters.AddWithValue("@p_OTP", otp);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { throw new Exception("Error generating OTP", ex); }
        }

        public bool VerifyOTP(string email, string otp, string purpose)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_verify_otp", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_Email", email);
                cmd.Parameters.AddWithValue("@p_OTP", otp);
                cmd.Parameters.AddWithValue("@p_Purpose", purpose);
                var pResult = new MySqlParameter("@p_Result", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pResult);
                con.Open();
                cmd.ExecuteNonQuery();
                return Convert.ToBoolean(pResult.Value);
            }
            catch (Exception ex) { throw new Exception("Error verifying OTP", ex); }
        }

        public void ResetPasswordAccount(string email, string newPassword)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_reset_password", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_Email", email);
                cmd.Parameters.AddWithValue("@p_NewPassword", newPassword);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { throw new Exception("Error resetting password", ex); }
        }

        public bool EmailExistsInAccount(string email)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("SELECT COUNT(*) FROM tbl_patient_account WHERE Email = @email", con);
                cmd.Parameters.AddWithValue("@email", email);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (Exception ex) { throw new Exception("Error checking email", ex); }
        }

        public void AddFamilyMember(int accountId, string firstName, string lastName,
                                    string relation, int age, string gender, string email)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
                    INSERT INTO tbl_patient (FirstName, LastName, Gender, Age, Email, AccountId, Relation)
                    VALUES (@FirstName, @LastName, @Gender, @Age, @Email, @AccountId, @Relation)", con);
                cmd.Parameters.AddWithValue("@FirstName", firstName);
                cmd.Parameters.AddWithValue("@LastName", lastName);
                cmd.Parameters.AddWithValue("@Gender", gender ?? "");
                cmd.Parameters.AddWithValue("@Age", age);
                cmd.Parameters.AddWithValue("@Email", email ?? "");
                cmd.Parameters.AddWithValue("@AccountId", accountId);
                cmd.Parameters.AddWithValue("@Relation", relation);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { throw new Exception("Error adding family member", ex); }
        }

        public bool IsSamePassword(string email, string newPassword)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
                    SELECT COUNT(*) FROM tbl_patient_account
                    WHERE Email = @email AND Password = @password", con);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@password", newPassword);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (Exception ex) { throw new Exception("Error checking password", ex); }
        }
    }
}
