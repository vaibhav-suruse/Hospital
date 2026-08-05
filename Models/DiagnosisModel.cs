using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    // ============================================================================
    // EXISTING CLASS — all original members preserved exactly as-is.
    // Only NEW nullable/optional properties were appended at the bottom so that
    // every existing usage (object initializers, positional reads, etc.) keeps
    // compiling and behaving exactly the same.
    // ============================================================================
    public class DiagnosisModel
    {
        public int DiagnosisId { get; set; }

        public int IpdId { get; set; }

        public int PatientId { get; set; }

        public string DiagnosisName { get; set; }

        public string DiagnosisType { get; set; }

        public string RecordedBy { get; set; }

        public DateTime DiagnosisDate { get; set; }

        // ── NEW (Phase 1 enterprise fields) — all optional, all additive ──────
        public string DiagnosisSubType { get; set; }      // Provisional/Working/Differential/Principal/Secondary/Comorbidity/Complication/HospitalAcquired/Chronic/Acute/PreExisting/RuleOut/Suspected/Confirmed/Operative/PostOperative/CauseOfDeath/Autopsy/Recurring/FollowUp
        public string IcdCode { get; set; }
        public string IcdCodeSystem { get; set; } = "ICD-10";
        public int? DiagnosisMasterId { get; set; }
        public string Severity { get; set; }              // Mild/Moderate/Severe/Critical
        public bool IsPrincipal { get; set; }
        public bool? PresentOnAdmission { get; set; }
        public bool HospitalAcquired { get; set; }
        public string Status { get; set; } = "Active";    // Active/Verified/Approved/Resolved/RuledOut
        public string ClinicalNotes { get; set; }

        public int? CreatedByUserId { get; set; }
        public int? DoctorId { get; set; }          // NEW: the diagnosing doctor (FK to doctor.Doctor_Id)
        public string EnteredByName { get; set; }         // who was logged in when the record was created (audit) — NOT the diagnosing doctor

        public int? VerifiedByUserId { get; set; }
        public string VerifiedByName { get; set; }
        public DateTime? VerifiedDate { get; set; }

        public int? ApprovedByUserId { get; set; }
        public string ApprovedByName { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public int? HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string DeletedReason { get; set; }

        // ── NEW (Tier 1) — ICD-11 / SNOMED CT ──────────────────────────────
        public string Icd11Code { get; set; }
        public string SnomedCode { get; set; }

        // ── NEW (Tier 2) — resolution tracking ─────────────────────────────
        public DateTime? ResolvedDate { get; set; }

        // ── NEW (Tier 4) — additional clinical fields ──────────────────────
        public string Stage { get; set; }
        public string Grade { get; set; }
        public string Laterality { get; set; }
        public string BodySite { get; set; }
        public string Prognosis { get; set; }
        public string TreatmentPlan { get; set; }
        public string ExpectedOutcome { get; set; }
        public string SignatureData { get; set; }
        public int? SignedByUserId { get; set; }
        public DateTime? SignedDate { get; set; }

        // ── NEW (Tier 3) — freeze/unlock ────────────────────────────────────
        public bool IsFrozen { get; set; }
        public int? FrozenByUserId { get; set; }
        public DateTime? FrozenDate { get; set; }

        // ── NEW (Tier 7) — billing/insurance coding flags ───────────────────
        public bool InsuranceRelevant { get; set; }
        public bool CodingCompleted { get; set; }
        public bool ClaimSubmitted { get; set; }
    }

    // ============================================================================
    // EXISTING CLASS — unchanged.
    // ============================================================================
    public class DiagnosisPageVM
    {
        public int IpdId { get; set; }

        public int PatientId { get; set; }

        public List<DiagnosisModel> AdmissionDiagnosis { get; set; }
            = new List<DiagnosisModel>();

        public List<DiagnosisModel> DischargeDiagnosis { get; set; }
            = new List<DiagnosisModel>();

        // ── NEW: curated reference lists the view uses to render dropdowns ────
        public List<string> DiagnosisSubTypes { get; set; } = DiagnosisReference.SubTypes;
        public List<string> SeverityLevels { get; set; } = DiagnosisReference.SeverityLevels;
        public string CurrentUserRole { get; set; }

        // ── NEW (Tier 3) — action names this session's role is allowed to
        // perform on THIS module only (Create/Update/Delete/Approve/Reject/
        // Verify/Freeze/Unlock/Print/Export). Drives button visibility; the
        // controller still re-checks server-side on every action.
        public HashSet<string> AllowedActions { get; set; } = new HashSet<string>();

        // ── NEW (Tier 2) — "missing ICD before discharge" clinical alert,
        // surfaced in the UI instead of only being a save-time rejection.
        public bool HasMissingIcdAlert { get; set; }
        public List<string> ClinicalAlerts { get; set; } = new List<string>();

        // ── NEW (Tier 6) — this user's quick-add favourites
        public List<DiagnosisFavorite> Favorites { get; set; } = new List<DiagnosisFavorite>();
    }

    // ============================================================================
    // NEW — static reference lists (Section 2 of the target spec). Kept as plain
    // C# constants rather than a DB-driven master table for Phase 1, so this can
    // ship with zero schema risk; can be promoted to a `diagnosis_type_master`
    // lookup table in a later phase without touching this contract.
    // ============================================================================
    public static class DiagnosisReference
    {
        public static readonly List<string> SubTypes = new List<string>
        {
            "Provisional", "Working", "Differential", "Principal", "Secondary",
            "Comorbidity", "Complication", "HospitalAcquired", "Chronic", "Acute",
            "PreExisting", "RuleOut", "Suspected", "Confirmed", "Operative",
            "PostOperative", "Final", "CauseOfDeath", "Autopsy", "Recurring", "FollowUp"
        };

        public static readonly List<string> SeverityLevels = new List<string>
        {
            "Mild", "Moderate", "Severe", "Critical"
        };

        // Roles permitted to Verify / Approve / delete-with-reason a diagnosis.
        // Reception and other non-clinical roles are excluded by design.
        public static readonly HashSet<string> ClinicalApproverRoles = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "Doctor", "Consultant", "HOD", "Admin", "SuperAdmin"
        };

        // NEW (Tier 7) — roles allowed to update billing/insurance coding
        // flags. Separate from the diagnosis_role_permission matrix's
        // generic "Update" action (which intentionally does NOT grant
        // Billing/Insurance clinical edit rights) — this is scoped only to
        // the three billing flags, nothing clinical.
        public static readonly HashSet<string> BillingCodingRoles = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "Billing", "Insurance", "MedicalRecords", "Admin", "SuperAdmin"
        };
    }

    // ============================================================================
    // NEW — request DTO for the enterprise Add/Save action.
    // ============================================================================
    public class SaveDiagnosisRequest
    {
        public int IpdId { get; set; }
        public int PatientId { get; set; }
        public string DiagnosisName { get; set; }
        public string DiagnosisType { get; set; }       // Admission / Discharge (unchanged enum)
        public string DiagnosisSubType { get; set; }
        public string IcdCode { get; set; }
        public int? DiagnosisMasterId { get; set; }
        public string Severity { get; set; }
        public bool IsPrincipal { get; set; }
        public bool? PresentOnAdmission { get; set; }
        public bool HospitalAcquired { get; set; }
        public string ClinicalNotes { get; set; }
        public int? DoctorId { get; set; }          // NEW: required — the diagnosing doctor
    }

    // ============================================================================
    // NEW — request DTO for Verify / Approve / Delete-with-reason actions.
    // ============================================================================
    public class DiagnosisActionRequest
    {
        public int DiagnosisId { get; set; }
        public int IpdId { get; set; }
        public int PatientId { get; set; }
        public string Reason { get; set; }              // used for delete
    }

    // ============================================================================
    // NEW — audit trail row for the "View History" modal.
    // ============================================================================
    public class DiagnosisAuditModel
    {
        public int AuditId { get; set; }
        public int DiagnosisId { get; set; }
        public string Action { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Reason { get; set; }
        public DateTime ActionDate { get; set; }
        public int? ActionByUserId { get; set; }
        public string ActionByName { get; set; }
        public string DiagnosisName { get; set; }     // NEW: for the History modal header
        public string DiagnosedByName { get; set; }    // NEW: the diagnosing doctor (RecordedBy on the diagnosis row)
    }

    // ============================================================================
    // NEW — generic result wrapper so AJAX calls get a real success flag +
    // message instead of relying on HTTP status alone (existing action methods
    // still just redirect, this is only used by the new enterprise endpoints).
    // ============================================================================
    public class DiagnosisOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int DiagnosisId { get; set; }
    }

    // ============================================================================
    // NEW (Phase 2) — reporting filter + result row DTOs. All read-only.
    // ============================================================================
    public class DiagnosisReportFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class DoctorWiseReportRow
    {
        public int? DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string Specialization { get; set; }
        public int DiagnosisCount { get; set; }
        public int PrincipalCount { get; set; }
    }

    public class IcdDistributionRow
    {
        public string IcdCode { get; set; }
        public string DiagnosisName { get; set; }
        public int DiagnosisCount { get; set; }
    }

    public class StatusSummaryRow
    {
        public string Status { get; set; }
        public int DiagnosisCount { get; set; }
    }

    public class SeverityDistributionRow
    {
        public string Severity { get; set; }
        public int DiagnosisCount { get; set; }
    }

    public class SubTypeDistributionRow
    {
        public string DiagnosisSubType { get; set; }
        public int DiagnosisCount { get; set; }
    }

    public class HacReportRow
    {
        public int DiagnosisId { get; set; }
        public int IpdId { get; set; }
        public int PatientId { get; set; }
        public string DiagnosisName { get; set; }
        public string IcdCode { get; set; }
        public string Severity { get; set; }
        public string DoctorName { get; set; }
        public DateTime DiagnosisDate { get; set; }
        public string Status { get; set; }
    }

    public class DiagnosisReportBundle
    {
        public List<DoctorWiseReportRow> DoctorWise { get; set; } = new List<DoctorWiseReportRow>();
        public List<IcdDistributionRow> IcdDistribution { get; set; } = new List<IcdDistributionRow>();
        public List<StatusSummaryRow> StatusSummary { get; set; } = new List<StatusSummaryRow>();
        public List<SeverityDistributionRow> SeverityDistribution { get; set; } = new List<SeverityDistributionRow>();
        public List<SubTypeDistributionRow> SubTypeDistribution { get; set; } = new List<SubTypeDistributionRow>();
        public List<HacReportRow> HospitalAcquiredConditions { get; set; } = new List<HacReportRow>();
    }

    // ============================================================================
    // NEW (Phase 3) — DB-driven diagnosis type master with per-type business rules.
    // ============================================================================
    public class DiagnosisTypeMaster
    {
        public int Id { get; set; }
        public string TypeName { get; set; }
        public string Description { get; set; }
        public bool RequiresIcdCode { get; set; }
        public bool RequiresApprovalBeforeDischarge { get; set; }
        public int SortOrder { get; set; }
    }

    // ============================================================================
    // NEW (Tier 1) — extends the enterprise save request with ICD-11/SNOMED and
    // the Tier 4 clinical detail fields. Kept as a SEPARATE class rather than
    // adding to SaveDiagnosisRequest so any existing caller of that DTO keeps
    // compiling unchanged; the controller's AddDiagnosisEnterprise action now
    // accepts this richer DTO instead (model-binds the same posted fields plus
    // the new ones — old form posts that don't include the new fields still
    // bind fine since everything new here is optional).
    // ============================================================================
    public class SaveDiagnosisRequestV2 : SaveDiagnosisRequest
    {
        public string Icd11Code { get; set; }
        public string SnomedCode { get; set; }
        public string Stage { get; set; }
        public string Grade { get; set; }
        public string Laterality { get; set; }
        public string BodySite { get; set; }
        public string Prognosis { get; set; }
        public string TreatmentPlan { get; set; }
        public string ExpectedOutcome { get; set; }
        public bool InsuranceRelevant { get; set; }
        public DateTime? DiagnosisDate { get; set; }   // optional backdate; blocked server-side if future
    }

    // ============================================================================
    // NEW (Tier 2) — resolve action (requires a resolved date)
    // ============================================================================
    public class ResolveDiagnosisRequest
    {
        public int DiagnosisId { get; set; }
        public DateTime ResolvedDate { get; set; }
        public string Reason { get; set; }
    }

    // ============================================================================
    // NEW (Tier 4) — linked clinical evidence
    // ============================================================================
    public class DiagnosisEvidenceRequest
    {
        public int DiagnosisId { get; set; }
        public string EvidenceType { get; set; }   // Lab/Radiology/Procedure/Surgery/Other
        public int? ReferenceId { get; set; }
        public string ReferenceLabel { get; set; }
        public string Notes { get; set; }
    }

    public class DiagnosisEvidenceModel
    {
        public int EvidenceId { get; set; }
        public int DiagnosisId { get; set; }
        public string EvidenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string ReferenceLabel { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // ============================================================================
    // NEW (Tier 6) — favourites / quick-add templates
    // ============================================================================
    public class DiagnosisFavorite
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string DiagnosisName { get; set; }
        public string IcdCode { get; set; }
        public int? DiagnosisMasterId { get; set; }
        public string DiagnosisSubType { get; set; }
        public int UsageCount { get; set; }
    }

    public class AddFavoriteRequest
    {
        public string DiagnosisName { get; set; }
        public string IcdCode { get; set; }
        public int? DiagnosisMasterId { get; set; }
        public string DiagnosisSubType { get; set; }
    }

    // ============================================================================
    // NEW (Tier 6) — bulk actions + pagination/filters
    // ============================================================================
    public class BulkDiagnosisActionRequest
    {
        public List<int> DiagnosisIds { get; set; } = new List<int>();
        public string Reason { get; set; }
    }

    public class DiagnosisFilterRequest
    {
        public int IpdId { get; set; }
        public string DiagnosisType { get; set; }
        public string Status { get; set; }
        public string Severity { get; set; }
        public string DiagnosisSubType { get; set; }
        public string SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PagedDiagnosisResult
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<DiagnosisModel> Items { get; set; } = new List<DiagnosisModel>();
    }

    // ============================================================================
    // NEW (Tier 7) — billing / insurance coding flags
    // ============================================================================
    public class DiagnosisBillingFlagsRequest
    {
        public int DiagnosisId { get; set; }
        public bool InsuranceRelevant { get; set; }
        public bool CodingCompleted { get; set; }
        public bool ClaimSubmitted { get; set; }
    }

    // ============================================================================
    // NEW (Tier 5) — additional reports (mortality/morbidity/readmission/
    // notifiable disease). Kept separate from DiagnosisReportBundle (which
    // stays as-is) so the existing Reports() view/action are untouched.
    // ============================================================================
    public class MortalityReportRow
    {
        public int DiagnosisId { get; set; }
        public int IpdId { get; set; }
        public int PatientId { get; set; }
        public string DiagnosisName { get; set; }
        public string IcdCode { get; set; }
        public string DiagnosisSubType { get; set; }
        public DateTime DiagnosisDate { get; set; }
        public string DoctorName { get; set; }
        public string Status { get; set; }
    }

    public class MorbidityReportRow
    {
        public string DiagnosisName { get; set; }
        public string IcdCode { get; set; }
        public int CaseCount { get; set; }
        public int PrincipalCount { get; set; }
    }

    public class ReadmissionReportRow
    {
        public int PatientId { get; set; }
        public int PreviousIpdId { get; set; }
        public DateTime? PreviousDischargeDate { get; set; }
        public int ReadmissionIpdId { get; set; }
        public DateTime ReadmissionDate { get; set; }
        public int DaysBetween { get; set; }
        public string PreviousDiagnosis { get; set; }
        public string ReadmissionDiagnosis { get; set; }
    }

    public class NotifiableDiseaseReportRow
    {
        public int DiagnosisId { get; set; }
        public int IpdId { get; set; }
        public int PatientId { get; set; }
        public string DiagnosisName { get; set; }
        public string IcdCode { get; set; }
        public DateTime DiagnosisDate { get; set; }
        public string DoctorName { get; set; }
        public string Status { get; set; }
    }

    // ============================================================================
    // NEW (Tier 3) — one row of the role/action permission matrix
    // ============================================================================
    public class DiagnosisRolePermission
    {
        public string Role { get; set; }
        public string ActionName { get; set; }
        public bool IsAllowed { get; set; }
    }
}
