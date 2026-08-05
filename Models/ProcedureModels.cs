using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    // ── MASTER ────────────────────────────────────────────────────────
    public class ProcedureMaster
    {
        public int ProcedureId { get; set; }
        public string ProcedureUID { get; set; }
        public int PatientId { get; set; }
        public string VisitContextType { get; set; }   // IPD / OPD / Emergency
        public int VisitContextId { get; set; }
        public string ProcedureCode { get; set; }
        public string ProcedureName { get; set; }
        public string ProcedureCategory { get; set; }   // Surgical / Diagnostic / Therapeutic / Minor / Emergency
        public int? DepartmentId { get; set; }
        public string Priority { get; set; }            // Routine / Urgent / Emergency
        public string Status { get; set; }
        public DateTime? PlannedDate { get; set; }
        public DateTime? ActualDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationMinutes { get; set; }
        public bool IsActive { get; set; } = true;
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // joined / display-only
        public string PrimaryDoctorName { get; set; }
        public int? PrimaryDoctorId { get; set; }
    }

    public class ProcedureDetails
    {
        public int DetailId { get; set; }
        public int ProcedureId { get; set; }
        public string ReasonForProcedure { get; set; }
        public string ClinicalIndication { get; set; }
        public string PreProcedureDiagnosis { get; set; }
        public string PostProcedureDiagnosis { get; set; }
        public string ProcedureDescription { get; set; }
        public string ProcedureObjective { get; set; }
        public string ProcedureFindings { get; set; }
        public string FinalOutcome { get; set; }
    }

    public class ProcedureTeamMember
    {
        public int TeamMemberId { get; set; }
        public int ProcedureId { get; set; }
        public int StaffId { get; set; }
        public string RoleInTeam { get; set; }   // Primary / Assistant / Anaesthetist / ScrubNurse / CirculatingNurse / Technician
        public bool IsActive { get; set; } = true;
        public string StaffName { get; set; }
    }

    public class ProcedureAnaesthesia
    {
        public int AnaesthesiaId { get; set; }
        public int ProcedureId { get; set; }
        public string AnaesthesiaType { get; set; }
        public int? AnaesthetistId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string PreAssessmentNotes { get; set; }
        public string Complications { get; set; }
        public string Remarks { get; set; }
    }

    public class ProcedureChecklistItem
    {
        public int ChecklistItemId { get; set; }
        public int ProcedureId { get; set; }
        public string ItemText { get; set; }
        public bool IsMandatory { get; set; }
        public bool IsChecked { get; set; }
        public int? CheckedBy { get; set; }
        public DateTime? CheckedDate { get; set; }
    }

    public class ProcedureTimelineEvent
    {
        public int TimelineId { get; set; }
        public int ProcedureId { get; set; }
        public string EventType { get; set; }  // PatientArrival / RoomEntry / AnaesthesiaStart / ProcedureStart / ProcedureEnd / RecoveryStart
        public DateTime EventTime { get; set; }
        public int? RecordedBy { get; set; }
    }

    public class ProcedureNoteVersion
    {
        public int NoteId { get; set; }
        public int ProcedureId { get; set; }
        public int? TemplateId { get; set; }
        public string ProcedureStepsPerformed { get; set; }
        public string KeyObservations { get; set; }
        public string InstrumentsUsed { get; set; }
        public int? BloodLossMl { get; set; }
        public string UnexpectedEvents { get; set; }
        public string FinalPatientCondition { get; set; }
        public int Version { get; set; }
        public bool IsLatest { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class ProcedureComplication
    {
        public int ComplicationId { get; set; }
        public int ProcedureId { get; set; }
        public bool Occurred { get; set; }
        public string ComplicationType { get; set; }
        public string Description { get; set; }
        public string ActionTaken { get; set; }
        public string Outcome { get; set; }
        public int? RecordedBy { get; set; }
        public DateTime? RecordedDate { get; set; }
    }

    public class ProcedureSpecimen
    {
        public int SpecimenId { get; set; }
        public int ProcedureId { get; set; }
        public bool Collected { get; set; }
        public string SpecimenType { get; set; }
        public string Description { get; set; }
        public DateTime? CollectionTime { get; set; }
        public bool SentToLab { get; set; }
        public string Remarks { get; set; }
    }

    public class ProcedurePostCare
    {
        public int PostCareId { get; set; }
        public int ProcedureId { get; set; }
        public string PatientConditionAfter { get; set; }
        public string RecoveryStatus { get; set; }
        public string ImmediateInstructions { get; set; }
        public string MonitoringInstructions { get; set; }
        public string DoctorRemarks { get; set; }
        public string FollowUpInstructions { get; set; }
        public DateTime? FollowUpDate { get; set; }
    }

    public class ProcedureOutcome
    {
        public int OutcomeId { get; set; }
        public int ProcedureId { get; set; }
        public string Outcome { get; set; }   // Successful / Partial / Failed / StoppedDueToComplication
        public string FinalRemarks { get; set; }
        public string DoctorConclusion { get; set; }
        public int? ApprovedByDoctorId { get; set; }
        public string DigitalSignatureRef { get; set; }
        public DateTime? VerificationDate { get; set; }
        public int? VerifiedBy { get; set; }
    }

    public class ProcedureAttachment
    {
        public int AttachmentId { get; set; }
        public int ProcedureId { get; set; }
        public string FileType { get; set; }
        public string FilePath { get; set; }
        public DateTime? UploadDate { get; set; }
        public int? UploadedBy { get; set; }
        public string Description { get; set; }
    }

    public class ProcedureStatusHistory
    {
        public int HistoryId { get; set; }
        public int ProcedureId { get; set; }
        public string FromStatus { get; set; }
        public string ToStatus { get; set; }
        public string Reason { get; set; }
        public int? ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
    }

    // ── TEMPLATES ─────────────────────────────────────────────────────
    public class ProcedureTemplate
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string Category { get; set; }   // Surgery / Diagnostic / Therapeutic / Minor Procedure
        public int? DepartmentId { get; set; }
        public bool IsActive { get; set; } = true;
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int UsageCount { get; set; }
        public List<ProcedureTemplateStep> Steps { get; set; } = new List<ProcedureTemplateStep>();
    }

    public class ProcedureTemplateStep
    {
        public int StepId { get; set; }
        public int TemplateId { get; set; }
        public int StepOrder { get; set; }
        public string StepText { get; set; }
    }

    // ── COMPOSITE VIEW MODELS ─────────────────────────────────────────

    /// <summary>Full detail bundle used by the Procedure detail screen (one round trip).</summary>
    public class ProcedureFullDetailVM
    {
        public ProcedureMaster Master { get; set; }
        public ProcedureDetails Details { get; set; }
        public List<ProcedureTeamMember> Team { get; set; } = new List<ProcedureTeamMember>();
        public ProcedureAnaesthesia Anaesthesia { get; set; }
        public List<ProcedureChecklistItem> Checklist { get; set; } = new List<ProcedureChecklistItem>();
        public List<ProcedureTimelineEvent> Timeline { get; set; } = new List<ProcedureTimelineEvent>();
        public ProcedureNoteVersion LatestNote { get; set; }
        public List<ProcedureComplication> Complications { get; set; } = new List<ProcedureComplication>();
        public List<ProcedureSpecimen> Specimens { get; set; } = new List<ProcedureSpecimen>();
        public ProcedurePostCare PostCare { get; set; }
        public ProcedureOutcome Outcome { get; set; }
        public List<ProcedureAttachment> Attachments { get; set; } = new List<ProcedureAttachment>();
        public List<ProcedureStatusHistory> StatusHistory { get; set; } = new List<ProcedureStatusHistory>();
    }

    /// <summary>Payload for the "New Procedure" request screen.</summary>
    public class ProcedureRequestVM
    {
        public int PatientId { get; set; }
        public string VisitContextType { get; set; }
        public int VisitContextId { get; set; }
        public string ProcedureCode { get; set; }
        public string ProcedureName { get; set; }
        public string ProcedureCategory { get; set; }
        public int? DepartmentId { get; set; }
        public string Priority { get; set; }
        public string ReasonForProcedure { get; set; }
        public string ClinicalIndication { get; set; }
        public int CreatedBy { get; set; }
    }

    /// <summary>Result of an operation that can fail a business rule (checklist incomplete, etc.)</summary>
    public class ProcedureOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? PendingCount { get; set; }
    }

    /// <summary>Filter payload for the procedure list / search screen.</summary>
    public class ProcedureSearchFilter
    {
        public int? PatientId { get; set; }
        public int? DoctorId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public int? DepartmentId { get; set; }
    }
}
