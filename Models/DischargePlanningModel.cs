using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    public class DischargePlanningModel
    {
        public int Id { get; set; }
        public int IPDId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EstimatedDischargeDate { get; set; }

        public string Disposition { get; set; }

        public string BarriersToDischarge { get; set; }

        public string EquipmentNeeded { get; set; }
        public bool EquipmentArranged { get; set; }

        public string ReferralsNeeded { get; set; }
        public bool ReferralsArranged { get; set; }

        // Not Started / In Progress / Completed
        public string PatientEducationStatus { get; set; } = "Not Started";

        public bool FollowUpBooked { get; set; }
        public bool PendingReportsCollected { get; set; }

        public string CaseManagerNotes { get; set; }

        public int? UpdatedByUserId { get; set; }
        public string UpdatedByName { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // ── Display-only header info (populated by the controller from the
        // admission record, never stored on this table) ──
        public string PatientName { get; set; }
        public string AdmissionNumber { get; set; }
        public DateTime AdmissionDateTime { get; set; }
        public string BedNumber { get; set; }
        public string WardName { get; set; }
        public string Status { get; set; }

        // ── Live billing snapshot (read from IPD Billing, never stored here) ──
        public decimal? BillingDueAmount { get; set; }

        // ── Computed checklist progress (never stored, computed on read) ──
        public int ChecklistCompleted { get; set; }
        public int ChecklistTotal { get; set; } = 7;
        public int ProgressPercent =>
            ChecklistTotal == 0 ? 0 : (int)Math.Round(ChecklistCompleted * 100.0 / ChecklistTotal);
    }
}
