// Repository/TreatmentSheetRepository.cs
// NEW FILE. Talks only to the brand-new ipd_general_order(_template)
// tables and the new, additive columns on ipd_round_prescription /
// ipd_round_investigation. Never touches DailyNotesRepository,
// DoctorRoundRepository or their stored procedures.
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class TreatmentSheetRepository : ITreatmentSheet
    {
        private readonly string _connectionString;

        public TreatmentSheetRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // ================= GENERAL ORDERS =================
        public List<GeneralOrderModel> GetGeneralOrders(int ipdId)
        {
            var list = new List<GeneralOrderModel>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_GetGeneralOrdersByIPD", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new GeneralOrderModel
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            IPDId = Convert.ToInt32(r["IPDId"]),
                            OrderDetails = r["OrderDetails"].ToString(),
                            Priority = r["Priority"].ToString(),
                            Status = r["Status"].ToString(),
                            CreatedDate = Convert.ToDateTime(r["CreatedDate"]),
                            UpdatedDate = r["UpdatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["UpdatedDate"]),
                            DoctorId = Convert.ToInt32(r["DoctorId"]),
                            OrderByName = r["OrderByName"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public int InsertGeneralOrder(GeneralOrderModel model, DateTime orderDate)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_InsertGeneralOrder", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_ParentHospitalId", model.ParentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
                cmd.Parameters.AddWithValue("p_DoctorId", model.DoctorId);
                cmd.Parameters.AddWithValue("p_OrderDetails", model.OrderDetails);
                cmd.Parameters.AddWithValue("p_Priority", model.Priority ?? "Routine");
                cmd.Parameters.AddWithValue("p_OrderDate", orderDate);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void UpdateGeneralOrder(GeneralOrderModel model)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_UpdateGeneralOrder", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", model.Id);
                cmd.Parameters.AddWithValue("p_OrderDetails", model.OrderDetails);
                cmd.Parameters.AddWithValue("p_Priority", model.Priority ?? "Routine");
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void SetGeneralOrderStatus(int id, string status)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_SetGeneralOrderStatus", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", id);
                cmd.Parameters.AddWithValue("p_Status", status);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteGeneralOrder(int id)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_DeleteGeneralOrder", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ================= GENERAL ORDER TEMPLATES =================
        public List<GeneralOrderTemplateModel> GetGeneralOrderTemplates(int hospitalId, string search)
        {
            var list = new List<GeneralOrderTemplateModel>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_GetGeneralOrderTemplates", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_Search", string.IsNullOrWhiteSpace(search) ? (object)DBNull.Value : search);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new GeneralOrderTemplateModel
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

        public int SaveGeneralOrderTemplate(int hospitalId, GeneralOrderTemplateModel model, int createdBy)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_SaveGeneralOrderTemplate", con))
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

        // ================= MEDICATIONS =================
        public List<TreatmentMedicineVM> GetMedications(int ipdId)
        {
            var list = new List<TreatmentMedicineVM>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_GetMedicationsByIPD", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new TreatmentMedicineVM
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            IPDId = Convert.ToInt32(r["IPDId"]),
                            MedicineId = Convert.ToInt32(r["MedicineId"]),
                            MedicineName = r["MedicineName"].ToString(),
                            MedicineType = r["MedicineType"] == DBNull.Value ? "" : r["MedicineType"].ToString(),
                            Morning = Convert.ToBoolean(r["Morning"]),
                            Afternoon = Convert.ToBoolean(r["Afternoon"]),
                            Evening = Convert.ToBoolean(r["Evening"]),
                            FrequencyText = r["FrequencyText"] == DBNull.Value ? null : r["FrequencyText"].ToString(),
                            Days = r["Days"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Days"]),
                            Route = r["Route"] == DBNull.Value ? "Oral" : r["Route"].ToString(),
                            Dosage = r["Dosage"] == DBNull.Value ? "" : r["Dosage"].ToString(),
                            FoodInstruction = r["FoodInstruction"] == DBNull.Value ? null : r["FoodInstruction"].ToString(),
                            DurationType = r["DurationType"] == DBNull.Value ? "Duration" : r["DurationType"].ToString(),
                            Qty = r["Qty"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Qty"]),
                            Instructions = r["Instructions"] == DBNull.Value ? "" : r["Instructions"].ToString(),
                            Status = r["Status"].ToString(),
                            SortOrder = Convert.ToInt32(r["SortOrder"]),
                            CreatedDate = Convert.ToDateTime(r["CreatedDate"]),
                            DoctorId = Convert.ToInt32(r["DoctorId"]),
                            OrderByName = r["OrderByName"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public int InsertMedicine(int parentHospitalId, int? subHospitalId, int ipdId, int doctorId, TreatmentMedicineVM model, DateTime orderDate)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_InsertMedicine", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                cmd.Parameters.AddWithValue("p_DoctorId", doctorId);
                cmd.Parameters.AddWithValue("p_MedicineId", model.MedicineId);
                cmd.Parameters.AddWithValue("p_Morning", model.Morning);
                cmd.Parameters.AddWithValue("p_Afternoon", model.Afternoon);
                cmd.Parameters.AddWithValue("p_Evening", model.Evening);
                cmd.Parameters.AddWithValue("p_FrequencyText", model.FrequencyText ?? "");
                cmd.Parameters.AddWithValue("p_Days", model.Days.HasValue ? (object)model.Days.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_Route", model.Route ?? "Oral");
                cmd.Parameters.AddWithValue("p_Dosage", model.Dosage ?? "");
                cmd.Parameters.AddWithValue("p_FoodInstruction", model.FoodInstruction ?? "");
                cmd.Parameters.AddWithValue("p_DurationType", model.DurationType ?? "Duration");
                cmd.Parameters.AddWithValue("p_Qty", model.Qty.HasValue ? (object)model.Qty.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_Instructions", model.Instructions ?? "");
                cmd.Parameters.AddWithValue("p_OrderDate", orderDate);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void UpdateMedicine(TreatmentMedicineVM model)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_UpdateMedicine", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", model.Id);
                cmd.Parameters.AddWithValue("p_Morning", model.Morning);
                cmd.Parameters.AddWithValue("p_Afternoon", model.Afternoon);
                cmd.Parameters.AddWithValue("p_Evening", model.Evening);
                cmd.Parameters.AddWithValue("p_FrequencyText", model.FrequencyText ?? "");
                cmd.Parameters.AddWithValue("p_Days", model.Days.HasValue ? (object)model.Days.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_Route", model.Route ?? "Oral");
                cmd.Parameters.AddWithValue("p_Dosage", model.Dosage ?? "");
                cmd.Parameters.AddWithValue("p_FoodInstruction", model.FoodInstruction ?? "");
                cmd.Parameters.AddWithValue("p_DurationType", model.DurationType ?? "Duration");
                cmd.Parameters.AddWithValue("p_Qty", model.Qty.HasValue ? (object)model.Qty.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_Instructions", model.Instructions ?? "");
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateMedicineSortOrder(int id, int sortOrder)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_UpdateMedicineSortOrder", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", id);
                cmd.Parameters.AddWithValue("p_SortOrder", sortOrder);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ================= INVESTIGATIONS =================
        public List<TreatmentInvestigationVM> GetInvestigations(int ipdId)
        {
            var list = new List<TreatmentInvestigationVM>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_GetInvestigationsByIPD", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new TreatmentInvestigationVM
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            IPDId = Convert.ToInt32(r["IPDId"]),
                            InvestigationType = r["InvestigationType"].ToString(),
                            TestName = r["TestName"].ToString(),
                            Priority = r["Priority"] == DBNull.Value ? "Routine" : r["Priority"].ToString(),
                            Instructions = r["Instructions"] == DBNull.Value ? "" : r["Instructions"].ToString(),
                            Status = r["Status"].ToString(),
                            OrderedDateTime = Convert.ToDateTime(r["OrderedDateTime"]),
                            CollectedDateTime = r["CollectedDateTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CollectedDateTime"]),
                            CompletedDateTime = r["CompletedDateTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CompletedDateTime"]),
                            Result = r["Result"] == DBNull.Value ? null : r["Result"].ToString(),
                            ResultFilePath = r["ResultFilePath"] == DBNull.Value ? null : r["ResultFilePath"].ToString(),
                            ReviewedByDoctor = Convert.ToBoolean(r["ReviewedByDoctor"]),
                            DoctorId = Convert.ToInt32(r["DoctorId"]),
                            OrderByName = r["OrderByName"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public int InsertInvestigation(int parentHospitalId, int? subHospitalId, int ipdId, int doctorId, TreatmentInvestigationVM model, DateTime orderDate)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_InsertInvestigation", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                cmd.Parameters.AddWithValue("p_DoctorId", doctorId);
                cmd.Parameters.AddWithValue("p_InvestigationType", model.InvestigationType ?? "Lab");
                cmd.Parameters.AddWithValue("p_TestName", model.TestName);
                cmd.Parameters.AddWithValue("p_Priority", model.Priority ?? "Routine");
                cmd.Parameters.AddWithValue("p_Instructions", model.Instructions ?? "");
                cmd.Parameters.AddWithValue("p_OrderDate", orderDate);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void UpdateInvestigation(TreatmentInvestigationVM model)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_UpdateInvestigation", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", model.Id);
                cmd.Parameters.AddWithValue("p_TestName", model.TestName);
                cmd.Parameters.AddWithValue("p_Priority", model.Priority ?? "Routine");
                cmd.Parameters.AddWithValue("p_Instructions", model.Instructions ?? "");
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteInvestigation(int id)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_TS_DeleteInvestigation", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
