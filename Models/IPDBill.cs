// Models/IPDBillingVM.cs
using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    // Main bill header
    public class IPDBill
    {
        public int BillId { get; set; }
        public int IPDId { get; set; }
        public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public string BillNumber { get; set; }
        public DateTime BillDate { get; set; }

        public decimal BedCharges { get; set; }
        public decimal DoctorVisitCharges { get; set; }
        public decimal MedicineCharges { get; set; }
        public decimal InvestigationCharges { get; set; }
        public decimal DischargeMedicineCharges { get; set; }
        public decimal NursingCharges { get; set; }
        public decimal OperationCharges { get; set; }
        public decimal ProcedureCharges { get; set; }
        public decimal OtherCharges { get; set; }

        public decimal SubTotal { get; set; }
        public decimal LineDiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
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
        public string Notes { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    // Bill line item (the editable real-time grid row)
    public class IPDBillItem
    {
        public int ItemId { get; set; }
        public int BillId { get; set; }
        public int IPDId { get; set; }
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

    // Payment ledger entry
    public class IPDPayment
    {
        public int PaymentId { get; set; }
        public string ReceiptNumber { get; set; }
        public int BillId { get; set; }
        public int IPDId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; }
        public string TransactionRef { get; set; }
        public string Notes { get; set; }
        public int ReceivedBy { get; set; }
        public string CollectedByName { get; set; }
    }

    // Bed charge breakdown — auto Days × ChargesPerDay (one row per allocation)
    public class BedChargeDetail
    {
        public int AllocationId { get; set; }
        public string BedNumber { get; set; }
        public string WardName { get; set; }
        public string RoomNumber { get; set; }
        public decimal ChargesPerDay { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int Days { get; set; }
        public decimal BedCharge { get; set; }
    }

    // Doctor visit — auto-priced from Billing Master (Category = DoctorVisit)
    public class DoctorVisitDetail
    {
        public int RoundId { get; set; }
        public DateTime RoundDateTime { get; set; }
        public string RoundType { get; set; }
        public string DoctorName { get; set; }
        public decimal VisitCharge { get; set; }
    }

    // Medicine charge — priced from tbl_medicine.SellingPrice
    public class MedicineChargeDetail
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string Type { get; set; }
        public int? Days { get; set; }
        public string Dosage { get; set; }
        public string Status { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    // Investigation — reference only; SuggestedCharge is a best-effort
    // exact-name match against Billing Master ("Lab" category). Staff always
    // sees and can edit this price before it's actually billed.
    public class InvestigationChargeDetail
    {
        public int Id { get; set; }
        public string InvestigationType { get; set; }
        public string TestName { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public decimal SuggestedCharge { get; set; }
    }

    // Procedure performed during this admission — reference only;
    // SuggestedCharge is a best-effort exact-name match against Billing
    // Master ("Procedure" category).
    public class ProcedureChargeDetail
    {
        public int ProcedureId { get; set; }
        public string ProcedureName { get; set; }
        public string ProcedureCategory { get; set; }
        public string Status { get; set; }
        public DateTime? ProcedureDate { get; set; }
        public decimal SuggestedCharge { get; set; }
    }

    // Full bill VM for the GenerateBill view
    public class IPDBillVM
    {
        // Patient info
        public int IPDId { get; set; }
        public string AdmissionNumber { get; set; }
        public string PatientName { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public string DoctorName { get; set; }
        public DateTime AdmissionDateTime { get; set; }
        public DateTime? ActualDischargeDateTime { get; set; }
        public int TotalDays { get; set; }

        // Suggested / reference charge breakdowns — used to seed the editable
        // item grid the first time a bill is generated for this admission
        public List<BedChargeDetail> BedCharges { get; set; } = new List<BedChargeDetail>();
        public List<DoctorVisitDetail> DoctorVisits { get; set; } = new List<DoctorVisitDetail>();
        public List<MedicineChargeDetail> Medicines { get; set; } = new List<MedicineChargeDetail>();
        public List<InvestigationChargeDetail> Investigations { get; set; } = new List<InvestigationChargeDetail>();
        public List<MedicineChargeDetail> DischargeMedicines { get; set; } = new List<MedicineChargeDetail>();
        public List<ProcedureChargeDetail> Procedures { get; set; } = new List<ProcedureChargeDetail>();

        // Saved items of an existing bill (used to re-hydrate the screen on
        // reopen so nothing has to be re-entered)
        public List<IPDBillItem> OtherItems { get; set; } = new List<IPDBillItem>();

        // Nursing charges — already priced by the Nursing Charges module;
        // pulled in read-only and rolled into the bill total
        public List<IPDNursingCharge> NursingCharges { get; set; } = new List<IPDNursingCharge>();
        public decimal TotalNursingCharges { get; set; }

        // Operations — already priced by OT Management; pulled in read-only
        // and rolled into the bill total
        public List<IPDOperationModel> Operations { get; set; } = new List<IPDOperationModel>();
        public decimal TotalOperationCharges { get; set; }

        // Totals (reference figures shown before item-grid recalculation)
        public decimal TotalBedCharges { get; set; }
        public decimal TotalDoctorCharges { get; set; }
        public decimal TotalMedicineCharges { get; set; }
        public decimal TotalInvestigationCharges { get; set; }
        public decimal TotalDischargeMedCharges { get; set; }
        public decimal TotalProcedureCharges { get; set; }
        public decimal TotalOtherCharges { get; set; }

        // Existing bill (if already generated for this admission)
        public int? BillId { get; set; }
        public string BillNumber { get; set; }
        public DateTime? BillDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal LineDiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string BillStatus { get; set; } = "Draft";
        public string CancelledReason { get; set; }
        public DateTime? CancelledDate { get; set; }
        public string Notes { get; set; }

        // Payment history (ledger)
        public List<IPDPayment> Payments { get; set; } = new List<IPDPayment>();
    }

    // ── Save Bill Request (from JS fetch) ────────────────────────────────
    public class SaveIPDBillRequest
    {
        public int IPDId { get; set; }
        public DateTime? BillDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal LineDiscountAmt { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; }
        public bool IsDraft { get; set; }
        public List<SaveIPDBillItemDto> Items { get; set; } = new List<SaveIPDBillItemDto>();
    }

    // Item shape as sent by collectItems() in GenerateBill.cshtml
    public class SaveIPDBillItemDto
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
    public class PayIPDBillRequest
    {
        public int BillId { get; set; }
        public int IPDId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; }
        public string TransactionRef { get; set; }
        public string Notes { get; set; }
    }

    // ── Cancel Bill Request ───────────────────────────────────────────────
    public class CancelIPDBillRequest
    {
        public int BillId { get; set; }
        public string Reason { get; set; }
    }
}
