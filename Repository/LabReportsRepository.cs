// Repository/LabReportsRepository.cs
// NEW FILE. Talks only to the new lab_test_category / lab_test_parameter /
// ipd_lab_report_value tables via the new sp_LR_* procedures.
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class LabReportsRepository : ILabReports
    {
        private readonly string _connectionString;

        public LabReportsRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // ================= CATEGORIES =================
        public List<LabTestCategoryModel> GetCategories(int hospitalId)
        {
            var list = new List<LabTestCategoryModel>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_LR_GetCategories", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new LabTestCategoryModel
                        {
                            CategoryId = Convert.ToInt32(r["CategoryId"]),
                            CategoryName = r["CategoryName"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public int InsertCategory(int hospitalId, string categoryName)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_LR_InsertCategory", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_CategoryName", categoryName);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void DeleteCategory(int categoryId)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_LR_DeleteCategory", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_CategoryId", categoryId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ================= PARAMETERS =================
        public List<LabTestParameterModel> GetParametersByCategory(int categoryId)
        {
            var list = new List<LabTestParameterModel>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_LR_GetParametersByCategory", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_CategoryId", categoryId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new LabTestParameterModel
                        {
                            ParameterId = Convert.ToInt32(r["ParameterId"]),
                            CategoryId = Convert.ToInt32(r["CategoryId"]),
                            ParameterName = r["ParameterName"].ToString(),
                            Unit = r["Unit"] == DBNull.Value ? null : r["Unit"].ToString(),
                            NormalRangeLow = r["NormalRangeLow"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["NormalRangeLow"]),
                            NormalRangeHigh = r["NormalRangeHigh"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["NormalRangeHigh"]),
                            NormalRangeText = r["NormalRangeText"] == DBNull.Value ? null : r["NormalRangeText"].ToString(),
                            CriticalLow = r["CriticalLow"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["CriticalLow"]),
                            CriticalHigh = r["CriticalHigh"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["CriticalHigh"]),
                            CriticalText = r["CriticalText"] == DBNull.Value ? null : r["CriticalText"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public int InsertParameter(LabTestParameterModel model)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_LR_InsertParameterWithCritical", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_CategoryId", model.CategoryId);
                cmd.Parameters.AddWithValue("p_ParameterName", model.ParameterName);
                cmd.Parameters.AddWithValue("p_Unit", model.Unit ?? "");
                cmd.Parameters.AddWithValue("p_RangeLow", model.NormalRangeLow.HasValue ? (object)model.NormalRangeLow.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_RangeHigh", model.NormalRangeHigh.HasValue ? (object)model.NormalRangeHigh.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_RangeText", model.NormalRangeText ?? "");
                cmd.Parameters.AddWithValue("p_CriticalLow", model.CriticalLow.HasValue ? (object)model.CriticalLow.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_CriticalHigh", model.CriticalHigh.HasValue ? (object)model.CriticalHigh.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_CriticalText", model.CriticalText ?? "");
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void UpdateParameter(LabTestParameterModel model)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_LR_UpdateParameterWithCritical", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_ParameterId", model.ParameterId);
                cmd.Parameters.AddWithValue("p_ParameterName", model.ParameterName);
                cmd.Parameters.AddWithValue("p_Unit", model.Unit ?? "");
                cmd.Parameters.AddWithValue("p_RangeLow", model.NormalRangeLow.HasValue ? (object)model.NormalRangeLow.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_RangeHigh", model.NormalRangeHigh.HasValue ? (object)model.NormalRangeHigh.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_RangeText", model.NormalRangeText ?? "");
                cmd.Parameters.AddWithValue("p_CriticalLow", model.CriticalLow.HasValue ? (object)model.CriticalLow.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_CriticalHigh", model.CriticalHigh.HasValue ? (object)model.CriticalHigh.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_CriticalText", model.CriticalText ?? "");
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteParameter(int parameterId)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_LR_DeleteParameter", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_ParameterId", parameterId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ================= PATIENT LAB VALUES =================
        public List<LabReportValueVM> GetLabValues(int ipdId)
        {
            var list = new List<LabReportValueVM>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_LR_GetLabValuesByIPD", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new LabReportValueVM
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            IPDId = Convert.ToInt32(r["IPDId"]),
                            ParameterId = Convert.ToInt32(r["ParameterId"]),
                            ParameterName = r["ParameterName"].ToString(),
                            CategoryName = r["CategoryName"].ToString(),
                            Unit = r["Unit"] == DBNull.Value ? null : r["Unit"].ToString(),
                            NormalRangeLow = r["NormalRangeLow"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["NormalRangeLow"]),
                            NormalRangeHigh = r["NormalRangeHigh"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["NormalRangeHigh"]),
                            NormalRangeText = r["NormalRangeText"] == DBNull.Value ? null : r["NormalRangeText"].ToString(),
                            CriticalLow = r["CriticalLow"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["CriticalLow"]),
                            CriticalHigh = r["CriticalHigh"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["CriticalHigh"]),
                            CriticalText = r["CriticalText"] == DBNull.Value ? null : r["CriticalText"].ToString(),
                            ResultValue = r["ResultValue"].ToString(),
                            ReportDate = Convert.ToDateTime(r["ReportDate"]),
                            CreatedDate = Convert.ToDateTime(r["CreatedDate"]),
                            EnteredByDoctorId = r["EnteredByDoctorId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["EnteredByDoctorId"]),
                            EnteredByName = r["EnteredByName"] == DBNull.Value ? null : r["EnteredByName"].ToString(),
                            NotificationId = r["NotificationId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["NotificationId"]),
                            NotifiedAt = r["NotifiedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["NotifiedAt"]),
                            NotifiedToDoctorName = r["NotifiedToDoctorName"] == DBNull.Value ? null : r["NotifiedToDoctorName"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public int InsertLabValue(int parentHospitalId, int? subHospitalId, int ipdId, int parameterId, string resultValue, DateTime reportDate, int? doctorId)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_LR_InsertLabValue", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                cmd.Parameters.AddWithValue("p_ParameterId", parameterId);
                cmd.Parameters.AddWithValue("p_ResultValue", resultValue ?? "");
                cmd.Parameters.AddWithValue("p_ReportDate", reportDate);
                cmd.Parameters.AddWithValue("p_EnteredByDoctorId", doctorId.HasValue ? (object)doctorId.Value : DBNull.Value);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void UpdateLabValue(int id, string resultValue)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_LR_UpdateLabValue", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", id);
                cmd.Parameters.AddWithValue("p_ResultValue", resultValue ?? "");
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteLabValue(int id)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_LR_DeleteLabValue", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public int InsertCriticalNotification(int labValueId, int notifiedToDoctorId, int? notifiedByUserId, string remarks)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_LR_InsertCriticalNotification", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_LabValueId", labValueId);
                cmd.Parameters.AddWithValue("p_NotifiedToDoctorId", notifiedToDoctorId);
                cmd.Parameters.AddWithValue("p_NotifiedByUserId", notifiedByUserId.HasValue ? (object)notifiedByUserId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_Remarks", remarks ?? "");
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
