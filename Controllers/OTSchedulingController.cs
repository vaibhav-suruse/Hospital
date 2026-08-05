// Controllers/OTSchedulingController.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    // ── NEW: OT Management — Theatre & Booking Scheduling ────────────────
    // Purely additive controller. Never modifies ProcedureController.cs —
    // clinical documentation (team/checklist/anaesthesia/timeline/etc.)
    // stays exactly where it already lives; this controller only owns
    // theatre resourcing (which theatre, which time slot).
    public class OTSchedulingController : Controller
    {
        private readonly IOTScheduling _otRepo;
        private readonly IProcedure _procedureRepo;
        private readonly Ipatient _patientRepo;
        private readonly IIPDAdmission _admissionRepo;
        private readonly IInventory _inventoryRepo;
        private readonly IIPDOperation _ipdOperationRepo;
        private readonly IOperationMaster _operationMasterRepo;
        private readonly ILogger<OTSchedulingController> _logger;

        public OTSchedulingController(
            IOTScheduling otRepo,
            IProcedure procedureRepo,
            Ipatient patientRepo,
            IIPDAdmission admissionRepo,
            IInventory inventoryRepo,
            IIPDOperation ipdOperationRepo,
            IOperationMaster operationMasterRepo,
            ILogger<OTSchedulingController> logger)
        {
            _otRepo = otRepo;
            _procedureRepo = procedureRepo;
            _patientRepo = patientRepo;
            _admissionRepo = admissionRepo;
            _inventoryRepo = inventoryRepo;
            _ipdOperationRepo = ipdOperationRepo;
            _operationMasterRepo = operationMasterRepo;
            _logger = logger;
        }

        private int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 0;

        private (int hospitalId, int? subHospitalId) GetHospitalContext()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            return (hospitalId, subHospitalId);
        }

        // Same shape as ProcedureController.IsPatientAuthorized — duplicated
        // deliberately rather than shared, since it is a small private helper
        // and ProcedureController.cs is not to be modified for this feature.
        private bool IsPatientAuthorized(int patientId)
        {
            var (hospitalId, subHospitalId) = GetHospitalContext();
            return _patientRepo.GetPatientById(patientId, hospitalId, subHospitalId) != null;
        }

        private async Task<bool> IsBookingAuthorizedAsync(int bookingId)
        {
            var ownerPatientId = await _otRepo.GetBookingOwnerPatientIdAsync(bookingId);
            return ownerPatientId.HasValue && IsPatientAuthorized(ownerPatientId.Value);
        }

        // ===============================================================
        // PAGE — OT Board (hospital-wide), opened from a patient's IPD tab
        // ===============================================================
        [HttpGet]
        public async Task<IActionResult> Index(int ipdId)
        {
            var (hospitalId, subHospitalId) = GetHospitalContext();

            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, hospitalId, subHospitalId);
            if (admission == null) return NotFound();

            var today = DateTime.Today;

            var vm = new OTBoardVM
            {
                BoardDate = today,
                IPDId = ipdId,
                PatientId = admission.PatientId,
                PatientName = admission.PatientName,
                Theatres = await _otRepo.GetTheatresByHospital(hospitalId, subHospitalId),
                Bookings = await _otRepo.GetBookingsByDateRange(hospitalId, subHospitalId, null, today, today)
            };

            return View(vm);
        }

        // ===============================================================
        // BOARD PARTIAL — AJAX date navigation, no full page reload
        // ===============================================================
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> BoardPartial(int ipdId, DateTime date)
        {
            var (hospitalId, subHospitalId) = GetHospitalContext();

            var vm = new OTBoardVM
            {
                BoardDate = date.Date,
                IPDId = ipdId,
                Theatres = await _otRepo.GetTheatresByHospital(hospitalId, subHospitalId),
                Bookings = await _otRepo.GetBookingsByDateRange(hospitalId, subHospitalId, null, date.Date, date.Date)
            };

            return PartialView("_OTBoardPartial", vm);
        }

        // ===============================================================
        // Procedures awaiting OT scheduling — powers the "Book New Case"
        // search box. patientId narrows to the admission this was opened
        // from; leave it out (0/null) to search the whole hospital.
        // ===============================================================
        [HttpGet]
        public async Task<IActionResult> SchedulableProcedures(int? patientId, string search)
        {
            var (hospitalId, subHospitalId) = GetHospitalContext();

            if (patientId.HasValue && patientId.Value > 0 && !IsPatientAuthorized(patientId.Value))
                return Json(new { success = false, message = "Patient not found for this hospital." });

            var list = await _otRepo.GetUnscheduledSurgicalProcedures(
                hospitalId, subHospitalId, patientId.HasValue && patientId.Value > 0 ? patientId : null, search);

            return Json(new
            {
                success = true,
                data = list.Select(p => new
                {
                    p.ProcedureId,
                    p.ProcedureUID,
                    p.ProcedureName,
                    p.ProcedureCategory,
                    p.Priority,
                    p.Status,
                    PlannedDate = p.PlannedDate?.ToString("dd/MM/yyyy HH:mm"),
                    p.PatientId,
                    p.PatientName,
                    p.PrimarySurgeonName
                })
            });
        }

        // ===============================================================
        // Theatres — for the booking modal's theatre dropdown (Active only
        // by default) or the admin Theatres management modal (all=true).
        // ===============================================================
        [HttpGet]
        public async Task<IActionResult> Theatres(bool all = false)
        {
            var (hospitalId, subHospitalId) = GetHospitalContext();
            var list = await _otRepo.GetTheatresByHospital(hospitalId, subHospitalId, includeInactive: all);
            var data = all ? list : list.Where(t => t.Status == "Active" && t.IsActive);
            return Json(new { success = true, data });
        }

        // ===============================================================
        // Theatre master management (add / edit)
        // ===============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTheatre(OTTheatreSaveModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.TheatreName))
                    return Json(new { success = false, message = "Theatre name is required." });

                var (hospitalId, subHospitalId) = GetHospitalContext();

                if (model.TheatreId > 0)
                {
                    await _otRepo.UpdateTheatre(model);
                }
                else
                {
                    await _otRepo.AddTheatre(hospitalId, subHospitalId, model);
                }

                return Json(new { success = true, message = "Theatre saved." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveTheatre. TheatreId={TheatreId}", model?.TheatreId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // BOOK — the safety-critical action. Clash conflicts (theatre OR
        // staff) come back as a clean, friendly message.
        // ===============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking(OTBookingCreateModel model)
        {
            try
            {
                if (model.ScheduledStart < DateTime.Now.AddMinutes(-5))
                    return Json(new { success = false, message = "Scheduled start cannot be in the past." });

                var (hospitalId, subHospitalId) = GetHospitalContext();

                // Defense-in-depth: confirm the chosen procedure's owning patient
                // really belongs to this hospital before booking against it,
                // even though sp_OT_GetUnscheduledSurgicalProcedures already
                // only ever offered hospital-scoped procedures to pick from.
                var ownerPatientId = await _procedureRepo.GetProcedureOwnerPatientIdAsync(model.ProcedureId);
                if (!ownerPatientId.HasValue || !IsPatientAuthorized(ownerPatientId.Value))
                    return Json(new { success = false, message = "Selected procedure was not found for this hospital." });

                // Friendly staff-conflict pre-check (names + which theatre/time they
                // clash with). The DB-level SIGNAL inside sp_OT_CreateBooking is the
                // actual safety net if this is ever bypassed or races another request.
                var scheduledEnd = model.ScheduledStart.AddMinutes(model.DurationMinutes);
                var staffConflicts = await _otRepo.CheckStaffConflict(model.ProcedureId, model.ScheduledStart, scheduledEnd);
                if (staffConflicts.Count > 0)
                {
                    var c = staffConflicts[0];
                    return Json(new
                    {
                        success = false,
                        message = $"{c.StaffName} ({c.RoleInTeam}) already has an overlapping OT booking in {c.ConflictingTheatre} from {c.ConflictStart:hh:mm tt} to {c.ConflictEnd:hh:mm tt}."
                    });
                }

                var newId = await _otRepo.CreateBooking(hospitalId, subHospitalId, model, CurrentUserId);

                return Json(new { success = true, bookingId = newId, message = "Theatre booked." });
            }
            catch (InvalidOperationException clashEx)
            {
                // Expected, friendly clash message from sp_OT_CreateBooking's SIGNAL.
                return Json(new { success = false, message = clashEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateBooking. TheatreId={TheatreId}, ProcedureId={ProcedureId}", model?.TheatreId, model?.ProcedureId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // CONSENT — must be on file before Start is allowed (see below).
        // ===============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordConsent(ConsentSaveModel model)
        {
            try
            {
                if (!await IsBookingAuthorizedAsync(model.BookingId))
                    return Json(new { success = false, message = "Booking not found for this hospital." });

                if (string.IsNullOrWhiteSpace(model.ConsentWitnessName))
                    return Json(new { success = false, message = "Witness name is required." });

                await _otRepo.RecordConsent(model, CurrentUserId);
                return Json(new { success = true, message = "Consent recorded." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RecordConsent. BookingId={BookingId}", model?.BookingId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // START — gated on TWO things: (1) consent recorded on THIS
        // booking (checked here — ot_booking is this feature's own table,
        // so this check belongs here, not in ProcedureRepository), and
        // (2) the EXISTING sp_StartProcedure business rule (all mandatory
        // checklist items ticked). We never re-implement #2 ourselves;
        // we just surface whatever IProcedure.StartProcedure decides.
        // ===============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartBooking(int bookingId, int procedureId)
        {
            try
            {
                if (!await IsBookingAuthorizedAsync(bookingId))
                    return Json(new { success = false, message = "Booking not found for this hospital." });

                var booking = await _otRepo.GetBookingById(bookingId);
                if (booking == null)
                    return Json(new { success = false, message = "Booking not found." });

                if (!booking.ConsentRecorded)
                    return Json(new { success = false, message = "Cannot start: signed consent has not been recorded for this case yet." });

                var result = await _procedureRepo.StartProcedure(procedureId, CurrentUserId);
                if (!result.Success)
                {
                    return Json(new
                    {
                        success = false,
                        message = result.PendingCount.HasValue && result.PendingCount.Value > 0
                            ? $"Cannot start: {result.PendingCount} mandatory checklist item(s) are still unchecked."
                            : result.Message
                    });
                }

                await _otRepo.UpdateBookingStatus(new OTBookingStatusUpdateModel { BookingId = bookingId, ProcedureId = procedureId, Status = "InProgress" }, CurrentUserId);
                return Json(new { success = true, message = "Case started." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StartBooking. BookingId={BookingId}, ProcedureId={ProcedureId}", bookingId, procedureId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // COMPLETE — gated on the EXISTING sp_CompleteProcedure business
        // rule: an operative note and a primary doctor must already exist.
        // If they don't, we surface that message and point the user back
        // to the Procedures tab rather than trying to collect that data
        // here (that documentation genuinely belongs there).
        // ===============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteBooking(CompleteBookingModel model)
        {
            try
            {
                if (!await IsBookingAuthorizedAsync(model.BookingId))
                    return Json(new { success = false, message = "Booking not found for this hospital." });

                var result = await _procedureRepo.CompleteProcedure(
                    model.ProcedureId, model.Outcome, model.FinalRemarks, null, null, CurrentUserId);

                if (!result.Success)
                    return Json(new { success = false, message = result.Message, needsDocumentation = true });

                await _otRepo.UpdateBookingStatus(new OTBookingStatusUpdateModel { BookingId = model.BookingId, ProcedureId = model.ProcedureId, Status = "Completed" }, CurrentUserId);
                return Json(new { success = true, message = "Case completed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CompleteBooking. BookingId={BookingId}, ProcedureId={ProcedureId}", model?.BookingId, model?.ProcedureId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // Prefill for "Record OT Charges" — IPDId + surgeon/anaesthetist,
        // read from the EXISTING procedure record so nobody retypes data
        // that's already there.
        // ===============================================================
        [HttpGet]
        public async Task<IActionResult> GetProcedureContextForCharges(int procedureId)
        {
            if (!await IsProcedureAuthorizedForConsumablesAsync(procedureId))
                return Json(new { success = false, message = "Procedure not found for this hospital." });

            var detail = await _procedureRepo.GetProcedureFullDetail(procedureId);
            if (detail?.Master == null)
                return Json(new { success = false, message = "Procedure not found." });

            if (detail.Master.VisitContextType != "IPD")
                return Json(new { success = false, message = "OT charges can currently be recorded for IPD admissions only." });

            var surgeon = detail.Team?.FirstOrDefault(t => t.RoleInTeam == "Primary" && t.IsActive);
            var anaesthetist = detail.Team?.FirstOrDefault(t => t.RoleInTeam == "Anaesthetist" && t.IsActive);

            return Json(new
            {
                success = true,
                ipdId = detail.Master.VisitContextId,
                surgeonId = surgeon?.StaffId,
                surgeonName = surgeon?.StaffName,
                anesthesistId = anaesthetist?.StaffId,
                anesthesistName = anaesthetist?.StaffName
            });
        }

        // ===============================================================
        // Operation type master — for the "Record OT Charges" dropdown.
        // Reuses the EXISTING IOperationMaster as-is (already has
        // DefaultCharge/AnesthesiaCharge/SurgeonCharge/OTCharge per type,
        // for auto-fill on the client, same pattern as Billing Sheet's
        // service picker).
        // ===============================================================
        [HttpGet]
        public IActionResult GetOperationTypes()
        {
            var (hospitalId, subHospitalId) = GetHospitalContext();
            var list = _operationMasterRepo.GetAll(hospitalId, subHospitalId);
            return Json(new { success = true, data = list });
        }

        // ===============================================================
        // RECORD OT CHARGES — the actual billing-linkage step. Builds an
        // IPDOperationModel exactly the way IPDOperationController.
        // SaveOperation already does, and calls the SAME
        // IIPDOperation.SaveAndReturnId(...) — no new charge logic, no new
        // billing table. Once saved, ipdoperations already flows into
        // sp_GetIPDBillSummary automatically (verified against
        // IPDBillingRepository.cs), which is the whole point: complete a
        // case here and its charges show up on the final bill with no
        // re-typing. The booking is then linked so this can't be billed
        // twice from the board.
        // ===============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordOTCharges(OTChargesSaveModel model)
        {
            try
            {
                if (!await IsBookingAuthorizedAsync(model.BookingId))
                    return Json(new { success = false, message = "Booking not found for this hospital." });

                var booking = await _otRepo.GetBookingById(model.BookingId);
                if (booking == null)
                    return Json(new { success = false, message = "Booking not found." });

                if (booking.ChargesRecorded)
                    return Json(new { success = false, message = "OT charges have already been recorded for this case." });

                var (hospitalId, subHospitalId) = GetHospitalContext();

                var opModel = new IPDOperationModel
                {
                    IPDId = model.IPDId,
                    OperationId = model.OperationId,
                    OperationDate = model.OperationDate,
                    SurgeonId = model.SurgeonId,
                    AnesthesistId = model.AnesthesistId,
                    ActualCharge = model.ActualCharge,
                    AnesthesiaCharge = model.AnesthesiaCharge,
                    SurgeonCharge = model.SurgeonCharge,
                    OTCharge = model.OTCharge,
                    Notes = model.Notes,
                    Staff = new System.Collections.Generic.List<IPDOperationStaff>()
                };

                int ipdOperationId = _ipdOperationRepo.SaveAndReturnId(opModel, hospitalId, subHospitalId);

                await _otRepo.LinkBookingToOperation(model.BookingId, ipdOperationId);

                return Json(new { success = true, ipdOperationId, message = "OT charges recorded and added to the patient's bill." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RecordOTCharges. BookingId={BookingId}", model?.BookingId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // CANCEL — the only status this action still handles directly;
        // also syncs procedure_master back via the existing
        // IProcedure.UpdateProcedureStatus so the two records never drift.
        // ===============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(OTBookingStatusUpdateModel model)
        {
            try
            {
                if (model.Status != "Cancelled")
                    return Json(new { success = false, message = "Invalid status. Use StartBooking/CompleteBooking for those transitions." });

                if (string.IsNullOrWhiteSpace(model.CancelReason))
                    return Json(new { success = false, message = "A cancellation reason is required." });

                if (!await IsBookingAuthorizedAsync(model.BookingId))
                    return Json(new { success = false, message = "Booking not found for this hospital." });

                await _otRepo.UpdateBookingStatus(model, CurrentUserId);

                try
                {
                    await _procedureRepo.UpdateProcedureStatus(model.ProcedureId, "Cancelled", model.CancelReason, CurrentUserId);
                }
                catch (Exception syncEx)
                {
                    _logger.LogError(syncEx, "OT booking {BookingId} cancelled, but syncing procedure_master status failed.", model.BookingId);
                }

                return Json(new { success = true, message = "Booking cancelled." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateBookingStatus. BookingId={BookingId}", model?.BookingId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // QUICK REQUEST — closes the empty-state dead end: raise a new
        // procedure request right from the OT board instead of forcing a
        // trip to the Procedures tab first. Reuses the EXACT same sequence
        // ProcedureController.RequestProcedure already uses.
        // ===============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickRequestProcedure(QuickProcedureRequestModel model)
        {
            try
            {
                if (!IsPatientAuthorized(model.PatientId))
                    return Json(new { success = false, message = "Access denied: this patient does not belong to your hospital." });

                var requestVm = new ProcedureRequestVM
                {
                    PatientId = model.PatientId,
                    VisitContextType = model.VisitContextType,
                    VisitContextId = model.VisitContextId,
                    ProcedureName = model.ProcedureName,
                    ProcedureCategory = model.ProcedureCategory,
                    Priority = string.IsNullOrWhiteSpace(model.Priority) ? "Routine" : model.Priority,
                    ReasonForProcedure = model.ReasonForProcedure,
                    ClinicalIndication = model.ClinicalIndication,
                    CreatedBy = CurrentUserId
                };

                var result = await _procedureRepo.CreateProcedureRequest(requestVm);
                await _procedureRepo.SeedChecklist(result.ProcedureId, model.ProcedureCategory);

                return Json(new
                {
                    success = result.ProcedureId > 0,
                    procedureId = result.ProcedureId,
                    procedureUID = result.ProcedureUID,
                    procedureName = model.ProcedureName,
                    procedureCategory = model.ProcedureCategory,
                    priority = requestVm.Priority
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in QuickRequestProcedure. PatientId={PatientId}", model?.PatientId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // CONSUMABLES / IMPLANTS
        // ===============================================================
        [HttpGet]
        public async Task<IActionResult> GetConsumables(int procedureId)
        {
            if (!await IsProcedureAuthorizedForConsumablesAsync(procedureId))
                return Json(new { success = false, message = "Procedure not found for this hospital." });

            var list = await _otRepo.GetConsumablesByProcedure(procedureId);
            var total = await _otRepo.GetConsumableTotal(procedureId);
            return Json(new { success = true, data = list, total });
        }

        [HttpGet]
        public IActionResult SearchInventory(string term)
        {
            var (hospitalId, subHospitalId) = GetHospitalContext();
            var all = _inventoryRepo.GetAllInventory(hospitalId, subHospitalId);
            var filtered = string.IsNullOrWhiteSpace(term)
                ? all.Take(20)
                : all.Where(i => i.MedicineName != null && i.MedicineName.Contains(term, StringComparison.OrdinalIgnoreCase)).Take(20);

            return Json(new
            {
                success = true,
                data = filtered.Select(i => new { i.MedicineId, i.BatchId, i.MedicineName, i.Stock, i.SellingPrice, i.Unit })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddConsumable(ConsumableSaveModel model)
        {
            try
            {
                if (!await IsProcedureAuthorizedForConsumablesAsync(model.ProcedureId))
                    return Json(new { success = false, message = "Procedure not found for this hospital." });

                var (hospitalId, subHospitalId) = GetHospitalContext();
                var newId = await _otRepo.AddConsumable(hospitalId, subHospitalId, model, CurrentUserId);
                return Json(new { success = true, consumableId = newId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddConsumable. ProcedureId={ProcedureId}", model?.ProcedureId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConsumable(int consumableId)
        {
            try
            {
                await _otRepo.DeleteConsumable(consumableId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteConsumable. ConsumableId={ConsumableId}", consumableId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // CHECKLIST (WHO 3-phase overlay on the existing procedure_checklist)
        // ===============================================================
        [HttpGet]
        public async Task<IActionResult> GetChecklist(int procedureId)
        {
            if (!await IsProcedureAuthorizedForConsumablesAsync(procedureId))
                return Json(new { success = false, message = "Procedure not found for this hospital." });

            var items = await _otRepo.GetChecklistPhases(procedureId);
            return Json(new { success = true, data = items });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleChecklistItem(int checklistItemId, bool isChecked)
        {
            try
            {
                if (!await IsChecklistItemAuthorizedAsync(checklistItemId))
                    return Json(new { success = false, message = "Checklist item not found for this hospital." });

                await _procedureRepo.UpdateChecklistItem(checklistItemId, isChecked, CurrentUserId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ToggleChecklistItem. ChecklistItemId={ChecklistItemId}", checklistItemId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TagChecklistPhase(int checklistItemId, string phase)
        {
            try
            {
                var validPhases = new[] { "SignIn", "TimeOut", "SignOut" };
                if (!validPhases.Contains(phase))
                    return Json(new { success = false, message = "Invalid phase." });

                if (!await IsChecklistItemAuthorizedAsync(checklistItemId))
                    return Json(new { success = false, message = "Checklist item not found for this hospital." });

                await _otRepo.TagChecklistPhase(checklistItemId, phase, CurrentUserId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TagChecklistPhase. ChecklistItemId={ChecklistItemId}", checklistItemId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===============================================================
        // OT REGISTER — printable report
        // ===============================================================
        [HttpGet]
        public async Task<IActionResult> Register(DateTime? from, DateTime? to)
        {
            var (hospitalId, subHospitalId) = GetHospitalContext();
            var dateFrom = from ?? DateTime.Today.AddDays(-7);
            var dateTo = to ?? DateTime.Today;

            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            var rows = await _otRepo.GetRegister(hospitalId, subHospitalId, dateFrom, dateTo);
            return View(rows);
        }

        // ── Small helper: a procedure (for consumables/checklist) is
        // authorized the same way a booking is — via its owning patient.
        private async Task<bool> IsProcedureAuthorizedForConsumablesAsync(int procedureId)
        {
            var ownerPatientId = await _procedureRepo.GetProcedureOwnerPatientIdAsync(procedureId);
            return ownerPatientId.HasValue && IsPatientAuthorized(ownerPatientId.Value);
        }

        private async Task<bool> IsChecklistItemAuthorizedAsync(int checklistItemId)
        {
            var procedureId = await _procedureRepo.GetProcedureIdByChecklistItemAsync(checklistItemId);
            return procedureId.HasValue && await IsProcedureAuthorizedForConsumablesAsync(procedureId.Value);
        }
    }
}
