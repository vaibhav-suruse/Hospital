using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class PharmacyQueueRepository : IPharmacyQueue
    {
        private readonly string _conn;

        public PharmacyQueueRepository(IConfiguration configuration)
        {
            _conn = configuration.GetConnectionString("MySqlConnection");
        }

        // =====================================================================
        // OPD
        // =====================================================================

        // ── 1. Get OPD Queue ─────────────────────────────────────────────────
        public List<MedicineNotificationModel> GetOPDQueue(int hospitalId, int? subHospitalId)
        {
            var list = new List<MedicineNotificationModel>();
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand("GetMedicineNotifications", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(MapOPDNotification(r, hospitalId, subHospitalId));
            }
            catch { /* log */ }
            return list;
        }

        // ── 2. Get OPD Medicines ─────────────────────────────────────────────
        public List<PharmacyMedicineLineVM> GetMedicinesForPharmacy(int opdId, int hospitalId, int? subHospitalId)
        {
            var list = new List<PharmacyMedicineLineVM>();
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand("sp_GetOPDMedicinesForPharmacy", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_OPDId", opdId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new PharmacyMedicineLineVM
                    {
                        OPDMedicineId = Convert.ToInt32(r["OPDMedicineId"]),
                        MedicineId = Convert.ToInt32(r["MedicineId"]),
                        StoreMedicineId = Convert.ToInt32(r["StoreMedicineId"]),
                        MedicineName = r["MedicineName"]?.ToString(),
                        MedicineType = r["MedicineType"]?.ToString() ?? "Tablet",
                        Morning = Convert.ToBoolean(r["Morning"]),
                        Afternoon = Convert.ToBoolean(r["Afternoon"]),
                        Evening = Convert.ToBoolean(r["Evening"]),
                        Days = Convert.ToInt32(r["Days"]),
                        QtyPerDay = Convert.ToInt32(r["QtyPerDay"]),
                        TotalQty = Convert.ToInt32(r["TotalQty"]),
                        SellingPrice = r["SellingPrice"] != DBNull.Value ? Convert.ToDecimal(r["SellingPrice"]) : 0,
                    });
                }
            }
            catch { /* log */ }

            // ── Enrich each line with live stock availability ──────────────
            foreach (var m in list)
            {
                m.AvailableStock = GetAvailableStock(m.MedicineId, hospitalId);
                if (m.AvailableStock <= 0) m.StockStatus = "outofstock";
                else if (m.AvailableStock < m.TotalQty) m.StockStatus = "partial";
                else m.StockStatus = "sufficient";
            }

            return list;
        }

        // ── Helper: get live stock qty for a medicine ─────────────────────────
        private int GetAvailableStock(int medicineId, int hospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand(
                    "SELECT COALESCE(TotalQuantity,0) FROM stock WHERE MedicineId=@m AND HospitalId=@h LIMIT 1", con);
                cmd.Parameters.AddWithValue("@m", medicineId);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                con.Open();
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
            catch { return -1; }
        }

        // ── 3. Save OPD Medicine Bill ─────────────────────────────────────────
        public int SaveMedicineBill(PharmacyBillVM bill, int hospitalId, int? subHospitalId, int createdBy)
        {
            int billId = 0;
            using var con = new MySqlConnection(_conn);
            con.Open();
            using var tx = con.BeginTransaction();
            try
            {
                // Recalculate totals from actual items to prevent header/items mismatch
                // (sp_Counter_InsertBillItem silently skips items with insufficient stock)
                var billableItems = bill.Items.Where(i => i.Quantity > 0).ToList();
                decimal actualSub = billableItems.Sum(i => i.Quantity * i.UnitPrice);
                decimal discAmt = bill.DiscountType == "Percentage"
                                     ? actualSub * bill.DiscountValue / 100m
                                     : bill.DiscountAmount;
                decimal actualTotal = actualSub - discAmt;
                decimal actualPaid = Math.Min(bill.PaidAmount, actualTotal);
                decimal actualDue = Math.Max(0, actualTotal - actualPaid);

                int customerId = GetOrCreateCustomer(con, tx,
                    bill.PatientName ?? "OPD Patient",
                    bill.MobileNumber, hospitalId, subHospitalId);

                billId = InsertBillHeader(con, tx,
                    $"PHARM-{DateTime.Now:yyyyMMdd-HHmmss}",
                    customerId, bill.PatientName ?? "OPD Patient",
                    bill.MobileNumber, actualSub, bill.DiscountType,
                    bill.DiscountValue, discAmt, actualTotal,
                    bill.PaymentMode, actualPaid, actualDue,
                    hospitalId, subHospitalId, createdBy);

                foreach (var item in billableItems)
                    InsertBillItem(con, tx, billId, item, hospitalId, subHospitalId);

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
            return billId;
        }

        // =====================================================================
        // IPD
        // =====================================================================

        // ── 4. Get IPD Queue ─────────────────────────────────────────────────
        public List<MedicineNotificationModel> GetIPDQueue(int hospitalId, int? subHospitalId)
        {
            var list = new List<MedicineNotificationModel>();
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand("GetIPDMedicineNotifications", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(MapIPDNotification(r, hospitalId, subHospitalId));
            }
            catch { /* log */ }
            return list;
        }

        // ── 5. Get IPD Round Medicines ────────────────────────────────────────
        public List<PharmacyMedicineLineVM> GetMedicinesForIPDPharmacy(int ipdId, int roundId, int hospitalId, int? subHospitalId)
        {
            var list = new List<PharmacyMedicineLineVM>();
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand("sp_GetIPDRoundMedicinesForPharmacy", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                cmd.Parameters.AddWithValue("p_RoundId", roundId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new PharmacyMedicineLineVM
                    {
                        OPDMedicineId = Convert.ToInt32(r["PrescriptionId"]),
                        MedicineId = Convert.ToInt32(r["MedicineId"]),
                        StoreMedicineId = r["StoreMedicineId"] != DBNull.Value ? Convert.ToInt32(r["StoreMedicineId"]) : 0,
                        MedicineName = r["MedicineName"]?.ToString(),
                        MedicineType = r["MedicineType"]?.ToString() ?? "Tablet",
                        Route = r["Route"]?.ToString() ?? "Oral",
                        Morning = r["Morning"] != DBNull.Value && Convert.ToInt32(r["Morning"]) == 1,
                        Afternoon = r["Afternoon"] != DBNull.Value && Convert.ToInt32(r["Afternoon"]) == 1,
                        Evening = r["Evening"] != DBNull.Value && Convert.ToInt32(r["Evening"]) == 1,
                        Days = r["Days"] != DBNull.Value ? Convert.ToInt32(r["Days"]) : 0,
                        QtyPerDay = r["QtyPerDay"] != DBNull.Value ? Convert.ToInt32(r["QtyPerDay"]) : 1,
                        TotalQty = r["TotalQty"] != DBNull.Value ? Convert.ToInt32(r["TotalQty"]) : 1,
                        SellingPrice = r["SellingPrice"] != DBNull.Value ? Convert.ToDecimal(r["SellingPrice"]) : 0,
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"sp_GetIPDRoundMedicinesForPharmacy failed — IPDId:{ipdId} RoundId:{roundId} HospitalId:{hospitalId} SubHospitalId:{subHospitalId} — {ex.Message}", ex);
            }

            // ── Enrich each line with live stock availability ──────────────
            foreach (var m in list)
            {
                m.AvailableStock = GetAvailableStock(m.MedicineId, hospitalId);
                if (m.AvailableStock <= 0) m.StockStatus = "outofstock";
                else if (m.AvailableStock < m.TotalQty) m.StockStatus = "partial";
                else m.StockStatus = "sufficient";
            }

            return list;
        }

        // ── 6. Save IPD Medicine Bill ─────────────────────────────────────────
        public int SaveIPDMedicineBill(IPDPharmacyBillVM bill, int hospitalId, int? subHospitalId, int createdBy)
        {
            int billId = 0;
            using var con = new MySqlConnection(_conn);
            con.Open();
            using var tx = con.BeginTransaction();
            try
            {
                // Recalculate totals from actual items to prevent header/items mismatch
                var billableItems = bill.Items.Where(i => i.Quantity > 0).ToList();
                decimal actualSub = billableItems.Sum(i => i.Quantity * i.UnitPrice);
                decimal discAmt = bill.DiscountType == "Percentage"
                                     ? actualSub * bill.DiscountValue / 100m
                                     : bill.DiscountAmount;
                decimal actualTotal = actualSub - discAmt;
                decimal actualPaid = Math.Min(bill.PaidAmount, actualTotal);
                decimal actualDue = Math.Max(0, actualTotal - actualPaid);

                int customerId = GetOrCreateCustomer(con, tx,
                    bill.PatientName ?? "IPD Patient",
                    bill.MobileNumber, hospitalId, subHospitalId);

                billId = InsertBillHeader(con, tx,
                    $"IPD-PHARM-{DateTime.Now:yyyyMMdd-HHmmss}",
                    customerId, bill.PatientName ?? "IPD Patient",
                    bill.MobileNumber, actualSub, bill.DiscountType,
                    bill.DiscountValue, discAmt, actualTotal,
                    bill.PaymentMode, actualPaid, actualDue,
                    hospitalId, subHospitalId, createdBy);

                foreach (var item in billableItems)
                    InsertBillItem(con, tx, billId, item, hospitalId, subHospitalId);

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
            return billId;
        }

        // =====================================================================
        // Shared
        // =====================================================================

        // ── 7. Mark Dispensed ─────────────────────────────────────────────────
        public void MarkDispensed(int notificationId, int hospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand("MarkNotificationDispensed", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_NotificationId", notificationId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch { /* log */ }
        }

        // ── 8. Mark Billed ────────────────────────────────────────────────────
        public void MarkBilled(int notificationId, int hospitalId, int billId)
        {
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand("sp_MarkNotificationBilled", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_NotificationId", notificationId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_BillId", billId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch { /* log */ }
        }

        // ── 9. Get Patient Mobile ─────────────────────────────────────────────
        public string GetPatientMobile(int patientId, int hospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand(
                    "SELECT PhoneNumber FROM tbl_patient WHERE Id = @patientId AND Hospital_Id = @hospitalId LIMIT 1", con);
                cmd.Parameters.AddWithValue("@patientId", patientId);
                cmd.Parameters.AddWithValue("@hospitalId", hospitalId);
                con.Open();
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? "" : result.ToString();
            }
            catch { return ""; }
        }

        // =====================================================================
        // Private helpers — shared by OPD and IPD bill saving
        // =====================================================================

        private int GetOrCreateCustomer(MySqlConnection con, MySqlTransaction tx,
            string name, string mobile, int hospitalId, int? subHospitalId)
        {
            using var cmd = new MySqlCommand("sp_Counter_GetOrCreateCustomer", con, tx);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_CustomerName", name);
            cmd.Parameters.AddWithValue("p_MobileNumber", string.IsNullOrEmpty(mobile) ? "0000000000" : mobile);
            cmd.Parameters.AddWithValue("p_Address", "");
            cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
            cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
            var outId = new MySqlParameter("p_CustomerId", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
            var outIsNew = new MySqlParameter("p_IsNew", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outId);
            cmd.Parameters.Add(outIsNew);
            cmd.ExecuteNonQuery();
            return Convert.ToInt32(outId.Value);
        }

        private int InsertBillHeader(MySqlConnection con, MySqlTransaction tx,
            string billNumber, int customerId, string customerName, string mobile,
            decimal subTotal, string discountType, decimal discountValue, decimal discountAmount,
            decimal totalAmount, string paymentMode, decimal paidAmount, decimal dueAmount,
            int hospitalId, int? subHospitalId, int createdBy)
        {
            using var cmd = new MySqlCommand("sp_Counter_InsertBill", con, tx);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_BillNumber", billNumber);
            cmd.Parameters.AddWithValue("p_CustomerId", customerId);
            cmd.Parameters.AddWithValue("p_CustomerName", customerName);
            cmd.Parameters.AddWithValue("p_MobileNumber", string.IsNullOrEmpty(mobile) ? "0000000000" : mobile);
            cmd.Parameters.AddWithValue("p_SubTotal", subTotal);
            cmd.Parameters.AddWithValue("p_DiscountType", discountType ?? "Percentage");
            cmd.Parameters.AddWithValue("p_DiscountValue", discountValue);
            cmd.Parameters.AddWithValue("p_DiscountAmount", discountAmount);
            cmd.Parameters.AddWithValue("p_TotalAmount", totalAmount);
            cmd.Parameters.AddWithValue("p_PaymentMode", paymentMode ?? "Cash");
            cmd.Parameters.AddWithValue("p_PaidAmount", paidAmount);
            cmd.Parameters.AddWithValue("p_DueAmount", dueAmount);
            cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
            cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
            cmd.Parameters.AddWithValue("p_CreatedBy", createdBy);
            var outBillId = new MySqlParameter("p_BillId", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outBillId);
            cmd.ExecuteNonQuery();
            return Convert.ToInt32(outBillId.Value);
        }

        private void InsertBillItem(MySqlConnection con, MySqlTransaction tx,
            int billId, PharmacyBillItemVM item, int hospitalId, int? subHospitalId)
        {
            using var cmd = new MySqlCommand("sp_Counter_InsertBillItem", con, tx);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("p_BillId", billId);
            cmd.Parameters.AddWithValue("p_MedicineId", item.MedicineId);
            cmd.Parameters.AddWithValue("p_MedicineName", item.MedicineName);
            cmd.Parameters.AddWithValue("p_Quantity", item.Quantity);
            cmd.Parameters.AddWithValue("p_UnitPrice", item.UnitPrice);
            cmd.Parameters.AddWithValue("p_TotalPrice", item.TotalPrice);
            cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
            cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
            var outSuccess = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outSuccess);
            cmd.ExecuteNonQuery();
        }

        // =====================================================================
        // Private mappers
        // =====================================================================

        private MedicineNotificationModel MapOPDNotification(IDataReader r, int hospitalId, int? subHospitalId)
        {
            return new MedicineNotificationModel
            {
                NotificationId = Convert.ToInt32(r["NotificationId"]),
                PatientId = Convert.ToInt32(r["PatientId"]),
                PatientName = r["PatientName"]?.ToString(),
                OPDId = r["OPDId"] != DBNull.Value ? Convert.ToInt32(r["OPDId"]) : (int?)null,
                AppointmentId = r["AppointmentId"] != DBNull.Value ? Convert.ToInt32(r["AppointmentId"]) : (int?)null,
                IPDId = r["IPDId"] != DBNull.Value ? Convert.ToInt32(r["IPDId"]) : (int?)null,
                RoundId = r["RoundId"] != DBNull.Value ? Convert.ToInt32(r["RoundId"]) : (int?)null,
                DoctorName = (r["DoctorName"]?.ToString() ?? "").Replace("Dr. ", "").Replace("Dr.", "").Trim(),
                MedicineCount = r["MedicineCount"] != DBNull.Value ? Convert.ToInt32(r["MedicineCount"]) : 0,
                MedicinesSummary = r["MedicinesSummary"]?.ToString(),
                Type = r["Type"]?.ToString(),
                Status = r["Status"]?.ToString(),
                HospitalId = hospitalId,
                SubHospitalId = subHospitalId,
                CreatedAt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now,
                DispensedAt = r["DispensedAt"] != DBNull.Value ? Convert.ToDateTime(r["DispensedAt"]) : (DateTime?)null,
                WardName = r["WardName"]?.ToString(),
                RoomNo = r["RoomNo"]?.ToString(),
                BedNo = r["BedNo"]?.ToString(),
            };
        }

        private MedicineNotificationModel MapIPDNotification(IDataReader r, int hospitalId, int? subHospitalId)
        {
            return new MedicineNotificationModel
            {
                NotificationId = Convert.ToInt32(r["NotificationId"]),
                PatientId = Convert.ToInt32(r["PatientId"]),
                PatientName = r["PatientName"]?.ToString(),
                OPDId = r["OPDId"] != DBNull.Value ? Convert.ToInt32(r["OPDId"]) : (int?)null,
                AppointmentId = r["AppointmentId"] != DBNull.Value ? Convert.ToInt32(r["AppointmentId"]) : (int?)null,
                IPDId = r["IPDId"] != DBNull.Value ? Convert.ToInt32(r["IPDId"]) : (int?)null,
                RoundId = r["RoundId"] != DBNull.Value ? Convert.ToInt32(r["RoundId"]) : (int?)null,
                DoctorName = (r["DoctorName"]?.ToString() ?? "").Replace("Dr. ", "").Replace("Dr.", "").Trim(),
                MedicineCount = r["MedicineCount"] != DBNull.Value ? Convert.ToInt32(r["MedicineCount"]) : 0,
                MedicinesSummary = r["MedicinesSummary"]?.ToString(),
                Type = r["Type"]?.ToString(),
                Status = r["Status"]?.ToString(),
                HospitalId = hospitalId,
                SubHospitalId = subHospitalId,
                CreatedAt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now,
                DispensedAt = r["DispensedAt"] != DBNull.Value ? Convert.ToDateTime(r["DispensedAt"]) : (DateTime?)null,
                WardName = r["WardName"]?.ToString(),
                RoomNo = r["RoomNo"]?.ToString(),
                BedNo = r["BedNo"]?.ToString(),
            };
        }
    }
}


//using Microsoft.Extensions.Configuration;
//using MySql.Data.MySqlClient;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using WebApplicationSampleTest2.Models;

//namespace WebApplicationSampleTest2.Repository
//{
//    public class PharmacyQueueRepository : IPharmacyQueue
//    {
//        private readonly string _conn;

//        public PharmacyQueueRepository(IConfiguration configuration)
//        {
//            _conn = configuration.GetConnectionString("MySqlConnection");
//        }

//        // ── 1. Get OPD Queue (today's Pending + Dispensed notifications) ─
//        public List<MedicineNotificationModel> GetOPDQueue(int hospitalId, int? subHospitalId)
//        {
//            var list = new List<MedicineNotificationModel>();
//            try
//            {
//                using var con = new MySqlConnection(_conn);
//                using var cmd = new MySqlCommand("GetMedicineNotifications", con);
//                cmd.CommandType = CommandType.StoredProcedure;
//                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
//                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
//                con.Open();
//                using var r = cmd.ExecuteReader();
//                while (r.Read())
//                {
//                    list.Add(new MedicineNotificationModel
//                    {
//                        NotificationId = Convert.ToInt32(r["NotificationId"]),
//                        PatientId = Convert.ToInt32(r["PatientId"]),
//                        PatientName = r["PatientName"]?.ToString(),
//                        OPDId = r["OPDId"] != DBNull.Value ? Convert.ToInt32(r["OPDId"]) : (int?)null,
//                        AppointmentId = r["AppointmentId"] != DBNull.Value ? Convert.ToInt32(r["AppointmentId"]) : (int?)null,
//                        DoctorName = (r["DoctorName"]?.ToString() ?? "").Replace("Dr. ", "").Replace("Dr.", "").Trim(),
//                        MedicineCount = r["MedicineCount"] != DBNull.Value ? Convert.ToInt32(r["MedicineCount"]) : 0,
//                        MedicinesSummary = r["MedicinesSummary"]?.ToString(),
//                        Type = r["Type"]?.ToString(),
//                        Status = r["Status"]?.ToString(),
//                        HospitalId = hospitalId,
//                        SubHospitalId = subHospitalId,
//                        CreatedAt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now,
//                        DispensedAt = r["DispensedAt"] != DBNull.Value ? Convert.ToDateTime(r["DispensedAt"]) : (DateTime?)null,
//                    });
//                }
//            }
//            catch { /* log */ }
//            return list;
//        }

//        // ── 2. Get medicines for one OPD (with qty + price) ─────────────
//        public List<PharmacyMedicineLineVM> GetMedicinesForPharmacy(int opdId, int hospitalId, int? subHospitalId)
//        {
//            var list = new List<PharmacyMedicineLineVM>();
//            try
//            {
//                using var con = new MySqlConnection(_conn);
//                using var cmd = new MySqlCommand("sp_GetOPDMedicinesForPharmacy", con);
//                cmd.CommandType = CommandType.StoredProcedure;
//                cmd.Parameters.AddWithValue("p_OPDId", opdId);
//                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
//                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
//                con.Open();
//                using var r = cmd.ExecuteReader();
//                while (r.Read())
//                {
//                    list.Add(new PharmacyMedicineLineVM
//                    {
//                        OPDMedicineId = Convert.ToInt32(r["OPDMedicineId"]),
//                        MedicineId = Convert.ToInt32(r["MedicineId"]),
//                        StoreMedicineId = Convert.ToInt32(r["StoreMedicineId"]),
//                        MedicineName = r["MedicineName"]?.ToString(),
//                        MedicineType = r["MedicineType"]?.ToString() ?? "Tablet",
//                        Morning = Convert.ToBoolean(r["Morning"]),
//                        Afternoon = Convert.ToBoolean(r["Afternoon"]),
//                        Evening = Convert.ToBoolean(r["Evening"]),
//                        Days = Convert.ToInt32(r["Days"]),
//                        QtyPerDay = Convert.ToInt32(r["QtyPerDay"]),
//                        TotalQty = Convert.ToInt32(r["TotalQty"]),
//                        SellingPrice = r["SellingPrice"] != DBNull.Value ? Convert.ToDecimal(r["SellingPrice"]) : 0,
//                    });
//                }
//            }
//            catch { /* log */ }
//            return list;
//        }

//        // ── 3. Mark Dispensed ─────────────────────────────────────────────
//        public void MarkDispensed(int notificationId, int hospitalId)
//        {
//            try
//            {
//                using var con = new MySqlConnection(_conn);
//                using var cmd = new MySqlCommand("MarkNotificationDispensed", con);
//                cmd.CommandType = CommandType.StoredProcedure;
//                cmd.Parameters.AddWithValue("p_NotificationId", notificationId);
//                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
//                con.Open();
//                cmd.ExecuteNonQuery();
//            }
//            catch { /* log */ }
//        }

//        // ── 4. Mark Billed ────────────────────────────────────────────────
//        public void MarkBilled(int notificationId, int hospitalId, int billId)
//        {
//            try
//            {
//                using var con = new MySqlConnection(_conn);
//                using var cmd = new MySqlCommand("sp_MarkNotificationBilled", con);
//                cmd.CommandType = CommandType.StoredProcedure;
//                cmd.Parameters.AddWithValue("p_NotificationId", notificationId);
//                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
//                cmd.Parameters.AddWithValue("p_BillId", billId);
//                con.Open();
//                cmd.ExecuteNonQuery();
//            }
//            catch { /* log */ }
//        }

//        // ── 5. Save Medicine Bill ─────────────────────────────────────────
//        // Uses existing sp_Counter_GetOrCreateCustomer + sp_Counter_InsertBill + sp_Counter_InsertBillItem
//        public int SaveMedicineBill(PharmacyBillVM bill, int hospitalId, int? subHospitalId, int createdBy)
//        {
//            int billId = 0;
//            using var con = new MySqlConnection(_conn);
//            con.Open();
//            using var tx = con.BeginTransaction();
//            try
//            {
//                // Step 1: Get or create customer (patient as counter customer)
//                int customerId = 0;
//                using (var cmd = new MySqlCommand("sp_Counter_GetOrCreateCustomer", con, tx))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("p_CustomerName", bill.PatientName ?? "OPD Patient");
//                    cmd.Parameters.AddWithValue("p_MobileNumber", string.IsNullOrEmpty(bill.MobileNumber) ? "0000000000" : bill.MobileNumber);
//                    cmd.Parameters.AddWithValue("p_Address", "");
//                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
//                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
//                    var outId = new MySqlParameter("p_CustomerId", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
//                    cmd.Parameters.Add(outId);
//                    var outIsNew = new MySqlParameter("p_IsNew", MySqlDbType.Byte) { Direction = ParameterDirection.Output }; 
//                    cmd.Parameters.Add(outIsNew);
//                    cmd.ExecuteNonQuery();
//                    customerId = Convert.ToInt32(outId.Value);
//                }

//                // Step 2: Generate bill number
//                string billNumber = $"PHARM-{DateTime.Now:yyyyMMdd-HHmmss}";

//                // Step 3: Insert bill header
//                using (var cmd = new MySqlCommand("sp_Counter_InsertBill", con, tx))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("p_BillNumber", billNumber);
//                    cmd.Parameters.AddWithValue("p_CustomerId", customerId);
//                    cmd.Parameters.AddWithValue("p_CustomerName", bill.PatientName ?? "OPD Patient");
//                    cmd.Parameters.AddWithValue("p_MobileNumber", string.IsNullOrEmpty(bill.MobileNumber) ? "0000000000" : bill.MobileNumber);
//                    cmd.Parameters.AddWithValue("p_SubTotal", bill.SubTotal);
//                    cmd.Parameters.AddWithValue("p_DiscountType", bill.DiscountType ?? "Percentage");
//                    cmd.Parameters.AddWithValue("p_DiscountValue", bill.DiscountValue);
//                    cmd.Parameters.AddWithValue("p_DiscountAmount", bill.DiscountAmount);
//                    cmd.Parameters.AddWithValue("p_TotalAmount", bill.TotalAmount);
//                    cmd.Parameters.AddWithValue("p_PaymentMode", bill.PaymentMode ?? "Cash");
//                    cmd.Parameters.AddWithValue("p_PaidAmount", bill.PaidAmount);
//                    cmd.Parameters.AddWithValue("p_DueAmount", bill.DueAmount);
//                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
//                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
//                    cmd.Parameters.AddWithValue("p_CreatedBy", createdBy);
//                    var outBillId = new MySqlParameter("p_BillId", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
//                    cmd.Parameters.Add(outBillId);
//                    cmd.ExecuteNonQuery();
//                    billId = Convert.ToInt32(outBillId.Value);
//                }

//                // Step 4: Insert each medicine line
//                foreach (var item in bill.Items)
//                {
//                    using var cmd = new MySqlCommand("sp_Counter_InsertBillItem", con, tx);
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("p_BillId", billId);
//                    cmd.Parameters.AddWithValue("p_MedicineId", item.MedicineId);
//                    cmd.Parameters.AddWithValue("p_MedicineName", item.MedicineName);
//                    cmd.Parameters.AddWithValue("p_Quantity", item.Quantity);
//                    cmd.Parameters.AddWithValue("p_UnitPrice", item.UnitPrice);
//                    cmd.Parameters.AddWithValue("p_TotalPrice", item.TotalPrice);
//                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
//                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
//                    var outSuccess = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
//                    cmd.Parameters.Add(outSuccess);
//                    cmd.ExecuteNonQuery();
//                    // Note: if stock is insufficient, sp_Counter_InsertBillItem sets p_Success=0
//                    // We still commit — pharmacist can adjust stock separately
//                }

//                tx.Commit();
//            }
//            catch
//            {
//                tx.Rollback();
//                throw;
//            }
//            return billId;
//        }

//        public string GetPatientMobile(int patientId, int hospitalId)
//        {
//            try
//            {
//                using var con = new MySqlConnection(_conn);
//                using var cmd = new MySqlCommand(
//                    "SELECT PhoneNumber FROM tbl_patient WHERE Id = @patientId AND Hospital_Id = @hospitalId LIMIT 1", con);
//                cmd.Parameters.AddWithValue("@patientId", patientId);
//                cmd.Parameters.AddWithValue("@hospitalId", hospitalId);
//                con.Open();
//                var result = cmd.ExecuteScalar();
//                return result == null || result == DBNull.Value ? "" : result.ToString();
//            }
//            catch { return ""; }
//        }






//}
//}
