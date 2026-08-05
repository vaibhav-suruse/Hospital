using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    // ── IPD Billing Sheet: day-to-day ad-hoc charge log made by a Doctor
    // or a Nurse against an admission (e.g. "visiting charge ₹600").
    // This is intentionally separate from IPDBill/IPDBillItem (the final
    // consolidated invoice) - those remain completely untouched. ─────────
    public class BillingSheetEntryModel
    {
        public int Id { get; set; }
        public int IPDId { get; set; }

        // "Doctor" or "Nurse"
        public string BillingType { get; set; } = "Doctor";

        public DateTime EntryDate { get; set; }

        public int? DoctorId { get; set; }
        public int? NurseId { get; set; }

        // Denormalized at insert time from the resolved master record -
        // never trusted from client input, always resolved server-side.
        public string ProviderName { get; set; }

        public string ServiceName { get; set; }
        public int? BillingMasterId { get; set; }

        public decimal Charge { get; set; }

        public int? EnteredByUserId { get; set; }
        public string EnteredByName { get; set; }

        public string Remarks { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    // ── POST model for "Add New Bill" ────────────────────────────────────
    public class BillingSheetAddModel
    {
        [Required]
        public int IPDId { get; set; }

        [Required(ErrorMessage = "Billing type is required.")]
        public string BillingType { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        public DateTime EntryDate { get; set; }

        // Exactly one of these must be supplied, matching BillingType.
        public int? DoctorId { get; set; }
        public int? NurseId { get; set; }

        [Required(ErrorMessage = "Service is required.")]
        public string ServiceName { get; set; }

        public int? BillingMasterId { get; set; }

        [Range(0.01, 9999999, ErrorMessage = "Charge must be greater than 0.")]
        public decimal Charge { get; set; }

        public string Remarks { get; set; }
    }

    // ── POST model for editing an existing entry ─────────────────────────
    public class BillingSheetEditModel
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        public DateTime EntryDate { get; set; }

        [Required(ErrorMessage = "Service is required.")]
        public string ServiceName { get; set; }

        [Range(0.01, 9999999, ErrorMessage = "Charge must be greater than 0.")]
        public decimal Charge { get; set; }

        public string Remarks { get; set; }
    }

    // ── Aggregate ViewModel that powers the Billing Sheet page ───────────
    public class BillingSheetVM
    {
        public IPDAdmissionModel Admission { get; set; }

        public List<BillingSheetEntryModel> DoctorEntries { get; set; } = new List<BillingSheetEntryModel>();
        public List<BillingSheetEntryModel> NurseEntries { get; set; } = new List<BillingSheetEntryModel>();

        public decimal DoctorTotal { get; set; }
        public decimal NurseTotal { get; set; }

        // "Doctor" or "Nurse" - which sub-tab is shown first
        public string ActiveTab { get; set; } = "Doctor";
    }
}
