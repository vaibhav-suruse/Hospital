using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    // ── OT Management — Phase 1: Theatre & Booking Scheduling ────────────
    // Resource layer only. Clinical documentation (team, checklist,
    // anaesthesia, timeline, complications, outcome) stays entirely in
    // procedure_master and its existing tables — never duplicated here.

    public class OTTheatreModel
    {
        public int TheatreId { get; set; }
        public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }

        [Required(ErrorMessage = "Theatre name is required.")]
        public string TheatreName { get; set; }

        public string TheatreNumber { get; set; }

        // Active | Maintenance | Blocked
        public string Status { get; set; } = "Active";
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class OTTheatreSaveModel
    {
        public int TheatreId { get; set; } // 0 = new

        [Required(ErrorMessage = "Theatre name is required.")]
        public string TheatreName { get; set; }

        public string TheatreNumber { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsActive { get; set; } = true;
    }

    // ── A procedure that is ready to be scheduled into a theatre ─────────
    public class SchedulableProcedureModel
    {
        public int ProcedureId { get; set; }
        public string ProcedureUID { get; set; }
        public string ProcedureName { get; set; }
        public string ProcedureCategory { get; set; }
        public string Priority { get; set; }     // Routine / Urgent / Emergency
        public string Status { get; set; }
        public DateTime? PlannedDate { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string VisitContextType { get; set; }
        public int VisitContextId { get; set; }
        public string PrimarySurgeonName { get; set; }
    }

    // ── A confirmed OT booking (theatre + time slot) ──────────────────────
    public class OTBookingModel
    {
        public int BookingId { get; set; }
        public int TheatreId { get; set; }
        public string TheatreName { get; set; }
        public string TheatreNumber { get; set; }

        public int ProcedureId { get; set; }
        public string ProcedureUID { get; set; }
        public string ProcedureName { get; set; }
        public string ProcedureCategory { get; set; }
        public string Priority { get; set; }

        public DateTime ScheduledStart { get; set; }
        public DateTime ScheduledEnd { get; set; }

        // Booked | InProgress | Completed | Cancelled
        public string Status { get; set; }
        public string CancelReason { get; set; }

        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string PrimarySurgeonName { get; set; }

        // ── Phase 2: pre-op safety + billing visibility, at a glance ──────
        public DateTime? ConsentTakenDate { get; set; }
        public string ConsentWitnessName { get; set; }
        public int? IPDOperationId { get; set; }     // set once OT charges are recorded
        public int MandatoryChecklistCount { get; set; }
        public int MandatoryChecklistDone { get; set; }

        public bool ConsentRecorded => ConsentTakenDate.HasValue;
        public bool ChecklistComplete => MandatoryChecklistCount == 0 || MandatoryChecklistDone >= MandatoryChecklistCount;
        public bool ChargesRecorded => IPDOperationId.HasValue;
    }

    // ── POST model: book a theatre for a procedure ────────────────────────
    public class OTBookingCreateModel
    {
        [Required]
        public int TheatreId { get; set; }

        [Required]
        public int ProcedureId { get; set; }

        [Required(ErrorMessage = "Start time is required.")]
        public DateTime ScheduledStart { get; set; }

        [Range(15, 1440, ErrorMessage = "Duration must be between 15 minutes and 24 hours.")]
        public int DurationMinutes { get; set; } = 60;
    }

    public class OTBookingStatusUpdateModel
    {
        [Required]
        public int BookingId { get; set; }

        // Needed so Cancel can also sync procedure_master.Status via the
        // existing IProcedure.UpdateProcedureStatus — Start/Complete use
        // their own dedicated actions (StartBooking/CompleteBooking) instead,
        // since those business rules need more than a status string.
        public int ProcedureId { get; set; }

        [Required]
        public string Status { get; set; } // Cancelled (Start/Complete go through StartBooking/CompleteBooking)

        public string CancelReason { get; set; }
    }

    // ── Page ViewModel for the OT Board ────────────────────────────────────
    public class OTBoardVM
    {
        public DateTime BoardDate { get; set; } = DateTime.Today;
        public List<OTTheatreModel> Theatres { get; set; } = new List<OTTheatreModel>();
        public List<OTBookingModel> Bookings { get; set; } = new List<OTBookingModel>();

        // Context if opened from a specific IPD admission
        public int? IPDId { get; set; }
        public int? PatientId { get; set; }
        public string PatientName { get; set; }
    }

    // ═══════════════════════════ PHASE 2 ═══════════════════════════════

    // ── Staff (surgeon/assistant/anaesthetist/nurse) clash pre-check ────
    public class StaffConflictModel
    {
        public int StaffId { get; set; }
        public string RoleInTeam { get; set; }
        public string StaffName { get; set; }
        public int ConflictingBookingId { get; set; }
        public string ConflictingTheatre { get; set; }
        public DateTime ConflictStart { get; set; }
        public DateTime ConflictEnd { get; set; }
    }

    // ── Consumables / Implants (linked to existing Inventory) ────────────
    public class ProcedureConsumableModel
    {
        public int ConsumableId { get; set; }
        public int ProcedureId { get; set; }
        public int MedicineId { get; set; }
        public int BatchId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; } = 1;
        public bool IsImplant { get; set; }
        public string LotOrSerialNumber { get; set; }
        public decimal Charge { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ConsumableSaveModel
    {
        [Required]
        public int ProcedureId { get; set; }

        [Required]
        public int MedicineId { get; set; }

        [Required]
        public int BatchId { get; set; }

        [Required(ErrorMessage = "Item name is required.")]
        public string ItemName { get; set; }

        [Range(1, 9999, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; } = 1;

        public bool IsImplant { get; set; }
        public string LotOrSerialNumber { get; set; }

        [Range(0, 9999999)]
        public decimal Charge { get; set; }
    }

    // ── WHO 3-phase checklist tagging ────────────────────────────────────
    public class ChecklistPhaseItemModel
    {
        public int ChecklistItemId { get; set; }
        public string ItemText { get; set; }
        public bool IsMandatory { get; set; }
        public bool IsChecked { get; set; }
        public int? CheckedBy { get; set; }
        public DateTime? CheckedDate { get; set; }

        // SignIn | TimeOut | SignOut | null (untagged / general)
        public string Phase { get; set; }
    }

    // ── OT Register report row ───────────────────────────────────────────
    public class OTRegisterRowModel
    {
        public int BookingId { get; set; }
        public DateTime ScheduledStart { get; set; }
        public DateTime ScheduledEnd { get; set; }
        public string BookingStatus { get; set; }
        public string CancelReason { get; set; }
        public string TheatreName { get; set; }
        public string TheatreNumber { get; set; }
        public int ProcedureId { get; set; }
        public string ProcedureUID { get; set; }
        public string ProcedureName { get; set; }
        public string ProcedureCategory { get; set; }
        public string Priority { get; set; }
        public string ProcedureStatus { get; set; }
        public int? DurationMinutes { get; set; }
        public string PatientName { get; set; }
        public string PrimarySurgeonName { get; set; }
        public string AnaesthetistName { get; set; }
        public string Outcome { get; set; }
    }

    // ── Complete a case (mirrors IProcedure.CompleteProcedure's contract) ─
    public class CompleteBookingModel
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        public int ProcedureId { get; set; }

        [Required(ErrorMessage = "Outcome is required.")]
        public string Outcome { get; set; } // Successful | Partial | Failed | StoppedDueToComplication

        public string FinalRemarks { get; set; }
    }

    // ── Quick "Request New Procedure" from the OT board's empty state ────
    public class QuickProcedureRequestModel
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public string VisitContextType { get; set; } // IPD / OPD / Emergency

        [Required]
        public int VisitContextId { get; set; }

        [Required(ErrorMessage = "Procedure name is required.")]
        public string ProcedureName { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public string ProcedureCategory { get; set; } // Surgical / Emergency (etc.)

        public string Priority { get; set; } = "Routine";
        public string ReasonForProcedure { get; set; }
        public string ClinicalIndication { get; set; }
    }

    // ── Pre-op consent — gates Start alongside the checklist ─────────────
    public class ConsentSaveModel
    {
        [Required]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Witness name is required.")]
        public string ConsentWitnessName { get; set; }
    }

    // ── Record OT charges into the EXISTING ipdoperations table, reusing
    // IIPDOperation.SaveAndReturnId(...) exactly as IPDOperationController
    // already does — this is not a new billing table or a new charge
    // engine, just the missing link from a completed booking to it. ─────
    public class OTChargesSaveModel
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        public int ProcedureId { get; set; }

        [Required]
        public int IPDId { get; set; }

        [Required(ErrorMessage = "Operation type is required.")]
        public int OperationId { get; set; }

        [Required]
        public DateTime OperationDate { get; set; }

        public int? SurgeonId { get; set; }
        public int? AnesthesistId { get; set; }

        [Range(0, 9999999)]
        public decimal ActualCharge { get; set; }

        [Range(0, 9999999)]
        public decimal AnesthesiaCharge { get; set; }

        [Range(0, 9999999)]
        public decimal SurgeonCharge { get; set; }

        [Range(0, 9999999)]
        public decimal OTCharge { get; set; }

        public string Notes { get; set; }
    }
}
