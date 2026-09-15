using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class AdvancedPharmacyRepository : IAdvancedPharmacy
    {
        private readonly string _conn;
        private readonly ILogger<AdvancedPharmacyRepository> _logger;

        public AdvancedPharmacyRepository(IConfiguration configuration, ILogger<AdvancedPharmacyRepository> logger)
        {
            _conn = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        // =====================================================================
        // PURCHASE ORDER
        // =====================================================================
        public List<PurchaseOrder> GetPurchaseOrders(int hospitalId, int? subHospitalId)
        {
            var list = new List<PurchaseOrder>();
            try
            {
                using var con = new MySqlConnection(_conn);
                string sql = @"SELECT po.PONumber, po.SupplierId, COALESCE(s.SupplierName,'—') AS SupplierName,
                                      po.HospitalId, po.SubHospitalId, po.OrderDate, po.ExpectedDate,
                                      po.Status, po.Notes, po.CreatedBy,
                                      COALESCE(SUM(poi.Quantity * poi.UnitCost),0) AS TotalValue
                               FROM purchase_order po
                               LEFT JOIN store_suppliers s ON s.SupplierId = po.SupplierId
                               LEFT JOIN purchase_order_item poi ON poi.PONumber = po.PONumber
                               WHERE po.HospitalId = @h
                                 AND (po.SubHospitalId = @sh OR @sh = 0)
                               GROUP BY po.PONumber
                               ORDER BY po.OrderDate DESC";
                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                cmd.Parameters.AddWithValue("@sh", subHospitalId ?? 0);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new PurchaseOrder
                    {
                        PONumber = r["PONumber"].ToString(),
                        SupplierId = Convert.ToInt32(r["SupplierId"]),
                        SupplierName = r["SupplierName"]?.ToString(),
                        HospitalId = Convert.ToInt32(r["HospitalId"]),
                        SubHospitalId = r["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["SubHospitalId"]),
                        OrderDate = Convert.ToDateTime(r["OrderDate"]),
                        ExpectedDate = r["ExpectedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["ExpectedDate"]),
                        Status = r["Status"]?.ToString(),
                        Notes = r["Notes"]?.ToString(),
                        CreatedBy = r["CreatedBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["CreatedBy"])
                    });
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "GetPurchaseOrders failed"); }
            return list;
        }

        public PurchaseOrder GetPurchaseOrder(string poNumber, int hospitalId)
        {
            PurchaseOrder po = null;
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand(@"
                    SELECT po.PONumber, po.SupplierId, COALESCE(s.SupplierName,'—') AS SupplierName,
                           po.HospitalId, po.SubHospitalId, po.OrderDate, po.ExpectedDate,
                           po.Status, po.Notes, po.CreatedBy
                    FROM purchase_order po
                    LEFT JOIN store_suppliers s ON s.SupplierId = po.SupplierId
                    WHERE po.PONumber = @po AND po.HospitalId = @h", con);
                cmd.Parameters.AddWithValue("@po", poNumber);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                con.Open();
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    po = new PurchaseOrder
                    {
                        PONumber = r["PONumber"].ToString(),
                        SupplierId = Convert.ToInt32(r["SupplierId"]),
                        SupplierName = r["SupplierName"]?.ToString(),
                        HospitalId = Convert.ToInt32(r["HospitalId"]),
                        SubHospitalId = r["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["SubHospitalId"]),
                        OrderDate = Convert.ToDateTime(r["OrderDate"]),
                        ExpectedDate = r["ExpectedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["ExpectedDate"]),
                        Status = r["Status"]?.ToString(),
                        Notes = r["Notes"]?.ToString(),
                        CreatedBy = r["CreatedBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["CreatedBy"])
                    };
                }
                // close reader before next command
                con.Close();

                // load items
                if (po != null)
                {
                    using var con2 = new MySqlConnection(_conn);
                    using var cmd2 = new MySqlCommand(@"
                        SELECT poi.Id, poi.PONumber, poi.MedicineId, COALESCE(m.MedicineName,'—') AS MedicineName,
                               poi.Quantity, poi.UnitCost
                        FROM purchase_order_item poi
                        LEFT JOIN tbl_medicine m ON m.MedicineId = poi.MedicineId
                        WHERE poi.PONumber = @po", con2);
                    cmd2.Parameters.AddWithValue("@po", poNumber);
                    con2.Open();
                    using var r2 = cmd2.ExecuteReader();
                    while (r2.Read())
                    {
                        po.Items.Add(new PurchaseOrderItem
                        {
                            Id = Convert.ToInt32(r2["Id"]),
                            PONumber = r2["PONumber"].ToString(),
                            MedicineId = Convert.ToInt32(r2["MedicineId"]),
                            MedicineName = r2["MedicineName"]?.ToString(),
                            Quantity = Convert.ToInt32(r2["Quantity"]),
                            UnitCost = Convert.ToDecimal(r2["UnitCost"])
                        });
                    }
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "GetPurchaseOrder failed"); }
            return po;
        }

        public string CreatePurchaseOrder(PurchaseOrder po, int hospitalId, int? subHospitalId, int createdBy)
        {
            string poNumber = $"PO-{DateTime.Now:yyyyMMdd-HHmmss}";
            using var con = new MySqlConnection(_conn);
            con.Open();
            using var tx = con.BeginTransaction();
            try
            {
                using var cmd = new MySqlCommand("sp_PO_Create", con, tx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_PONumber", poNumber);
                cmd.Parameters.AddWithValue("p_SupplierId", po.SupplierId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                cmd.Parameters.AddWithValue("p_ExpectedDate", po.ExpectedDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_Notes", po.Notes ?? "");
                cmd.Parameters.AddWithValue("p_CreatedBy", createdBy);
                cmd.ExecuteNonQuery();

                foreach (var item in po.Items)
                {
                    if (item.Quantity <= 0) continue;
                    using var cmdItem = new MySqlCommand("sp_PO_AddItem", con, tx);
                    cmdItem.CommandType = CommandType.StoredProcedure;
                    cmdItem.Parameters.AddWithValue("p_PONumber", poNumber);
                    cmdItem.Parameters.AddWithValue("p_MedicineId", item.MedicineId);
                    cmdItem.Parameters.AddWithValue("p_Quantity", item.Quantity);
                    cmdItem.Parameters.AddWithValue("p_UnitCost", item.UnitCost);
                    cmdItem.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
            return poNumber;
        }

        public void UpdatePurchaseOrderStatus(string poNumber, string status, int hospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand("sp_PO_UpdateStatus", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_PONumber", poNumber);
                cmd.Parameters.AddWithValue("p_Status", status);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { _logger.LogError(ex, "UpdatePurchaseOrderStatus failed"); }
        }

        // =====================================================================
        // GOODS RECEIPT (GRN)
        // =====================================================================
        public List<GoodsReceipt> GetGoodsReceipts(int hospitalId, int? subHospitalId)
        {
            var list = new List<GoodsReceipt>();
            try
            {
                using var con = new MySqlConnection(_conn);
                string sql = @"SELECT gr.GRNNumber, gr.PONumber, gr.SupplierId, COALESCE(s.SupplierName,'—') AS SupplierName,
                                      gr.HospitalId, gr.SubHospitalId, gr.GRNDate, gr.SupplierInvoiceNo, gr.Notes
                               FROM goods_receipt gr
                               LEFT JOIN store_suppliers s ON s.SupplierId = gr.SupplierId
                               WHERE gr.HospitalId = @h
                                 AND (gr.SubHospitalId = @sh OR @sh = 0)
                               ORDER BY gr.GRNDate DESC";
                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                cmd.Parameters.AddWithValue("@sh", subHospitalId ?? 0);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new GoodsReceipt
                    {
                        GRNNumber = r["GRNNumber"].ToString(),
                        PONumber = r["PONumber"]?.ToString(),
                        SupplierId = Convert.ToInt32(r["SupplierId"]),
                        SupplierName = r["SupplierName"]?.ToString(),
                        HospitalId = Convert.ToInt32(r["HospitalId"]),
                        SubHospitalId = r["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["SubHospitalId"]),
                        GRNDate = Convert.ToDateTime(r["GRNDate"]),
                        SupplierInvoiceNo = r["SupplierInvoiceNo"]?.ToString(),
                        Notes = r["Notes"]?.ToString()
                    });
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "GetGoodsReceipts failed"); }
            return list;
        }

        public string CreateGoodsReceipt(GoodsReceipt grn, int hospitalId, int? subHospitalId, int createdBy)
        {
            string grnNumber = $"GRN-{DateTime.Now:yyyyMMdd-HHmmss}";
            using var con = new MySqlConnection(_conn);
            con.Open();
            using var tx = con.BeginTransaction();
            try
            {
                using var cmd = new MySqlCommand("sp_GRN_Create", con, tx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_GRNNumber", grnNumber);
                cmd.Parameters.AddWithValue("p_PONumber", grn.PONumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_SupplierId", grn.SupplierId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                cmd.Parameters.AddWithValue("p_SupplierInvoiceNo", grn.SupplierInvoiceNo ?? "");
                cmd.Parameters.AddWithValue("p_Notes", grn.Notes ?? "");
                cmd.Parameters.AddWithValue("p_CreatedBy", createdBy);
                cmd.ExecuteNonQuery();

                foreach (var item in grn.Items)
                {
                    if (item.Quantity <= 0) continue;
                    using var itemCmd = new MySqlCommand("sp_GRN_AddItem", con, tx);
                    itemCmd.CommandType = CommandType.StoredProcedure;
                    itemCmd.Parameters.AddWithValue("p_GRNNumber", grnNumber);
                    itemCmd.Parameters.AddWithValue("p_MedicineId", item.MedicineId);
                    itemCmd.Parameters.AddWithValue("p_BatchNumber", item.BatchNumber ?? "");
                    itemCmd.Parameters.AddWithValue("p_ExpiryDate", item.ExpiryDate ?? (object)DBNull.Value);
                    itemCmd.Parameters.AddWithValue("p_Mrp", item.Mrp);
                    itemCmd.Parameters.AddWithValue("p_SellingPrice", item.SellingPrice);
                    itemCmd.Parameters.AddWithValue("p_PurchasePrice", item.PurchasePrice);
                    itemCmd.Parameters.AddWithValue("p_Quantity", item.Quantity);
                    itemCmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    itemCmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                    itemCmd.Parameters.AddWithValue("p_CreatedBy", createdBy);
                    itemCmd.ExecuteNonQuery();
                }

                // Mark the PO as Received if it exists
                if (!string.IsNullOrEmpty(grn.PONumber))
                {
                    using var poCmd = new MySqlCommand("sp_PO_UpdateStatus", con, tx);
                    poCmd.CommandType = CommandType.StoredProcedure;
                    poCmd.Parameters.AddWithValue("p_PONumber", grn.PONumber);
                    poCmd.Parameters.AddWithValue("p_Status", "Received");
                    poCmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    poCmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
            return grnNumber;
        }

        // =====================================================================
        // STOCK ADJUSTMENT
        // =====================================================================
        public void AdjustStock(StockAdjustment adj, int hospitalId, int? subHospitalId, int changedBy)
        {
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand("sp_Stock_Adjust", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_MedicineId", adj.MedicineId);
                cmd.Parameters.AddWithValue("p_BatchId", adj.BatchId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_QuantityChange", adj.QuantityChange);
                cmd.Parameters.AddWithValue("p_Reason", adj.Reason);
                cmd.Parameters.AddWithValue("p_Notes", adj.Notes ?? "");
                cmd.Parameters.AddWithValue("p_ChangedBy", changedBy);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { _logger.LogError(ex, "AdjustStock failed"); throw; }
        }

        public List<StockAdjustment> GetStockAdjustmentLog(int hospitalId, int? subHospitalId)
        {
            var list = new List<StockAdjustment>();
            try
            {
                using var con = new MySqlConnection(_conn);
                string sql = @"SELECT sal.MedicineId, COALESCE(m.MedicineName,'—') AS MedicineName,
                                      sal.BatchId, COALESCE(b.BatchNumber,'—') AS BatchNumber,
                                      sal.QuantityChange, sal.Reason, sal.Notes, sal.ChangedDate
                               FROM stock_adjustment_log sal
                               LEFT JOIN tbl_medicine m ON m.MedicineId = sal.MedicineId
                               LEFT JOIN batches b ON b.BatchId = sal.BatchId
                               WHERE sal.HospitalId = @h
                                 AND (sal.SubHospitalId = @sh OR @sh = 0)
                               ORDER BY sal.ChangedDate DESC LIMIT 100";
                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                cmd.Parameters.AddWithValue("@sh", subHospitalId ?? 0);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new StockAdjustment
                    {
                        MedicineId = Convert.ToInt32(r["MedicineId"]),
                        MedicineName = r["MedicineName"]?.ToString(),
                        BatchId = r["BatchId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["BatchId"]),
                        BatchNumber = r["BatchNumber"]?.ToString(),
                        QuantityChange = Convert.ToInt32(r["QuantityChange"]),
                        Reason = r["Reason"]?.ToString(),
                        Notes = r["Notes"]?.ToString()
                    });
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "GetStockAdjustmentLog failed"); }
            return list;
        }

        // =====================================================================
        // RETURNS / REFUNDS
        // =====================================================================
        public List<PharmacyReturn> GetReturns(int hospitalId, int? subHospitalId)
        {
            var list = new List<PharmacyReturn>();
            try
            {
                using var con = new MySqlConnection(_conn);
                string sql = @"SELECT pr.ReturnNumber, pr.BillId, pr.CustomerName, pr.MobileNumber,
                                      pr.ReturnDate, pr.ReturnType, pr.Reason, pr.TotalRefund
                               FROM pharmacy_return pr
                               WHERE pr.HospitalId = @h
                                 AND (pr.SubHospitalId = @sh OR @sh = 0)
                               ORDER BY pr.ReturnDate DESC";
                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                cmd.Parameters.AddWithValue("@sh", subHospitalId ?? 0);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new PharmacyReturn
                    {
                        ReturnNumber = r["ReturnNumber"].ToString(),
                        BillId = r["BillId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["BillId"]),
                        CustomerName = r["CustomerName"]?.ToString(),
                        MobileNumber = r["MobileNumber"]?.ToString(),
                        ReturnDate = Convert.ToDateTime(r["ReturnDate"]),
                        ReturnType = r["ReturnType"]?.ToString(),
                        Reason = r["Reason"]?.ToString(),
                        TotalRefund = Convert.ToDecimal(r["TotalRefund"])
                    });
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "GetReturns failed"); }
            return list;
        }

        public string CreateReturn(PharmacyReturn pr, int hospitalId, int? subHospitalId, int createdBy)
        {
            string returnNumber = $"RET-{DateTime.Now:yyyyMMdd-HHmmss}";
            using var con = new MySqlConnection(_conn);
            con.Open();
            using var tx = con.BeginTransaction();
            try
            {
                using var cmd = new MySqlCommand("sp_Return_Create", con, tx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_ReturnNumber", returnNumber);
                cmd.Parameters.AddWithValue("p_BillId", pr.BillId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_CustomerName", pr.CustomerName ?? "");
                cmd.Parameters.AddWithValue("p_MobileNumber", pr.MobileNumber ?? "");
                cmd.Parameters.AddWithValue("p_ReturnType", pr.ReturnType ?? "Patient");
                cmd.Parameters.AddWithValue("p_Reason", pr.Reason ?? "");
                cmd.Parameters.AddWithValue("p_TotalRefund", pr.TotalRefund);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                cmd.Parameters.AddWithValue("p_CreatedBy", createdBy);
                cmd.ExecuteNonQuery();

                // Restock each returned item via the adjustment log
                foreach (var item in pr.Items)
                {
                    if (item.Quantity <= 0) continue;
                    using var adjCmd = new MySqlCommand("sp_Stock_Adjust", con, tx);
                    adjCmd.CommandType = CommandType.StoredProcedure;
                    adjCmd.Parameters.AddWithValue("p_MedicineId", item.MedicineId);
                    adjCmd.Parameters.AddWithValue("p_BatchId", item.BatchId ?? (object)DBNull.Value);
                    adjCmd.Parameters.AddWithValue("p_QuantityChange", item.Quantity);      // +ve restock
                    adjCmd.Parameters.AddWithValue("p_Reason", "Return");
                    adjCmd.Parameters.AddWithValue("p_Notes", $"Return {returnNumber}");
                    adjCmd.Parameters.AddWithValue("p_ChangedBy", createdBy);
                    adjCmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    adjCmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);
                    adjCmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
            return returnNumber;
        }

        // =====================================================================
        // NARCOTICS REGISTER
        // =====================================================================
        public List<NarcoticsRegisterEntry> GetNarcoticsRegister(int hospitalId, int? subHospitalId)
        {
            var list = new List<NarcoticsRegisterEntry>();
            try
            {
                using var con = new MySqlConnection(_conn);
                string sql = @"SELECT nr.Id, nr.RegisterDate, nr.MedicineId, COALESCE(m.MedicineName,'—') AS MedicineName,
                                      nr.BatchId, nr.QuantityOut, nr.BillId, nr.PatientName, nr.PrescriberName,
                                      nr.DispensedBy, nr.AuthorizedBy
                               FROM narcotics_register nr
                               LEFT JOIN tbl_medicine m ON m.MedicineId = nr.MedicineId
                               WHERE nr.HospitalId = @h
                                 AND (nr.SubHospitalId = @sh OR @sh = 0)
                               ORDER BY nr.RegisterDate DESC";
                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                cmd.Parameters.AddWithValue("@sh", subHospitalId ?? 0);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new NarcoticsRegisterEntry
                    {
                        Id = Convert.ToInt32(r["Id"]),
                        RegisterDate = Convert.ToDateTime(r["RegisterDate"]),
                        MedicineId = Convert.ToInt32(r["MedicineId"]),
                        MedicineName = r["MedicineName"]?.ToString(),
                        BatchId = r["BatchId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["BatchId"]),
                        QuantityOut = Convert.ToInt32(r["QuantityOut"]),
                        BillId = r["BillId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["BillId"]),
                        PatientName = r["PatientName"]?.ToString(),
                        PrescriberName = r["PrescriberName"]?.ToString(),
                        DispensedBy = Convert.ToInt32(r["DispensedBy"]),
                        AuthorizedBy = Convert.ToInt32(r["AuthorizedBy"])
                    });
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "GetNarcoticsRegister failed"); }
            return list;
        }

        public void AddNarcoticsEntry(NarcoticsRegisterEntry entry, int hospitalId, int? subHospitalId, int dispensedBy, int authorizedBy)
        {
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand(@"
                    INSERT INTO narcotics_register (MedicineId, BatchId, QuantityOut, BillId, PatientName, PrescriberName, DispensedBy, AuthorizedBy, HospitalId, SubHospitalId)
                    VALUES (@m, @b, @q, @bill, @pn, @pr, @db, @ab, @h, @sh)", con);
                cmd.Parameters.AddWithValue("@m", entry.MedicineId);
                cmd.Parameters.AddWithValue("@b", entry.BatchId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@q", entry.QuantityOut);
                cmd.Parameters.AddWithValue("@bill", entry.BillId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@pn", entry.PatientName ?? "");
                cmd.Parameters.AddWithValue("@pr", entry.PrescriberName ?? "");
                cmd.Parameters.AddWithValue("@db", dispensedBy);
                cmd.Parameters.AddWithValue("@ab", authorizedBy);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                cmd.Parameters.AddWithValue("@sh", subHospitalId ?? 0);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { _logger.LogError(ex, "AddNarcoticsEntry failed"); throw; }
        }

        // =====================================================================
        // CLINICAL SAFETY
        // =====================================================================
        public ClinicalSafetyCheckVM CheckClinicalSafety(List<int> medicineIds, int patientId, int hospitalId)
        {
            var result = new ClinicalSafetyCheckVM();
            if (medicineIds == null || medicineIds.Count == 0) return result;

            try
            {
                using var con = new MySqlConnection(_conn);
                con.Open();

                // 1. Drug interactions between medicines in the same prescription
                foreach (var a in medicineIds)
                {
                    foreach (var b in medicineIds)
                    {
                        if (a >= b) continue;
                        using var cmd = new MySqlCommand(@"
                            SELECT di.Severity, di.Description, ma.MedicineName AS AN, mb.MedicineName AS BN
                            FROM drug_interaction di
                            JOIN tbl_medicine ma ON ma.MedicineId = di.MedicineA
                            JOIN tbl_medicine mb ON mb.MedicineId = di.MedicineB
                            WHERE ((di.MedicineA = @a AND di.MedicineB = @b) OR (di.MedicineA = @b AND di.MedicineB = @a))
                              AND di.HospitalId = @h", con);
                        cmd.Parameters.AddWithValue("@a", a);
                        cmd.Parameters.AddWithValue("@b", b);
                        cmd.Parameters.AddWithValue("@h", hospitalId);
                        using var r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            result.HasAlert = true;
                            result.Alerts.Add(new ClinicalAlert
                            {
                                Type = "Interaction",
                                Severity = r["Severity"]?.ToString(),
                                Message = $"{r["AN"]} + {r["BN"]}: {r["Description"]}"
                            });
                        }
                    }
                }

                // 2. Patient allergies
                using var con2 = new MySqlConnection(_conn);
                con2.Open();
                foreach (var mid in medicineIds)
                {
                    using var cmd = new MySqlCommand(@"
                        SELECT pa.AllergyName, pa.Severity, pa.Reaction
                        FROM patient_allergy pa
                        WHERE pa.PatientId = @pid AND pa.HospitalId = @h
                          AND (pa.MedicineId = @mid OR pa.MedicineId IS NULL)", con2);
                    cmd.Parameters.AddWithValue("@pid", patientId);
                    cmd.Parameters.AddWithValue("@h", hospitalId);
                    cmd.Parameters.AddWithValue("@mid", mid);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        result.HasAlert = true;
                        result.Alerts.Add(new ClinicalAlert
                        {
                            Type = "Allergy",
                            Severity = r["Severity"]?.ToString(),
                            Message = $"Allergy: {r["AllergyName"]} (Reaction: {r["Reaction"]})"
                        });
                    }
                }

                // 3. Expired-batch check for the prescribed medicines
                using var con3 = new MySqlConnection(_conn);
                con3.Open();
                foreach (var mid in medicineIds)
                {
                    using var cmd = new MySqlCommand(@"
                        SELECT COUNT(*) FROM batches
                        WHERE MedicineId = @mid AND HospitalId = @h AND Quantity > 0
                          AND ExpiryDate IS NOT NULL AND ExpiryDate < CURDATE()", con3);
                    cmd.Parameters.AddWithValue("@mid", mid);
                    cmd.Parameters.AddWithValue("@h", hospitalId);
                    var cnt = Convert.ToInt32(cmd.ExecuteScalar());
                    if (cnt > 0)
                    {
                        result.HasAlert = true;
                        result.Alerts.Add(new ClinicalAlert
                        {
                            Type = "Expired",
                            Severity = "Major",
                            Message = "One or more batches of this medicine are expired and must not be dispensed."
                        });
                    }
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "CheckClinicalSafety failed"); }
            return result;
        }

        public List<DrugInteraction> GetDrugInteractions(int hospitalId)
        {
            var list = new List<DrugInteraction>();
            try
            {
                using var con = new MySqlConnection(_conn);
                string sql = @"SELECT di.Id, di.MedicineA, ma.MedicineName AS AN, di.MedicineB, mb.MedicineName AS BN,
                                      di.Severity, di.Description
                               FROM drug_interaction di
                               JOIN tbl_medicine ma ON ma.MedicineId = di.MedicineA
                               JOIN tbl_medicine mb ON mb.MedicineId = di.MedicineB
                               WHERE di.HospitalId = @h";
                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new DrugInteraction
                    {
                        Id = Convert.ToInt32(r["Id"]),
                        MedicineA = Convert.ToInt32(r["MedicineA"]),
                        MedicineAName = r["AN"]?.ToString(),
                        MedicineB = Convert.ToInt32(r["MedicineB"]),
                        MedicineBName = r["BN"]?.ToString(),
                        Severity = r["Severity"]?.ToString(),
                        Description = r["Description"]?.ToString(),
                        HospitalId = hospitalId
                    });
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "GetDrugInteractions failed"); }
            return list;
        }

        public void AddDrugInteraction(DrugInteraction interaction, int hospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand(@"
                    INSERT INTO drug_interaction (MedicineA, MedicineB, Severity, Description, HospitalId)
                    VALUES (@a, @b, @sev, @desc, @h)", con);
                cmd.Parameters.AddWithValue("@a", interaction.MedicineA);
                cmd.Parameters.AddWithValue("@b", interaction.MedicineB);
                cmd.Parameters.AddWithValue("@sev", interaction.Severity ?? "Moderate");
                cmd.Parameters.AddWithValue("@desc", interaction.Description ?? "");
                cmd.Parameters.AddWithValue("@h", hospitalId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { _logger.LogError(ex, "AddDrugInteraction failed"); throw; }
        }

        public List<PatientAllergyRecord> GetPatientAllergies(int patientId, int hospitalId)
        {
            var list = new List<PatientAllergyRecord>();
            try
            {
                using var con = new MySqlConnection(_conn);
                string sql = @"SELECT Id, PatientId, MedicineId, AllergyName, Severity, Reaction, HospitalId, CreatedDate
                               FROM patient_allergy
                               WHERE PatientId = @pid AND HospitalId = @h
                               ORDER BY CreatedDate DESC";
                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@pid", patientId);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new PatientAllergyRecord
                    {
                        Id = Convert.ToInt32(r["Id"]),
                        PatientId = Convert.ToInt32(r["PatientId"]),
                        MedicineId = r["MedicineId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["MedicineId"]),
                        AllergyName = r["AllergyName"]?.ToString(),
                        Severity = r["Severity"]?.ToString(),
                        Reaction = r["Reaction"]?.ToString(),
                        HospitalId = Convert.ToInt32(r["HospitalId"]),
                        CreatedDate = Convert.ToDateTime(r["CreatedDate"])
                    });
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "GetPatientAllergies failed"); }
            return list;
        }

        public void AddPatientAllergy(PatientAllergyRecord allergy, int hospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand(@"
                    INSERT INTO patient_allergy (PatientId, MedicineId, AllergyName, Severity, Reaction, HospitalId)
                    VALUES (@pid, @mid, @name, @sev, @react, @h)", con);
                cmd.Parameters.AddWithValue("@pid", allergy.PatientId);
                cmd.Parameters.AddWithValue("@mid", allergy.MedicineId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@name", allergy.AllergyName);
                cmd.Parameters.AddWithValue("@sev", allergy.Severity ?? "Moderate");
                cmd.Parameters.AddWithValue("@react", allergy.Reaction ?? "");
                cmd.Parameters.AddWithValue("@h", hospitalId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { _logger.LogError(ex, "AddPatientAllergy failed"); throw; }
        }
    }
}
