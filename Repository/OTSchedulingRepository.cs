using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    // ── NEW: OT Management — Theatre & Booking Scheduling ────────────────
    // Purely additive: own tables (ot_theatre, ot_booking), own sp_OT_*
    // stored procedures. Reuses the EXISTING, already-tested
    // IProcedure.ScheduleProcedure(...) to keep procedure_master's own
    // Status/PlannedDate in sync after a successful booking — this
    // repository never writes to procedure_master directly, and
    // ProcedureRepository.cs is never modified.
    public class OTSchedulingRepository : IOTScheduling
    {
        private readonly ILogger<OTSchedulingRepository> _logger;
        private readonly string ConnectionString;
        private readonly IProcedure _procedureRepo;

        public OTSchedulingRepository(
            IConfiguration configuration,
            ILogger<OTSchedulingRepository> logger,
            IProcedure procedureRepo)
        {
            ConnectionString = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
            _procedureRepo = procedureRepo;
        }

        private static object OrDbNull(object value)
        {
            if (value == null) return DBNull.Value;
            if (value is string s && string.IsNullOrWhiteSpace(s)) return DBNull.Value;
            return value;
        }

        // ── THEATRES ─────────────────────────────────────────────────────
        public async Task<List<OTTheatreModel>> GetTheatresByHospital(int parentHospitalId, int? subHospitalId, bool includeInactive = false)
        {
            var list = new List<OTTheatreModel>();
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_GetTheatresByHospital", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", OrDbNull(subHospitalId));
                cmd.Parameters.AddWithValue("p_IncludeInactive", includeInactive ? 1 : 0);

                await con.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(MapTheatre(reader));
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in GetTheatresByHospital. HospitalId={HospitalId}", parentHospitalId);
                throw new Exception("Error fetching OT theatres.", ex);
            }
            return list;
        }

        public async Task<int> AddTheatre(int parentHospitalId, int? subHospitalId, OTTheatreSaveModel model)
        {
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_AddTheatre", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", OrDbNull(subHospitalId));
                cmd.Parameters.AddWithValue("p_TheatreName", model.TheatreName ?? "");
                cmd.Parameters.AddWithValue("p_TheatreNumber", OrDbNull(model.TheatreNumber));

                await con.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in AddTheatre. HospitalId={HospitalId}", parentHospitalId);
                throw new Exception("Error adding OT theatre.", ex);
            }
        }

        public async Task UpdateTheatre(OTTheatreSaveModel model)
        {
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_UpdateTheatre", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_TheatreId", model.TheatreId);
                cmd.Parameters.AddWithValue("p_TheatreName", model.TheatreName ?? "");
                cmd.Parameters.AddWithValue("p_TheatreNumber", OrDbNull(model.TheatreNumber));
                cmd.Parameters.AddWithValue("p_Status", model.Status ?? "Active");
                cmd.Parameters.AddWithValue("p_IsActive", model.IsActive ? 1 : 0);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in UpdateTheatre. TheatreId={TheatreId}", model?.TheatreId);
                throw new Exception("Error updating OT theatre.", ex);
            }
        }

        // ── SCHEDULABLE PROCEDURES ──────────────────────────────────────
        public async Task<List<SchedulableProcedureModel>> GetUnscheduledSurgicalProcedures(
            int parentHospitalId, int? subHospitalId, int? patientId, string searchTerm)
        {
            var list = new List<SchedulableProcedureModel>();
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_GetUnscheduledSurgicalProcedures", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", OrDbNull(subHospitalId));
                cmd.Parameters.AddWithValue("p_PatientId", OrDbNull(patientId));
                cmd.Parameters.AddWithValue("p_SearchTerm", OrDbNull(searchTerm));

                await con.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new SchedulableProcedureModel
                    {
                        ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                        ProcedureUID = reader["ProcedureUID"]?.ToString(),
                        ProcedureName = reader["ProcedureName"]?.ToString(),
                        ProcedureCategory = reader["ProcedureCategory"]?.ToString(),
                        Priority = reader["Priority"]?.ToString(),
                        Status = reader["Status"]?.ToString(),
                        PlannedDate = reader["PlannedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["PlannedDate"]),
                        PatientId = Convert.ToInt32(reader["PatientId"]),
                        PatientName = reader["PatientName"]?.ToString()?.Trim(),
                        VisitContextType = reader["VisitContextType"]?.ToString(),
                        VisitContextId = Convert.ToInt32(reader["VisitContextId"]),
                        PrimarySurgeonName = reader["PrimarySurgeonName"] == DBNull.Value ? null : reader["PrimarySurgeonName"]?.ToString()
                    });
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in GetUnscheduledSurgicalProcedures. HospitalId={HospitalId}", parentHospitalId);
                throw new Exception("Error fetching procedures awaiting OT scheduling.", ex);
            }
            return list;
        }

        // ── BOOKINGS ─────────────────────────────────────────────────────
        public async Task<List<OTBookingModel>> GetBookingsByDateRange(
            int parentHospitalId, int? subHospitalId, int? theatreId, DateTime dateFrom, DateTime dateTo)
        {
            var list = new List<OTBookingModel>();
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_GetBookingsByDateRange", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", OrDbNull(subHospitalId));
                cmd.Parameters.AddWithValue("p_TheatreId", OrDbNull(theatreId));
                cmd.Parameters.AddWithValue("p_DateFrom", dateFrom.Date);
                cmd.Parameters.AddWithValue("p_DateTo", dateTo.Date);

                await con.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(MapBooking(reader));
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in GetBookingsByDateRange. HospitalId={HospitalId}", parentHospitalId);
                throw new Exception("Error fetching OT bookings.", ex);
            }
            return list;
        }

        public async Task<int> CreateBooking(int parentHospitalId, int? subHospitalId, OTBookingCreateModel model, int? createdBy)
        {
            int newId;
            try
            {
                var scheduledEnd = model.ScheduledStart.AddMinutes(model.DurationMinutes);

                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_CreateBooking", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_TheatreId", model.TheatreId);
                cmd.Parameters.AddWithValue("p_ProcedureId", model.ProcedureId);
                cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", OrDbNull(subHospitalId));
                cmd.Parameters.AddWithValue("p_ScheduledStart", model.ScheduledStart);
                cmd.Parameters.AddWithValue("p_ScheduledEnd", scheduledEnd);
                cmd.Parameters.AddWithValue("p_CreatedBy", OrDbNull(createdBy));

                await con.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                newId = Convert.ToInt32(result);
            }
            catch (MySqlException ex)
            {
                // 1644 = ER_SIGNAL_EXCEPTION — this is the clash message raised by
                // sp_OT_CreateBooking's SIGNAL, not an unexpected error. Surface it
                // to the user as-is; log anything else as a real failure.
                if (ex.Number == 1644)
                {
                    throw new InvalidOperationException(ex.Message);
                }
                _logger.LogError(ex, "DB error in CreateBooking. TheatreId={TheatreId}, ProcedureId={ProcedureId}", model?.TheatreId, model?.ProcedureId);
                throw new Exception("Error creating OT booking.", ex);
            }

            // Keep the clinical record (procedure_master) in sync by reusing the
            // EXISTING, already-tested repository methods — not duplicating their
            // SQL. Booking a theatre + time slot is, in real OT workflow, the
            // "team/resource confirmed" step, so we advance Scheduled -> Confirmed
            // here rather than leaving it stuck at Scheduled.
            try
            {
                await _procedureRepo.ScheduleProcedure(model.ProcedureId, model.ScheduledStart, createdBy ?? 0);
                await _procedureRepo.ConfirmProcedure(model.ProcedureId, createdBy ?? 0);
            }
            catch (Exception ex)
            {
                // The theatre/staff booking itself already succeeded and is the
                // source of truth for the clash guarantee; failing to also sync
                // procedure_master's status shouldn't roll back a valid booking,
                // but it must not be silent either — it's fixable from the
                // Procedures screen either way.
                _logger.LogError(ex, "OT booking {BookingId} created, but syncing procedure_master status failed for ProcedureId={ProcedureId}", newId, model.ProcedureId);
            }

            return newId;
        }

        public async Task UpdateBookingStatus(OTBookingStatusUpdateModel model, int? updatedBy)
        {
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_UpdateBookingStatus", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_BookingId", model.BookingId);
                cmd.Parameters.AddWithValue("p_Status", model.Status);
                cmd.Parameters.AddWithValue("p_CancelReason", OrDbNull(model.CancelReason));
                cmd.Parameters.AddWithValue("p_UpdatedBy", OrDbNull(updatedBy));

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in UpdateBookingStatus. BookingId={BookingId}", model?.BookingId);
                throw new Exception("Error updating OT booking status.", ex);
            }
        }

        public async Task<int?> GetBookingOwnerPatientIdAsync(int bookingId)
        {
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_GetBookingOwnerPatientId", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_BookingId", bookingId);

                await con.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in GetBookingOwnerPatientIdAsync. BookingId={BookingId}", bookingId);
                throw new Exception("Error resolving OT booking owner.", ex);
            }
        }

        // ═══════════════════════════ PHASE 2 ═══════════════════════════

        public async Task<List<StaffConflictModel>> CheckStaffConflict(int procedureId, DateTime scheduledStart, DateTime scheduledEnd)
        {
            var list = new List<StaffConflictModel>();
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_CheckStaffConflict", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);
                cmd.Parameters.AddWithValue("p_ScheduledStart", scheduledStart);
                cmd.Parameters.AddWithValue("p_ScheduledEnd", scheduledEnd);

                await con.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new StaffConflictModel
                    {
                        StaffId = Convert.ToInt32(reader["StaffId"]),
                        RoleInTeam = reader["RoleInTeam"]?.ToString(),
                        StaffName = reader["StaffName"]?.ToString(),
                        ConflictingBookingId = Convert.ToInt32(reader["ConflictingBookingId"]),
                        ConflictingTheatre = reader["ConflictingTheatre"]?.ToString(),
                        ConflictStart = Convert.ToDateTime(reader["ConflictStart"]),
                        ConflictEnd = Convert.ToDateTime(reader["ConflictEnd"])
                    });
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in CheckStaffConflict. ProcedureId={ProcedureId}", procedureId);
                throw new Exception("Error checking staff schedule conflicts.", ex);
            }
            return list;
        }

        public async Task<int> AddConsumable(int parentHospitalId, int? subHospitalId, ConsumableSaveModel model, int? createdBy)
        {
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_AddConsumable", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_ProcedureId", model.ProcedureId);
                cmd.Parameters.AddWithValue("p_MedicineId", model.MedicineId);
                cmd.Parameters.AddWithValue("p_BatchId", model.BatchId);
                cmd.Parameters.AddWithValue("p_ItemName", model.ItemName ?? "");
                cmd.Parameters.AddWithValue("p_Quantity", model.Quantity);
                cmd.Parameters.AddWithValue("p_IsImplant", model.IsImplant ? 1 : 0);
                cmd.Parameters.AddWithValue("p_LotOrSerialNumber", OrDbNull(model.LotOrSerialNumber));
                cmd.Parameters.AddWithValue("p_Charge", model.Charge);
                cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", OrDbNull(subHospitalId));
                cmd.Parameters.AddWithValue("p_CreatedBy", OrDbNull(createdBy));

                await con.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in AddConsumable. ProcedureId={ProcedureId}", model?.ProcedureId);
                throw new Exception("Error adding consumable/implant.", ex);
            }
        }

        public async Task<List<ProcedureConsumableModel>> GetConsumablesByProcedure(int procedureId)
        {
            var list = new List<ProcedureConsumableModel>();
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_GetConsumablesByProcedure", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);

                await con.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new ProcedureConsumableModel
                    {
                        ConsumableId = Convert.ToInt32(reader["ConsumableId"]),
                        ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                        MedicineId = Convert.ToInt32(reader["MedicineId"]),
                        BatchId = Convert.ToInt32(reader["BatchId"]),
                        ItemName = reader["ItemName"]?.ToString(),
                        Quantity = Convert.ToInt32(reader["Quantity"]),
                        IsImplant = Convert.ToBoolean(reader["IsImplant"]),
                        LotOrSerialNumber = reader["LotOrSerialNumber"] == DBNull.Value ? null : reader["LotOrSerialNumber"].ToString(),
                        Charge = Convert.ToDecimal(reader["Charge"]),
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    });
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in GetConsumablesByProcedure. ProcedureId={ProcedureId}", procedureId);
                throw new Exception("Error fetching consumables.", ex);
            }
            return list;
        }

        public async Task DeleteConsumable(int consumableId)
        {
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_DeleteConsumable", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_ConsumableId", consumableId);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in DeleteConsumable. ConsumableId={ConsumableId}", consumableId);
                throw new Exception("Error removing consumable/implant.", ex);
            }
        }

        public async Task<decimal> GetConsumableTotal(int procedureId)
        {
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_GetConsumableTotal", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);

                await con.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in GetConsumableTotal. ProcedureId={ProcedureId}", procedureId);
                throw new Exception("Error calculating consumables total.", ex);
            }
        }

        public async Task TagChecklistPhase(int checklistItemId, string phase, int? taggedBy)
        {
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_TagChecklistPhase", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_ChecklistItemId", checklistItemId);
                cmd.Parameters.AddWithValue("p_Phase", phase);
                cmd.Parameters.AddWithValue("p_TaggedBy", OrDbNull(taggedBy));

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in TagChecklistPhase. ChecklistItemId={ChecklistItemId}", checklistItemId);
                throw new Exception("Error tagging checklist phase.", ex);
            }
        }

        public async Task<List<ChecklistPhaseItemModel>> GetChecklistPhases(int procedureId)
        {
            var list = new List<ChecklistPhaseItemModel>();
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_GetChecklistPhases", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);

                await con.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new ChecklistPhaseItemModel
                    {
                        ChecklistItemId = Convert.ToInt32(reader["ChecklistItemId"]),
                        ItemText = reader["ItemText"]?.ToString(),
                        IsMandatory = Convert.ToBoolean(reader["IsMandatory"]),
                        IsChecked = Convert.ToBoolean(reader["IsChecked"]),
                        CheckedBy = reader["CheckedBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["CheckedBy"]),
                        CheckedDate = reader["CheckedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["CheckedDate"]),
                        Phase = reader["Phase"] == DBNull.Value ? null : reader["Phase"].ToString()
                    });
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in GetChecklistPhases. ProcedureId={ProcedureId}", procedureId);
                throw new Exception("Error fetching checklist.", ex);
            }
            return list;
        }

        public async Task<List<OTRegisterRowModel>> GetRegister(int parentHospitalId, int? subHospitalId, DateTime dateFrom, DateTime dateTo)
        {
            var list = new List<OTRegisterRowModel>();
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_GetRegister", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", OrDbNull(subHospitalId));
                cmd.Parameters.AddWithValue("p_DateFrom", dateFrom.Date);
                cmd.Parameters.AddWithValue("p_DateTo", dateTo.Date);

                await con.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new OTRegisterRowModel
                    {
                        BookingId = Convert.ToInt32(reader["BookingId"]),
                        ScheduledStart = Convert.ToDateTime(reader["ScheduledStart"]),
                        ScheduledEnd = Convert.ToDateTime(reader["ScheduledEnd"]),
                        BookingStatus = reader["BookingStatus"]?.ToString(),
                        CancelReason = reader["CancelReason"] == DBNull.Value ? null : reader["CancelReason"].ToString(),
                        TheatreName = reader["TheatreName"]?.ToString(),
                        TheatreNumber = reader["TheatreNumber"] == DBNull.Value ? null : reader["TheatreNumber"].ToString(),
                        ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                        ProcedureUID = reader["ProcedureUID"]?.ToString(),
                        ProcedureName = reader["ProcedureName"]?.ToString(),
                        ProcedureCategory = reader["ProcedureCategory"]?.ToString(),
                        Priority = reader["Priority"]?.ToString(),
                        ProcedureStatus = reader["ProcedureStatus"]?.ToString(),
                        DurationMinutes = reader["DurationMinutes"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["DurationMinutes"]),
                        PatientName = reader["PatientName"]?.ToString()?.Trim(),
                        PrimarySurgeonName = reader["PrimarySurgeonName"] == DBNull.Value ? null : reader["PrimarySurgeonName"]?.ToString(),
                        AnaesthetistName = reader["AnaesthetistName"] == DBNull.Value ? null : reader["AnaesthetistName"]?.ToString(),
                        Outcome = reader["Outcome"] == DBNull.Value ? null : reader["Outcome"].ToString()
                    });
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in GetRegister. HospitalId={HospitalId}", parentHospitalId);
                throw new Exception("Error generating OT register.", ex);
            }
            return list;
        }

        // ── MAPPERS ──────────────────────────────────────────────────────
        private static OTTheatreModel MapTheatre(DbDataReader r)
        {
            return new OTTheatreModel
            {
                TheatreId = Convert.ToInt32(r["TheatreId"]),
                ParentHospitalId = Convert.ToInt32(r["ParentHospitalId"]),
                SubHospitalId = r["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["SubHospitalId"]),
                TheatreName = r["TheatreName"]?.ToString(),
                TheatreNumber = r["TheatreNumber"] == DBNull.Value ? null : r["TheatreNumber"].ToString(),
                Status = r["Status"]?.ToString(),
                IsActive = Convert.ToBoolean(r["IsActive"]),
                CreatedDate = Convert.ToDateTime(r["CreatedDate"]),
                UpdatedDate = r["UpdatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["UpdatedDate"])
            };
        }

        private static OTBookingModel MapBooking(DbDataReader r)
        {
            return new OTBookingModel
            {
                BookingId = Convert.ToInt32(r["BookingId"]),
                TheatreId = Convert.ToInt32(r["TheatreId"]),
                TheatreName = r["TheatreName"]?.ToString(),
                TheatreNumber = r["TheatreNumber"] == DBNull.Value ? null : r["TheatreNumber"].ToString(),
                ProcedureId = Convert.ToInt32(r["ProcedureId"]),
                ProcedureUID = r["ProcedureUID"]?.ToString(),
                ProcedureName = r["ProcedureName"]?.ToString(),
                ProcedureCategory = r["ProcedureCategory"]?.ToString(),
                Priority = r["Priority"]?.ToString(),
                ScheduledStart = Convert.ToDateTime(r["ScheduledStart"]),
                ScheduledEnd = Convert.ToDateTime(r["ScheduledEnd"]),
                Status = r["Status"]?.ToString(),
                CancelReason = r["CancelReason"] == DBNull.Value ? null : r["CancelReason"].ToString(),
                PatientId = Convert.ToInt32(r["PatientId"]),
                PatientName = r["PatientName"]?.ToString()?.Trim(),
                PrimarySurgeonName = r["PrimarySurgeonName"] == DBNull.Value ? null : r["PrimarySurgeonName"]?.ToString(),
                ConsentTakenDate = r["ConsentTakenDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["ConsentTakenDate"]),
                ConsentWitnessName = r["ConsentWitnessName"] == DBNull.Value ? null : r["ConsentWitnessName"].ToString(),
                IPDOperationId = r["IPDOperationId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["IPDOperationId"]),
                MandatoryChecklistCount = Convert.ToInt32(r["MandatoryChecklistCount"]),
                MandatoryChecklistDone = Convert.ToInt32(r["MandatoryChecklistDone"])
            };
        }

        // ═══════════════════════ PHASE 2 (cont.) ═══════════════════════

        public async Task<OTBookingModel> GetBookingById(int bookingId)
        {
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_GetBookingById", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_BookingId", bookingId);

                await con.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapBooking(reader);
                }
                return null;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in GetBookingById. BookingId={BookingId}", bookingId);
                throw new Exception("Error fetching OT booking.", ex);
            }
        }

        public async Task RecordConsent(ConsentSaveModel model, int? consentTakenBy)
        {
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_RecordConsent", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_BookingId", model.BookingId);
                cmd.Parameters.AddWithValue("p_ConsentTakenBy", OrDbNull(consentTakenBy));
                cmd.Parameters.AddWithValue("p_ConsentWitnessName", model.ConsentWitnessName ?? "");

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in RecordConsent. BookingId={BookingId}", model?.BookingId);
                throw new Exception("Error recording consent.", ex);
            }
        }

        public async Task LinkBookingToOperation(int bookingId, int ipdOperationId)
        {
            try
            {
                using var con = new MySqlConnection(ConnectionString);
                using var cmd = new MySqlCommand("sp_OT_LinkBookingToOperation", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_BookingId", bookingId);
                cmd.Parameters.AddWithValue("p_IPDOperationId", ipdOperationId);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "DB error in LinkBookingToOperation. BookingId={BookingId}", bookingId);
                throw new Exception("Error linking booking to OT charges.", ex);
            }
        }
    }
}
