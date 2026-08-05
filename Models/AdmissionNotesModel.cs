using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{

    public class AdmissionNotesViewModel
    {
        public int IpdId { get; set; }
        public int PatientId { get; set; }

        // Current Complaint + Examination
        public string CurrentComplaint { get; set; }
        public string Examination { get; set; }
        public List<AdmissionNoteTemplateModel> ComplaintTemplates { get; set; } = new List<AdmissionNoteTemplateModel>();
        public List<AdmissionNoteTemplateModel> ExaminationTemplates { get; set; } = new List<AdmissionNoteTemplateModel>();

        // Structured Chief Complaints (new)
        public List<ChiefComplaintModel> ChiefComplaints { get; set; } = new List<ChiefComplaintModel>();

        // Allergies
        public List<PatientAllergyModel> Allergies { get; set; } = new List<PatientAllergyModel>();

        // Medical History
        public List<PatientMedicalHistoryModel> MedicalHistory { get; set; } = new List<PatientMedicalHistoryModel>();

        // Remarks
        public string Remarks { get; set; }
        public List<RemarksTemplateModel> RemarksTemplates { get; set; } = new List<RemarksTemplateModel>();

        // Structured HPI (new)
        public HpiModel Hpi { get; set; } = new HpiModel();

        // Professional Examination (new, structured)
        public ExaminationDetailModel ExaminationDetail { get; set; } = new ExaminationDetailModel();

        // Department Templates (new)
        public List<DepartmentTemplateModel> DepartmentTemplates { get; set; } = new List<DepartmentTemplateModel>();

        // Previous Admission Comparison (new)
        public PreviousAdmissionSummaryModel PreviousAdmission { get; set; }
    }


    public class AdmissionNoteTemplateModel
    {
        public int TemplateId { get; set; }
        public string TemplateType { get; set; }   // "COMPLAINT" or "EXAMINATION"
        public string TemplateName { get; set; }
        public string TemplateText { get; set; }
    }

    // ─── Structured Chief Complaints ────────────────────────────
    public class ChiefComplaintModel
    {
        public int ComplaintId { get; set; }
        public int IpdId { get; set; }
        public int PatientId { get; set; }
        public string ComplaintName { get; set; }
        public int? DurationValue { get; set; }
        public string DurationUnit { get; set; }   // Minutes / Hours / Days / Weeks / Months / Years
        public string Severity { get; set; }       // Mild / Moderate / Severe
        public string Onset { get; set; }          // Sudden / Gradual
        public string Frequency { get; set; }
        public string BodyLocation { get; set; }
        public int? PainScore { get; set; }         // 0-10
        public string Remarks { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class SaveChiefComplaintRequest
    {
        public string Action { get; set; }   // ADD / UPDATE / DELETE
        public int ComplaintId { get; set; }
        public int IpdId { get; set; }
        public int PatientId { get; set; }
        public string ComplaintName { get; set; }
        public int? DurationValue { get; set; }
        public string DurationUnit { get; set; }
        public string Severity { get; set; }
        public string Onset { get; set; }
        public string Frequency { get; set; }
        public string BodyLocation { get; set; }
        public int? PainScore { get; set; }
        public string Remarks { get; set; }
    }

    public class MasterComplaintModel
    {
        public int ComplaintMasterId { get; set; }
        public string ComplaintName { get; set; }
        public string Category { get; set; }
    }

    public class AdmissionNotesModel
    {
        public int AdmissionNoteId { get; set; }
        public int IpdId { get; set; }
        public int PatientId { get; set; }
        public string CurrentComplaint { get; set; }
        public string Examination { get; set; }
    }

    // Page load la controller -> view la pass karaycha combined model


    // Save Notes (Current Complaint / Examination) AJAX request
    public class SaveNotesRequest
    {
        public int IpdId { get; set; }
        public int PatientId { get; set; }
        public string CurrentComplaint { get; set; }
        public string Examination { get; set; }
    }

    // Clear field AJAX request
    public class ClearNotesRequest
    {
        public int IpdId { get; set; }
        public string Field { get; set; } // COMPLAINT / EXAMINATION / BOTH
    }

    // Load previous AJAX request
    public class LoadPreviousRequest
    {
        public int PatientId { get; set; }
        public int IpdId { get; set; }
    }

    // Add / Update / Delete Template AJAX request
    public class TemplateSaveRequest
    {
        public string Action { get; set; }       // ADD, UPDATE, DELETE
        public int TemplateId { get; set; }
        public string TemplateType { get; set; } // COMPLAINT / EXAMINATION
        public string TemplateName { get; set; }
        public string TemplateText { get; set; }
    }
    public class PatientAllergyModel
    {
        public int AllergyId { get; set; }
        public int PatientId { get; set; }
        public int IpdId { get; set; }
        public string AllergyType { get; set; }  // Drug / Food / Dust / Latex / Contrast / Other
        public string AllergyName { get; set; }
        public string Reaction { get; set; }
        public string Severity { get; set; }  // Mild / Moderate / Severe
        public DateTime? DateIdentified { get; set; }
        public int? VerifiedBy { get; set; }
        public string ClinicalStatus { get; set; }   // Active / Resolved / Unconfirmed
        public string Remarks { get; set; }
        public DateTime RecordedDate { get; set; }
        public bool IsCritical => string.Equals(Severity, "Severe", StringComparison.OrdinalIgnoreCase);
    }

    public class SaveAllergyRequest
    {
        public string Action { get; set; }  // ADD / UPDATE / DELETE
        public int AllergyId { get; set; }
        public int PatientId { get; set; }
        public int IpdId { get; set; }
        public string AllergyType { get; set; }
        public string AllergyName { get; set; }
        public string Reaction { get; set; }
        public string Severity { get; set; }
        public DateTime? DateIdentified { get; set; }
        public int? VerifiedBy { get; set; }
        public string ClinicalStatus { get; set; }
        public string Remarks { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
    }

    // ─── Medical History ───────────────────────────────────────
    public class PatientMedicalHistoryModel
    {
        public int HistoryId { get; set; }
        public int PatientId { get; set; }
        public string HistoryType { get; set; }  // Diabetes, Hypertension, Surgery, etc.
        public string Description { get; set; }
        public string Medication { get; set; }
        public int? SinceYear { get; set; }
        public string Status { get; set; }  // Active / Resolved / Unknown
        public string ControlStatus { get; set; }   // Controlled / Uncontrolled
        public DateTime? LastFollowupDate { get; set; }
        public string Remarks { get; set; }
        public DateTime RecordedDate { get; set; }
    }

    public class SaveMedicalHistoryRequest
    {
        public string Action { get; set; }  // ADD / UPDATE / DELETE
        public int HistoryId { get; set; }
        public int PatientId { get; set; }
        public string HistoryType { get; set; }
        public string Description { get; set; }
        public string Medication { get; set; }
        public int? SinceYear { get; set; }
        public string Status { get; set; }
        public string ControlStatus { get; set; }
        public DateTime? LastFollowupDate { get; set; }
        public string Remarks { get; set; }
    }

    // ─── Remarks ───────────────────────────────────────────────
    public class AdmissionRemarksModel
    {
        public int RemarkId { get; set; }
        public int IpdId { get; set; }
        public int PatientId { get; set; }
        public string Remarks { get; set; }
    }

    public class SaveRemarksRequest
    {
        public int IpdId { get; set; }
        public int PatientId { get; set; }
        public string Remarks { get; set; }
    }

    public class RemarksTemplateModel
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string TemplateText { get; set; }
    }

    public class RemarksTemplateSaveRequest
    {
        public string Action { get; set; }  // ADD / UPDATE / DELETE
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string TemplateText { get; set; }
    }

    // ─── Structured HPI ──────────────────────────────────────────
    public class HpiModel
    {
        public int HpiId { get; set; }
        public int IpdId { get; set; }
        public int PatientId { get; set; }
        public string SymptomsStartedOn { get; set; }
        public string Progression { get; set; }          // Improving/Worsening/Static/Fluctuating
        public string AssociatedSymptoms { get; set; }
        public string AggravatingFactors { get; set; }
        public string RelievingFactors { get; set; }
        public string PreviousTreatment { get; set; }
        public string PreviousConsultation { get; set; }
        public string PreviousHospitalization { get; set; }
        public string TimelineNotes { get; set; }
    }

    public class SaveHpiRequest
    {
        public int IpdId { get; set; }
        public int PatientId { get; set; }
        public string SymptomsStartedOn { get; set; }
        public string Progression { get; set; }
        public string AssociatedSymptoms { get; set; }
        public string AggravatingFactors { get; set; }
        public string RelievingFactors { get; set; }
        public string PreviousTreatment { get; set; }
        public string PreviousConsultation { get; set; }
        public string PreviousHospitalization { get; set; }
        public string TimelineNotes { get; set; }
    }

    // ─── Professional Examination Module ─────────────────────────
    public class ExaminationDetailModel
    {
        public int ExamId { get; set; }
        public int IpdId { get; set; }
        public int PatientId { get; set; }

        // General Exam
        public string Conscious { get; set; }
        public string Orientation { get; set; }
        public string Pallor { get; set; }
        public string Icterus { get; set; }
        public string Cyanosis { get; set; }
        public string Clubbing { get; set; }
        public string Edema { get; set; }
        public string LymphNodes { get; set; }
        public string Hydration { get; set; }

        // Vitals
        public int? BpSystolic { get; set; }
        public int? BpDiastolic { get; set; }
        public int? Pulse { get; set; }
        public decimal? Temperature { get; set; }
        public int? RespiratoryRate { get; set; }
        public int? Spo2 { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? WeightKg { get; set; }
        public decimal? Bmi { get; set; }

        // Systemic Exam
        public string Cvs { get; set; }
        public string Rs { get; set; }
        public string Cns { get; set; }
        public string Abdomen { get; set; }
        public string Ent { get; set; }
        public string Skin { get; set; }
        public string Musculoskeletal { get; set; }
    }

    public class SaveExaminationDetailRequest
    {
        public int IpdId { get; set; }
        public int PatientId { get; set; }
        public string Conscious { get; set; }
        public string Orientation { get; set; }
        public string Pallor { get; set; }
        public string Icterus { get; set; }
        public string Cyanosis { get; set; }
        public string Clubbing { get; set; }
        public string Edema { get; set; }
        public string LymphNodes { get; set; }
        public string Hydration { get; set; }
        public int? BpSystolic { get; set; }
        public int? BpDiastolic { get; set; }
        public int? Pulse { get; set; }
        public decimal? Temperature { get; set; }
        public int? RespiratoryRate { get; set; }
        public int? Spo2 { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? WeightKg { get; set; }
        public string Cvs { get; set; }
        public string Rs { get; set; }
        public string Cns { get; set; }
        public string Abdomen { get; set; }
        public string Ent { get; set; }
        public string Skin { get; set; }
        public string Musculoskeletal { get; set; }
    }

    public class ClinicalFindingModel
    {
        public int FindingId { get; set; }
        public string FieldName { get; set; }     // CVS/RS/CNS/ABDOMEN/ENT/SKIN/MSK
        public string FindingName { get; set; }
        public string FindingText { get; set; }
    }

    public class SaveClinicalFindingRequest
    {
        public string FieldName { get; set; }
        public string FindingName { get; set; }
        public string FindingText { get; set; }
    }

    // ─── Department Templates ─────────────────────────────────────
    public class DepartmentTemplateModel
    {
        public int DeptTemplateId { get; set; }
        public string DepartmentName { get; set; }
        public bool IsHospitalWide { get; set; }
        public string CommonComplaints { get; set; }       // comma separated
        public string ExaminationFindings { get; set; }    // JSON string
        public string StandardNotes { get; set; }
        public string FrequentDiagnosis { get; set; }      // comma separated, display-only
    }

    public class SaveDepartmentTemplateRequest
    {
        public string DepartmentName { get; set; }
        public bool IsHospitalWide { get; set; }
        public string CommonComplaints { get; set; }
        public string ExaminationFindings { get; set; }
        public string StandardNotes { get; set; }
        public string FrequentDiagnosis { get; set; }
    }

    // ─── Previous Admission Comparison (read-only) ────────────────
    public class PreviousAdmissionSummaryModel
    {
        public int PreviousIpdId { get; set; }
        public string AdmissionNumber { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public string Status { get; set; }

        public string PreviousCurrentComplaint { get; set; }
        public string PreviousExamination { get; set; }
        public List<ChiefComplaintModel> PreviousChiefComplaints { get; set; } = new List<ChiefComplaintModel>();
        public List<PatientAllergyModel> PreviousAllergies { get; set; } = new List<PatientAllergyModel>();
        public List<PatientMedicalHistoryModel> PreviousMedicalHistory { get; set; } = new List<PatientMedicalHistoryModel>();
        public List<PreviousDiagnosisModel> PreviousDiagnoses { get; set; } = new List<PreviousDiagnosisModel>();
    }

    public class PreviousDiagnosisModel
    {
        public string DiagnosisName { get; set; }
        public string DiagnosisSubType { get; set; }
        public string Severity { get; set; }
        public string DiagnosisType { get; set; }
        public string Status { get; set; }
    }

    // ─── Audit Trail ───────────────────────────────────────────────
    public class AdmissionAuditLogModel
    {
        public long LogId { get; set; }
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public int? ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; }
        public string IpAddress { get; set; }
        public string DeviceInfo { get; set; }
    }

    

}
