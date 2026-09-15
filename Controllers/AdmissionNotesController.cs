using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;
using Microsoft.Extensions.Logging;

namespace WebApplicationSampleTest2.Controllers
{
    public class AdmissionNotesController : Controller
    {
        private readonly ILogger<AdmissionNotesController> _logger;

        private readonly IAdmissionNotes _repo;
        private readonly IIPDAdmission _ipdRepo;
        private readonly Ipatient _patientRepo;

        public AdmissionNotesController(IAdmissionNotes repo, IIPDAdmission ipdRepo, Ipatient patientRepo, ILogger<AdmissionNotesController> logger)
        {
            _repo = repo;
            _logger = logger;
            _ipdRepo = ipdRepo;
            _patientRepo = patientRepo;
        }

        // ── Audit context helper ──────────────────────────────
        private string GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
        private string GetDeviceInfo() => Request.Headers["User-Agent"].ToString();

        // ── Multi-hospital / sub-hospital data isolation helpers ──────────
        // Every table in this app is scoped to a hospital (and optionally a
        // sub-hospital). These helpers make sure a logged-in user from
        // Hospital A can never read or write Admission Notes data that
        // belongs to a patient/admission in Hospital B, even if they type a
        // different ipdId/patientId directly into the URL or an API call.
        private int GetSessionHospitalId() => HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        private int? GetSessionSubHospitalId() => HttpContext.Session.GetInt32("SubHospitalId");

        /// <summary>Returns the IPD admission only if it belongs to the current session's hospital/sub-hospital, else null.</summary>
        private IPDAdmissionModel GetAuthorizedIpd(int ipdId)
        {
            return _ipdRepo.GetIPDAdmissionById(ipdId, GetSessionHospitalId(), GetSessionSubHospitalId());
        }

        /// <summary>True if the given patient belongs to the current session's hospital/sub-hospital.</summary>
        private bool IsPatientAuthorized(int patientId)
        {
            return _patientRepo.GetPatientById(patientId, GetSessionHospitalId(), GetSessionSubHospitalId()) != null;
        }

        /// <summary>True if the IPD exists in this hospital AND (when patientId is supplied) actually belongs to that patient.</summary>
        private bool IsIpdAuthorized(int ipdId, int? patientId = null)
        {
            var admission = GetAuthorizedIpd(ipdId);
            if (admission == null) return false;
            if (patientId.HasValue && admission.PatientId != patientId.Value) return false;
            return true;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int ipdId, int patientId)
        {
            // ── Data isolation check ──
            if (!IsIpdAuthorized(ipdId, patientId))
                return NotFound(); // matches the pattern used elsewhere in this app (e.g. IPDAdmissionController) —
                                   // Forbid() would throw here since no authentication scheme is configured

            int? hospitalId = GetSessionHospitalId();

            var notes = await _repo.GetAdmissionNotesAsync(ipdId);
            var remarks = await _repo.GetRemarksAsync(ipdId);
            int userId = HttpContext.Session.GetInt32("UserId") ?? 1;

            AdmissionNotesViewModel vm = new AdmissionNotesViewModel
            {
                IpdId = ipdId,
                PatientId = patientId,

                CurrentComplaint = notes?.CurrentComplaint,
                Examination = notes?.Examination,

                ComplaintTemplates = await _repo.GetTemplatesAsync("COMPLAINT", null, hospitalId),
                ExaminationTemplates = await _repo.GetTemplatesAsync("EXAMINATION", null, hospitalId),

                ChiefComplaints = await _repo.GetChiefComplaintsAsync(ipdId),

                Allergies = await _repo.GetAllergiesAsync(patientId, ipdId),
                MedicalHistory = await _repo.GetMedicalHistoryAsync(patientId),

                Remarks = remarks?.Remarks,
                RemarksTemplates = await _repo.GetRemarksTemplatesAsync(null, hospitalId),

                Hpi = await _repo.GetHpiAsync(ipdId),
                ExaminationDetail = await _repo.GetExaminationDetailAsync(ipdId),
                DepartmentTemplates = await _repo.GetDepartmentTemplatesAsync(userId, hospitalId),
                PreviousAdmission = await _repo.GetPreviousAdmissionSummaryAsync(patientId, ipdId)
            };

            return View(vm);
        }

        // ── Save Notes ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> SaveNotes([FromBody] SaveNotesRequest request)
        {
            try
            {
                if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                    return Json(new { success = false, message = "Access denied: this admission does not belong to your hospital." });

                int createdBy = HttpContext.Session.GetInt32("UserId") ?? 1;

                await _repo.SaveAdmissionNotesAsync(request, createdBy);

                return Json(new
                {
                    success = true,
                    message = "Saved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Action");
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        // ── Load Previous ─────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> LoadPrevious([FromBody] LoadPreviousRequest request)
        {
            if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                return Json(new { success = false, message = "Access denied." });

            var prev = await _repo.LoadPreviousAsync(request);
            if (prev == null) return Json(new { success = false, message = "No previous record found" });
            return Json(new { success = true, currentComplaint = prev.CurrentComplaint, examination = prev.Examination });
        }

        // ── Clear Notes ───────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ClearNotes([FromBody] ClearNotesRequest request)
        {
            if (!IsIpdAuthorized(request.IpdId))
                return Json(new { success = false, message = "Access denied." });

            await _repo.ClearNotesAsync(request);
            return Json(new { success = true });
        }

        // ── Structured Chief Complaints ────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetChiefComplaints(int ipdId)
        {
            if (!IsIpdAuthorized(ipdId))
                return Unauthorized();

            var list = await _repo.GetChiefComplaintsAsync(ipdId);
            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> SaveChiefComplaint([FromBody] SaveChiefComplaintRequest request)
        {
            try
            {
                // ADD/UPDATE carry ipdId+patientId directly; DELETE only carries complaintId,
                // so we look up which admission it belongs to before allowing the delete.
                if (request.Action == "DELETE")
                {
                    var ownerIpdId = await _repo.GetComplaintOwnerIpdIdAsync(request.ComplaintId);
                    if (!ownerIpdId.HasValue || !IsIpdAuthorized(ownerIpdId.Value))
                        return Json(new { success = false, message = "Access denied." });
                }
                else if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                {
                    return Json(new { success = false, message = "Access denied: this admission does not belong to your hospital." });
                }

                if (string.IsNullOrWhiteSpace(request.ComplaintName) && request.Action != "DELETE")
                    return Json(new { success = false, message = "Complaint name is required." });

                if (request.PainScore.HasValue && (request.PainScore < 0 || request.PainScore > 10))
                    return Json(new { success = false, message = "Pain score must be between 0 and 10." });

                int createdBy = HttpContext.Session.GetInt32("UserId") ?? 1;
                await _repo.SaveChiefComplaintAsync(request, createdBy, GetClientIp(), GetDeviceInfo());
                return Json(new { success = true, message = "Chief complaint saved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Action");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchComplaints(string search)
        {
            var list = await _repo.SearchMasterComplaintsAsync(search, GetSessionHospitalId());
            return Json(list);
        }

        // ── Templates ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetTemplates(string type, string search)
        {
            var list = await _repo.GetTemplatesAsync(type, search, GetSessionHospitalId());
            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> SaveTemplate([FromBody] TemplateSaveRequest request)
        {
            int createdBy = HttpContext.Session.GetInt32("UserId") ?? 1;
            await _repo.SaveTemplateAsync(request, GetSessionHospitalId(), createdBy);
            return Json(new { success = true, message = "Template saved" });
        }

        // ── Allergies ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> SaveAllergy([FromBody] SaveAllergyRequest request)
        {
            try
            {
                if (request.Action == "DELETE")
                {
                    var ownerIpdId = await _repo.GetAllergyOwnerIpdIdAsync(request.AllergyId);
                    if (!ownerIpdId.HasValue || !IsIpdAuthorized(ownerIpdId.Value))
                        return Json(new { success = false, message = "Access denied." });
                }
                else if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                {
                    return Json(new { success = false, message = "Access denied: this admission does not belong to your hospital." });
                }

                int createdBy = HttpContext.Session.GetInt32("UserId") ?? 1;
                int hospitalId = GetSessionHospitalId();
                int? subHospitalId = GetSessionSubHospitalId();

                request.HospitalId = hospitalId;
                request.SubHospitalId = subHospitalId;

                await _repo.SaveAllergyAsync(request, createdBy, GetClientIp(), GetDeviceInfo());
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Action");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllergies(int patientId, int ipdId)
        {
            if (!IsPatientAuthorized(patientId) || !IsIpdAuthorized(ipdId, patientId))
                return Unauthorized();

            var list = await _repo.GetAllergiesAsync(patientId, ipdId);
            return Json(list);
        }

        // ── Medical History ───────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> SaveMedicalHistory([FromBody] SaveMedicalHistoryRequest request)
        {
            try
            {
                if (request.Action == "DELETE")
                {
                    var ownerPatientId = await _repo.GetHistoryOwnerPatientIdAsync(request.HistoryId);
                    if (!ownerPatientId.HasValue || !IsPatientAuthorized(ownerPatientId.Value))
                        return Json(new { success = false, message = "Access denied." });
                }
                else if (!IsPatientAuthorized(request.PatientId))
                {
                    return Json(new { success = false, message = "Access denied: this patient does not belong to your hospital." });
                }

                int createdBy = HttpContext.Session.GetInt32("UserId") ?? 1;

                await _repo.SaveMedicalHistoryAsync(request, createdBy, GetClientIp(), GetDeviceInfo());

                return Json(new
                {
                    success = true,
                    message = "Medical History Saved Successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Action");
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMedicalHistory(int patientId)
        {
            if (!IsPatientAuthorized(patientId))
                return Unauthorized();

            var list = await _repo.GetMedicalHistoryAsync(patientId);
            return Json(list);
        }

        // ── Remarks ───────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> SaveRemarks([FromBody] SaveRemarksRequest request)
        {
            try
            {
                if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                    return Json(new { success = false, message = "Access denied: this admission does not belong to your hospital." });

                int createdBy = HttpContext.Session.GetInt32("UserId") ?? 1;

                await _repo.SaveRemarksAsync(request, createdBy);

                return Json(new
                {
                    success = true,
                    message = "Remarks Saved Successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Action");
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ClearRemarks([FromBody] int ipdId)
        {
            if (!IsIpdAuthorized(ipdId))
                return Json(new { success = false, message = "Access denied." });

            await _repo.ClearRemarksAsync(ipdId);
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetRemarksTemplates(string search)
        {
            var list = await _repo.GetRemarksTemplatesAsync(search, GetSessionHospitalId());
            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> SaveRemarksTemplate([FromBody] RemarksTemplateSaveRequest request)
        {
            int createdBy = HttpContext.Session.GetInt32("UserId") ?? 1;
            await _repo.SaveRemarksTemplateAsync(request, GetSessionHospitalId(), createdBy);
            return Json(new { success = true });
        }

        // ── Structured HPI ─────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetHpi(int ipdId)
        {
            if (!IsIpdAuthorized(ipdId))
                return Unauthorized();

            var hpi = await _repo.GetHpiAsync(ipdId);
            return Json(hpi);
        }

        [HttpPost]
        public async Task<IActionResult> SaveHpi([FromBody] SaveHpiRequest request)
        {
            try
            {
                if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                    return Json(new { success = false, message = "Access denied: this admission does not belong to your hospital." });

                int createdBy = HttpContext.Session.GetInt32("UserId") ?? 1;
                await _repo.SaveHpiAsync(request, createdBy, GetClientIp(), GetDeviceInfo());
                return Json(new { success = true, message = "HPI saved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Action");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── Professional Examination Module ────────────────────
        [HttpGet]
        public async Task<IActionResult> GetExaminationDetail(int ipdId)
        {
            if (!IsIpdAuthorized(ipdId))
                return Unauthorized();

            var exam = await _repo.GetExaminationDetailAsync(ipdId);
            return Json(exam);
        }

        [HttpPost]
        public async Task<IActionResult> SaveExaminationDetail([FromBody] SaveExaminationDetailRequest request)
        {
            try
            {
                if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                    return Json(new { success = false, message = "Access denied: this admission does not belong to your hospital." });

                int createdBy = HttpContext.Session.GetInt32("UserId") ?? 1;
                await _repo.SaveExaminationDetailAsync(request, createdBy, GetClientIp(), GetDeviceInfo());
                return Json(new { success = true, message = "Examination saved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Action");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── One-Click Clinical Findings ─────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetClinicalFindings(string fieldName)
        {
            var list = await _repo.GetClinicalFindingsAsync(fieldName, GetSessionHospitalId());
            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> SaveClinicalFinding([FromBody] SaveClinicalFindingRequest request)
        {
            try
            {
                int createdBy = HttpContext.Session.GetInt32("UserId") ?? 1;
                await _repo.SaveClinicalFindingAsync(request, GetSessionHospitalId(), createdBy);
                return Json(new { success = true, message = "Custom finding saved." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Action");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── Department-Based Clinical Templates ─────────────────
        [HttpGet]
        public async Task<IActionResult> GetDepartmentTemplates()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
            var list = await _repo.GetDepartmentTemplatesAsync(userId, GetSessionHospitalId());
            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> SaveDepartmentTemplate([FromBody] SaveDepartmentTemplateRequest request)
        {
            try
            {
                int createdBy = HttpContext.Session.GetInt32("UserId") ?? 1;
                await _repo.SaveDepartmentTemplateAsync(request, GetSessionHospitalId(), createdBy);
                return Json(new { success = true, message = "Department template saved." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Action");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── Previous Admission Comparison (read-only) ───────────
        [HttpGet]
        public async Task<IActionResult> GetPreviousAdmission(int patientId, int ipdId)
        {
            if (!IsPatientAuthorized(patientId) || !IsIpdAuthorized(ipdId, patientId))
                return Unauthorized();

            var summary = await _repo.GetPreviousAdmissionSummaryAsync(patientId, ipdId);
            return Json(summary);
        }

        // ── Audit Trail ──────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAuditLog(int ipdId)
        {
            if (!IsIpdAuthorized(ipdId))
                return Unauthorized();

            var list = await _repo.GetAuditLogAsync(ipdId);
            return Json(list);
        }

    }
}
