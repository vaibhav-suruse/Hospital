using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class ExtraOrdersRepository : IExtraOrders
    {
        private readonly string _connectionString;

        public ExtraOrdersRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // ================= EXTRA MEDICATION =================
        public List<ExtraMedicationModel> GetExtraMedications(int ipdId)
        {
            var list = new List<ExtraMedicationModel>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_EO_GetExtraMedicationsByIPD", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ExtraMedicationModel
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            IPDId = Convert.ToInt32(r["IPDId"]),
                            MedicineId = r["MedicineId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["MedicineId"]),
                            MedicineName = r["MedicineName"].ToString(),
                            Reason = r["Reason"] == DBNull.Value ? null : r["Reason"].ToString(),
                            Remark = r["Remark"] == DBNull.Value ? null : r["Remark"].ToString(),
                            TimeGiven = Convert.ToDateTime(r["TimeGiven"]),
                            CreatedDate = Convert.ToDateTime(r["CreatedDate"]),
                            EnteredByDoctorId = r["EnteredByDoctorId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["EnteredByDoctorId"]),
                            EnteredByName = r["EnteredByName"] == DBNull.Value ? null : r["EnteredByName"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public int InsertExtraMedication(int parentHospitalId, int? subHospitalId, ExtraMedicationModel model, DateTime timeGiven)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_EO_InsertExtraMedication", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
                cmd.Parameters.AddWithValue("p_MedicineId", model.MedicineId.HasValue ? (object)model.MedicineId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_MedicineName", model.MedicineName ?? "");
                cmd.Parameters.AddWithValue("p_Reason", model.Reason ?? "");
                cmd.Parameters.AddWithValue("p_Remark", model.Remark ?? "");
                cmd.Parameters.AddWithValue("p_EnteredByDoctorId", model.EnteredByDoctorId.HasValue ? (object)model.EnteredByDoctorId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_TimeGiven", timeGiven);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void UpdateExtraMedication(ExtraMedicationModel model)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_EO_UpdateExtraMedication", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", model.Id);
                cmd.Parameters.AddWithValue("p_MedicineName", model.MedicineName ?? "");
                cmd.Parameters.AddWithValue("p_Reason", model.Reason ?? "");
                cmd.Parameters.AddWithValue("p_Remark", model.Remark ?? "");
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteExtraMedication(int id)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_EO_DeleteExtraMedication", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ================= EXTRA ORDERS =================
        public List<ExtraOrderModel> GetExtraOrders(int ipdId)
        {
            var list = new List<ExtraOrderModel>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_EO_GetExtraOrdersByIPD", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ExtraOrderModel
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            IPDId = Convert.ToInt32(r["IPDId"]),
                            OrderDetails = r["OrderDetails"].ToString(),
                            Remark = r["Remark"] == DBNull.Value ? null : r["Remark"].ToString(),
                            TimeGiven = Convert.ToDateTime(r["TimeGiven"]),
                            CreatedDate = Convert.ToDateTime(r["CreatedDate"]),
                            EnteredByDoctorId = r["EnteredByDoctorId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["EnteredByDoctorId"]),
                            EnteredByName = r["EnteredByName"] == DBNull.Value ? null : r["EnteredByName"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public int InsertExtraOrder(int parentHospitalId, int? subHospitalId, ExtraOrderModel model, DateTime timeGiven)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_EO_InsertExtraOrder", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
                cmd.Parameters.AddWithValue("p_OrderDetails", model.OrderDetails);
                cmd.Parameters.AddWithValue("p_Remark", model.Remark ?? "");
                cmd.Parameters.AddWithValue("p_EnteredByDoctorId", model.EnteredByDoctorId.HasValue ? (object)model.EnteredByDoctorId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_TimeGiven", timeGiven);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void UpdateExtraOrder(ExtraOrderModel model)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_EO_UpdateExtraOrder", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", model.Id);
                cmd.Parameters.AddWithValue("p_OrderDetails", model.OrderDetails);
                cmd.Parameters.AddWithValue("p_Remark", model.Remark ?? "");
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteExtraOrder(int id)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_EO_DeleteExtraOrder", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_Id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
