using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class ProcedureController : Controller
    {
private readonly IProcedure _repository;
        private readonly IDoctor _IDoctor;
        private readonly Ipatient _patientRepo; // NEW — multi-hospital data isolation
        private readonly IIPDAdmission _ipdRepo; // NEW — hospital-scoped IPD admission check

        public ProcedureController(IProcedure repository, IDoctor doctor, Ipatient patientRepo, IIPDAdmission ipdRepo)
        {
            _repository = repository;
            _IDoctor = doctor;
            _patientRepo = patientRepo;
            _ipdRepo = ipdRepo;
        }

        private int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 0;

        private (int hospitalId, int? subHospitalId) GetHospitalContext()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            return (hospitalId, subHospitalId);
        }

        // ── Multi-hospital / sub-hospital data isolation helpers (NEW) ──────
        // procedure_master has no hospital_id column of its own — a procedure's
        // hospital is always determined via its PatientId (tbl_patient.Hospital_Id
        // is the source of truth). So every check here ultimately verifies the
        // owning patient belongs to the current session's hospital/sub-hospital.
        private bool IsPatientAuthorized(int patientId)
        {
            var (hospitalId, subHospitalId) = GetHospitalContext();
            return _patientRepo.GetPatientById(patientId, hospitalId, subHospitalId) != null;
        }

        private async Task<bool> IsProcedureAuthorizedAsync(int procedureId)
        {
            var ownerPatientId = await _repository.GetProcedureOwnerPatientIdAsync(procedureId);
            return ownerPatientId.HasValue && IsPatientAuthorized(ownerPatientId.Value);
        }

        private async Task<bool> IsChecklistItemAuthorizedAsync(int checklistItemId)
        {
            var procedureId = await _repository.GetProcedureIdByChecklistItemAsync(checklistItemId);
            return procedureId.HasValue && await IsProcedureAuthorizedAsync(procedureId.Value);
        }

        private async Task<bool> IsTeamMemberAuthorizedAsync(int teamMemberId)
        {
            var procedureId = await _repository.GetProcedureIdByTeamMemberAsync(teamMemberId);
            return procedureId.HasValue && await IsProcedureAuthorizedAsync(procedureId.Value);
        }

        // ── PAGE ─────────────────────────────────────────────────────

public IActionResult Index(int patientId, string visitContextType = "IPD", int visitContextId = 0, int ipdId = 0, int ipId = 0)
        {
            if (!IsPatientAuthorized(patientId))
                return NotFound();

            if (visitContextId == 0)
            {
                visitContextId = ipdId > 0 ? ipdId : ipId;
            }

            // NEW — hospital-scoped IPD admission check. When opened from an
            // IPD record, the admission must belong to the current session's
            // hospital/sub-hospital. Otherwise return NotFound (same as the
            // Admission Notes tab) so a foreign ipdId can't leak another
            // hospital's patient/procedure data.
if (visitContextType == "IPD" && visitContextId > 0)
            {
                var (ctxHospitalId, ctxSubHospitalId) = GetHospitalContext();
                var admission = _ipdRepo.GetIPDAdmissionById(visitContextId, ctxHospitalId, ctxSubHospitalId);
                if (admission == null)
                    return NotFound();
            }

            ViewBag.PatientId = patientId;
            ViewBag.VisitContextType = visitContextType;
            ViewBag.VisitContextId = visitContextId;
            ViewBag.pTitle = "Procedures";
            ViewBag.pageTitle = "Patient";

            var (hospitalId, subHospitalId) = GetHospitalContext();
            var doctors = _IDoctor.GetAllDoctor(hospitalId, subHospitalId);
            ViewBag.Doctors = new SelectList(doctors, "Doctor_Id", "FirstName");

            return View();
        }

        // ── REQUEST / PLANNING ──────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> RequestProcedure([FromBody] ProcedureRequestVM model)
        {
            if (!IsPatientAuthorized(model.PatientId))
                return Json(new { success = false, message = "Access denied: this patient does not belong to your hospital." });

            model.CreatedBy = CurrentUserId;
            var result = await _repository.CreateProcedureRequest(model);

            // Seed the default checklist for the chosen category immediately
            // so the Preparation tab has items to show as soon as the record exists.
            await _repository.SeedChecklist(result.ProcedureId, model.ProcedureCategory);

            return Json(new { success = result.ProcedureId > 0, procedureId = result.ProcedureId, uid = result.ProcedureUID });
        }

        [HttpPost]
        public async Task<IActionResult> ScheduleProcedure(int procedureId, DateTime plannedDate)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Json(new { success = false, message = "Access denied." });

            var result = await _repository.ScheduleProcedure(procedureId, plannedDate, CurrentUserId);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmProcedure(int procedureId)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Json(new { success = false, message = "Access denied." });

            var result = await _repository.ConfirmProcedure(procedureId, CurrentUserId);
            return Json(result);
        }

        // ── TEAM ─────────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> AddTeamMember(int procedureId, int staffId, string roleInTeam)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Json(new { success = false, message = "Access denied." });

            var id = await _repository.AddTeamMember(procedureId, staffId, roleInTeam);
            return Json(new { success = id > 0, teamMemberId = id });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveTeamMember(int teamMemberId)
        {
            if (!await IsTeamMemberAuthorizedAsync(teamMemberId))
                return Json(new { success = false, message = "Access denied." });

            var rows = await _repository.RemoveTeamMember(teamMemberId);
            return Json(new { success = rows > 0 });
        }

        [HttpGet]
        public async Task<IActionResult> GetTeam(int procedureId)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Unauthorized();

            return Json(await _repository.GetTeam(procedureId));
        }

        // ── ANAESTHESIA ──────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> SaveAnaesthesia(ProcedureAnaesthesia model)
        {
            if (!await IsProcedureAuthorizedAsync(model.ProcedureId))
                return Json(new { success = false, message = "Access denied." });

            var rows = await _repository.SaveAnaesthesia(model);
            return Json(new { success = rows > 0 });
        }

        // ── PREPARATION / CHECKLIST ──────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetChecklist(int procedureId)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Unauthorized();

            return Json(await _repository.GetChecklist(procedureId));
        }

        [HttpPost]
        public async Task<IActionResult> AddChecklistItem(int procedureId, string itemText, bool isMandatory)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Json(new { success = false, message = "Access denied." });

            var id = await _repository.AddChecklistItem(procedureId, itemText, isMandatory);
            return Json(new { success = id > 0, checklistItemId = id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateChecklistItem(int checklistItemId, bool isChecked)
        {
            if (!await IsChecklistItemAuthorizedAsync(checklistItemId))
                return Json(new { success = false, message = "Access denied." });

            var rows = await _repository.UpdateChecklistItem(checklistItemId, isChecked, CurrentUserId);
            return Json(new { success = rows > 0 });
        }

        // ── EXECUTION / TIMELINE ─────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> StartProcedure(int procedureId)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Json(new { success = false, message = "Access denied." });

            var result = await _repository.StartProcedure(procedureId, CurrentUserId);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> RecordTimelineEvent(int procedureId, string eventType, DateTime? eventTime)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Json(new { success = false, message = "Access denied." });

            var result = await _repository.RecordTimelineEvent(procedureId, eventType, eventTime, CurrentUserId);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetTimeline(int procedureId)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Unauthorized();

            return Json(await _repository.GetTimeline(procedureId));
        }

        // ── DOCUMENTATION ─────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> SaveOperativeNote(ProcedureNoteVersion model)
        {
            if (!await IsProcedureAuthorizedAsync(model.ProcedureId))
                return Json(new { success = false, message = "Access denied." });

            model.CreatedBy = CurrentUserId;
            var saved = await _repository.SaveOperativeNote(model);
            return Json(new { success = saved.NoteId > 0, noteId = saved.NoteId, version = saved.Version });
        }

        [HttpGet]
        public async Task<IActionResult> GetNoteHistory(int procedureId)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Unauthorized();

            return Json(await _repository.GetNoteHistory(procedureId));
        }

        [HttpPost]
        public async Task<IActionResult> SaveComplication(ProcedureComplication model)
        {
            if (!await IsProcedureAuthorizedAsync(model.ProcedureId))
                return Json(new { success = false, message = "Access denied." });

            model.RecordedBy = CurrentUserId;
            var id = await _repository.SaveComplication(model);
            return Json(new { success = id > 0, complicationId = id });
        }

        [HttpPost]
        public async Task<IActionResult> SaveSpecimen(ProcedureSpecimen model)
        {
            if (!await IsProcedureAuthorizedAsync(model.ProcedureId))
                return Json(new { success = false, message = "Access denied." });

            var id = await _repository.SaveSpecimen(model);
            return Json(new { success = id > 0, specimenId = id });
        }

        [HttpPost]
        public async Task<IActionResult> SavePostCare(ProcedurePostCare model)
        {
            if (!await IsProcedureAuthorizedAsync(model.ProcedureId))
                return Json(new { success = false, message = "Access denied." });

            var rows = await _repository.SavePostCare(model);
            return Json(new { success = rows > 0 });
        }

        // ── COMPLETION / CANCELLATION ─────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> CompleteProcedure(int procedureId, string outcome, string finalRemarks,
            string doctorConclusion, int? approvedByDoctorId)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Json(new { success = false, message = "Access denied." });

            var result = await _repository.CompleteProcedure(
                procedureId, outcome, finalRemarks, doctorConclusion, approvedByDoctorId, CurrentUserId);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> CancelProcedure(int procedureId, string reason)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Json(new { success = false, message = "Access denied." });

            var result = await _repository.UpdateProcedureStatus(procedureId, "Cancelled", reason, CurrentUserId);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> PostponeProcedure(int procedureId, string reason)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Json(new { success = false, message = "Access denied." });

            var result = await _repository.UpdateProcedureStatus(procedureId, "Postponed", reason, CurrentUserId);
            return Json(result);
        }

        // ── SEARCH / DETAIL / HISTORY ──────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetProcedureList([FromQuery] ProcedureSearchFilter filter)
        {
            // If a specific patient is requested, verify it belongs to this hospital.
            if (filter.PatientId.HasValue && !IsPatientAuthorized(filter.PatientId.Value))
                return Json(new object[0]);

            // NOTE: when filter.PatientId is NOT supplied, this searches across ALL
            // patients matching the other criteria. procedure_master has no hospital_id
            // column, so a hospital-wide filter would need a join against tbl_patient in
            // the repository query — that isn't in place yet. Flagging this as a known
            // remaining gap for the "browse all procedures" (no patient selected) case;
            // every other path in this controller is fully hospital-scoped.
            return Json(await _repository.GetProcedureList(filter));
        }

        [HttpGet]
        public async Task<IActionResult> GetProcedureDetail(int procedureId)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Unauthorized();

            return Json(await _repository.GetProcedureFullDetail(procedureId));
        }

        [HttpGet]
        public async Task<IActionResult> GetPatientHistory(int patientId)
        {
            if (!IsPatientAuthorized(patientId))
                return Unauthorized();

            return Json(await _repository.GetPatientProcedureHistory(patientId));
        }

        // ── TEMPLATES ───────────────────────────────────────────────────
        // Templates (procedure_template) have no patient/hospital linkage in the
        // schema at all — they're shared reference content (procedure name + steps),
        // not patient data, so no hospital check applies here.

        [HttpGet]
        public async Task<IActionResult> GetTemplates(string category, int? departmentId)
        {
            return Json(await _repository.GetTemplates(category, departmentId));
        }

        [HttpGet]
        public async Task<IActionResult> GetTemplateById(int templateId)
        {
            return Json(await _repository.GetTemplateById(templateId));
        }

        [HttpPost]
        public async Task<IActionResult> CreateTemplate([FromBody] ProcedureTemplate model)
        {
            model.CreatedBy = CurrentUserId;
            var id = await _repository.CreateTemplate(model);
            return Json(new { success = id > 0, templateId = id });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTemplate(int templateId)
        {
            var rows = await _repository.DeleteTemplate(templateId);
            return Json(new { success = rows > 0 });
        }

        // ── ATTACHMENTS ──────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> AddAttachment(ProcedureAttachment model)
        {
            if (!await IsProcedureAuthorizedAsync(model.ProcedureId))
                return Json(new { success = false, message = "Access denied." });

            model.UploadedBy = CurrentUserId;
            var id = await _repository.AddAttachment(model);
            return Json(new { success = id > 0, attachmentId = id });
        }

        [HttpGet]
        public async Task<IActionResult> GetAttachments(int procedureId)
        {
            if (!await IsProcedureAuthorizedAsync(procedureId))
                return Unauthorized();

            return Json(await _repository.GetAttachments(procedureId));
        }

        // ── DOCTOR DROPDOWN (kept from original controller) ───────────

        [HttpGet]
        public IActionResult GetDoctors()
        {
            var (hospitalId, subHospitalId) = GetHospitalContext();
            var doctors = _IDoctor.GetAllDoctor(hospitalId, subHospitalId);
            return Json(doctors);
        }
    }
}
