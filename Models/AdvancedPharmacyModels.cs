using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    // =========================================================================
    // PHASE B — Purchase Order
    // =========================================================================
    public class PurchaseOrder
    {
        public string PONumber { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDate { get; set; }
        public string Status { get; set; }        // Draft / Ordered / Received / Cancelled
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public List<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    }

    public class PurchaseOrderItem
    {
        public int Id { get; set; }
        public string PONumber { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal LineTotal => Quantity * UnitCost;
    }

    // =========================================================================
    // PHASE B — Goods Receipt (GRN)
    // =========================================================================
    public class GoodsReceipt
    {
        public string GRNNumber { get; set; }
        public string PONumber { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public DateTime GRNDate { get; set; }
        public string SupplierInvoiceNo { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public List<GoodsReceiptItem> Items { get; set; } = new List<GoodsReceiptItem>();
    }

    public class GoodsReceiptItem
    {
        public int Id { get; set; }
        public string GRNNumber { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal Mrp { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public int Quantity { get; set; }
    }

    // =========================================================================
    // PHASE C — Stock Adjustment
    // =========================================================================
    public class StockAdjustment
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int? BatchId { get; set; }
        public string BatchNumber { get; set; }
        public int QuantityChange { get; set; }   // +ve stock-in, -ve stock-out
        public string Reason { get; set; }        // Damage / Expired / Correction / Return / Breakage
        public string Notes { get; set; }
        public int ChangedBy { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
    }

    // =========================================================================
    // PHASE C — Return / Refund
    // =========================================================================
    public class PharmacyReturn
    {
        public string ReturnNumber { get; set; }
        public int? BillId { get; set; }
        public string CustomerName { get; set; }
        public string MobileNumber { get; set; }
        public DateTime ReturnDate { get; set; }
        public string ReturnType { get; set; }    // Patient / Supplier / Damage
        public string Reason { get; set; }
        public decimal TotalRefund { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public List<PharmacyReturnItem> Items { get; set; } = new List<PharmacyReturnItem>();
    }

    public class PharmacyReturnItem
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal RefundAmount { get; set; }
        public int? BatchId { get; set; }
    }

    // =========================================================================
    // PHASE D — Controlled substance / narcotics register entry
    // =========================================================================
    public class NarcoticsRegisterEntry
    {
        public int Id { get; set; }
        public DateTime RegisterDate { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int? BatchId { get; set; }
        public int QuantityOut { get; set; }
        public int? BillId { get; set; }
        public string PatientName { get; set; }
        public string PrescriberName { get; set; }
        public int DispensedBy { get; set; }
        public string DispensedByName { get; set; }
        public int AuthorizedBy { get; set; }
        public string AuthorizedByName { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
    }

    // =========================================================================
    // PHASE E — Clinical safety
    // =========================================================================
    public class DrugInteraction
    {
        public int Id { get; set; }
        public int MedicineA { get; set; }
        public string MedicineAName { get; set; }
        public int MedicineB { get; set; }
        public string MedicineBName { get; set; }
        public string Severity { get; set; }   // Minor / Moderate / Major
        public string Description { get; set; }
        public int HospitalId { get; set; }
    }

public class PatientAllergyRecord
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int? MedicineId { get; set; }
        public string AllergyName { get; set; }
        public string Severity { get; set; }
        public string Reaction { get; set; }
        public int HospitalId { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // VM for clinical safety check result returned to the dispensing screen
    public class ClinicalSafetyCheckVM
    {
        public bool HasAlert { get; set; }
        public List<ClinicalAlert> Alerts { get; set; } = new List<ClinicalAlert>();
    }

    public class ClinicalAlert
    {
        public string Type { get; set; }        // "Interaction" | "Allergy" | "Expired"
        public string Severity { get; set; }    // Minor / Moderate / Major
        public string Message { get; set; }
    }
}
