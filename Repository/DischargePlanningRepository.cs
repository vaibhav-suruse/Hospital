using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class DischargePlanningRepository : IDischargePlanning
    {
        private readonly string _connectionString;

        public DischargePlanningRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        public DischargePlanningModel GetByIPD(int ipdId, int hospitalId, int? subHospitalId)
        {
            DischargePlanningModel model = null;
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_DPL_GetByIPD", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : 0);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        model = new DischargePlanningModel
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            IPDId = Convert.ToInt32(r["IPDId"]),
                            EstimatedDischargeDate = r["EstimatedDischargeDate"] == DBNull.Value
                                ? (DateTime?)null : Convert.ToDateTime(r["EstimatedDischargeDate"]),
                            Disposition = r["Disposition"] == DBNull.Value ? null : r["Disposition"].ToString(),
                            BarriersToDischarge = r["BarriersToDischarge"] == DBNull.Value ? null : r["BarriersToDischarge"].ToString(),
                            EquipmentNeeded = r["EquipmentNeeded"] == DBNull.Value ? null : r["EquipmentNeeded"].ToString(),
                            EquipmentArranged = Convert.ToInt32(r["EquipmentArranged"]) == 1,
                            ReferralsNeeded = r["ReferralsNeeded"] == DBNull.Value ? null : r["ReferralsNeeded"].ToString(),
                            ReferralsArranged = Convert.ToInt32(r["ReferralsArranged"]) == 1,
                            PatientEducationStatus = r["PatientEducationStatus"]?.ToString() ?? "Not Started",
                            FollowUpBooked = Convert.ToInt32(r["FollowUpBooked"]) == 1,
                            PendingReportsCollected = Convert.ToInt32(r["PendingReportsCollected"]) == 1,
                            CaseManagerNotes = r["CaseManagerNotes"] == DBNull.Value ? null : r["CaseManagerNotes"].ToString(),
                            UpdatedByUserId = r["UpdatedByUserId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["UpdatedByUserId"]),
                            UpdatedByName = r["UpdatedByName"] == DBNull.Value ? null : r["UpdatedByName"].ToString(),
                            UpdatedDate = r["UpdatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["UpdatedDate"])
                        };
                    }
                }
            }
            return model; // null means no planning record exists yet for this admission
        }

        public void SavePlanning(DischargePlanningModel model, int updatedBy, int hospitalId, int? subHospitalId)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_DPL_Upsert", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
                cmd.Parameters.AddWithValue("p_ParentHospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : 0);
                cmd.Parameters.AddWithValue("p_EstimatedDischargeDate",
                    model.EstimatedDischargeDate.HasValue ? (object)model.EstimatedDischargeDate.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_Disposition", model.Disposition ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_BarriersToDischarge", model.BarriersToDischarge ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_EquipmentNeeded", model.EquipmentNeeded ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_EquipmentArranged", model.EquipmentArranged ? 1 : 0);
                cmd.Parameters.AddWithValue("p_ReferralsNeeded", model.ReferralsNeeded ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_ReferralsArranged", model.ReferralsArranged ? 1 : 0);
                cmd.Parameters.AddWithValue("p_PatientEducationStatus", model.PatientEducationStatus ?? "Not Started");
                cmd.Parameters.AddWithValue("p_FollowUpBooked", model.FollowUpBooked ? 1 : 0);
                cmd.Parameters.AddWithValue("p_PendingReportsCollected", model.PendingReportsCollected ? 1 : 0);
                cmd.Parameters.AddWithValue("p_CaseManagerNotes", model.CaseManagerNotes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_UpdatedByUserId", updatedBy);

                var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(successParam);
                cmd.Parameters.Add(messageParam);

                con.Open();
                cmd.ExecuteNonQuery();

                if (!Convert.ToBoolean(successParam.Value))
                    throw new Exception(messageParam.Value?.ToString());
            }
        }
    }
}
