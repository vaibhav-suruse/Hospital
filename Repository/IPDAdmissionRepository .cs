using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class IPDAdmissionRepository: IIPDAdmission
    {
        private readonly string _connectionString;
        private readonly ILogger<IPDAdmissionRepository> _logger;
        public IPDAdmissionRepository(IConfiguration configuration, ILogger<IPDAdmissionRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        // ===================== ADD =====================
        public int AddIPDAdmission(IPDAdmissionModel model, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_InsertIPDAdmission", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_ParentHospitalId", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@p_AdmissionNumber", model.AdmissionNumber);
                        cmd.Parameters.AddWithValue("@p_PatientId", model.PatientId);
                        cmd.Parameters.AddWithValue("@p_OPDVisitId", model.OPDVisitId.HasValue ? model.OPDVisitId.Value : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@p_PrimaryDoctorId", model.PrimaryDoctorId);
                        cmd.Parameters.AddWithValue("@p_ReferringDoctorId", model.ReferringDoctorId.HasValue ? model.ReferringDoctorId.Value : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@p_AdmissionType", model.AdmissionType);
                        cmd.Parameters.AddWithValue("@p_ExpectedDischargeDateTime", model.ExpectedDischargeDateTime.HasValue ? model.ExpectedDischargeDateTime.Value : (object)DBNull.Value);

                        // Add missing SP parameters
                        cmd.Parameters.AddWithValue("@p_ActualDischargeDateTime", model.ActualDischargeDateTime.HasValue ? model.ActualDischargeDateTime.Value : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@p_DischargeType", model.DischargeType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@p_Status", model.Status);
                        cmd.Parameters.AddWithValue("@p_ReasonForAdmission", model.ReasonForAdmission);
                        cmd.Parameters.AddWithValue("@p_CancellationReason", model.CancellationReason ?? (object)DBNull.Value);

                        conn.Open();
                       cmd.ExecuteNonQuery();

                        // Get the last inserted IPDId
                        cmd.CommandText = "SELECT LAST_INSERT_ID();";
                        cmd.CommandType = CommandType.Text;
                        int newIpdId = Convert.ToInt32(cmd.ExecuteScalar());

                        _logger.LogInformation("IPD Admission added successfully with IPDId: {0}", newIpdId);
                        return newIpdId;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding IPD Admission");
                throw;
            }
        }
        public int UpdateIPDAdmission(IPDAdmissionModel model, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_UpdateIPDAdmission", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_IPDId", model.IPDId);
                        cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospitalId",
                subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        cmd.Parameters.AddWithValue("@p_AdmissionNumber", model.AdmissionNumber);
                        cmd.Parameters.AddWithValue("@p_PrimaryDoctorId", model.PrimaryDoctorId);

                        cmd.Parameters.AddWithValue("@p_ReferringDoctorId",
                            model.ReferringDoctorId.HasValue ? model.ReferringDoctorId.Value : (object)DBNull.Value);

                       
                        cmd.Parameters.AddWithValue("@p_AdmissionType", model.AdmissionType);

                        cmd.Parameters.AddWithValue("@p_ExpectedDischargeDateTime",
                            model.ExpectedDischargeDateTime ?? (object)DBNull.Value);

                        cmd.Parameters.AddWithValue("@p_ActualDischargeDateTime",
                            model.ActualDischargeDateTime ?? (object)DBNull.Value);

                        cmd.Parameters.AddWithValue("@p_DischargeType", model.DischargeType);
                        cmd.Parameters.AddWithValue("@p_Status", model.Status);
                        cmd.Parameters.AddWithValue("@p_ReasonForAdmission", model.ReasonForAdmission);
                        cmd.Parameters.AddWithValue("@p_CancellationReason", model.CancellationReason);


                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        _logger.LogInformation("IPD Admission updated. ID: {IPDId}", model.IPDId);
                        return rows;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating IPD Admission ID: {IPDId}", model.IPDId);
                throw;
            }
        }

        public List<IPDAdmissionModel> GetAllIPDAdmissions(int hospitalId, int? subHospitalId)
        {
            List<IPDAdmissionModel> list = new List<IPDAdmissionModel>();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_GetAllIPDAdmissions", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_HospitalId", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospitalId",
                            subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        conn.Open();

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                IPDAdmissionModel model = new IPDAdmissionModel
                                {
                                    IPDId = reader.GetInt32("IPDId"),
                                    ParentHospitalId = reader.GetInt32("ParentHospitalId"),
                                    SubHospitalId = reader.IsDBNull(reader.GetOrdinal("SubHospitalId"))
                                                        ? (int?)null
                                                        : reader.GetInt32("SubHospitalId"),
                                    AdmissionNumber = reader["AdmissionNumber"]?.ToString(),
                                    PatientId = reader.GetInt32("PatientId"),
                                    OPDVisitId = reader.IsDBNull(reader.GetOrdinal("OPDVisitId"))
                                                        ? (int?)null
                                                        : reader.GetInt32("OPDVisitId"),
                                    PrimaryDoctorId = reader.GetInt32("PrimaryDoctorId"),
                                    ReferringDoctorId = reader.IsDBNull(reader.GetOrdinal("ReferringDoctorId"))
                                                        ? (int?)null
                                                        : reader.GetInt32("ReferringDoctorId"),
                                  
                                    AdmissionType = reader["AdmissionType"]?.ToString(),
                                    AdmissionDateTime = reader.GetDateTime("AdmissionDateTime"),
                                    ExpectedDischargeDateTime = reader.IsDBNull(reader.GetOrdinal("ExpectedDischargeDateTime"))
                                                        ? (DateTime?)null
                                                        : reader.GetDateTime("ExpectedDischargeDateTime"),
                                    ActualDischargeDateTime = reader.IsDBNull(reader.GetOrdinal("ActualDischargeDateTime"))
                                                        ? (DateTime?)null
                                                        : reader.GetDateTime("ActualDischargeDateTime"),
                                    DischargeType = reader["DischargeType"]?.ToString(),
                                    Status = reader["Status"]?.ToString(),
                                    ReasonForAdmission = reader["ReasonForAdmission"]?.ToString(),
                                    CancellationReason = reader["CancellationReason"]?.ToString(),
                                    IsActiveAdmission = reader.GetBoolean("IsActiveAdmission"),
                                    CreatedDate = reader.GetDateTime("CreatedDate"),
                                    UpdatedDate = reader.IsDBNull(reader.GetOrdinal("UpdatedDate"))
                                                        ? (DateTime?)null
                                                        : reader.GetDateTime("UpdatedDate")
                                };

                                list.Add(model);
                            }
                        }
                    }
                }

                _logger.LogInformation("Fetched IPD Admissions list.");
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching IPD Admissions");
                throw;
            }
        }

        public IPDAdmissionModel GetIPDAdmissionById(int ipdId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_GetIPDAdmissionById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_IPDId", ipdId);
                        cmd.Parameters.AddWithValue("@p_HospitalId", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospitalId",
                            subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        conn.Open();

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                IPDAdmissionModel model = new IPDAdmissionModel
                                {
                                    IPDId = reader.GetInt32("IPDId"),
                                    ParentHospitalId = reader.GetInt32("ParentHospitalId"),
                                    SubHospitalId = reader.IsDBNull(reader.GetOrdinal("SubHospitalId"))
                                                        ? (int?)null
                                                        : reader.GetInt32("SubHospitalId"),
                                    AdmissionNumber = reader["AdmissionNumber"]?.ToString(),
                                    PatientId = reader.GetInt32("PatientId"),
                                    OPDVisitId = reader.IsDBNull(reader.GetOrdinal("OPDVisitId"))
                                                        ? (int?)null
                                                        : reader.GetInt32("OPDVisitId"),
                                    PrimaryDoctorId = reader.GetInt32("PrimaryDoctorId"),
                                    ReferringDoctorId = reader.IsDBNull(reader.GetOrdinal("ReferringDoctorId"))
                                                        ? (int?)null
                                                        : reader.GetInt32("ReferringDoctorId"),
                                  
                                    AdmissionType = reader["AdmissionType"]?.ToString(),
                                    AdmissionDateTime = reader.GetDateTime("AdmissionDateTime"),
                                    ExpectedDischargeDateTime = reader.IsDBNull(reader.GetOrdinal("ExpectedDischargeDateTime"))
                                                        ? (DateTime?)null
                                                        : reader.GetDateTime("ExpectedDischargeDateTime"),
                                    ActualDischargeDateTime = reader.IsDBNull(reader.GetOrdinal("ActualDischargeDateTime"))
                                                        ? (DateTime?)null
                                                        : reader.GetDateTime("ActualDischargeDateTime"),
                                    DischargeType = reader["DischargeType"]?.ToString(),
                                    Status = reader["Status"]?.ToString(),
                                    ReasonForAdmission = reader["ReasonForAdmission"]?.ToString(),
                                    CancellationReason = reader["CancellationReason"]?.ToString(),
                                    IsActiveAdmission = reader.GetBoolean("IsActiveAdmission"),
                                    CreatedDate = reader.GetDateTime("CreatedDate"),
                                    UpdatedDate = reader.IsDBNull(reader.GetOrdinal("UpdatedDate"))
                                                        ? (DateTime?)null
                                                        : reader.GetDateTime("UpdatedDate")
                                };

                                return model;
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching IPD Admission ID: {IPDId}", ipdId);
                throw;
            }
        }

        public int DeleteIPDAdmission(int ipdId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_DeleteIPDAdmission", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_IPDId", ipdId);
                        cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId); // fixed
                        cmd.Parameters.AddWithValue("@p_SubHospitalId",
                            subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        _logger.LogInformation("IPD Admission deleted successfully. ID: {IPDId}", ipdId);

                        return rowsAffected;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting IPD Admission ID: {IPDId}", ipdId);
                throw;
            }
        }
        public List<IPDAdmissionWithBedVM> GetAll_IPDAdmissions(int hospitalId, int? subHospitalId)
        {
            List<IPDAdmissionWithBedVM> list = new List<IPDAdmissionWithBedVM>();

            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                using (MySqlCommand cmd = new MySqlCommand("sp_GetAll_IPDAdmissions", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);

                    con.Open();

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            IPDAdmissionWithBedVM model = new IPDAdmissionWithBedVM
                            {
                                IPDId = Convert.ToInt32(reader["IPDId"]),
                                ParentHospitalId = Convert.ToInt32(reader["ParentHospitalId"]),

                                SubHospitalId = reader["SubHospitalId"] != DBNull.Value
                                    ? Convert.ToInt32(reader["SubHospitalId"])
                                    : (int?)null,

                                PatientId = Convert.ToInt32(reader["PatientId"]),
                                AdmissionDate = Convert.ToDateTime(reader["AdmissionDate"]),

                                AllocationId = reader["AllocationId"] != DBNull.Value
                                    ? Convert.ToInt32(reader["AllocationId"])
                                    : (int?)null,

                                BedId = reader["BedId"] != DBNull.Value
                                    ? Convert.ToInt32(reader["BedId"])
                                    : (int?)null,

                                StartDateTime = reader["StartDateTime"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["StartDateTime"])
                                    : (DateTime?)null,

                                EndDateTime = reader["EndDateTime"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["EndDateTime"])
                                    : (DateTime?)null,

                                IsCurrent = reader["IsCurrent"] != DBNull.Value
                                    ? Convert.ToBoolean(reader["IsCurrent"])
                                    : (bool?)null,

                                AllocatedBy = reader["AllocatedBy"] != DBNull.Value
                                    ? Convert.ToInt32(reader["AllocatedBy"])
                                    : (int?)null,

                                Days = reader["Days"] != DBNull.Value
                                    ? Convert.ToInt32(reader["Days"])
                                    : (int?)null,

                                BedFullLocation = reader["BedFullLocation"].ToString(),
                                PatientName = reader["PatientName"]?.ToString(),
                                ReferenceDoctorName = reader["ReferenceDoctorName"] == DBNull.Value ? "" : reader["ReferenceDoctorName"].ToString(),
                                AdmissionNumber = reader["AdmissionNumber"]?.ToString(),
                                AdmissionType = reader["AdmissionType"]?.ToString(),
                                Status = reader["Status"]?.ToString(),
                                DoctorName = reader["DoctorName"].ToString()    // <-- old: maybe FirstName + LastName separate

                            };

                            list.Add(model);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while fetching IPD Admissions. HospitalId: {HospitalId}, SubHospitalId: {SubHospitalId}",
                    hospitalId, subHospitalId);

                throw;
            }

            return list;
        }
    }
}
