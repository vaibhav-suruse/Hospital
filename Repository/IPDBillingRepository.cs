using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class IPDBillingRepository : IIPDBilling
    {
        private readonly string _connectionString;
        private readonly ILogger<IPDBillingRepository> _logger;
        private readonly IIPDNursingCharge _nursingRepo;
        private readonly IIPDOperation _operationRepo;

        public IPDBillingRepository(
            IConfiguration configuration,
            ILogger<IPDBillingRepository> logger,
            IIPDNursingCharge nursingRepo,
            IIPDOperation operationRepo)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
            _nursingRepo = nursingRepo;
            _operationRepo = operationRepo;
        }

        public IPDBillVM GetBillSummary(int ipdId, int hospitalId, int? subHospitalId)
        {
            var vm = new IPDBillVM { IPDId = ipdId };
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_IPB_GetBillSummary", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                conn.Open();

                using var reader = cmd.ExecuteReader();

                // RS1 — Patient + admission info
                if (reader.Read())
                {
                    vm.IPDId = Convert.ToInt32(reader["IPDId"]);
                    vm.AdmissionNumber = reader["AdmissionNumber"]?.ToString();
                    vm.AdmissionDateTime = Convert.ToDateTime(reader["AdmissionDateTime"]);
                    vm.ActualDischargeDateTime = reader["ActualDischargeDateTime"] == DBNull.Value
                        ? (DateTime?)null : Convert.ToDateTime(reader["ActualDischargeDateTime"]);
                    vm.TotalDays = Convert.ToInt32(reader["TotalDays"]);
                    vm.PatientName = reader["PatientName"]?.ToString();
                    vm.Age = reader["Age"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Age"]);
                    vm.Gender = reader["Gender"]?.ToString();
                    vm.PhoneNumber = reader["PhoneNumber"]?.ToString();
                    vm.DoctorName = reader["DoctorName"]?.ToString();
                }

                // RS2 — Bed charges (auto Days × ChargesPerDay per allocation)
                reader.NextResult();
                while (reader.Read())
                {
                    vm.BedCharges.Add(new BedChargeDetail
                    {
                        AllocationId = Convert.ToInt32(reader["AllocationId"]),
                        BedNumber = reader["BedNumber"]?.ToString(),
                        WardName = reader["WardName"]?.ToString(),
                        RoomNumber = reader["RoomNumber"]?.ToString(),
                        ChargesPerDay = Convert.ToDecimal(reader["ChargesPerDay"]),
                        StartDateTime = Convert.ToDateTime(reader["StartDateTime"]),
                        EndDateTime = Convert.ToDateTime(reader["EndDateTime"]),
                        Days = Convert.ToInt32(reader["Days"]),
                        BedCharge = Convert.ToDecimal(reader["BedCharge"])
                    });
                }
                vm.TotalBedCharges = vm.BedCharges.Sum(b => b.BedCharge);

                // RS3 — Doctor round charges (auto-priced from Billing Master)
                reader.NextResult();
                while (reader.Read())
                {
                    vm.DoctorVisits.Add(new DoctorVisitDetail
                    {
                        RoundId = Convert.ToInt32(reader["RoundId"]),
                        RoundDateTime = Convert.ToDateTime(reader["RoundDateTime"]),
                        RoundType = reader["RoundType"]?.ToString(),
                        DoctorName = reader["DoctorName"]?.ToString(),
                        VisitCharge = Convert.ToDecimal(reader["VisitCharge"])
                    });
                }
                vm.TotalDoctorCharges = vm.DoctorVisits.Sum(d => d.VisitCharge);

                // RS4 — Prescribed medicines (priced from tbl_medicine)
                reader.NextResult();
                while (reader.Read())
                {
                    vm.Medicines.Add(new MedicineChargeDetail
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        MedicineId = Convert.ToInt32(reader["MedicineId"]),
                        MedicineName = reader["MedicineName"]?.ToString(),
                        Type = reader["Type"]?.ToString(),
                        Days = reader["Days"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Days"]),
                        Dosage = reader["Dosage"]?.ToString(),
                        Status = reader["Status"]?.ToString(),
                        UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                        TotalPrice = Convert.ToDecimal(reader["TotalPrice"])
                    });
                }
                vm.TotalMedicineCharges = vm.Medicines.Sum(m => m.TotalPrice);

                // RS5 — Discharge medicines (priced from tbl_medicine)
                reader.NextResult();
                while (reader.Read())
                {
                    vm.DischargeMedicines.Add(new MedicineChargeDetail
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        MedicineId = Convert.ToInt32(reader["MedicineId"]),
                        MedicineName = reader["MedicineName"]?.ToString(),
                        Type = reader["Type"]?.ToString(),
                        Days = reader["Days"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Days"]),
                        Dosage = reader["Dosage"]?.ToString(),
                        UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                        TotalPrice = Convert.ToDecimal(reader["TotalPrice"])
                    });
                }
                vm.TotalDischargeMedCharges = vm.DischargeMedicines.Sum(m => m.TotalPrice);

                // RS6 — Investigations (reference, with best-effort suggested price)
                reader.NextResult();
                while (reader.Read())
                {
                    vm.Investigations.Add(new InvestigationChargeDetail
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        InvestigationType = reader["InvestigationType"]?.ToString(),
                        TestName = reader["TestName"]?.ToString(),
                        Priority = reader["Priority"]?.ToString(),
                        Status = reader["Status"]?.ToString(),
                        SuggestedCharge = Convert.ToDecimal(reader["SuggestedCharge"])
                    });
                }

                // RS7 — Procedures performed (reference, with best-effort suggested price)
                reader.NextResult();
                while (reader.Read())
                {
                    vm.Procedures.Add(new ProcedureChargeDetail
                    {
                        ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                        ProcedureName = reader["ProcedureName"]?.ToString(),
                        ProcedureCategory = reader["ProcedureCategory"]?.ToString(),
                        Status = reader["Status"]?.ToString(),
                        ProcedureDate = reader["ProcedureDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ProcedureDate"]),
                        SuggestedCharge = Convert.ToDecimal(reader["SuggestedCharge"])
                    });
                }

                reader.Close();

                // Nursing charges — already priced by the Nursing Charges module
                vm.NursingCharges = _nursingRepo.GetByIPDId(ipdId) ?? new List<IPDNursingCharge>();
                vm.TotalNursingCharges = vm.NursingCharges.Sum(n => n.TotalCharge);

                // Operations — already priced by OT Management
                vm.Operations = _operationRepo.GetByIPDId(ipdId) ?? new List<IPDOperationModel>();
                vm.TotalOperationCharges = vm.Operations.Sum(o =>
                    o.ActualCharge + o.AnesthesiaCharge + o.SurgeonCharge + o.OTCharge + o.TotalStaffCharge);

                // If a bill already exists for this admission, hydrate it so
                // reopening the screen doesn't lose anything.
                var existingBill = GetBillByIPDId(ipdId, hospitalId, subHospitalId);
                if (existingBill != null)
                {
                    vm.BillId = existingBill.BillId;
                    vm.BillNumber = existingBill.BillNumber;
                    vm.BillDate = existingBill.BillDate;
                    vm.SubTotal = existingBill.SubTotal;
                    vm.LineDiscountAmount = existingBill.LineDiscountAmount;
                    vm.TaxAmount = existingBill.TaxAmount;
                    vm.DiscountPercent = existingBill.DiscountPercent;
                    vm.DiscountAmount = existingBill.DiscountAmount;
                    vm.TotalAmount = existingBill.TotalAmount;
                    vm.PaidAmount = existingBill.PaidAmount;
                    vm.DueAmount = existingBill.DueAmount;
                    vm.PaymentStatus = existingBill.PaymentStatus;
                    vm.BillStatus = existingBill.BillStatus;
                    vm.CancelledReason = existingBill.CancelledReason;
                    vm.CancelledDate = existingBill.CancelledDate;
                    vm.Notes = existingBill.Notes;
                    vm.OtherItems = GetBillItems(existingBill.BillId, hospitalId, subHospitalId);
                    vm.Payments = GetPayments(existingBill.BillId, hospitalId, subHospitalId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBillSummary. IPDId={Id}", ipdId);
                throw;
            }
            return vm;
        }

        public IPDBill GetBillByIPDId(int ipdId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_IPB_GetByIPD", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                conn.Open();
                using var r = cmd.ExecuteReader();
                if (r.Read()) return MapBill(r);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBillByIPDId. IPDId={Id}", ipdId);
                throw;
            }
        }

        public List<IPDBillItem> GetBillItems(int billId, int hospitalId, int? subHospitalId)
        {
            var list = new List<IPDBillItem>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_IPB_GetBillItems", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_BillId", billId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                conn.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new IPDBillItem
                    {
                        ItemId = Convert.ToInt32(r["ItemId"]),
                        BillId = Convert.ToInt32(r["BillId"]),
                        IPDId = Convert.ToInt32(r["IPDId"]),
                        ItemType = r["ItemType"].ToString(),
                        ItemName = r["ItemName"].ToString(),
                        Quantity = Convert.ToInt32(r["Quantity"]),
                        UnitPrice = Convert.ToDecimal(r["UnitPrice"]),
                        DiscountValue = Convert.ToDecimal(r["DiscountValue"]),
                        DiscountIsPercent = Convert.ToBoolean(r["DiscountIsPercent"]),
                        GstPercent = Convert.ToDecimal(r["GstPercent"]),
                        TotalPrice = Convert.ToDecimal(r["TotalPrice"]),
                        Notes = r["Notes"]?.ToString(),
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

        public List<IPDPayment> GetPayments(int billId, int hospitalId, int? subHospitalId)
        {
            var list = new List<IPDPayment>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_IPB_GetPayments", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_BillId", billId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                conn.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new IPDPayment
                    {
                        PaymentId = Convert.ToInt32(r["PaymentId"]),
                        ReceiptNumber = r["ReceiptNumber"]?.ToString(),
                        BillId = Convert.ToInt32(r["BillId"]),
                        IPDId = Convert.ToInt32(r["IPDId"]),
                        PaymentDate = Convert.ToDateTime(r["PaymentDate"]),
                        Amount = Convert.ToDecimal(r["Amount"]),
                        PaymentMode = r["PaymentMode"]?.ToString(),
                        TransactionRef = r["TransactionRef"]?.ToString(),
                        Notes = r["Notes"]?.ToString(),
                        ReceivedBy = r["ReceivedBy"] == DBNull.Value ? 0 : Convert.ToInt32(r["ReceivedBy"]),
                        CollectedByName = r["CollectedByName"]?.ToString()
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

        public int SaveBill(IPDBill bill, List<IPDBillItem> items, int hospitalId, int? subHospitalId, bool isDraft)
        {
            try
            {
                // Nursing & Operation charges are owned by their own modules
                // (Nursing Charges screen / OT Management) — always snapshot
                // the live total from the source of truth rather than trust
                // whatever the client sent, so the bill can never drift out
                // of sync with those screens.
                bill.NursingCharges = _nursingRepo.GetTotalByIPDId(bill.IPDId);
                bill.OperationCharges = _operationRepo.GetTotalByIPDId(bill.IPDId);

                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var tran = conn.BeginTransaction();
                try
                {
                    int billId;
                    using (var cmd = new MySqlCommand("sp_IPB_SaveBillHeader", conn, tran))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_IPDId", bill.IPDId);
                        cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                        cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                        cmd.Parameters.AddWithValue("p_BillDate", bill.BillDate);
                        cmd.Parameters.AddWithValue("p_BedCharges", bill.BedCharges);
                        cmd.Parameters.AddWithValue("p_DoctorCharges", bill.DoctorVisitCharges);
                        cmd.Parameters.AddWithValue("p_MedicineCharges", bill.MedicineCharges);
                        cmd.Parameters.AddWithValue("p_InvestigationCharges", bill.InvestigationCharges);
                        cmd.Parameters.AddWithValue("p_DischargeCharges", bill.DischargeMedicineCharges);
                        cmd.Parameters.AddWithValue("p_NursingCharges", bill.NursingCharges);
                        cmd.Parameters.AddWithValue("p_OperationCharges", bill.OperationCharges);
                        cmd.Parameters.AddWithValue("p_ProcedureCharges", bill.ProcedureCharges);
                        cmd.Parameters.AddWithValue("p_OtherCharges", bill.OtherCharges);
                        cmd.Parameters.AddWithValue("p_SubTotal", bill.SubTotal);
                        cmd.Parameters.AddWithValue("p_LineDiscountAmount", bill.LineDiscountAmount);
                        cmd.Parameters.AddWithValue("p_TaxAmount", bill.TaxAmount);
                        cmd.Parameters.AddWithValue("p_DiscountPercent", bill.DiscountPercent);
                        cmd.Parameters.AddWithValue("p_DiscountAmount", bill.DiscountAmount);
                        cmd.Parameters.AddWithValue("p_TotalAmount", bill.TotalAmount);
                        cmd.Parameters.AddWithValue("p_Notes", (object)bill.Notes ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("p_IsDraft", isDraft ? 1 : 0);
                        cmd.Parameters.AddWithValue("p_CreatedBy", bill.CreatedBy);

                        billId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Replace items: soft-delete existing, then insert the fresh
                    // set — this is what makes re-editing a bill work instead of
                    // piling up duplicate item rows every time it's saved.
                    using (var clearCmd = new MySqlCommand("sp_IPB_ClearBillItems", conn, tran))
                    {
                        clearCmd.CommandType = CommandType.StoredProcedure;
                        clearCmd.Parameters.AddWithValue("p_BillId", billId);
                        clearCmd.ExecuteNonQuery();
                    }

                    foreach (var item in items)
                    {
                        using var icmd = new MySqlCommand("sp_IPB_SaveBillItem", conn, tran);
                        icmd.CommandType = CommandType.StoredProcedure;
                        icmd.Parameters.AddWithValue("p_BillId", billId);
                        icmd.Parameters.AddWithValue("p_IPDId", bill.IPDId);
                        icmd.Parameters.AddWithValue("p_ItemType", item.ItemType);
                        icmd.Parameters.AddWithValue("p_BillingMasterId", item.BillingMasterId.HasValue ? (object)item.BillingMasterId.Value : DBNull.Value);
                        icmd.Parameters.AddWithValue("p_ItemName", item.ItemName);
                        icmd.Parameters.AddWithValue("p_Quantity", item.Quantity);
                        icmd.Parameters.AddWithValue("p_UnitPrice", item.UnitPrice);
                        icmd.Parameters.AddWithValue("p_DiscountValue", item.DiscountValue);
                        icmd.Parameters.AddWithValue("p_DiscountIsPercent", item.DiscountIsPercent ? 1 : 0);
                        icmd.Parameters.AddWithValue("p_GstPercent", item.GstPercent);
                        icmd.Parameters.AddWithValue("p_TotalPrice", item.TotalPrice);
                        icmd.Parameters.AddWithValue("p_Notes", (object)item.Notes ?? DBNull.Value);
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
                _logger.LogError(ex, "Error in SaveBill. IPDId={Id}", bill.IPDId);
                throw;
            }
        }

        public IPDBill CollectPayment(
            int billId, int ipdId, int hospitalId, int? subHospitalId,
            decimal amount, string paymentMode, string transactionRef, string notes, int receivedBy)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_IPB_CollectPayment", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_BillId", billId);
                cmd.Parameters.AddWithValue("p_IPDId", ipdId);
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

        public IPDBill CancelBill(int billId, int hospitalId, int? subHospitalId, string reason, int cancelledBy)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_IPB_CancelBill", conn);
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

        public BillingSummaryVM GetPatientBillingSummary(int ipdId)
        {
            var vm = new BillingSummaryVM { IPDId = ipdId };
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand(@"
    SELECT
        a.IPDId, a.AdmissionNumber, a.AdmissionDateTime, a.ActualDischargeDateTime,
        a.Status AS AdmissionStatus, a.ReasonForAdmission,
        GREATEST(1, DATEDIFF(IFNULL(a.ActualDischargeDateTime, NOW()), a.AdmissionDateTime) + 1) AS TotalDays,
        CONCAT(p.FirstName,' ',IFNULL(p.LastName,'')) AS PatientName,
        p.Age, p.Gender, p.PhoneNumber, p.Address,
        CONCAT(d.FirstName,' ',IFNULL(d.LastName,'')) AS DoctorName,
        w.WardName, b.BedNumber
    FROM ipdadmission a
    INNER JOIN tbl_patient p ON a.PatientId = p.Id
    LEFT JOIN doctor d ON a.PrimaryDoctorId = d.Doctor_Id
    LEFT JOIN ipdbedallocation ba ON a.IPDId = ba.IPDId AND ba.IsCurrent = 1
    LEFT JOIN bed b ON ba.BedId = b.BedId
    LEFT JOIN ward w ON b.WardId = w.WardId
    WHERE a.IPDId = @ipdId
    LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@ipdId", ipdId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                vm.AdmissionNumber = r["AdmissionNumber"].ToString();
                                vm.AdmissionDateTime = Convert.ToDateTime(r["AdmissionDateTime"]);
                                vm.DischargeDateTime = r["ActualDischargeDateTime"] == DBNull.Value
                                    ? (DateTime?)null : Convert.ToDateTime(r["ActualDischargeDateTime"]);
                                vm.AdmissionStatus = r["AdmissionStatus"].ToString();
                                vm.TotalDays = Convert.ToInt32(r["TotalDays"]);
                                vm.PatientName = r["PatientName"].ToString();
                                vm.Age = r["Age"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Age"]);
                                vm.Gender = r["Gender"]?.ToString();
                                vm.PhoneNumber = r["PhoneNumber"]?.ToString();
                                vm.Address = r["Address"]?.ToString();
                                vm.DoctorName = r["DoctorName"]?.ToString() ?? "-";
                                vm.WardName = r["WardName"]?.ToString() ?? "-";
                                vm.BedNumber = r["BedNumber"]?.ToString() ?? "-";
                            }
                        }
                    }

                    using (var cmd = new MySqlCommand(@"
                SELECT * FROM ipd_bill
                WHERE IPDId = @ipdId AND IsActive = 1
                ORDER BY BillId DESC
                LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@ipdId", ipdId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                vm.BillId = Convert.ToInt32(r["BillId"]);
                                vm.BillNumber = r["BillNumber"]?.ToString();
                                vm.BillDate = Convert.ToDateTime(r["BillDate"]);
                                vm.BedCharges = Convert.ToDecimal(r["BedCharges"]);
                                vm.DoctorCharges = Convert.ToDecimal(r["DoctorVisitCharges"]);
                                vm.MedicineCharges = Convert.ToDecimal(r["MedicineCharges"]);
                                vm.InvestigationCharges = Convert.ToDecimal(r["InvestigationCharges"]);
                                vm.DischargeMedCharges = Convert.ToDecimal(r["DischargeMedicineCharges"]);
                                vm.NursingCharges = Convert.ToDecimal(r["NursingCharges"]);
                                vm.OperationCharges = Convert.ToDecimal(r["OperationCharges"]);
                                vm.OtherCharges = Convert.ToDecimal(r["OtherCharges"]);
                                vm.SubTotal = Convert.ToDecimal(r["SubTotal"]);
                                vm.DiscountPercent = Convert.ToDecimal(r["DiscountPercent"]);
                                vm.DiscountAmount = Convert.ToDecimal(r["DiscountAmount"]);
                                vm.TotalAmount = Convert.ToDecimal(r["TotalAmount"]);
                                vm.PaidAmount = Convert.ToDecimal(r["PaidAmount"]);
                                vm.DueAmount = Convert.ToDecimal(r["DueAmount"]);
                                vm.PaymentStatus = r["PaymentStatus"]?.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPatientBillingSummary. IPDId={Id}", ipdId);
                throw new Exception("Error fetching patient billing summary.", ex);
            }
            return vm;
        }

        private IPDBill MapBill(MySqlDataReader r)
        {
            return new IPDBill
            {
                BillId = Convert.ToInt32(r["BillId"]),
                IPDId = Convert.ToInt32(r["IPDId"]),
                ParentHospitalId = Convert.ToInt32(r["ParentHospitalId"]),
                SubHospitalId = r["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["SubHospitalId"]),
                BillNumber = r["BillNumber"]?.ToString(),
                BillDate = Convert.ToDateTime(r["BillDate"]),
                BedCharges = Convert.ToDecimal(r["BedCharges"]),
                DoctorVisitCharges = Convert.ToDecimal(r["DoctorVisitCharges"]),
                MedicineCharges = Convert.ToDecimal(r["MedicineCharges"]),
                InvestigationCharges = Convert.ToDecimal(r["InvestigationCharges"]),
                DischargeMedicineCharges = Convert.ToDecimal(r["DischargeMedicineCharges"]),
                NursingCharges = Convert.ToDecimal(r["NursingCharges"]),
                OperationCharges = Convert.ToDecimal(r["OperationCharges"]),
                ProcedureCharges = Convert.ToDecimal(r["ProcedureCharges"]),
                OtherCharges = Convert.ToDecimal(r["OtherCharges"]),
                SubTotal = Convert.ToDecimal(r["SubTotal"]),
                LineDiscountAmount = Convert.ToDecimal(r["LineDiscountAmount"]),
                TaxAmount = Convert.ToDecimal(r["TaxAmount"]),
                DiscountPercent = Convert.ToDecimal(r["DiscountPercent"]),
                DiscountAmount = Convert.ToDecimal(r["DiscountAmount"]),
                TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
                PaidAmount = Convert.ToDecimal(r["PaidAmount"]),
                DueAmount = Convert.ToDecimal(r["DueAmount"]),
                PaymentStatus = r["PaymentStatus"]?.ToString(),
                BillStatus = r["BillStatus"]?.ToString() ?? "Draft",
                CancelledReason = r["CancelledReason"]?.ToString(),
                CancelledBy = r["CancelledBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["CancelledBy"]),
                CancelledDate = r["CancelledDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CancelledDate"]),
                Notes = r["Notes"]?.ToString(),
                CreatedBy = r["CreatedBy"] == DBNull.Value ? 0 : Convert.ToInt32(r["CreatedBy"]),
                UpdatedBy = r["UpdatedBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["UpdatedBy"]),
                UpdatedDate = r["UpdatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["UpdatedDate"])
            };
        }
    }
}
