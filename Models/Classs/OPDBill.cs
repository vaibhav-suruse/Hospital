// Models/OPDBillingModels.cs
using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    // ── Bill Header ──────────────────────────────────────────────────────
    public class OPDBill
    {
        public int BillId { get; set; }
        public int AppointmentId { get; set; }
        public int? OPDId { get; set; }
        public int PatientId { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public string BillNumber { get; set; }
        public DateTime BillDate { get; set; }
        public decimal ConsultationFee { get; set; }
        public decimal MedicineCharges { get; set; }
        public decimal ProcedureCharges { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal SubTotal { get; set; }
        public decimal LineDiscountAmount { get; set; }
        public decimal GstAmount { get; set; }
        public decimal ExtraDiscountValue { get; set; }
        public bool ExtraDiscountIsPercent { get; set; } = true;
        public decimal ExtraDiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string BillStatus { get; set; } = "Draft"; // Draft / Finalized / Cancelled
        public string CancelledReason { get; set; }
        public int? CancelledBy { get; set; }
        public DateTime? CancelledDate { get; set; }
        public string PaymentMode { get; set; }
        public string TransactionRef { get; set; }
        public string Notes { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    // ── Bill Item ────────────────────────────────────────────────────────
    public class OPDBillItem
    {
        public int ItemId { get; set; }
        public int BillId { get; set; }
        public int AppointmentId { get; set; }
        public string ItemType { get; set; }
        public int? BillingMasterId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal DiscountValue { get; set; }
        public bool DiscountIsPercent { get; set; } = true;
        public decimal GstPercent { get; set; }
        public decimal TotalPrice { get; set; }
        public string Notes { get; set; }
    }

    // ── Medicine line in billing ─────────────────────────────────────────
    public class OPDBillMedicine
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string Type { get; set; }
        public bool Morning { get; set; }
        public bool Afternoon { get; set; }
        public bool Evening { get; set; }
        public int? Days { get; set; }
        // Real dispensed quantity from opdmedicine.Quantity (falls back to
        // Days, then 1, inside the SP itself when never entered). This is
        // what the OPD bill line quantity should use — NOT Days.
        public int DispensedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    // ── Payment ledger entry ─────────────────────────────────────────────
    public class OPDPayment
    {
        public int PaymentId { get; set; }
        public string ReceiptNumber { get; set; }
        public int BillId { get; set; }
        public int AppointmentId { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; }
        public string TransactionRef { get; set; }
        public string Notes { get; set; }
        public int? ReceivedBy { get; set; }
        public string CollectedByName { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    // ── Payment entry as sent from the split-payment UI ──────────────────
    public class PaymentEntryDto
    {
        public string Mode { get; set; }
        public decimal Amount { get; set; }
    }

    // ── Full Bill ViewModel ──────────────────────────────────────────────
    public class OPDBillVM
    {
        // Appointment + Patient info
        public int AppointmentId { get; set; }
        public int? OPDId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public string DoctorName { get; set; }
        public string Specialization { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string AppointmentStatus { get; set; }

        // Medicines from OPD (prescribed) — shown as suggested items
        public List<OPDBillMedicine> Medicines { get; set; }
            = new List<OPDBillMedicine>();

        // Suggested consultation fee from billing master (0 if none configured)
        public decimal SuggestedConsultationFee { get; set; }

        // Other/Procedure items added manually, OR the saved items of an
        // existing bill (used to re-hydrate the screen on reopen so nothing
        // has to be re-entered from scratch)
        public List<OPDBillItem> OtherItems { get; set; }
            = new List<OPDBillItem>();

        // Payment history (ledger) for an existing bill
        public List<OPDPayment> Payments { get; set; }
            = new List<OPDPayment>();

        // Existing bill (if already generated)
        public int? BillId { get; set; }
        public string BillNumber { get; set; }
        public DateTime? BillDate { get; set; }
        public decimal ConsultationFee { get; set; }
        public decimal MedicineCharges { get; set; }
        public decimal ProcedureCharges { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal SubTotal { get; set; }
        public decimal LineDiscountAmount { get; set; }
        public decimal GstAmount { get; set; }
        public decimal ExtraDiscountValue { get; set; }
        public bool ExtraDiscountIsPercent { get; set; } = true;
        public decimal ExtraDiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string BillStatus { get; set; } = "Draft";
        public string CancelledReason { get; set; }
        public DateTime? CancelledDate { get; set; }
        public string PaymentMode { get; set; }
        public string Notes { get; set; }
    }

    // ── Save Bill Request (from JS fetch) ────────────────────────────────
    // Field names below match GenerateBill.cshtml's saveBill() payload
    // exactly (System.Text.Json / Newtonsoft bind case-insensitively).
    public class SaveOPDBillRequest
    {
        public int AppointmentId { get; set; }
        public int? OPDId { get; set; }
        public int PatientId { get; set; }
        public DateTime? BillDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal LineDiscountAmt { get; set; }
        public decimal GstAmount { get; set; }
        public decimal ExtraDiscountValue { get; set; }
        public bool ExtraDiscountIsPercent { get; set; }
        public decimal ExtraDiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentMode { get; set; }
        public List<PaymentEntryDto> PaymentEntries { get; set; } = new List<PaymentEntryDto>();
        public string Notes { get; set; }
        public bool IsDraft { get; set; }
        public List<SaveOPDBillItemDto> Items { get; set; } = new List<SaveOPDBillItemDto>();
    }

    // Item shape as sent by collectItems() in GenerateBill.cshtml
    public class SaveOPDBillItemDto
    {
        public string ItemType { get; set; }
        public int? BillingMasterId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal DiscountValue { get; set; }
        public bool DiscountIsPercent { get; set; } = true;
        public decimal GstPercent { get; set; }
        public decimal TotalPrice { get; set; }
    }

    // ── Pay Bill Request (single-payment "Collect Payment" action) ───────
    public class PayOPDBillRequest
    {
        public int BillId { get; set; }
        public int AppointmentId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; }
        public string TransactionRef { get; set; }
        public string Notes { get; set; }
    }

    // ── Cancel Bill Request ───────────────────────────────────────────────
    public class CancelOPDBillRequest
    {
        public int BillId { get; set; }
        public string Reason { get; set; }
    }
}
