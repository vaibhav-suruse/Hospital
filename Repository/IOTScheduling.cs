using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IOTScheduling
    {
        // Theatres
        Task<List<OTTheatreModel>> GetTheatresByHospital(int parentHospitalId, int? subHospitalId, bool includeInactive = false);
        Task<int> AddTheatre(int parentHospitalId, int? subHospitalId, OTTheatreSaveModel model);
        Task UpdateTheatre(OTTheatreSaveModel model);

        // Schedulable procedures (Requested/Scheduled, Surgical/Emergency, not already booked)
        Task<List<SchedulableProcedureModel>> GetUnscheduledSurgicalProcedures(
            int parentHospitalId, int? subHospitalId, int? patientId, string searchTerm);

        // Bookings
        Task<List<OTBookingModel>> GetBookingsByDateRange(
            int parentHospitalId, int? subHospitalId, int? theatreId, DateTime dateFrom, DateTime dateTo);

        // Throws with a friendly message (from SIGNAL in sp_OT_CreateBooking) on a real clash.
        Task<int> CreateBooking(
            int parentHospitalId, int? subHospitalId, OTBookingCreateModel model, int? createdBy);

        Task UpdateBookingStatus(OTBookingStatusUpdateModel model, int? updatedBy);

        // Tenant-isolation helper — mirrors IProcedure.GetProcedureOwnerPatientIdAsync
        Task<int?> GetBookingOwnerPatientIdAsync(int bookingId);

        // ═══════════════════════════ PHASE 2 ═══════════════════════════

        // Friendly pre-check (see sp_OT_CheckStaffConflict) — the DB-level
        // SIGNAL inside CreateBooking is the actual safety net.
        Task<List<StaffConflictModel>> CheckStaffConflict(int procedureId, DateTime scheduledStart, DateTime scheduledEnd);

        // Consumables / Implants
        Task<int> AddConsumable(int parentHospitalId, int? subHospitalId, ConsumableSaveModel model, int? createdBy);
        Task<List<ProcedureConsumableModel>> GetConsumablesByProcedure(int procedureId);
        Task DeleteConsumable(int consumableId);
        Task<decimal> GetConsumableTotal(int procedureId);

        // WHO 3-phase checklist tagging (overlay — procedure_checklist untouched)
        Task TagChecklistPhase(int checklistItemId, string phase, int? taggedBy);
        Task<List<ChecklistPhaseItemModel>> GetChecklistPhases(int procedureId);

        // OT Register report
        Task<List<OTRegisterRowModel>> GetRegister(int parentHospitalId, int? subHospitalId, DateTime dateFrom, DateTime dateTo);

        // ═══════════════════════ PHASE 2 (cont.) ═══════════════════════

        // Single-booking lookup — gates Start on consent, and prefills
        // the Record OT Charges modal (surgeon/anaesthetist/theatre/times).
        Task<OTBookingModel> GetBookingById(int bookingId);

        // Pre-op consent — recorded before Start is allowed.
        Task RecordConsent(ConsentSaveModel model, int? consentTakenBy);

        // Remembers which ipdoperations row (created via the EXISTING
        // IIPDOperation.SaveAndReturnId in the controller) belongs to this
        // booking, so the board never offers to bill the same case twice.
        Task LinkBookingToOperation(int bookingId, int ipdOperationId);
    }
}
