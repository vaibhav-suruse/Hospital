
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class RadiologyRepository : IRadiology
    {
        private readonly string _connectionString;

        public RadiologyRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // ================= PATIENT RADIOLOGY REPORTS =================
        public List<RadiologyReportModel> GetReportsByIPD(int ipdId)
        {
            var list = new List<RadiologyReportModel>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_RR_GetReportsByIPD", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new RadiologyReportModel
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            ParentHospitalId = Convert.ToInt32(r["ParentHospitalId"]),
                            SubHospitalId = r["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["SubHospitalId"]),
                            IPDId = Convert.ToInt32(r["IPDId"]),
                            Title = r["Title"].ToString(),
                            Notes = r["Notes"].ToString(),
                            RadiologistDoctorId = r["RadiologistDoctorId"] == DBNull.Value ? 0 : Convert.ToInt32(r["RadiologistDoctorId"]),
                            RadiologistName = r["RadiologistName"] == DBNull.Value ? null : r["RadiologistName"].ToString(),
                            ReportDateTime = Convert.ToDateTime(r["ReportDateTime"]),
                            EnteredByUserId = r["EnteredByUserId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["EnteredByUserId"]),
                            EnteredByName = r["EnteredByName"] == DBNull.Value ? null : r["EnteredByName"].ToString(),
                            CreatedDate = Convert.ToDateTime(r["CreatedDate"]),
                            UpdatedDate = r["UpdatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["UpdatedDate"])
                        });
                    }
                }
            }
            return list;
        }

        public int InsertReport(RadiologyReportModel model)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_RR_InsertReport", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_ParentHospitalId", model.ParentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
                cmd.Parameters.AddWithValue("p_Title", model.Title);
                cmd.Parameters.AddWithValue("p_Notes", model.Notes);
                cmd.Parameters.AddWithValue("p_RadiologistDoctorId", model.RadiologistDoctorId > 0 ? (object)model.RadiologistDoctorId : DBNull.Value);
                cmd.Parameters.AddWithValue("p_ReportDateTime", model.ReportDateTime);
                cmd.Parameters.AddWithValue("p_EnteredByUserId", model.EnteredByUserId.HasValue ? (object)model.EnteredByUserId.Value : DBNull.Value);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void UpdateReport(RadiologyReportModel model)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_RR_UpdateReport", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", model.Id);
                cmd.Parameters.AddWithValue("p_Title", model.Title);
                cmd.Parameters.AddWithValue("p_Notes", model.Notes);
                cmd.Parameters.AddWithValue("p_RadiologistDoctorId", model.RadiologistDoctorId > 0 ? (object)model.RadiologistDoctorId : DBNull.Value);
                cmd.Parameters.AddWithValue("p_ReportDateTime", model.ReportDateTime);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteReport(int id)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_RR_DeleteReport", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ================= TEMPLATES =================
        public List<RadiologyTemplateModel> GetTemplates(int hospitalId, string search)
        {
            var list = new List<RadiologyTemplateModel>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_RR_GetTemplates", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_Search", string.IsNullOrWhiteSpace(search) ? (object)DBNull.Value : search);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new RadiologyTemplateModel
                        {
                            TemplateId = Convert.ToInt32(r["TemplateId"]),
                            TemplateName = r["TemplateName"].ToString(),
                            TemplateText = r["TemplateText"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public int SaveTemplate(int hospitalId, RadiologyTemplateModel model, int createdBy)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_RR_SaveTemplate", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_TemplateName", model.TemplateName);
                cmd.Parameters.AddWithValue("p_TemplateText", model.TemplateText);
                cmd.Parameters.AddWithValue("p_CreatedBy", createdBy);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
