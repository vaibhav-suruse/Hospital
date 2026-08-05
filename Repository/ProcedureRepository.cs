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
    public class ProcedureRepository : IProcedure
    {
        private readonly ILogger<ProcedureRepository> _logger;
        private readonly string ConnectionString;

        public ProcedureRepository(IConfiguration configuration, ILogger<ProcedureRepository> logger)
        {
            ConnectionString = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        private static object OrDbNull(object value)
        {
            if (value == null) return DBNull.Value;
            if (value is string s && string.IsNullOrWhiteSpace(s)) return DBNull.Value;
            return value;
        }

        private static int? ReadNullableInt(DbDataReader reader, string column) =>
            reader[column] == DBNull.Value ? (int?)null : Convert.ToInt32(reader[column]);

        private static DateTime? ReadNullableDate(DbDataReader reader, string column) =>
            reader[column] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader[column]);

        private static bool ReadBool(DbDataReader reader, string column) =>
            reader[column] != DBNull.Value && Convert.ToInt32(reader[column]) == 1;

        #region Lifecycle

        public async Task<ProcedureMaster> CreateProcedureRequest(ProcedureRequestVM model)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_CreateProcedureRequest", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_PatientId", model.PatientId);
            cmd.Parameters.AddWithValue("p_VisitContextType", model.VisitContextType);
            cmd.Parameters.AddWithValue("p_VisitContextId", model.VisitContextId);
            cmd.Parameters.AddWithValue("p_ProcedureCode", OrDbNull(model.ProcedureCode));
            cmd.Parameters.AddWithValue("p_ProcedureName", model.ProcedureName);
            cmd.Parameters.AddWithValue("p_ProcedureCategory", model.ProcedureCategory);
            cmd.Parameters.AddWithValue("p_DepartmentId", OrDbNull(model.DepartmentId));
            cmd.Parameters.AddWithValue("p_Priority", OrDbNull(model.Priority));
            cmd.Parameters.AddWithValue("p_ReasonForProcedure", OrDbNull(model.ReasonForProcedure));
            cmd.Parameters.AddWithValue("p_ClinicalIndication", OrDbNull(model.ClinicalIndication));
            cmd.Parameters.AddWithValue("p_CreatedBy", model.CreatedBy);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            var result = new ProcedureMaster
            {
                PatientId = model.PatientId,
                VisitContextType = model.VisitContextType,
                VisitContextId = model.VisitContextId,
                ProcedureName = model.ProcedureName,
                ProcedureCategory = model.ProcedureCategory,
                Status = "Requested"
            };

            if (await reader.ReadAsync())
            {
                result.ProcedureId = Convert.ToInt32(reader["ProcedureId"]);
                result.ProcedureUID = reader["ProcedureUID"]?.ToString();
            }

            return result;
        }

        public async Task<ProcedureOperationResult> ScheduleProcedure(int procedureId, DateTime plannedDate, int updatedBy)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_ScheduleProcedure", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);
            cmd.Parameters.AddWithValue("p_PlannedDate", plannedDate);
            cmd.Parameters.AddWithValue("p_UpdatedBy", updatedBy);

            await con.OpenAsync();
            var rows = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return new ProcedureOperationResult { Success = rows > 0, Message = rows > 0 ? "Scheduled" : "Schedule failed" };
        }

        public async Task<ProcedureOperationResult> ConfirmProcedure(int procedureId, int updatedBy)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_ConfirmProcedure", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);
            cmd.Parameters.AddWithValue("p_UpdatedBy", updatedBy);

            await con.OpenAsync();
            var rows = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return new ProcedureOperationResult { Success = rows > 0, Message = rows > 0 ? "Confirmed" : "Confirm failed" };
        }

        public async Task<ProcedureOperationResult> StartProcedure(int procedureId, int startedBy)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_StartProcedure", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);
            cmd.Parameters.AddWithValue("p_StartedBy", startedBy);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            var result = new ProcedureOperationResult();
            if (await reader.ReadAsync())
            {
                result.Success = Convert.ToInt32(reader["Success"]) == 1;
                result.Message = reader["Message"]?.ToString();
                result.PendingCount = ReadNullableInt(reader, "PendingCount");
            }
            return result;
        }

        public async Task<ProcedureOperationResult> CompleteProcedure(int procedureId, string outcome, string finalRemarks,
            string doctorConclusion, int? approvedByDoctorId, int completedBy)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_CompleteProcedure", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);
            cmd.Parameters.AddWithValue("p_Outcome", outcome);
            cmd.Parameters.AddWithValue("p_FinalRemarks", OrDbNull(finalRemarks));
            cmd.Parameters.AddWithValue("p_DoctorConclusion", OrDbNull(doctorConclusion));
            cmd.Parameters.AddWithValue("p_ApprovedByDoctorId", OrDbNull(approvedByDoctorId));
            cmd.Parameters.AddWithValue("p_CompletedBy", completedBy);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            var result = new ProcedureOperationResult();
            if (await reader.ReadAsync())
            {
                result.Success = Convert.ToInt32(reader["Success"]) == 1;
                result.Message = reader["Message"]?.ToString();
            }
            return result;
        }

        public async Task<ProcedureOperationResult> UpdateProcedureStatus(int procedureId, string toStatus, string reason, int changedBy)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_UpdateProcedureStatus", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);
            cmd.Parameters.AddWithValue("p_ToStatus", toStatus);
            cmd.Parameters.AddWithValue("p_Reason", OrDbNull(reason));
            cmd.Parameters.AddWithValue("p_ChangedBy", changedBy);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            var result = new ProcedureOperationResult();
            if (await reader.ReadAsync())
            {
                result.Success = Convert.ToInt32(reader["Success"]) == 1;
                result.Message = reader["Message"]?.ToString();
            }
            return result;
        }

        #endregion

        #region Team

        public async Task<int> AddTeamMember(int procedureId, int staffId, string roleInTeam)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_AddProcedureTeamMember", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);
            cmd.Parameters.AddWithValue("p_StaffId", staffId);
            cmd.Parameters.AddWithValue("p_RoleInTeam", roleInTeam);

            await con.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<int> RemoveTeamMember(int teamMemberId)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_RemoveProcedureTeamMember", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_TeamMemberId", teamMemberId);

            await con.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<List<ProcedureTeamMember>> GetTeam(int procedureId)
        {
            var list = new List<ProcedureTeamMember>();
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_GetProcedureTeam", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ProcedureTeamMember
                {
                    TeamMemberId = Convert.ToInt32(reader["TeamMemberId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    StaffId = Convert.ToInt32(reader["StaffId"]),
                    RoleInTeam = reader["RoleInTeam"]?.ToString(),
                    IsActive = ReadBool(reader, "IsActive"),
                    StaffName = reader["StaffName"]?.ToString()
                });
            }
            return list;
        }

        #endregion

        #region Anaesthesia

        public async Task<int> SaveAnaesthesia(ProcedureAnaesthesia model)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_SaveProcedureAnaesthesia", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_ProcedureId", model.ProcedureId);
            cmd.Parameters.AddWithValue("p_AnaesthesiaType", model.AnaesthesiaType);
            cmd.Parameters.AddWithValue("p_AnaesthetistId", OrDbNull(model.AnaesthetistId));
            cmd.Parameters.AddWithValue("p_PreAssessmentNotes", OrDbNull(model.PreAssessmentNotes));

            await con.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        #endregion

        #region Checklist

        public async Task<int> SeedChecklist(int procedureId, string category)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_SeedProcedureChecklist", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);
            cmd.Parameters.AddWithValue("p_Category", category);

            await con.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<int> AddChecklistItem(int procedureId, string itemText, bool isMandatory)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_AddChecklistItem", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);
            cmd.Parameters.AddWithValue("p_ItemText", itemText);
            cmd.Parameters.AddWithValue("p_IsMandatory", isMandatory ? 1 : 0);

            await con.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<int> UpdateChecklistItem(int checklistItemId, bool isChecked, int checkedBy)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_UpdateChecklistItem", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_ChecklistItemId", checklistItemId);
            cmd.Parameters.AddWithValue("p_IsChecked", isChecked ? 1 : 0);
            cmd.Parameters.AddWithValue("p_CheckedBy", checkedBy);

            await con.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<List<ProcedureChecklistItem>> GetChecklist(int procedureId)
        {
            var list = new List<ProcedureChecklistItem>();
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_GetProcedureChecklist", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ProcedureChecklistItem
                {
                    ChecklistItemId = Convert.ToInt32(reader["ChecklistItemId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    ItemText = reader["ItemText"]?.ToString(),
                    IsMandatory = ReadBool(reader, "IsMandatory"),
                    IsChecked = ReadBool(reader, "IsChecked"),
                    CheckedBy = ReadNullableInt(reader, "CheckedBy"),
                    CheckedDate = ReadNullableDate(reader, "CheckedDate")
                });
            }
            return list;
        }

        #endregion

        #region Timeline

        public async Task<ProcedureOperationResult> RecordTimelineEvent(int procedureId, string eventType, DateTime? eventTime, int recordedBy)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_RecordTimelineEvent", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);
            cmd.Parameters.AddWithValue("p_EventType", eventType);
            cmd.Parameters.AddWithValue("p_EventTime", OrDbNull(eventTime));
            cmd.Parameters.AddWithValue("p_RecordedBy", recordedBy);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            var result = new ProcedureOperationResult();
            if (await reader.ReadAsync())
            {
                result.Success = Convert.ToInt32(reader["Success"]) == 1;
                result.Message = reader["Message"]?.ToString();
            }
            return result;
        }

        public async Task<List<ProcedureTimelineEvent>> GetTimeline(int procedureId)
        {
            var list = new List<ProcedureTimelineEvent>();
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_GetProcedureTimeline", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ProcedureTimelineEvent
                {
                    TimelineId = Convert.ToInt32(reader["TimelineId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    EventType = reader["EventType"]?.ToString(),
                    EventTime = Convert.ToDateTime(reader["EventTime"]),
                    RecordedBy = ReadNullableInt(reader, "RecordedBy")
                });
            }
            return list;
        }

        #endregion

        #region Operative Note

        public async Task<ProcedureNoteVersion> SaveOperativeNote(ProcedureNoteVersion model)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_SaveProcedureNote", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_ProcedureId", model.ProcedureId);
            cmd.Parameters.AddWithValue("p_TemplateId", OrDbNull(model.TemplateId));
            cmd.Parameters.AddWithValue("p_ProcedureStepsPerformed", OrDbNull(model.ProcedureStepsPerformed));
            cmd.Parameters.AddWithValue("p_KeyObservations", OrDbNull(model.KeyObservations));
            cmd.Parameters.AddWithValue("p_InstrumentsUsed", OrDbNull(model.InstrumentsUsed));
            cmd.Parameters.AddWithValue("p_BloodLossMl", OrDbNull(model.BloodLossMl));
            cmd.Parameters.AddWithValue("p_UnexpectedEvents", OrDbNull(model.UnexpectedEvents));
            cmd.Parameters.AddWithValue("p_FinalPatientCondition", OrDbNull(model.FinalPatientCondition));
            cmd.Parameters.AddWithValue("p_CreatedBy", OrDbNull(model.CreatedBy));

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                model.NoteId = Convert.ToInt32(reader["NoteId"]);
                model.Version = Convert.ToInt32(reader["Version"]);
                model.IsLatest = true;
            }
            return model;
        }

        public async Task<List<ProcedureNoteVersion>> GetNoteHistory(int procedureId)
        {
            var list = new List<ProcedureNoteVersion>();
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_GetProcedureNoteHistory", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapNote(reader));
            }
            return list;
        }

        private static ProcedureNoteVersion MapNote(DbDataReader reader) => new ProcedureNoteVersion
        {
            NoteId = Convert.ToInt32(reader["NoteId"]),
            ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
            TemplateId = ReadNullableInt(reader, "TemplateId"),
            ProcedureStepsPerformed = reader["ProcedureStepsPerformed"]?.ToString(),
            KeyObservations = reader["KeyObservations"]?.ToString(),
            InstrumentsUsed = reader["InstrumentsUsed"]?.ToString(),
            BloodLossMl = ReadNullableInt(reader, "BloodLossMl"),
            UnexpectedEvents = reader["UnexpectedEvents"]?.ToString(),
            FinalPatientCondition = reader["FinalPatientCondition"]?.ToString(),
            Version = Convert.ToInt32(reader["Version"]),
            IsLatest = ReadBool(reader, "IsLatest"),
            CreatedBy = ReadNullableInt(reader, "CreatedBy"),
            CreatedDate = ReadNullableDate(reader, "CreatedDate")
        };

        #endregion

        #region Complications / Specimens / Post-care

        public async Task<int> SaveComplication(ProcedureComplication model)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_SaveComplication", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_ProcedureId", model.ProcedureId);
            cmd.Parameters.AddWithValue("p_Occurred", model.Occurred ? 1 : 0);
            cmd.Parameters.AddWithValue("p_ComplicationType", OrDbNull(model.ComplicationType));
            cmd.Parameters.AddWithValue("p_Description", OrDbNull(model.Description));
            cmd.Parameters.AddWithValue("p_ActionTaken", OrDbNull(model.ActionTaken));
            cmd.Parameters.AddWithValue("p_Outcome", OrDbNull(model.Outcome));
            cmd.Parameters.AddWithValue("p_RecordedBy", OrDbNull(model.RecordedBy));

            await con.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<int> SaveSpecimen(ProcedureSpecimen model)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_SaveSpecimen", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_ProcedureId", model.ProcedureId);
            cmd.Parameters.AddWithValue("p_Collected", model.Collected ? 1 : 0);
            cmd.Parameters.AddWithValue("p_SpecimenType", OrDbNull(model.SpecimenType));
            cmd.Parameters.AddWithValue("p_Description", OrDbNull(model.Description));
            cmd.Parameters.AddWithValue("p_CollectionTime", OrDbNull(model.CollectionTime));
            cmd.Parameters.AddWithValue("p_SentToLab", model.SentToLab ? 1 : 0);
            cmd.Parameters.AddWithValue("p_Remarks", OrDbNull(model.Remarks));

            await con.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<int> SavePostCare(ProcedurePostCare model)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_SavePostCare", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_ProcedureId", model.ProcedureId);
            cmd.Parameters.AddWithValue("p_PatientConditionAfter", OrDbNull(model.PatientConditionAfter));
            cmd.Parameters.AddWithValue("p_RecoveryStatus", OrDbNull(model.RecoveryStatus));
            cmd.Parameters.AddWithValue("p_ImmediateInstructions", OrDbNull(model.ImmediateInstructions));
            cmd.Parameters.AddWithValue("p_MonitoringInstructions", OrDbNull(model.MonitoringInstructions));
            cmd.Parameters.AddWithValue("p_DoctorRemarks", OrDbNull(model.DoctorRemarks));
            cmd.Parameters.AddWithValue("p_FollowUpInstructions", OrDbNull(model.FollowUpInstructions));
            cmd.Parameters.AddWithValue("p_FollowUpDate", OrDbNull(model.FollowUpDate));

            await con.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        #endregion

        #region Search / Detail / History

        public async Task<List<ProcedureMaster>> GetProcedureList(ProcedureSearchFilter filter)
        {
            var list = new List<ProcedureMaster>();
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_GetProcedureList", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_PatientId", OrDbNull(filter.PatientId));
            cmd.Parameters.AddWithValue("p_DoctorId", OrDbNull(filter.DoctorId));
            cmd.Parameters.AddWithValue("p_DateFrom", OrDbNull(filter.DateFrom?.Date));
            cmd.Parameters.AddWithValue("p_DateTo", OrDbNull(filter.DateTo?.Date));
            cmd.Parameters.AddWithValue("p_Category", OrDbNull(filter.Category));
            cmd.Parameters.AddWithValue("p_Status", OrDbNull(filter.Status));
            cmd.Parameters.AddWithValue("p_DepartmentId", OrDbNull(filter.DepartmentId));

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapMaster(reader));
            }
            return list;
        }

        private static ProcedureMaster MapMaster(DbDataReader reader)
        {
            var m = new ProcedureMaster
            {
                ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                ProcedureUID = reader["ProcedureUID"]?.ToString(),
                PatientId = Convert.ToInt32(reader["PatientId"]),
                VisitContextType = reader["VisitContextType"]?.ToString(),
                VisitContextId = Convert.ToInt32(reader["VisitContextId"]),
                ProcedureCode = reader["ProcedureCode"]?.ToString(),
                ProcedureName = reader["ProcedureName"]?.ToString(),
                ProcedureCategory = reader["ProcedureCategory"]?.ToString(),
                DepartmentId = ReadNullableInt(reader, "DepartmentId"),
                Priority = reader["Priority"]?.ToString(),
                Status = reader["Status"]?.ToString(),
                PlannedDate = ReadNullableDate(reader, "PlannedDate"),
                ActualDate = ReadNullableDate(reader, "ActualDate"),
                StartTime = ReadNullableDate(reader, "StartTime"),
                EndTime = ReadNullableDate(reader, "EndTime"),
                DurationMinutes = ReadNullableInt(reader, "DurationMinutes"),
                IsActive = ReadBool(reader, "IsActive"),
                CreatedBy = ReadNullableInt(reader, "CreatedBy"),
                CreatedDate = ReadNullableDate(reader, "CreatedDate"),
                UpdatedBy = ReadNullableInt(reader, "UpdatedBy"),
                UpdatedDate = ReadNullableDate(reader, "UpdatedDate")
            };

            // Optional joined columns present only on list/history queries
            try { m.PrimaryDoctorId = ReadNullableInt(reader, "PrimaryDoctorId"); } catch (IndexOutOfRangeException) { }
            try { m.PrimaryDoctorName = reader["PrimaryDoctorName"]?.ToString(); } catch (IndexOutOfRangeException) { }

            return m;
        }

        public async Task<ProcedureFullDetailVM> GetProcedureFullDetail(int procedureId)
        {
            var vm = new ProcedureFullDetailVM();

            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_GetProcedureFullDetail", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            // 1. Master
            if (await reader.ReadAsync())
                vm.Master = MapMaster(reader);

            // 2. Details
            await reader.NextResultAsync();
            if (await reader.ReadAsync())
            {
                vm.Details = new ProcedureDetails
                {
                    DetailId = Convert.ToInt32(reader["DetailId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    ReasonForProcedure = reader["ReasonForProcedure"]?.ToString(),
                    ClinicalIndication = reader["ClinicalIndication"]?.ToString(),
                    PreProcedureDiagnosis = reader["PreProcedureDiagnosis"]?.ToString(),
                    PostProcedureDiagnosis = reader["PostProcedureDiagnosis"]?.ToString(),
                    ProcedureDescription = reader["ProcedureDescription"]?.ToString(),
                    ProcedureObjective = reader["ProcedureObjective"]?.ToString(),
                    ProcedureFindings = reader["ProcedureFindings"]?.ToString(),
                    FinalOutcome = reader["FinalOutcome"]?.ToString()
                };
            }

            // 3. Team
            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                vm.Team.Add(new ProcedureTeamMember
                {
                    TeamMemberId = Convert.ToInt32(reader["TeamMemberId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    StaffId = Convert.ToInt32(reader["StaffId"]),
                    RoleInTeam = reader["RoleInTeam"]?.ToString(),
                    IsActive = ReadBool(reader, "IsActive"),
                    StaffName = reader["StaffName"]?.ToString()
                });
            }

            // 4. Anaesthesia
            await reader.NextResultAsync();
            if (await reader.ReadAsync())
            {
                vm.Anaesthesia = new ProcedureAnaesthesia
                {
                    AnaesthesiaId = Convert.ToInt32(reader["AnaesthesiaId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    AnaesthesiaType = reader["AnaesthesiaType"]?.ToString(),
                    AnaesthetistId = ReadNullableInt(reader, "AnaesthetistId"),
                    StartTime = ReadNullableDate(reader, "StartTime"),
                    EndTime = ReadNullableDate(reader, "EndTime"),
                    PreAssessmentNotes = reader["PreAssessmentNotes"]?.ToString(),
                    Complications = reader["Complications"]?.ToString(),
                    Remarks = reader["Remarks"]?.ToString()
                };
            }

            // 5. Checklist
            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                vm.Checklist.Add(new ProcedureChecklistItem
                {
                    ChecklistItemId = Convert.ToInt32(reader["ChecklistItemId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    ItemText = reader["ItemText"]?.ToString(),
                    IsMandatory = ReadBool(reader, "IsMandatory"),
                    IsChecked = ReadBool(reader, "IsChecked"),
                    CheckedBy = ReadNullableInt(reader, "CheckedBy"),
                    CheckedDate = ReadNullableDate(reader, "CheckedDate")
                });
            }

            // 6. Timeline
            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                vm.Timeline.Add(new ProcedureTimelineEvent
                {
                    TimelineId = Convert.ToInt32(reader["TimelineId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    EventType = reader["EventType"]?.ToString(),
                    EventTime = Convert.ToDateTime(reader["EventTime"]),
                    RecordedBy = ReadNullableInt(reader, "RecordedBy")
                });
            }

            // 7. Latest note
            await reader.NextResultAsync();
            if (await reader.ReadAsync())
                vm.LatestNote = MapNote(reader);

            // 8. Complications
            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                vm.Complications.Add(new ProcedureComplication
                {
                    ComplicationId = Convert.ToInt32(reader["ComplicationId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    Occurred = ReadBool(reader, "Occurred"),
                    ComplicationType = reader["ComplicationType"]?.ToString(),
                    Description = reader["Description"]?.ToString(),
                    ActionTaken = reader["ActionTaken"]?.ToString(),
                    Outcome = reader["Outcome"]?.ToString(),
                    RecordedBy = ReadNullableInt(reader, "RecordedBy"),
                    RecordedDate = ReadNullableDate(reader, "RecordedDate")
                });
            }

            // 9. Specimens
            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                vm.Specimens.Add(new ProcedureSpecimen
                {
                    SpecimenId = Convert.ToInt32(reader["SpecimenId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    Collected = ReadBool(reader, "Collected"),
                    SpecimenType = reader["SpecimenType"]?.ToString(),
                    Description = reader["Description"]?.ToString(),
                    CollectionTime = ReadNullableDate(reader, "CollectionTime"),
                    SentToLab = ReadBool(reader, "SentToLab"),
                    Remarks = reader["Remarks"]?.ToString()
                });
            }

            // 10. Post-care
            await reader.NextResultAsync();
            if (await reader.ReadAsync())
            {
                vm.PostCare = new ProcedurePostCare
                {
                    PostCareId = Convert.ToInt32(reader["PostCareId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    PatientConditionAfter = reader["PatientConditionAfter"]?.ToString(),
                    RecoveryStatus = reader["RecoveryStatus"]?.ToString(),
                    ImmediateInstructions = reader["ImmediateInstructions"]?.ToString(),
                    MonitoringInstructions = reader["MonitoringInstructions"]?.ToString(),
                    DoctorRemarks = reader["DoctorRemarks"]?.ToString(),
                    FollowUpInstructions = reader["FollowUpInstructions"]?.ToString(),
                    FollowUpDate = ReadNullableDate(reader, "FollowUpDate")
                };
            }

            // 11. Outcome
            await reader.NextResultAsync();
            if (await reader.ReadAsync())
            {
                vm.Outcome = new ProcedureOutcome
                {
                    OutcomeId = Convert.ToInt32(reader["OutcomeId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    Outcome = reader["Outcome"]?.ToString(),
                    FinalRemarks = reader["FinalRemarks"]?.ToString(),
                    DoctorConclusion = reader["DoctorConclusion"]?.ToString(),
                    ApprovedByDoctorId = ReadNullableInt(reader, "ApprovedByDoctorId"),
                    DigitalSignatureRef = reader["DigitalSignatureRef"]?.ToString(),
                    VerificationDate = ReadNullableDate(reader, "VerificationDate"),
                    VerifiedBy = ReadNullableInt(reader, "VerifiedBy")
                };
            }

            // 12. Attachments
            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                vm.Attachments.Add(new ProcedureAttachment
                {
                    AttachmentId = Convert.ToInt32(reader["AttachmentId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    FileType = reader["FileType"]?.ToString(),
                    FilePath = reader["FilePath"]?.ToString(),
                    UploadDate = ReadNullableDate(reader, "UploadDate"),
                    UploadedBy = ReadNullableInt(reader, "UploadedBy"),
                    Description = reader["Description"]?.ToString()
                });
            }

            // 13. Status history
            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                vm.StatusHistory.Add(new ProcedureStatusHistory
                {
                    HistoryId = Convert.ToInt32(reader["HistoryId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    FromStatus = reader["FromStatus"]?.ToString(),
                    ToStatus = reader["ToStatus"]?.ToString(),
                    Reason = reader["Reason"]?.ToString(),
                    ChangedBy = ReadNullableInt(reader, "ChangedBy"),
                    ChangedDate = ReadNullableDate(reader, "ChangedDate")
                });
            }

            return vm;
        }

        public async Task<List<ProcedureMaster>> GetPatientProcedureHistory(int patientId)
        {
            var list = new List<ProcedureMaster>();
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_GetProcedureHistory", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_PatientId", patientId);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ProcedureMaster
                {
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    ProcedureUID = reader["ProcedureUID"]?.ToString(),
                    ProcedureName = reader["ProcedureName"]?.ToString(),
                    ProcedureCategory = reader["ProcedureCategory"]?.ToString(),
                    Status = reader["Status"]?.ToString(),
                    ActualDate = ReadNullableDate(reader, "ActualDate"),
                    PlannedDate = ReadNullableDate(reader, "PlannedDate"),
                    PrimaryDoctorName = reader["PrimaryDoctorName"]?.ToString()
                });
            }
            return list;
        }

        #endregion

        #region Templates

        public async Task<int> CreateTemplate(ProcedureTemplate model)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_CreateTemplate", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_TemplateName", model.TemplateName);
            cmd.Parameters.AddWithValue("p_Category", model.Category);
            cmd.Parameters.AddWithValue("p_DepartmentId", OrDbNull(model.DepartmentId));
            cmd.Parameters.AddWithValue("p_CreatedBy", OrDbNull(model.CreatedBy));

            await con.OpenAsync();
            var templateId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            if (model.Steps != null)
            {
                int order = 1;
                foreach (var step in model.Steps)
                {
                    await AddTemplateStep(templateId, order++, step.StepText);
                }
            }

            return templateId;
        }

        public async Task<int> AddTemplateStep(int templateId, int stepOrder, string stepText)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_AddTemplateStep", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_TemplateId", templateId);
            cmd.Parameters.AddWithValue("p_StepOrder", stepOrder);
            cmd.Parameters.AddWithValue("p_StepText", stepText);

            await con.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<List<ProcedureTemplate>> GetTemplates(string category, int? departmentId)
        {
            var list = new List<ProcedureTemplate>();
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_GetProcedureTemplates", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_Category", OrDbNull(category));
            cmd.Parameters.AddWithValue("p_DepartmentId", OrDbNull(departmentId));

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ProcedureTemplate
                {
                    TemplateId = Convert.ToInt32(reader["TemplateId"]),
                    TemplateName = reader["TemplateName"]?.ToString(),
                    Category = reader["Category"]?.ToString(),
                    DepartmentId = ReadNullableInt(reader, "DepartmentId"),
                    IsActive = ReadBool(reader, "IsActive"),
                    CreatedBy = ReadNullableInt(reader, "CreatedBy"),
                    CreatedDate = ReadNullableDate(reader, "CreatedDate"),
                    UsageCount = Convert.ToInt32(reader["UsageCount"])
                });
            }
            return list;
        }

        public async Task<ProcedureTemplate> GetTemplateById(int templateId)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_GetTemplateById", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_TemplateId", templateId);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            ProcedureTemplate template = null;
            if (await reader.ReadAsync())
            {
                template = new ProcedureTemplate
                {
                    TemplateId = Convert.ToInt32(reader["TemplateId"]),
                    TemplateName = reader["TemplateName"]?.ToString(),
                    Category = reader["Category"]?.ToString(),
                    DepartmentId = ReadNullableInt(reader, "DepartmentId"),
                    IsActive = ReadBool(reader, "IsActive"),
                    CreatedBy = ReadNullableInt(reader, "CreatedBy"),
                    CreatedDate = ReadNullableDate(reader, "CreatedDate")
                };
            }

            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                template?.Steps.Add(new ProcedureTemplateStep
                {
                    StepId = Convert.ToInt32(reader["StepId"]),
                    TemplateId = Convert.ToInt32(reader["TemplateId"]),
                    StepOrder = Convert.ToInt32(reader["StepOrder"]),
                    StepText = reader["StepText"]?.ToString()
                });
            }

            return template;
        }

        public async Task<int> DeleteTemplate(int templateId)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_DeleteTemplate", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_TemplateId", templateId);

            await con.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        #endregion

        #region Attachments

        public async Task<int> AddAttachment(ProcedureAttachment model)
        {
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_AddProcedureAttachment", con) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("p_ProcedureId", model.ProcedureId);
            cmd.Parameters.AddWithValue("p_FileType", OrDbNull(model.FileType));
            cmd.Parameters.AddWithValue("p_FilePath", OrDbNull(model.FilePath));
            cmd.Parameters.AddWithValue("p_UploadedBy", OrDbNull(model.UploadedBy));
            cmd.Parameters.AddWithValue("p_Description", OrDbNull(model.Description));

            await con.OpenAsync();
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<List<ProcedureAttachment>> GetAttachments(int procedureId)
        {
            var list = new List<ProcedureAttachment>();
            using var con = new MySqlConnection(ConnectionString);
            using var cmd = new MySqlCommand("sp_GetProcedureAttachments", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_ProcedureId", procedureId);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ProcedureAttachment
                {
                    AttachmentId = Convert.ToInt32(reader["AttachmentId"]),
                    ProcedureId = Convert.ToInt32(reader["ProcedureId"]),
                    FileType = reader["FileType"]?.ToString(),
                    FilePath = reader["FilePath"]?.ToString(),
                    UploadDate = ReadNullableDate(reader, "UploadDate"),
                    UploadedBy = ReadNullableInt(reader, "UploadedBy"),
                    Description = reader["Description"]?.ToString()
                });
            }
            return list;
        }

        #endregion

        #region Multi-hospital data isolation helpers (NEW)

        public async Task<int?> GetProcedureOwnerPatientIdAsync(int procedureId)
        {
            using (var con = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand("SELECT PatientId FROM procedure_master WHERE ProcedureId = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", procedureId);
                await con.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
            }
        }

        public async Task<int?> GetProcedureIdByChecklistItemAsync(int checklistItemId)
        {
            using (var con = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand("SELECT ProcedureId FROM procedure_checklist WHERE ChecklistItemId = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", checklistItemId);
                await con.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
            }
        }

        public async Task<int?> GetProcedureIdByTeamMemberAsync(int teamMemberId)
        {
            using (var con = new MySqlConnection(ConnectionString))
            using (var cmd = new MySqlCommand("SELECT ProcedureId FROM procedure_team WHERE TeamMemberId = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", teamMemberId);
                await con.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
            }
        }

        #endregion
    }
}
