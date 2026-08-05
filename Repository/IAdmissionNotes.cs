using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IAdmissionNotes
    {
        // Current admission cha note (Current Complaint + Examination) fetch
        Task<AdmissionNotesModel> GetAdmissionNotesAsync(int ipdId);

        // Templates list - type wise + search
        Task<List<AdmissionNoteTemplateModel>> GetTemplatesAsync(string templateType, string search, int? hospitalId);

        // Save / Update admission notes (upsert)
        Task SaveAdmissionNotesAsync(SaveNotesRequest request, int createdBy);

        // Load Previous - same patient cha last admission cha data
        Task<AdmissionNotesModel> LoadPreviousAsync(LoadPreviousRequest request);

        // Clear - COMPLAINT / EXAMINATION / BOTH
        Task ClearNotesAsync(ClearNotesRequest request);

        // Template Add / Update / Delete
        Task SaveTemplateAsync(TemplateSaveRequest request, int? hospitalId, int createdBy);

        // ── Structured Chief Complaints ─────────────────────────
        Task<List<ChiefComplaintModel>> GetChiefComplaintsAsync(int ipdId);
        Task SaveChiefComplaintAsync(SaveChiefComplaintRequest request, int createdBy, string ipAddress = null, string deviceInfo = null);
        Task<List<MasterComplaintModel>> SearchMasterComplaintsAsync(string search, int? hospitalId);
        Task<int?> GetComplaintOwnerIpdIdAsync(int complaintId);

        // ── Allergies ─────────────────────────────────────────
        Task<List<PatientAllergyModel>> GetAllergiesAsync(int patientId, int ipdId);
        Task SaveAllergyAsync(SaveAllergyRequest request, int recordedBy, string ipAddress = null, string deviceInfo = null);
        Task<int?> GetAllergyOwnerIpdIdAsync(int allergyId);

        // ── Medical History ───────────────────────────────────
        Task<List<PatientMedicalHistoryModel>> GetMedicalHistoryAsync(int patientId);
        Task SaveMedicalHistoryAsync(SaveMedicalHistoryRequest request, int recordedBy, string ipAddress = null, string deviceInfo = null);
        Task<int?> GetHistoryOwnerPatientIdAsync(int historyId);

        // ── Remarks ───────────────────────────────────────────
        Task<AdmissionRemarksModel> GetRemarksAsync(int ipdId);
        Task SaveRemarksAsync(SaveRemarksRequest request, int createdBy);
        Task ClearRemarksAsync(int ipdId);
        Task<List<RemarksTemplateModel>> GetRemarksTemplatesAsync(string search, int? hospitalId);
        Task SaveRemarksTemplateAsync(RemarksTemplateSaveRequest request, int? hospitalId, int createdBy);

        // ── Structured HPI ─────────────────────────────────────
        Task<HpiModel> GetHpiAsync(int ipdId);
        Task SaveHpiAsync(SaveHpiRequest request, int createdBy, string ipAddress = null, string deviceInfo = null);

        // ── Professional Examination Module ────────────────────
        Task<ExaminationDetailModel> GetExaminationDetailAsync(int ipdId);
        Task SaveExaminationDetailAsync(SaveExaminationDetailRequest request, int createdBy, string ipAddress = null, string deviceInfo = null);

        // ── One-Click Clinical Findings ─────────────────────────
        Task<List<ClinicalFindingModel>> GetClinicalFindingsAsync(string fieldName, int? hospitalId);
        Task SaveClinicalFindingAsync(SaveClinicalFindingRequest request, int? hospitalId, int createdBy);

        // ── Department-Based Clinical Templates ─────────────────
        Task<List<DepartmentTemplateModel>> GetDepartmentTemplatesAsync(int createdBy, int? hospitalId);
        Task SaveDepartmentTemplateAsync(SaveDepartmentTemplateRequest request, int? hospitalId, int createdBy);

        // ── Previous Admission Comparison (read-only) ───────────
        Task<PreviousAdmissionSummaryModel> GetPreviousAdmissionSummaryAsync(int patientId, int currentIpdId);

        // ── Audit Trail ──────────────────────────────────────────
        Task<List<AdmissionAuditLogModel>> GetAuditLogAsync(int ipdId);
    }
}

