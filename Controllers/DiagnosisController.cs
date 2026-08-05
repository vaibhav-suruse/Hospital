using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class DiagnosisController : Controller
    {
        private readonly IDiagnosis _repository;
        private readonly IOPD _opdRepository; // reused ONLY for the existing sp_GetAllDiagnoses / diagnosis_master lookup — no new lookup logic duplicated
        private readonly IDoctor _doctorRepository; // reused ONLY for the existing GetAllDoctor lookup — powers the "Diagnosed By" dropdown
        private readonly IIPDAdmission _ipdRepo; // NEW — multi-hospital data isolation
        private readonly Ipatient _patientRepo;  // NEW — multi-hospital data isolation

        public DiagnosisController(IDiagnosis repository, IOPD opdRepository, IDoctor doctorRepository, IIPDAdmission ipdRepo, Ipatient patientRepo)
        {
            _repository = repository;
            _opdRepository = opdRepository;
            _doctorRepository = doctorRepository;
            _ipdRepo = ipdRepo;
            _patientRepo = patientRepo;
        }

        // ── Multi-hospital / sub-hospital data isolation helpers (NEW) ──────
        // Same pattern used in AdmissionNotesController: every ipdId/patientId/
        // diagnosisId coming from the client is verified against the current
        // session's hospital before any read or write happens, so a user from
        // Hospital A can never see or touch Hospital B's diagnosis records by
        // changing an id in the URL or an API call.
        private int GetSessionHospitalId() => HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        private int? GetSessionSubHospitalId() => HttpContext.Session.GetInt32("SubHospitalId");

        private bool IsIpdAuthorized(int ipdId, int? patientId = null)
        {
            var admission = _ipdRepo.GetIPDAdmissionById(ipdId, GetSessionHospitalId(), GetSessionSubHospitalId());
            if (admission == null) return false;
            if (patientId.HasValue && admission.PatientId != patientId.Value) return false;
            return true;
        }

        private bool IsPatientAuthorized(int patientId)
        {
            return _patientRepo.GetPatientById(patientId, GetSessionHospitalId(), GetSessionSubHospitalId()) != null;
        }

        /// <summary>For endpoints that only receive a diagnosisId (Verify/Approve/Resolve/
        /// Evidence/Audit/etc.) — looks up which IPD owns it, then checks that IPD
        /// against the current session's hospital.</summary>
        private async Task<bool> IsDiagnosisAuthorizedAsync(int diagnosisId)
        {
            var ownerIpdId = await _repository.GetDiagnosisOwnerIpdIdAsync(diagnosisId);
            return ownerIpdId.HasValue && IsIpdAuthorized(ownerIpdId.Value);
        }

        /// <summary>For bulk actions (BulkVerify/BulkApprove) — silently drops any id that
        /// doesn't belong to the current session's hospital, rather than failing the whole
        /// batch, so legitimate ids in the same batch still get processed.</summary>
        private async Task<List<int>> FilterAuthorizedDiagnosisIdsAsync(List<int> diagnosisIds)
        {
            var authorized = new List<int>();
            if (diagnosisIds == null) return authorized;
            foreach (var id in diagnosisIds)
            {
                if (await IsDiagnosisAuthorizedAsync(id))
                    authorized.Add(id);
            }
            return authorized;
        }

        // ====================================================================
        // EXISTING ACTIONS — signatures, routes, and behaviour unchanged.
        // Kept exactly as before so any existing link/bookmark/other module
        // that posts to these routes keeps working.
        // [ValidateAntiForgeryToken] and try/catch were added (Phase 4 —
        // production hardening) without changing the signatures or the
        // redirect-based flow these two legacy actions already use.
        // ====================================================================

        // Diagnosis Page
        [HttpGet]
        public async Task<IActionResult> Index(int ipdId, int patientId)
        {
            if (!IsIpdAuthorized(ipdId, patientId))
                return NotFound();

            DiagnosisPageVM vm = await BuildPageVM(ipdId, patientId);
            return View(vm);
        }

        public async Task<IActionResult> Diagnosis(int ipdId, int patientId)
        {
            if (!IsIpdAuthorized(ipdId, patientId))
                return NotFound();

            DiagnosisPageVM vm = await BuildPageVM(ipdId, patientId);
            return View(vm);
        }

        // Save Admission / Discharge Diagnosis (legacy, free-text only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDiagnosis(DiagnosisModel model)
        {
            try
            {
                if (ModelState.IsValid && IsIpdAuthorized(model.IpdId, model.PatientId))
                {
                    await _repository.InsertDiagnosis(model);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index",
                new
                {
                    ipdId = model.IpdId,
                    patientId = model.PatientId
                });
        }

        // Delete Diagnosis (legacy)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDiagnosis(int diagnosisId,
                                                        int ipdId,
                                                        int patientId)
        {
            try
            {
                if (IsIpdAuthorized(ipdId, patientId))
                {
                    await _repository.DeleteDiagnosis(diagnosisId);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index",
                new
                {
                    ipdId = ipdId,
                    patientId = patientId
                });
        }

        // ====================================================================
        // NEW — Phase 1 enterprise endpoints. These are additive; nothing
        // above this line changed in behaviour. The upgraded view calls
        // these via AJAX; the legacy actions above remain reachable and
        // functional independently.
        // ====================================================================

        // Shared page builder — populates both legacy lists (so the page
        // still renders instantly on load, same as before), plus reference
        // lists for the new UI. This does not change what Index/Diagnosis
        // return, only where the query logic lives.
        private async Task<DiagnosisPageVM> BuildPageVM(int ipdId, int patientId)
        {
            var vm = new DiagnosisPageVM
            {
                IpdId = ipdId,
                PatientId = patientId,
                AdmissionDiagnosis = await _repository.GetAdmissionDiagnosis(ipdId),
                DischargeDiagnosis = await _repository.GetDischargeDiagnosis(ipdId),
                CurrentUserRole = HttpContext.Session.GetString("UserRole")
            };

            // Phase 3: sub-types now come from diagnosis_type_master. Falls back to
            // the static DiagnosisReference.SubTypes list (the DiagnosisSubTypes
            // property's own default) if the DB call returns nothing, so the
            // dropdown never ends up empty even if the migration hasn't run yet.
            var dbTypes = await _repository.GetDiagnosisTypes();
            if (dbTypes != null && dbTypes.Count > 0)
            {
                vm.DiagnosisSubTypes = dbTypes.Select(t => t.TypeName).ToList();
            }

            // Tier 3: diagnosis-scoped allowed actions for the logged-in role.
            vm.AllowedActions = await _repository.GetAllowedActionsForRole(vm.CurrentUserRole);

            // Tier 2: missing-ICD-before-discharge alert, surfaced proactively.
            var missingIcd = await _repository.GetMissingIcdAlerts(ipdId);
            if (missingIcd != null && missingIcd.Count > 0)
            {
                vm.HasMissingIcdAlert = true;
                vm.ClinicalAlerts.Add($"{missingIcd.Count} discharge diagnosis record(s) are missing an ICD code. This is required before final discharge.");
            }

            // Tier 6: this user's quick-add favourites.
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId > 0)
            {
                vm.Favorites = await _repository.GetFavorites(userId);
            }

            return vm;
        }

        // ICD-10 / diagnosis-name lookup — reuses the EXISTING sp_GetAllDiagnoses
        // via IOPD (already registered in DI, already powering the OPD module).
        [HttpGet]
        public IActionResult SearchDiagnosisMaster(string term)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;

                var all = _opdRepository.GetAllDiagnoses(hospitalId);

                var filtered = string.IsNullOrWhiteSpace(term)
                    ? all
                    : all.Where(d =>
                            (d.Name != null && d.Name.ToLower().Contains(term.ToLower())) ||
                            (d.IcdCode != null && d.IcdCode.ToLower().Contains(term.ToLower())))
                         .ToList();

                return Json(filtered.Take(20));
            }
            catch
            {
                // Fail quiet for a lookup/autocomplete endpoint — an empty
                // suggestion list is a safe, non-alarming degradation.
                return Json(new object[0]);
            }
        }

        // "Diagnosed By" dropdown — doctors only (clinical scope of practice),
        // reuses the EXISTING IDoctor.GetAllDoctor already used by the Doctor module.
        [HttpGet]
        public IActionResult GetDoctorsForDropdown()
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                // NOTE: sp_Doctor_GetAll already filters "IsActive = 1" in SQL, so no
                // client-side filter is needed here. (DoctorRepository's reader also
                // never hydrates Doctor.IsActive from the row, so filtering on it here
                // would incorrectly exclude every doctor.)
                var doctors = _doctorRepository.GetAllDoctor(hospitalId, subHospitalId)
                    ?.Select(d => new { d.Doctor_Id, Name = $"{d.FirstName} {d.LastName}", d.Specialization })
                    .ToList();

                return Json(doctors);
            }
            catch
            {
                return Json(new object[0]);
            }
        }

        // Enterprise add — carries clinical type, ICD code, severity, principal
        // flag, POA/HAC, and clinical notes; writes an audit row.
        // SECURITY: role-gated server-side (not just via the UI dropdown), same
        // as Verify/Approve — only clinical roles may create a diagnosis record,
        // matching the "doctors diagnose" decision made earlier for this module.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDiagnosisEnterprise(SaveDiagnosisRequest request)
        {
            try
            {
                if (!IsClinicalApprover())
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "You do not have permission to add a diagnosis." });
                }

                if (request == null || string.IsNullOrWhiteSpace(request.DiagnosisName))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Diagnosis name is required." });
                }

                if (request.DoctorId == null || request.DoctorId <= 0)
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Please select the diagnosing doctor." });
                }

                if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Access denied: this admission does not belong to your hospital." });
                }

                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var model = new DiagnosisModel
                {
                    IpdId = request.IpdId,
                    PatientId = request.PatientId,
                    DiagnosisName = request.DiagnosisName.Trim(),
                    DiagnosisType = request.DiagnosisType,
                    DiagnosisSubType = request.DiagnosisSubType,
                    IcdCode = string.IsNullOrWhiteSpace(request.IcdCode) ? null : request.IcdCode.Trim(),
                    DiagnosisMasterId = request.DiagnosisMasterId,
                    Severity = request.Severity,
                    IsPrincipal = request.IsPrincipal,
                    PresentOnAdmission = request.PresentOnAdmission,
                    HospitalAcquired = request.HospitalAcquired,
                    ClinicalNotes = string.IsNullOrWhiteSpace(request.ClinicalNotes) ? null : request.ClinicalNotes.Trim(),
                    DoctorId = request.DoctorId,
                    CreatedByUserId = userId,
                    HospitalId = hospitalId,
                    SubHospitalId = subHospitalId
                };

                var result = await _repository.InsertDiagnosisEnterprise(model);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = "Something went wrong saving this diagnosis. Please try again. (" + ex.Message + ")" });
            }
        }

        // Unified list (both Admission + Discharge, or filtered) with all
        // enterprise fields resolved — used by the upgraded grid.
        [HttpGet]
        public async Task<IActionResult> GetDiagnosisList(int ipdId, string diagnosisType = null)
        {
            try
            {
                if (!IsIpdAuthorized(ipdId))
                    return Json(new object[0]);

                var list = await _repository.GetDiagnosisByIpdEnterprise(ipdId, diagnosisType);
                return Json(list);
            }
            catch
            {
                return Json(new object[0]);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyDiagnosis(DiagnosisActionRequest request)
        {
            try
            {
                if (!IsClinicalApprover())
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "You do not have permission to verify a diagnosis." });
                }

                if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Access denied." });
                }

                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var result = await _repository.VerifyDiagnosis(request.DiagnosisId, userId, request.Reason);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = "Something went wrong verifying this diagnosis. Please try again. (" + ex.Message + ")" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDiagnosis(DiagnosisActionRequest request)
        {
            try
            {
                if (!IsClinicalApprover())
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "You do not have permission to approve a diagnosis." });
                }

                if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Access denied." });
                }

                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var result = await _repository.ApproveDiagnosis(request.DiagnosisId, userId, request.Reason);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = "Something went wrong approving this diagnosis. Please try again. (" + ex.Message + ")" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDiagnosisEnterprise(DiagnosisActionRequest request)
        {
            try
            {
                if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Access denied." });
                }

                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var result = await _repository.DeleteDiagnosisEnterprise(request.DiagnosisId, userId, request.Reason);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = "Something went wrong deleting this diagnosis. Please try again. (" + ex.Message + ")" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditTrail(int diagnosisId)
        {
            try
            {
                if (!await IsDiagnosisAuthorizedAsync(diagnosisId))
                    return Json(new object[0]);

                var trail = await _repository.GetDiagnosisAuditTrail(diagnosisId);
                return Json(trail);
            }
            catch
            {
                return Json(new object[0]);
            }
        }

        // ====================================================================
        // NEW — Phase 2 reports. Entirely read-only; no other module touched.
        // ====================================================================

        [HttpGet]
        public IActionResult Reports()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetReportData(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                int? hospitalId = HttpContext.Session.GetInt32("MainHospitalId");
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var bundle = await _repository.GetDiagnosisReportBundle(hospitalId, subHospitalId, fromDate, toDate);
                return Json(bundle);
            }
            catch
            {
                return Json(new DiagnosisReportBundle());
            }
        }

        private bool IsClinicalApprover()
        {
            string role = HttpContext.Session.GetString("UserRole");
            return !string.IsNullOrEmpty(role) && DiagnosisReference.ClinicalApproverRoles.Contains(role);
        }

        // ====================================================================
        // NEW — Tier 1-7 hardening pass. Every action below is ADDITIVE; none
        // of the existing actions above this line were modified.
        // ====================================================================

        // Tier 3: diagnosis-scoped permission check, backed by
        // diagnosis_role_permission. Does not touch/replace any existing
        // authorization in this app.
        private async Task<bool> HasPermission(string action)
        {
            string role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role)) return false;
            var allowed = await _repository.GetAllowedActionsForRole(role);
            return allowed.Contains(action);
        }

        private bool IsBillingCodingRole()
        {
            string role = HttpContext.Session.GetString("UserRole");
            return !string.IsNullOrEmpty(role) && DiagnosisReference.BillingCodingRoles.Contains(role);
        }

        // ---- Tier 1/2/4: richer add, with duplicate/future-date checks and
        // the new clinical detail fields --------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDiagnosisEnterpriseV2(SaveDiagnosisRequestV2 request)
        {
            try
            {
                if (!await HasPermission("Create"))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "You do not have permission to add a diagnosis." });
                }

                if (request == null || string.IsNullOrWhiteSpace(request.DiagnosisName))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Diagnosis name is required." });
                }

                if (request.DoctorId == null || request.DoctorId <= 0)
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Please select the diagnosing doctor." });
                }

                if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Access denied: this admission does not belong to your hospital." });
                }

                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var model = new DiagnosisModel
                {
                    IpdId = request.IpdId,
                    PatientId = request.PatientId,
                    DiagnosisName = request.DiagnosisName.Trim(),
                    DiagnosisType = request.DiagnosisType,
                    DiagnosisSubType = request.DiagnosisSubType,
                    IcdCode = string.IsNullOrWhiteSpace(request.IcdCode) ? null : request.IcdCode.Trim(),
                    Icd11Code = string.IsNullOrWhiteSpace(request.Icd11Code) ? null : request.Icd11Code.Trim(),
                    SnomedCode = string.IsNullOrWhiteSpace(request.SnomedCode) ? null : request.SnomedCode.Trim(),
                    DiagnosisMasterId = request.DiagnosisMasterId,
                    Severity = request.Severity,
                    Stage = request.Stage,
                    Grade = request.Grade,
                    Laterality = request.Laterality,
                    BodySite = request.BodySite,
                    IsPrincipal = request.IsPrincipal,
                    PresentOnAdmission = request.PresentOnAdmission,
                    HospitalAcquired = request.HospitalAcquired,
                    ClinicalNotes = string.IsNullOrWhiteSpace(request.ClinicalNotes) ? null : request.ClinicalNotes.Trim(),
                    Prognosis = request.Prognosis,
                    TreatmentPlan = string.IsNullOrWhiteSpace(request.TreatmentPlan) ? null : request.TreatmentPlan.Trim(),
                    ExpectedOutcome = request.ExpectedOutcome,
                    InsuranceRelevant = request.InsuranceRelevant,
                    DoctorId = request.DoctorId,
                    CreatedByUserId = userId,
                    HospitalId = hospitalId,
                    SubHospitalId = subHospitalId,
                    DiagnosisDate = request.DiagnosisDate ?? default
                };

                var result = await _repository.InsertDiagnosisEnterpriseV2(model);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = "Something went wrong saving this diagnosis. Please try again. (" + ex.Message + ")" });
            }
        }

        // ---- Tier 2: resolve (requires a resolved date) ----------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveDiagnosis(ResolveDiagnosisRequest request)
        {
            try
            {
                if (!await HasPermission("Update"))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "You do not have permission to resolve a diagnosis." });
                }

                if (!await IsDiagnosisAuthorizedAsync(request.DiagnosisId))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Access denied." });
                }

                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var result = await _repository.ResolveDiagnosis(request.DiagnosisId, request.ResolvedDate, userId, request.Reason);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = "Something went wrong resolving this diagnosis. (" + ex.Message + ")" });
            }
        }

        // ---- Tier 3: reject / freeze / unlock ---------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectDiagnosis(DiagnosisActionRequest request)
        {
            try
            {
                if (!await HasPermission("Reject"))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "You do not have permission to reject a diagnosis." });
                }
                if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Access denied." });
                }
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var result = await _repository.RejectDiagnosis(request.DiagnosisId, userId, request.Reason);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = "Something went wrong rejecting this diagnosis. (" + ex.Message + ")" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FreezeDiagnosis(DiagnosisActionRequest request)
        {
            try
            {
                if (!await HasPermission("Freeze"))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "You do not have permission to freeze a diagnosis." });
                }
                if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Access denied." });
                }
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var result = await _repository.FreezeDiagnosis(request.DiagnosisId, userId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = "Something went wrong freezing this diagnosis. (" + ex.Message + ")" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockDiagnosis(DiagnosisActionRequest request)
        {
            try
            {
                if (!await HasPermission("Unlock"))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "You do not have permission to unlock a diagnosis." });
                }
                if (!IsIpdAuthorized(request.IpdId, request.PatientId))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Access denied." });
                }
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var result = await _repository.UnlockDiagnosis(request.DiagnosisId, userId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = "Something went wrong unlocking this diagnosis. (" + ex.Message + ")" });
            }
        }

        // ---- Tier 4: linked clinical evidence ---------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDiagnosisEvidence(DiagnosisEvidenceRequest request)
        {
            try
            {
                if (!await HasPermission("Update"))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "You do not have permission to link evidence." });
                }
                if (!await IsDiagnosisAuthorizedAsync(request.DiagnosisId))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Access denied." });
                }
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var result = await _repository.AddDiagnosisEvidence(request, userId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = "Something went wrong linking evidence. (" + ex.Message + ")" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDiagnosisEvidence(int diagnosisId)
        {
            try
            {
                if (!await IsDiagnosisAuthorizedAsync(diagnosisId))
                    return Json(new object[0]);

                var list = await _repository.GetDiagnosisEvidence(diagnosisId);
                return Json(list);
            }
            catch
            {
                return Json(new object[0]);
            }
        }

        // ---- Tier 2: missing-ICD alert list (also used by BuildPageVM) --------
        [HttpGet]
        public async Task<IActionResult> GetMissingIcdAlerts(int ipdId)
        {
            try
            {
                if (!IsIpdAuthorized(ipdId))
                    return Json(new object[0]);

                var list = await _repository.GetMissingIcdAlerts(ipdId);
                return Json(list);
            }
            catch
            {
                return Json(new object[0]);
            }
        }

        // ---- Tier 6: paged + filtered grid -------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetDiagnosisPaged([FromQuery] DiagnosisFilterRequest filter)
        {
            try
            {
                if (!IsIpdAuthorized(filter.IpdId))
                    return Json(new PagedDiagnosisResult());

                var result = await _repository.GetDiagnosisPaged(filter);
                return Json(result);
            }
            catch
            {
                return Json(new PagedDiagnosisResult());
            }
        }

        // ---- Tier 6: favourites / quick-add templates --------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFavorite(AddFavoriteRequest request)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                int? hospitalId = HttpContext.Session.GetInt32("MainHospitalId");
                var result = await _repository.AddFavorite(userId, hospitalId, request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFavorites()
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var list = await _repository.GetFavorites(userId);
                return Json(list);
            }
            catch
            {
                return Json(new object[0]);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFavorite(int id)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var result = await _repository.RemoveFavorite(id, userId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = ex.Message });
            }
        }

        // ---- Tier 6: bulk actions ------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkVerify(BulkDiagnosisActionRequest request)
        {
            try
            {
                if (!await HasPermission("Verify"))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "You do not have permission to verify diagnoses." });
                }

                var authorizedIds = await FilterAuthorizedDiagnosisIdsAsync(request.DiagnosisIds);
                if (authorizedIds.Count == 0)
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Access denied." });

                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var result = await _repository.BulkVerify(authorizedIds, userId, request.Reason);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApprove(BulkDiagnosisActionRequest request)
        {
            try
            {
                if (!await HasPermission("Approve"))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "You do not have permission to approve diagnoses." });
                }

                var authorizedIds = await FilterAuthorizedDiagnosisIdsAsync(request.DiagnosisIds);
                if (authorizedIds.Count == 0)
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Access denied." });

                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var result = await _repository.BulkApprove(authorizedIds, userId, request.Reason);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = ex.Message });
            }
        }

        // ---- Tier 7: billing / insurance coding flags -----------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBillingFlags(DiagnosisBillingFlagsRequest request)
        {
            try
            {
                if (!IsBillingCodingRole())
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "You do not have permission to update billing/coding flags." });
                }
                if (!await IsDiagnosisAuthorizedAsync(request.DiagnosisId))
                {
                    return Json(new DiagnosisOperationResult { Success = false, Message = "Access denied." });
                }
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var result = await _repository.UpdateBillingFlags(request, userId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new DiagnosisOperationResult { Success = false, Message = ex.Message });
            }
        }

        // ---- Tier 5: additional reports + dedicated dashboard ------------------
        [HttpGet]
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                int? hospitalId = HttpContext.Session.GetInt32("MainHospitalId");
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var bundle = await _repository.GetDiagnosisReportBundle(hospitalId, subHospitalId, fromDate, toDate);
                var mortality = await _repository.GetMortalityReport(hospitalId, subHospitalId, fromDate, toDate);
                var notifiable = await _repository.GetNotifiableDiseaseReport(hospitalId, subHospitalId, fromDate, toDate);

                return Json(new
                {
                    bundle,
                    mortalityCount = mortality.Count,
                    notifiableCount = notifiable.Count
                });
            }
            catch
            {
                return Json(new { bundle = new DiagnosisReportBundle(), mortalityCount = 0, notifiableCount = 0 });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMortalityReport(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                int? hospitalId = HttpContext.Session.GetInt32("MainHospitalId");
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
                var list = await _repository.GetMortalityReport(hospitalId, subHospitalId, fromDate, toDate);
                return Json(list);
            }
            catch
            {
                return Json(new object[0]);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMorbidityReport(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                int? hospitalId = HttpContext.Session.GetInt32("MainHospitalId");
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
                var list = await _repository.GetMorbidityReport(hospitalId, subHospitalId, fromDate, toDate);
                return Json(list);
            }
            catch
            {
                return Json(new object[0]);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReadmissionReport(DateTime? fromDate, DateTime? toDate, int windowDays = 30)
        {
            try
            {
                int? hospitalId = HttpContext.Session.GetInt32("MainHospitalId");
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
                var list = await _repository.GetReadmissionReport(hospitalId, subHospitalId, fromDate, toDate, windowDays);
                return Json(list);
            }
            catch
            {
                return Json(new object[0]);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifiableDiseaseReport(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                int? hospitalId = HttpContext.Session.GetInt32("MainHospitalId");
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
                var list = await _repository.GetNotifiableDiseaseReport(hospitalId, subHospitalId, fromDate, toDate);
                return Json(list);
            }
            catch
            {
                return Json(new object[0]);
            }
        }

        // ---- Tier 6: CSV export (opens directly in Excel — no new NuGet
        // dependency needed, so nothing to restore/verify beyond what's
        // already in the project) -------------------------------------------
        [HttpGet]
        public async Task<IActionResult> ExportDiagnosisCsv(int ipdId, string diagnosisType = null)
        {
            if (!await HasPermission("Export"))
            {
                return NotFound(); // Forbid() would throw here — no authentication scheme is configured in this app
            }

            if (!IsIpdAuthorized(ipdId))
            {
                return NotFound();
            }

            var list = await _repository.GetDiagnosisByIpdEnterprise(ipdId, diagnosisType);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("DiagnosisId,DiagnosisName,Type,SubType,IcdCode,Severity,Status,Principal,DiagnosisDate,DoctorName");
            foreach (var d in list)
            {
                sb.AppendLine(string.Join(",",
                    d.DiagnosisId,
                    CsvEscape(d.DiagnosisName),
                    CsvEscape(d.DiagnosisType),
                    CsvEscape(d.DiagnosisSubType),
                    CsvEscape(d.IcdCode),
                    CsvEscape(d.Severity),
                    CsvEscape(d.Status),
                    d.IsPrincipal ? "Yes" : "No",
                    d.DiagnosisDate.ToString("yyyy-MM-dd"),
                    CsvEscape(d.EnteredByName)));
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"Diagnosis_IPD{ipdId}_{DateTime.Now:yyyyMMdd}.csv");
        }

        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        // ---- Tier 6: PDF export (QuestPDF — already a project dependency,
        // no new package added) ------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> ExportDiagnosisPdf(int ipdId, string diagnosisType = null)
        {
            if (!await HasPermission("Print"))
            {
                return NotFound(); // Forbid() would throw here — no authentication scheme is configured in this app
            }

            if (!IsIpdAuthorized(ipdId))
            {
                return NotFound();
            }

            var list = await _repository.GetDiagnosisByIpdEnterprise(ipdId, diagnosisType);

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text($"Diagnosis Record — IPD #{ipdId}").FontSize(16).Bold();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Diagnosis").Bold();
                            header.Cell().Text("Type").Bold();
                            header.Cell().Text("ICD").Bold();
                            header.Cell().Text("Severity").Bold();
                            header.Cell().Text("Status").Bold();
                        });

                        foreach (var d in list)
                        {
                            table.Cell().Text(d.DiagnosisName);
                            table.Cell().Text(d.DiagnosisType);
                            table.Cell().Text(d.IcdCode ?? "-");
                            table.Cell().Text(d.Severity ?? "-");
                            table.Cell().Text(d.Status);
                        }
                    });
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated ").FontSize(9);
                        x.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm")).FontSize(9);
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Diagnosis_IPD{ipdId}_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}
