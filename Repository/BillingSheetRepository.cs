using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
  
    public class BillingSheetRepository : IBillingSheet
    {
        private readonly string _connectionString;
        private readonly ILogger<BillingSheetRepository> _logger;

        public BillingSheetRepository(
            IConfiguration configuration,
            ILogger<BillingSheetRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        // ── GET ENTRIES ───────────────────────────────────────────────────
        public List<BillingSheetEntryModel> GetEntriesByIPD(int ipdId, string billingType)
        {
            var list = new List<BillingSheetEntryModel>();
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_BS_GetEntriesByIPD", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                    cmd.Parameters.AddWithValue("p_BillingType",
                        string.IsNullOrWhiteSpace(billingType) ? (object)DBNull.Value : billingType);

                    con.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(Map(r));
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in GetEntriesByIPD BillingSheet. IPDId={IPDId}, BillingType={BillingType}",
                    ipdId, billingType);
                throw new Exception("Error fetching billing sheet entries.", ex);
            }
            return list;
        }

        // ── INSERT ────────────────────────────────────────────────────────
        public int InsertEntry(
            int parentHospitalId,
            int? subHospitalId,
            BillingSheetEntryModel model,
            int? enteredByUserId)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_BS_InsertEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
                    cmd.Parameters.AddWithValue("p_BillingType", model.BillingType);
                    cmd.Parameters.AddWithValue("p_EntryDate", model.EntryDate.Date);
                    cmd.Parameters.AddWithValue("p_DoctorId",
                        model.DoctorId.HasValue ? (object)model.DoctorId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_NurseId",
                        model.NurseId.HasValue ? (object)model.NurseId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_ProviderName", model.ProviderName ?? "");
                    cmd.Parameters.AddWithValue("p_ServiceName", model.ServiceName ?? "");
                    cmd.Parameters.AddWithValue("p_BillingMasterId",
                        model.BillingMasterId.HasValue ? (object)model.BillingMasterId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_Charge", model.Charge);
                    cmd.Parameters.AddWithValue("p_EnteredByUserId",
                        enteredByUserId.HasValue ? (object)enteredByUserId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_Remarks",
                        !string.IsNullOrWhiteSpace(model.Remarks) ? (object)model.Remarks : DBNull.Value);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in InsertEntry BillingSheet. IPDId={IPDId}, BillingType={BillingType}",
                    model?.IPDId, model?.BillingType);
                throw new Exception("Error saving billing sheet entry.", ex);
            }
        }

        // ── UPDATE ────────────────────────────────────────────────────────
        public void UpdateEntry(BillingSheetEditModel model)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_BS_UpdateEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_Id", model.Id);
                    cmd.Parameters.AddWithValue("p_EntryDate", model.EntryDate.Date);
                    cmd.Parameters.AddWithValue("p_ServiceName", model.ServiceName ?? "");
                    cmd.Parameters.AddWithValue("p_Charge", model.Charge);
                    cmd.Parameters.AddWithValue("p_Remarks",
                        !string.IsNullOrWhiteSpace(model.Remarks) ? (object)model.Remarks : DBNull.Value);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in UpdateEntry BillingSheet. Id={Id}", model?.Id);
                throw new Exception("Error updating billing sheet entry.", ex);
            }
        }

        // ── SOFT DELETE ───────────────────────────────────────────────────
        public void DeleteEntry(int id)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_BS_DeleteEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_Id", id);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in DeleteEntry BillingSheet. Id={Id}", id);
                throw new Exception("Error deleting billing sheet entry.", ex);
            }
        }

        // ── TOTAL ─────────────────────────────────────────────────────────
        public decimal GetTotalByIPD(int ipdId, string billingType)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_BS_GetTotalByIPD", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                    cmd.Parameters.AddWithValue("p_BillingType",
                        string.IsNullOrWhiteSpace(billingType) ? (object)DBNull.Value : billingType);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in GetTotalByIPD BillingSheet. IPDId={IPDId}, BillingType={BillingType}",
                    ipdId, billingType);
                throw new Exception("Error calculating billing sheet total.", ex);
            }
        }

        // ── MAP ───────────────────────────────────────────────────────────
        private BillingSheetEntryModel Map(MySqlDataReader r)
        {
            return new BillingSheetEntryModel
            {
                Id = Convert.ToInt32(r["Id"]),
                IPDId = Convert.ToInt32(r["IPDId"]),
                BillingType = r["BillingType"].ToString(),
                EntryDate = Convert.ToDateTime(r["EntryDate"]),
                DoctorId = r["DoctorId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["DoctorId"]),
                NurseId = r["NurseId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["NurseId"]),
                ProviderName = r["ProviderName"].ToString(),
                ServiceName = r["ServiceName"].ToString(),
                BillingMasterId = r["BillingMasterId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["BillingMasterId"]),
                Charge = Convert.ToDecimal(r["Charge"]),
                EnteredByUserId = r["EnteredByUserId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["EnteredByUserId"]),
                EnteredByName = r["EnteredByName"]?.ToString()?.Trim(),
                Remarks = r["Remarks"] == DBNull.Value ? null : r["Remarks"].ToString(),
                CreatedDate = Convert.ToDateTime(r["CreatedDate"]),
                UpdatedDate = r["UpdatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["UpdatedDate"])
            };
        }
    }
}
