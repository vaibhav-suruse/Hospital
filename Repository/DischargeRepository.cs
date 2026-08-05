using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class DischargeRepository : IDischarge
    {
        private readonly string _connectionString;

        public DischargeRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // Every query below is scoped by hospitalId/subHospitalId, matching the
        // exact convention used by sp_GetIPDAdmissionById: p_SubHospitalId = 0
        // means "no sub-hospital filter" (single-hospital / main-hospital-only
        // sessions), passed as DBNull from C# just like IPDAdmissionRepository
        // already does for the same table.

        public DischargeModel GetAdmissionForDischarge(int ipdId, int hospitalId, int? subHospitalId)
        {
            DischargeModel model = null;
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetDischargeSummary", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            model = MapDischarge(reader);
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Error fetching admission for discharge.", ex);
            }
            return model;
        }

        public void DischargePatient(DischargeModel model, int updatedBy, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_DischargePatient", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
                    cmd.Parameters.AddWithValue("p_DischargeDoctorId", model.DischargeDoctorId);
                    cmd.Parameters.AddWithValue("p_DischargeType", model.DischargeType);
                    cmd.Parameters.AddWithValue("p_DischargeCondition", model.DischargeCondition);
                    cmd.Parameters.AddWithValue("p_FinalDiagnosis", model.FinalDiagnosis);
                    cmd.Parameters.AddWithValue("p_TreatmentSummary",
                        model.TreatmentSummary ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p_FollowUpDate",
                        model.FollowUpDate.HasValue ? (object)model.FollowUpDate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_FollowUpDoctorId",
                        model.FollowUpDoctorId.HasValue ? (object)model.FollowUpDoctorId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_DischargeInstructions",
                        model.DischargeInstructions ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p_DietInstructions",
                        model.DietInstructions ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p_ActivityRestrictions",
                        model.ActivityRestrictions ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p_SpecialNotes",
                        model.SpecialNotes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p_UpdatedBy", updatedBy);
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);

                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte)
                    { Direction = ParameterDirection.Output };
                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255)
                    { Direction = ParameterDirection.Output };

                    cmd.Parameters.Add(successParam);
                    cmd.Parameters.Add(messageParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    bool success = Convert.ToBoolean(successParam.Value);
                    string message = messageParam.Value?.ToString();


                    if (!success)
                        throw new Exception(message);
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error: {ex.Message}", ex);
            }
        }

        public DischargeModel GetDischargeSummary(int ipdId, int hospitalId, int? subHospitalId)
        {
            return GetAdmissionForDischarge(ipdId, hospitalId, subHospitalId);
        }

        private DischargeModel MapDischarge(MySqlDataReader reader)
        {
            return new DischargeModel
            {
                IPDId = Convert.ToInt32(reader["IPDId"]),
                PatientId = Convert.ToInt32(reader["PatientId"]),
                AdmissionNumber = reader["AdmissionNumber"]?.ToString(),
                PatientName = reader["PatientName"]?.ToString(),
                Gender = reader["Gender"]?.ToString(),
                Age = reader["Age"] == DBNull.Value
            ? (int?)null : Convert.ToInt32(reader["Age"]),
                PhoneNumber = reader["PhoneNumber"]?.ToString(),
                Address = reader["Address"]?.ToString(),
                AdmissionDateTime = Convert.ToDateTime(reader["AdmissionDateTime"]),
                ActualDischargeDateTime = reader["ActualDischargeDateTime"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(reader["ActualDischargeDateTime"]),
                DischargeType = reader["DischargeType"]?.ToString(),
                DischargeCondition = reader["DischargeCondition"]?.ToString(),
                Status = reader["Status"]?.ToString(),
                ReasonForAdmission = reader["ReasonForAdmission"]?.ToString(),
                FinalDiagnosis = reader["FinalDiagnosis"]?.ToString(),
                TreatmentSummary = reader["TreatmentSummary"]?.ToString(),
                FollowUpDate = reader["FollowUpDate"] == DBNull.Value
                    ? (DateTime?)null : Convert.ToDateTime(reader["FollowUpDate"]),
                DischargeInstructions = reader["DischargeInstructions"]?.ToString(),
                DietInstructions = reader["DietInstructions"]?.ToString(),
                ActivityRestrictions = reader["ActivityRestrictions"]?.ToString(),
                SpecialNotes = reader["SpecialNotes"]?.ToString(),
                PrimaryDoctorName = reader["PrimaryDoctorName"]?.ToString(),
                PrimaryDoctorSpecialization = reader["PrimaryDoctorSpecialization"]?.ToString(),
                DischargeDoctorName = reader["DischargeDoctorName"]?.ToString(),
                FollowUpDoctorName = reader["FollowUpDoctorName"]?.ToString(),
                BedNumber = reader["BedNumber"]?.ToString(),
                WardName = reader["WardName"]?.ToString(),
                RoomNumber = reader["RoomNumber"]?.ToString(),
                TotalDaysStayed = reader["TotalDaysStayed"] == DBNull.Value
                    ? 0 : Convert.ToInt32(reader["TotalDaysStayed"])
            };
        }

        public List<DischargeMedicineModel> GetDischargeMedicines(int ipdId, int hospitalId, int? subHospitalId)
        {
            var list = new List<DischargeMedicineModel>();
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetDischargeMedicines", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new DischargeMedicineModel
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                IPDId = Convert.ToInt32(reader["IPDId"]),
                                MedicineId = Convert.ToInt32(reader["MedicineId"]),
                                MedicineName = reader["MedicineName"]?.ToString(),
                                MedicineType = reader["MedicineType"]?.ToString(),
                                Morning = Convert.ToInt32(reader["Morning"]) == 1,
                                Afternoon = Convert.ToInt32(reader["Afternoon"]) == 1,
                                Evening = Convert.ToInt32(reader["Evening"]) == 1,
                                Days = reader["Days"] == DBNull.Value
                                    ? (int?)null : Convert.ToInt32(reader["Days"]),
                                Route = reader["Route"]?.ToString(),
                                Dosage = reader["Dosage"]?.ToString(),
                                Instructions = reader["Instructions"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Error fetching discharge medicines.", ex);
            }
            return list;
        }

        public void SaveDischargeMedicine(DischargeMedicineModel model, int createdBy, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_SaveDischargeMedicines", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
                    cmd.Parameters.AddWithValue("p_MedicineId", model.MedicineId);
                    cmd.Parameters.AddWithValue("p_Morning", model.Morning ? 1 : 0);
                    cmd.Parameters.AddWithValue("p_Afternoon", model.Afternoon ? 1 : 0);
                    cmd.Parameters.AddWithValue("p_Evening", model.Evening ? 1 : 0);
                    cmd.Parameters.AddWithValue("p_Days",
                        model.Days.HasValue ? (object)model.Days.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_Route",
                        model.Route ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p_Dosage",
                        model.Dosage ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p_Instructions",
                        model.Instructions ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p_CreatedBy", createdBy);
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);

                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte)
                    { Direction = ParameterDirection.Output };
                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255)
                    { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(successParam);
                    cmd.Parameters.Add(messageParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    if (!Convert.ToBoolean(successParam.Value))
                        throw new Exception(messageParam.Value?.ToString());
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Error saving discharge medicine.", ex);
            }
        }

        public void DeleteDischargeMedicine(int id, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_DeleteDischargeMedicine", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_Id", id);
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);

                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte)
                    { Direction = ParameterDirection.Output };
                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255)
                    { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(successParam);
                    cmd.Parameters.Add(messageParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    if (!Convert.ToBoolean(successParam.Value))
                        throw new Exception(messageParam.Value?.ToString());
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Error deleting discharge medicine.", ex);
            }
        }

    }
}
