using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class OPDBillingRepository : IOPDBilling
    {
        private readonly string _connectionString;
        private readonly ILogger<OPDBillingRepository> _logger;

        public OPDBillingRepository(
            IConfiguration configuration,
            ILogger<OPDBillingRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        public OPDBillVM GetBillSummary(int appointmentId, int hospitalId, int? subHospitalId)
        {
            var vm = new OPDBillVM { AppointmentId = appointmentId };
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPB_GetBillSummary", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_AppointmentId", appointmentId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                conn.Open();

                using var reader = cmd.ExecuteReader();

                // RS1 — Patient + Appointment info
                if (reader.Read())
                {
                    vm.AppointmentId = Convert.ToInt32(reader["AppointmentId"]);
                    vm.PatientId = Convert.ToInt32(reader["PatientId"]);
                    vm.PatientName = reader["PatientName"].ToString();
                    vm.Age = reader["Age"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Age"]);
                    vm.Gender = reader["Gender"]?.ToString();
                    vm.PhoneNumber = reader["PhoneNumber"]?.ToString();
                    vm.DoctorName = reader["DoctorName"].ToString();
                    vm.Specialization = reader["Specialization"]?.ToString();
                    vm.AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]);
                    vm.AppointmentStatus = reader["Status"]?.ToString();
                    vm.OPDId = reader["OPDId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["OPDId"]);
                }

                // RS2 — Medicines (now with real SellingPrice)
                reader.NextResult();
                while (reader.Read())
                {
                    vm.Medicines.Add(new OPDBillMedicine
                    {
                        MedicineId = Convert.ToInt32(reader["MedicineId"]),
                        MedicineName = reader["MedicineName"].ToString(),
                        Type = reader["Type"]?.ToString(),
                        Morning = Convert.ToBoolean(reader["Morning"]),
                        Afternoon = Convert.ToBoolean(reader["Afternoon"]),
                        Evening = Convert.ToBoolean(reader["Evening"]),
                        Days = reader["Days"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Days"]),
                        DispensedQuantity = reader["DispensedQuantity"] == DBNull.Value ? 1 : Convert.ToInt32(reader["DispensedQuantity"]),
                        UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                        TotalPrice = Convert.ToDecimal(reader["TotalPrice"])
                    });
                }

                // RS3 — Suggested consultation fee
                reader.NextResult();
                if (reader.Read())
                {
                    vm.SuggestedConsultationFee = reader["SuggestedConsultationFee"] == DBNull.Value
                        ? 0 : Convert.ToDecimal(reader["SuggestedConsultationFee"]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBillSummary. AppointmentId={Id}", appointmentId);
                throw;
            }
            return vm;
        }

        public OPDBill GetBillByAppointmentId(int appointmentId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPB_GetByAppointment", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_AppointmentId", appointmentId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                conn.Open();
                using var r = cmd.ExecuteReader();
                if (r.Read()) return MapBill(r);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBillByAppointmentId. AppointmentId={Id}", appointmentId);
                throw;
            }
        }

        public List<OPDBillItem> GetBillItems(int billId, int hospitalId, int? subHospitalId)
        {
            var list = new List<OPDBillItem>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPB_GetBillItems", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_BillId", billId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                conn.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new OPDBillItem
                    {
                        ItemId = Convert.ToInt32(r["ItemId"]),
                        BillId = Convert.ToInt32(r["BillId"]),
                        AppointmentId = Convert.ToInt32(r["AppointmentId"]),
                        ItemType = r["ItemType"].ToString(),
                        ItemName = r["ItemName"].ToString(),
                        Quantity = Convert.ToInt32(r["Quantity"]),
                        UnitPrice = Convert.ToDecimal(r["UnitPrice"]),
                        DiscountValue = Convert.ToDecimal(r["DiscountValue"]),
                        DiscountIsPercent = Convert.ToBoolean(r["DiscountIsPercent"]),
                        GstPercent = Convert.ToDecimal(r["GstPercent"]),
                        TotalPrice = Convert.ToDecimal(r["TotalPrice"]),
                        BillingMasterId = r["BillingMasterId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["BillingMasterId"])
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBillItems. BillId={Id}", billId);
                throw;
            }
            return list;
        }

        public List<OPDPayment> GetPayments(int billId, int hospitalId, int? subHospitalId)
        {
            var list = new List<OPDPayment>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPB_GetPayments", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_BillId", billId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                conn.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new OPDPayment
                    {
                        PaymentId = Convert.ToInt32(r["PaymentId"]),
                        ReceiptNumber = r["ReceiptNumber"]?.ToString(),
                        BillId = Convert.ToInt32(r["BillId"]),
                        AppointmentId = Convert.ToInt32(r["AppointmentId"]),
                        HospitalId = Convert.ToInt32(r["HospitalId"]),
                        SubHospitalId = r["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["SubHospitalId"]),
                        Amount = Convert.ToDecimal(r["Amount"]),
                        PaymentMode = r["PaymentMode"].ToString(),
                        TransactionRef = r["TransactionRef"]?.ToString(),
                        Notes = r["Notes"]?.ToString(),
                        ReceivedBy = r["ReceivedBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["ReceivedBy"]),
                        CollectedByName = r["CollectedByName"]?.ToString(),
                        PaymentDate = Convert.ToDateTime(r["PaymentDate"])
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPayments. BillId={Id}", billId);
                throw;
            }
            return list;
        }

        // ── TRUE UPSERT: one active bill per appointment ──────────────────
        // Header is updated in place if one already exists; items are
        // soft-deleted and re-inserted inside the SAME transaction as the
        // header write, so a save can never leave items and header out of
        // sync, and can never silently create a second bill.
        public int SaveBill(OPDBill bill, List<OPDBillItem> items, int hospitalId, int? subHospitalId, bool isDraft)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var tran = conn.BeginTransaction();
                try
                {
                    using var cmd = new MySqlCommand("sp_OPB_SaveBillHeader", conn, tran);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_AppointmentId", bill.AppointmentId);
                    cmd.Parameters.AddWithValue("p_OPDId", bill.OPDId.HasValue ? (object)bill.OPDId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_PatientId", bill.PatientId);
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                    cmd.Parameters.AddWithValue("p_BillDate", bill.BillDate == default ? DateTime.Now : bill.BillDate);
                    cmd.Parameters.AddWithValue("p_ConsultationFee", bill.ConsultationFee);
                    cmd.Parameters.AddWithValue("p_MedicineCharges", bill.MedicineCharges);
                    cmd.Parameters.AddWithValue("p_ProcedureCharges", bill.ProcedureCharges);
                    cmd.Parameters.AddWithValue("p_OtherCharges", bill.OtherCharges);
                    cmd.Parameters.AddWithValue("p_SubTotal", bill.SubTotal);
                    cmd.Parameters.AddWithValue("p_LineDiscountAmount", bill.LineDiscountAmount);
                    cmd.Parameters.AddWithValue("p_GstAmount", bill.GstAmount);
                    cmd.Parameters.AddWithValue("p_ExtraDiscountValue", bill.ExtraDiscountValue);
                    cmd.Parameters.AddWithValue("p_ExtraDiscountIsPercent", bill.ExtraDiscountIsPercent ? 1 : 0);
                    cmd.Parameters.AddWithValue("p_ExtraDiscountAmount", bill.ExtraDiscountAmount);
                    cmd.Parameters.AddWithValue("p_TotalAmount", bill.TotalAmount);
                    cmd.Parameters.AddWithValue("p_Notes", (object)bill.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("p_IsDraft", isDraft ? 1 : 0);
                    cmd.Parameters.AddWithValue("p_CreatedBy", bill.CreatedBy);

                    int billId = Convert.ToInt32(cmd.ExecuteScalar());

                    // Replace items: soft-delete existing, then insert the fresh set.
                    // This is what makes re-editing a bill actually work instead of
                    // silently piling up duplicate item rows.
                    using (var clearCmd = new MySqlCommand("sp_OPB_ClearBillItems", conn, tran))
                    {
                        clearCmd.CommandType = CommandType.StoredProcedure;
                        clearCmd.Parameters.AddWithValue("p_BillId", billId);
                        clearCmd.ExecuteNonQuery();
                    }

                    foreach (var item in items)
                    {
                        using var icmd = new MySqlCommand("sp_OPB_SaveBillItem", conn, tran);
                        icmd.CommandType = CommandType.StoredProcedure;
                        icmd.Parameters.AddWithValue("p_BillId", billId);
                        icmd.Parameters.AddWithValue("p_AppointmentId", bill.AppointmentId);
                        icmd.Parameters.AddWithValue("p_ItemType", item.ItemType);
                        icmd.Parameters.AddWithValue("p_BillingMasterId", item.BillingMasterId.HasValue ? (object)item.BillingMasterId.Value : DBNull.Value);
                        icmd.Parameters.AddWithValue("p_ItemName", item.ItemName);
                        icmd.Parameters.AddWithValue("p_Quantity", item.Quantity);
                        icmd.Parameters.AddWithValue("p_UnitPrice", item.UnitPrice);
                        icmd.Parameters.AddWithValue("p_DiscountValue", item.DiscountValue);
                        icmd.Parameters.AddWithValue("p_DiscountIsPercent", item.DiscountIsPercent ? 1 : 0);
                        icmd.Parameters.AddWithValue("p_GstPercent", item.GstPercent);
                        icmd.Parameters.AddWithValue("p_TotalPrice", item.TotalPrice);
                        icmd.ExecuteNonQuery();
                    }

                    tran.Commit();
                    return billId;
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveBill. AppointmentId={Id}", bill.AppointmentId);
                throw;
            }
        }

        public OPDBill CollectPayment(
            int billId, int appointmentId, int hospitalId, int? subHospitalId,
            decimal amount, string paymentMode, string transactionRef, string notes, int receivedBy)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPB_CollectPayment", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_BillId", billId);
                cmd.Parameters.AddWithValue("p_AppointmentId", appointmentId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                cmd.Parameters.AddWithValue("p_Amount", amount);
                cmd.Parameters.AddWithValue("p_PaymentMode", paymentMode);
                cmd.Parameters.AddWithValue("p_TransactionRef", (object)transactionRef ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_Notes", (object)notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_ReceivedBy", receivedBy);
                conn.Open();
                using var r = cmd.ExecuteReader();
                if (r.Read()) return MapBill(r);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CollectPayment. BillId={Id}", billId);
                throw;
            }
        }

        public OPDBill CancelBill(int billId, int hospitalId, int? subHospitalId, string reason, int cancelledBy)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPB_CancelBill", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_BillId", billId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                cmd.Parameters.AddWithValue("p_Reason", (object)reason ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_CancelledBy", cancelledBy);
                conn.Open();
                using var r = cmd.ExecuteReader();
                if (r.Read()) return MapBill(r);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelBill. BillId={Id}", billId);
                throw;
            }
        }

        private OPDBill MapBill(MySqlDataReader r)
        {
            return new OPDBill
            {
                BillId = Convert.ToInt32(r["BillId"]),
                AppointmentId = Convert.ToInt32(r["AppointmentId"]),
                OPDId = r["OPDId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["OPDId"]),
                PatientId = Convert.ToInt32(r["PatientId"]),
                HospitalId = Convert.ToInt32(r["HospitalId"]),
                SubHospitalId = r["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["SubHospitalId"]),
                BillNumber = r["BillNumber"].ToString(),
                BillDate = Convert.ToDateTime(r["BillDate"]),
                ConsultationFee = Convert.ToDecimal(r["ConsultationFee"]),
                MedicineCharges = Convert.ToDecimal(r["MedicineCharges"]),
                ProcedureCharges = Convert.ToDecimal(r["ProcedureCharges"]),
                OtherCharges = Convert.ToDecimal(r["OtherCharges"]),
                SubTotal = Convert.ToDecimal(r["SubTotal"]),
                LineDiscountAmount = Convert.ToDecimal(r["LineDiscountAmount"]),
                GstAmount = Convert.ToDecimal(r["GstAmount"]),
                ExtraDiscountValue = Convert.ToDecimal(r["ExtraDiscountValue"]),
                ExtraDiscountIsPercent = Convert.ToBoolean(r["ExtraDiscountIsPercent"]),
                ExtraDiscountAmount = Convert.ToDecimal(r["ExtraDiscountAmount"]),
                DiscountPercent = Convert.ToDecimal(r["DiscountPercent"]),
                DiscountAmount = Convert.ToDecimal(r["DiscountAmount"]),
                TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
                PaidAmount = Convert.ToDecimal(r["PaidAmount"]),
                DueAmount = Convert.ToDecimal(r["DueAmount"]),
                PaymentStatus = r["PaymentStatus"].ToString(),
                BillStatus = r["BillStatus"]?.ToString() ?? "Draft",
                CancelledReason = r["CancelledReason"]?.ToString(),
                CancelledBy = r["CancelledBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["CancelledBy"]),
                CancelledDate = r["CancelledDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CancelledDate"]),
                PaymentMode = r["PaymentMode"]?.ToString(),
                TransactionRef = r["TransactionRef"]?.ToString(),
                Notes = r["Notes"]?.ToString(),
                CreatedBy = r["CreatedBy"] == DBNull.Value ? 0 : Convert.ToInt32(r["CreatedBy"]),
                UpdatedBy = r["UpdatedBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["UpdatedBy"]),
                UpdatedDate = r["UpdatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["UpdatedDate"])
            };
        }
    }
}
