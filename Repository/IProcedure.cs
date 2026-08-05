using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IProcedure
    {
        // Lifecycle
        Task<ProcedureMaster> CreateProcedureRequest(ProcedureRequestVM model);
        Task<ProcedureOperationResult> ScheduleProcedure(int procedureId, System.DateTime plannedDate, int updatedBy);
        Task<ProcedureOperationResult> ConfirmProcedure(int procedureId, int updatedBy);
        Task<ProcedureOperationResult> StartProcedure(int procedureId, int startedBy);
        Task<ProcedureOperationResult> CompleteProcedure(int procedureId, string outcome, string finalRemarks,
            string doctorConclusion, int? approvedByDoctorId, int completedBy);
        Task<ProcedureOperationResult> UpdateProcedureStatus(int procedureId, string toStatus, string reason, int changedBy);

        // Team
        Task<int> AddTeamMember(int procedureId, int staffId, string roleInTeam);
        Task<int> RemoveTeamMember(int teamMemberId);
        Task<List<ProcedureTeamMember>> GetTeam(int procedureId);

        // Anaesthesia
        Task<int> SaveAnaesthesia(ProcedureAnaesthesia model);

        // Checklist
        Task<int> SeedChecklist(int procedureId, string category);
        Task<int> AddChecklistItem(int procedureId, string itemText, bool isMandatory);
        Task<int> UpdateChecklistItem(int checklistItemId, bool isChecked, int checkedBy);
        Task<List<ProcedureChecklistItem>> GetChecklist(int procedureId);

        // Timeline
        Task<ProcedureOperationResult> RecordTimelineEvent(int procedureId, string eventType, System.DateTime? eventTime, int recordedBy);
        Task<List<ProcedureTimelineEvent>> GetTimeline(int procedureId);

        // Operative note (versioned)
        Task<ProcedureNoteVersion> SaveOperativeNote(ProcedureNoteVersion model);
        Task<List<ProcedureNoteVersion>> GetNoteHistory(int procedureId);

        // Complications / Specimens / Post-care
        Task<int> SaveComplication(ProcedureComplication model);
        Task<int> SaveSpecimen(ProcedureSpecimen model);
        Task<int> SavePostCare(ProcedurePostCare model);

        // Search / detail / history
        Task<List<ProcedureMaster>> GetProcedureList(ProcedureSearchFilter filter);
        Task<ProcedureFullDetailVM> GetProcedureFullDetail(int procedureId);
        Task<List<ProcedureMaster>> GetPatientProcedureHistory(int patientId);

        // Templates
        Task<int> CreateTemplate(ProcedureTemplate model);
        Task<int> AddTemplateStep(int templateId, int stepOrder, string stepText);
        Task<List<ProcedureTemplate>> GetTemplates(string category, int? departmentId);
        Task<ProcedureTemplate> GetTemplateById(int templateId);
        Task<int> DeleteTemplate(int templateId);

        // Attachments
        Task<int> AddAttachment(ProcedureAttachment model);
        Task<List<ProcedureAttachment>> GetAttachments(int procedureId);

        // ── NEW (multi-hospital data isolation) ─────────────────────────────
        // procedure_master has no hospital_id column of its own — ownership is
        // always via PatientId, which is itself hospital-scoped (tbl_patient).
        // These let the controller trace a checklist item / team member back
        // to its procedure, and a procedure back to its owning patient, before
        // allowing any read or write.
        Task<int?> GetProcedureOwnerPatientIdAsync(int procedureId);
        Task<int?> GetProcedureIdByChecklistItemAsync(int checklistItemId);
        Task<int?> GetProcedureIdByTeamMemberAsync(int teamMemberId);
    }
}
