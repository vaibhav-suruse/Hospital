using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IDiagnosis
    {
        // ── EXISTING — signatures unchanged, still backed by the original SPs ──
        Task<int> InsertDiagnosis(DiagnosisModel model);

        Task<List<DiagnosisModel>> GetAdmissionDiagnosis(int ipdId);

        Task<List<DiagnosisModel>> GetDischargeDiagnosis(int ipdId);

        Task<int> DeleteDiagnosis(int diagnosisId);

        // ── NEW (Phase 1 enterprise capability) — additive only ────────────────
        Task<DiagnosisOperationResult> InsertDiagnosisEnterprise(DiagnosisModel model);

        Task<List<DiagnosisModel>> GetDiagnosisByIpdEnterprise(int ipdId, string diagnosisType);

        Task<DiagnosisOperationResult> VerifyDiagnosis(int diagnosisId, int verifiedByUserId, string reason);

        Task<DiagnosisOperationResult> ApproveDiagnosis(int diagnosisId, int approvedByUserId, string reason);

        Task<DiagnosisOperationResult> DeleteDiagnosisEnterprise(int diagnosisId, int deletedByUserId, string reason);

        Task<List<DiagnosisAuditModel>> GetDiagnosisAuditTrail(int diagnosisId);

        // ── NEW (Phase 2) — read-only reports ───────────────────────────────────
        Task<DiagnosisReportBundle> GetDiagnosisReportBundle(int? hospitalId, int? subHospitalId, DateTime? fromDate, DateTime? toDate);

        // ── NEW (Phase 3) — DB-driven diagnosis type master ─────────────────────
        Task<List<DiagnosisTypeMaster>> GetDiagnosisTypes();

        // ── NEW (Tier 1/2/4) — richer insert with duplicate/future-date checks ──
        Task<DiagnosisOperationResult> InsertDiagnosisEnterpriseV2(DiagnosisModel model);

        // ── NEW (Tier 2) — resolve (requires resolved date) ──────────────────────
        Task<DiagnosisOperationResult> ResolveDiagnosis(int diagnosisId, DateTime resolvedDate, int userId, string reason);

        // ── NEW (Tier 3) — reject / freeze / unlock ───────────────────────────────
        Task<DiagnosisOperationResult> RejectDiagnosis(int diagnosisId, int userId, string reason);
        Task<DiagnosisOperationResult> FreezeDiagnosis(int diagnosisId, int userId);
        Task<DiagnosisOperationResult> UnlockDiagnosis(int diagnosisId, int userId);

        // ── NEW (Tier 3) — role permission matrix (diagnosis-scoped only) ────────
        Task<HashSet<string>> GetAllowedActionsForRole(string role);

        // ── NEW (Tier 4) — linked clinical evidence ───────────────────────────────
        Task<DiagnosisOperationResult> AddDiagnosisEvidence(DiagnosisEvidenceRequest request, int userId);
        Task<List<DiagnosisEvidenceModel>> GetDiagnosisEvidence(int diagnosisId);

        // ── NEW (Tier 2) — missing-ICD-before-discharge alert list for an IPD ─────
        Task<List<DiagnosisModel>> GetMissingIcdAlerts(int ipdId);

        // ── NEW (Tier 5) — mortality / morbidity / readmission / notifiable ───────
        Task<List<MortalityReportRow>> GetMortalityReport(int? hospitalId, int? subHospitalId, DateTime? fromDate, DateTime? toDate);
        Task<List<MorbidityReportRow>> GetMorbidityReport(int? hospitalId, int? subHospitalId, DateTime? fromDate, DateTime? toDate);
        Task<List<ReadmissionReportRow>> GetReadmissionReport(int? hospitalId, int? subHospitalId, DateTime? fromDate, DateTime? toDate, int windowDays);
        Task<List<NotifiableDiseaseReportRow>> GetNotifiableDiseaseReport(int? hospitalId, int? subHospitalId, DateTime? fromDate, DateTime? toDate);

        // ── NEW (Tier 6) — pagination/filters, favourites, bulk actions ───────────
        Task<PagedDiagnosisResult> GetDiagnosisPaged(DiagnosisFilterRequest filter);
        Task<DiagnosisOperationResult> AddFavorite(int userId, int? hospitalId, AddFavoriteRequest request);
        Task<List<DiagnosisFavorite>> GetFavorites(int userId);
        Task<DiagnosisOperationResult> RemoveFavorite(int id, int userId);
        Task<DiagnosisOperationResult> BulkVerify(List<int> diagnosisIds, int userId, string reason);
        Task<DiagnosisOperationResult> BulkApprove(List<int> diagnosisIds, int userId, string reason);

        // ── NEW (multi-hospital data isolation) — look up which IPD a diagnosis
        //    belongs to, so ID-only endpoints (Verify/Approve/Resolve/Evidence/
        //    Audit/etc.) can be checked against the current session's hospital
        //    before reading or mutating anything.
        Task<int?> GetDiagnosisOwnerIpdIdAsync(int diagnosisId);

        // ── NEW (Tier 7) — billing / insurance coding flags ────────────────────────
        Task<DiagnosisOperationResult> UpdateBillingFlags(DiagnosisBillingFlagsRequest request, int userId);
    }
}
