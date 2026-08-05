using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class AdmissionNoteRepository : IAdmissionNotes

    {
        private readonly string _connectionString;

        public AdmissionNoteRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // ── Audit Trail helper (best-effort: a logging failure never blocks the save) ──
        private async Task LogAuditAsync(string entityType, int? entityId, int? ipdId, int? patientId,
            string action, string description, int changedBy, string ipAddress, string deviceInfo)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_admission_audit_log_insert", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@p_entity_type", entityType);
                cmd.Parameters.AddWithValue("@p_entity_id", entityId.HasValue ? (object)entityId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@p_ipd_id", ipdId.HasValue ? (object)ipdId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@p_patient_id", patientId.HasValue ? (object)patientId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@p_action", action);
                cmd.Parameters.AddWithValue("@p_description", string.IsNullOrEmpty(description) ? (object)DBNull.Value : description);
                cmd.Parameters.AddWithValue("@p_changed_by", changedBy);
                cmd.Parameters.AddWithValue("@p_ip_address", string.IsNullOrEmpty(ipAddress) ? (object)DBNull.Value : ipAddress);
                cmd.Parameters.AddWithValue("@p_device_info", string.IsNullOrEmpty(deviceInfo) ? (object)DBNull.Value : deviceInfo);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Audit logging must never break the primary save operation.
            }
        }

        // -----------------------------------------------------------
        // GET current admission notes
        // -----------------------------------------------------------
        public async Task<AdmissionNotesModel> GetAdmissionNotesAsync(int ipdId)
        {
            AdmissionNotesModel model = null;

            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_admission_notes_get", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@p_ipd_id", ipdId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                model = new AdmissionNotesModel
                {
                    AdmissionNoteId = Convert.ToInt32(reader["admission_note_id"]),
                    IpdId = Convert.ToInt32(reader["ipd_id"]),
                    PatientId = Convert.ToInt32(reader["patient_id"]),
                    CurrentComplaint = reader["current_complaint"] as string,
                    Examination = reader["examination"] as string
                };
            }

            return model;
        }

        // -----------------------------------------------------------
        // GET templates (with search)
        // -----------------------------------------------------------
        public async Task<List<AdmissionNoteTemplateModel>> GetTemplatesAsync(string templateType, string search, int? hospitalId)
        {
            var list = new List<AdmissionNoteTemplateModel>();

            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_admission_template_get", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@p_template_type", templateType);
            cmd.Parameters.AddWithValue("@p_hospital_id", hospitalId.HasValue ? (object)hospitalId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@p_search", string.IsNullOrEmpty(search) ? (object)DBNull.Value : search);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new AdmissionNoteTemplateModel
                {
                    TemplateId = Convert.ToInt32(reader["template_id"]),
                    TemplateType = templateType,
                    TemplateName = reader["template_name"] as string,
                    TemplateText = reader["template_text"] as string
                });
            }

            return list;
        }

        // -----------------------------------------------------------
        // SAVE (upsert) admission notes
        // -----------------------------------------------------------
        public async Task SaveAdmissionNotesAsync(SaveNotesRequest request, int createdBy)
        {
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_admission_notes_save", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@p_ipd_id", request.IpdId);
            cmd.Parameters.AddWithValue("@p_patient_id", request.PatientId);
            cmd.Parameters.AddWithValue("@p_current_complaint",
                string.IsNullOrEmpty(request.CurrentComplaint) ? (object)DBNull.Value : request.CurrentComplaint);
            cmd.Parameters.AddWithValue("@p_examination",
                string.IsNullOrEmpty(request.Examination) ? (object)DBNull.Value : request.Examination);
            cmd.Parameters.AddWithValue("@p_created_by", createdBy);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // -----------------------------------------------------------
        // LOAD PREVIOUS - same patient cha last admission cha record
        // -----------------------------------------------------------
        public async Task<AdmissionNotesModel> LoadPreviousAsync(LoadPreviousRequest request)
        {
            AdmissionNotesModel model = null;

            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_admission_notes_load_previous", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@p_patient_id", request.PatientId);
            cmd.Parameters.AddWithValue("@p_ipd_id", request.IpdId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                model = new AdmissionNotesModel
                {
                    CurrentComplaint = reader["current_complaint"] as string,
                    Examination = reader["examination"] as string
                };
            }

            return model;
        }

        // -----------------------------------------------------------
        // CLEAR field(s)
        // -----------------------------------------------------------
        public async Task ClearNotesAsync(ClearNotesRequest request)
        {
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_admission_notes_clear", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@p_ipd_id", request.IpdId);
            cmd.Parameters.AddWithValue("@p_field", request.Field);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // -----------------------------------------------------------
        // Template ADD / UPDATE / DELETE
        // -----------------------------------------------------------
        public async Task SaveTemplateAsync(TemplateSaveRequest request, int? hospitalId, int createdBy)
        {
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_admission_template_save", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@p_action", request.Action);
            cmd.Parameters.AddWithValue("@p_template_id", request.TemplateId);
            cmd.Parameters.AddWithValue("@p_template_type", request.TemplateType);
            cmd.Parameters.AddWithValue("@p_template_name",
                string.IsNullOrEmpty(request.TemplateName) ? (object)DBNull.Value : request.TemplateName);
            cmd.Parameters.AddWithValue("@p_template_text",
                string.IsNullOrEmpty(request.TemplateText) ? (object)DBNull.Value : request.TemplateText);
            cmd.Parameters.AddWithValue("@p_hospital_id", hospitalId.HasValue ? hospitalId.Value : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@p_created_by", createdBy);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // ══════════════════════════════════════════════════════
        // STRUCTURED CHIEF COMPLAINTS
        // ══════════════════════════════════════════════════════
        public async Task<List<ChiefComplaintModel>> GetChiefComplaintsAsync(int ipdId)
        {
            var list = new List<ChiefComplaintModel>();
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_admission_chief_complaint_get", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_ipd_id", ipdId);
            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new ChiefComplaintModel
                {
                    ComplaintId = Convert.ToInt32(r["complaint_id"]),
                    IpdId = Convert.ToInt32(r["ipd_id"]),
                    PatientId = Convert.ToInt32(r["patient_id"]),
                    ComplaintName = r["complaint_name"] as string,
                    DurationValue = r["duration_value"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["duration_value"]),
                    DurationUnit = r["duration_unit"] as string,
                    Severity = r["severity"] as string,
                    Onset = r["onset"] as string,
                    Frequency = r["frequency"] as string,
                    BodyLocation = r["body_location"] as string,
                    PainScore = r["pain_score"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["pain_score"]),
                    Remarks = r["remarks"] as string,
                    CreatedDate = Convert.ToDateTime(r["created_date"])
                });
            }
            return list;
        }

        public async Task SaveChiefComplaintAsync(SaveChiefComplaintRequest req, int createdBy, string ipAddress = null, string deviceInfo = null)
        {
            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_admission_chief_complaint_save", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@p_action", req.Action);
                cmd.Parameters.AddWithValue("@p_complaint_id", req.ComplaintId);
                cmd.Parameters.AddWithValue("@p_ipd_id", req.IpdId);
                cmd.Parameters.AddWithValue("@p_patient_id", req.PatientId);
                cmd.Parameters.AddWithValue("@p_complaint_name", string.IsNullOrEmpty(req.ComplaintName) ? (object)DBNull.Value : req.ComplaintName);
                cmd.Parameters.AddWithValue("@p_duration_value", req.DurationValue.HasValue ? req.DurationValue.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p_duration_unit", string.IsNullOrEmpty(req.DurationUnit) ? (object)DBNull.Value : req.DurationUnit);
                cmd.Parameters.AddWithValue("@p_severity", string.IsNullOrEmpty(req.Severity) ? (object)DBNull.Value : req.Severity);
                cmd.Parameters.AddWithValue("@p_onset", string.IsNullOrEmpty(req.Onset) ? (object)DBNull.Value : req.Onset);
                cmd.Parameters.AddWithValue("@p_frequency", string.IsNullOrEmpty(req.Frequency) ? (object)DBNull.Value : req.Frequency);
                cmd.Parameters.AddWithValue("@p_body_location", string.IsNullOrEmpty(req.BodyLocation) ? (object)DBNull.Value : req.BodyLocation);
                cmd.Parameters.AddWithValue("@p_pain_score", req.PainScore.HasValue ? req.PainScore.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p_remarks", string.IsNullOrEmpty(req.Remarks) ? (object)DBNull.Value : req.Remarks);
                cmd.Parameters.AddWithValue("@p_created_by", createdBy);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            await LogAuditAsync("COMPLAINT", req.ComplaintId, req.IpdId, req.PatientId, req.Action,
                $"Chief complaint '{req.ComplaintName}' {req.Action?.ToLower()}ed", createdBy, ipAddress, deviceInfo);
        }

        public async Task<List<MasterComplaintModel>> SearchMasterComplaintsAsync(string search, int? hospitalId)
        {
            var list = new List<MasterComplaintModel>();
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_master_complaint_search", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_search", string.IsNullOrEmpty(search) ? (object)DBNull.Value : search);
            cmd.Parameters.AddWithValue("@p_hospital_id", hospitalId.HasValue ? (object)hospitalId.Value : DBNull.Value);
            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new MasterComplaintModel
                {
                    ComplaintMasterId = Convert.ToInt32(r["complaint_master_id"]),
                    ComplaintName = r["complaint_name"] as string,
                    Category = r["category"] as string
                });
            }
            return list;
        }

        public async Task<int?> GetComplaintOwnerIpdIdAsync(int complaintId)
        {
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("SELECT ipd_id FROM admission_chief_complaints WHERE complaint_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", complaintId);
            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
        }

        // ══════════════════════════════════════════════════════
        // ALLERGIES
        // ══════════════════════════════════════════════════════
        public async Task<List<PatientAllergyModel>> GetAllergiesAsync(int patientId, int ipdId)
        {
            var list = new List<PatientAllergyModel>();
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_patient_allergies_get", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_patient_id", patientId);
            cmd.Parameters.AddWithValue("@p_ipd_id", ipdId);
            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new PatientAllergyModel
                {
                    AllergyId = Convert.ToInt32(r["allergy_id"]),
                    PatientId = Convert.ToInt32(r["patient_id"]),

                    AllergyType = r["allergy_category"] as string ?? "Other",
                    AllergyName = r["allergy_name"] as string,
                    Reaction = r["reaction"] as string,
                    Severity = r["severity"] as string,
                    DateIdentified = r["date_identified"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(r["date_identified"]),
                    VerifiedBy = r["verified_by"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["verified_by"]),
                    ClinicalStatus = r["clinical_status"] as string,
                    Remarks = r["remarks"] as string,
                    RecordedDate = r["recorded_date"] == DBNull.Value
                                   ? DateTime.Now
                                   : Convert.ToDateTime(r["recorded_date"]),
                    IpdId = ipdId
                });
            }
            return list;
        }

        public async Task SaveAllergyAsync(SaveAllergyRequest req, int recordedBy, string ipAddress = null, string deviceInfo = null)
        {
            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_patient_allergy_save", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@p_action", req.Action);
                cmd.Parameters.AddWithValue("@p_allergy_id", req.AllergyId);
                cmd.Parameters.AddWithValue("@p_patient_id", req.PatientId);
                cmd.Parameters.AddWithValue("@p_ipd_id", req.IpdId);
                cmd.Parameters.AddWithValue("@p_allergy_category", string.IsNullOrEmpty(req.AllergyType) ? (object)DBNull.Value : req.AllergyType);
                cmd.Parameters.AddWithValue("@p_allergy_name", string.IsNullOrEmpty(req.AllergyName) ? (object)DBNull.Value : req.AllergyName);
                cmd.Parameters.AddWithValue("@p_reaction", string.IsNullOrEmpty(req.Reaction) ? (object)DBNull.Value : req.Reaction);
                cmd.Parameters.AddWithValue("@p_severity", string.IsNullOrEmpty(req.Severity) ? (object)DBNull.Value : req.Severity);
                cmd.Parameters.AddWithValue("@p_date_identified", req.DateIdentified.HasValue ? (object)req.DateIdentified.Value.Date : DBNull.Value);
                cmd.Parameters.AddWithValue("@p_verified_by", req.VerifiedBy.HasValue ? (object)req.VerifiedBy.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@p_clinical_status", string.IsNullOrEmpty(req.ClinicalStatus) ? "Active" : req.ClinicalStatus);
                cmd.Parameters.AddWithValue("@p_remarks", string.IsNullOrEmpty(req.Remarks) ? (object)DBNull.Value : req.Remarks);
                cmd.Parameters.AddWithValue("@p_hospital_id", req.HospitalId);
                cmd.Parameters.AddWithValue("@p_subhospital_id", req.SubHospitalId.HasValue ? (object)req.SubHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@p_recorded_by", recordedBy);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            await LogAuditAsync("ALLERGY", req.AllergyId, req.IpdId, req.PatientId, req.Action,
                $"Allergy '{req.AllergyName}' ({req.Severity}) {req.Action?.ToLower()}ed", recordedBy, ipAddress, deviceInfo);
        }

        public async Task<int?> GetAllergyOwnerIpdIdAsync(int allergyId)
        {
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("SELECT IpdId FROM patient_allergies WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@id", allergyId);
            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
        }

        // ══════════════════════════════════════════════════════
        // MEDICAL HISTORY
        // ══════════════════════════════════════════════════════
        public async Task<List<PatientMedicalHistoryModel>> GetMedicalHistoryAsync(int patientId)
        {
            var list = new List<PatientMedicalHistoryModel>();
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_patient_medical_history_get", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_patient_id", patientId);
            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(new PatientMedicalHistoryModel
                {
                    HistoryId = Convert.ToInt32(r["history_id"]),
                    HistoryType = r["history_type"] as string,
                    Description = r["description"] as string,
                    Medication = r["medication"] as string,
                    SinceYear = r["since_year"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["since_year"]),
                    Status = r["status"] as string,
                    ControlStatus = r["control_status"] as string,
                    LastFollowupDate = r["last_followup_date"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(r["last_followup_date"]),
                    Remarks = r["remarks"] as string,
                    RecordedDate = Convert.ToDateTime(r["recorded_date"])
                });
            return list;
        }

        public async Task SaveMedicalHistoryAsync(SaveMedicalHistoryRequest req, int recordedBy, string ipAddress = null, string deviceInfo = null)
        {
            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_patient_medical_history_save", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@p_action", req.Action);
                cmd.Parameters.AddWithValue("@p_history_id", req.HistoryId);
                cmd.Parameters.AddWithValue("@p_patient_id", req.PatientId);
                cmd.Parameters.AddWithValue("@p_history_type", string.IsNullOrEmpty(req.HistoryType) ? (object)DBNull.Value : req.HistoryType);
                cmd.Parameters.AddWithValue("@p_description", string.IsNullOrEmpty(req.Description) ? (object)DBNull.Value : req.Description);
                cmd.Parameters.AddWithValue("@p_medication", string.IsNullOrEmpty(req.Medication) ? (object)DBNull.Value : req.Medication);
                cmd.Parameters.AddWithValue("@p_since_year", req.SinceYear.HasValue ? req.SinceYear.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p_status", string.IsNullOrEmpty(req.Status) ? (object)DBNull.Value : req.Status);
                cmd.Parameters.AddWithValue("@p_control_status", string.IsNullOrEmpty(req.ControlStatus) ? (object)DBNull.Value : req.ControlStatus);
                cmd.Parameters.AddWithValue("@p_last_followup_date", req.LastFollowupDate.HasValue ? (object)req.LastFollowupDate.Value.Date : DBNull.Value);
                cmd.Parameters.AddWithValue("@p_remarks", string.IsNullOrEmpty(req.Remarks) ? (object)DBNull.Value : req.Remarks);
                cmd.Parameters.AddWithValue("@p_recorded_by", recordedBy);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            await LogAuditAsync("HISTORY", req.HistoryId, null, req.PatientId, req.Action,
                $"Medical history '{req.HistoryType}' {req.Action?.ToLower()}ed", recordedBy, ipAddress, deviceInfo);
        }

        public async Task<int?> GetHistoryOwnerPatientIdAsync(int historyId)
        {
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("SELECT patient_id FROM patient_medical_history WHERE history_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", historyId);
            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
        }

        // ══════════════════════════════════════════════════════
        // REMARKS
        // ══════════════════════════════════════════════════════
        public async Task<AdmissionRemarksModel> GetRemarksAsync(int ipdId)
        {
            AdmissionRemarksModel model = null;
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_admission_remarks_get", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_ipd_id", ipdId);
            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
                model = new AdmissionRemarksModel
                {
                    RemarkId = Convert.ToInt32(r["remark_id"]),
                    IpdId = Convert.ToInt32(r["ipd_id"]),
                    PatientId = Convert.ToInt32(r["patient_id"]),
                    Remarks = r["remarks"] as string
                };
            return model;
        }

        public async Task SaveRemarksAsync(SaveRemarksRequest req, int createdBy)
        {
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_admission_remarks_save", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_ipd_id", req.IpdId);
            cmd.Parameters.AddWithValue("@p_patient_id", req.PatientId);
            cmd.Parameters.AddWithValue("@p_remarks", string.IsNullOrEmpty(req.Remarks) ? (object)DBNull.Value : req.Remarks);
            cmd.Parameters.AddWithValue("@p_created_by", createdBy);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ClearRemarksAsync(int ipdId)
        {
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_admission_remarks_clear", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_ipd_id", ipdId);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<RemarksTemplateModel>> GetRemarksTemplatesAsync(string search, int? hospitalId)
        {
            var list = new List<RemarksTemplateModel>();
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_remarks_template_get", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_hospital_id", hospitalId.HasValue ? (object)hospitalId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@p_search", string.IsNullOrEmpty(search) ? (object)DBNull.Value : search);
            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(new RemarksTemplateModel
                {
                    TemplateId = Convert.ToInt32(r["template_id"]),
                    TemplateName = r["template_name"] as string,
                    TemplateText = r["template_text"] as string
                });
            return list;
        }

        public async Task SaveRemarksTemplateAsync(RemarksTemplateSaveRequest req, int? hospitalId, int createdBy)
        {
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_remarks_template_save", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_action", req.Action);
            cmd.Parameters.AddWithValue("@p_template_id", req.TemplateId);
            cmd.Parameters.AddWithValue("@p_template_name", string.IsNullOrEmpty(req.TemplateName) ? (object)DBNull.Value : req.TemplateName);
            cmd.Parameters.AddWithValue("@p_template_text", string.IsNullOrEmpty(req.TemplateText) ? (object)DBNull.Value : req.TemplateText);
            cmd.Parameters.AddWithValue("@p_hospital_id", hospitalId.HasValue ? hospitalId.Value : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@p_created_by", createdBy);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // ══════════════════════════════════════════════════════
        // STRUCTURED HPI
        // ══════════════════════════════════════════════════════
        public async Task<HpiModel> GetHpiAsync(int ipdId)
        {
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_admission_hpi_get", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_ipd_id", ipdId);
            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new HpiModel
                {
                    HpiId = Convert.ToInt32(r["hpi_id"]),
                    IpdId = Convert.ToInt32(r["ipd_id"]),
                    PatientId = Convert.ToInt32(r["patient_id"]),
                    SymptomsStartedOn = r["symptoms_started_on"] as string,
                    Progression = r["progression"] as string,
                    AssociatedSymptoms = r["associated_symptoms"] as string,
                    AggravatingFactors = r["aggravating_factors"] as string,
                    RelievingFactors = r["relieving_factors"] as string,
                    PreviousTreatment = r["previous_treatment"] as string,
                    PreviousConsultation = r["previous_consultation"] as string,
                    PreviousHospitalization = r["previous_hospitalization"] as string,
                    TimelineNotes = r["timeline_notes"] as string
                };
            }
            return new HpiModel { IpdId = ipdId };
        }

        public async Task SaveHpiAsync(SaveHpiRequest req, int createdBy, string ipAddress = null, string deviceInfo = null)
        {
            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_admission_hpi_save", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@p_ipd_id", req.IpdId);
                cmd.Parameters.AddWithValue("@p_patient_id", req.PatientId);
                cmd.Parameters.AddWithValue("@p_symptoms_started_on", string.IsNullOrEmpty(req.SymptomsStartedOn) ? (object)DBNull.Value : req.SymptomsStartedOn);
                cmd.Parameters.AddWithValue("@p_progression", string.IsNullOrEmpty(req.Progression) ? (object)DBNull.Value : req.Progression);
                cmd.Parameters.AddWithValue("@p_associated_symptoms", string.IsNullOrEmpty(req.AssociatedSymptoms) ? (object)DBNull.Value : req.AssociatedSymptoms);
                cmd.Parameters.AddWithValue("@p_aggravating_factors", string.IsNullOrEmpty(req.AggravatingFactors) ? (object)DBNull.Value : req.AggravatingFactors);
                cmd.Parameters.AddWithValue("@p_relieving_factors", string.IsNullOrEmpty(req.RelievingFactors) ? (object)DBNull.Value : req.RelievingFactors);
                cmd.Parameters.AddWithValue("@p_previous_treatment", string.IsNullOrEmpty(req.PreviousTreatment) ? (object)DBNull.Value : req.PreviousTreatment);
                cmd.Parameters.AddWithValue("@p_previous_consultation", string.IsNullOrEmpty(req.PreviousConsultation) ? (object)DBNull.Value : req.PreviousConsultation);
                cmd.Parameters.AddWithValue("@p_previous_hospitalization", string.IsNullOrEmpty(req.PreviousHospitalization) ? (object)DBNull.Value : req.PreviousHospitalization);
                cmd.Parameters.AddWithValue("@p_timeline_notes", string.IsNullOrEmpty(req.TimelineNotes) ? (object)DBNull.Value : req.TimelineNotes);
                cmd.Parameters.AddWithValue("@p_created_by", createdBy);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            await LogAuditAsync("HPI", null, req.IpdId, req.PatientId, "UPDATE", "History of Present Illness updated", createdBy, ipAddress, deviceInfo);
        }

        // ══════════════════════════════════════════════════════
        // PROFESSIONAL EXAMINATION MODULE
        // ══════════════════════════════════════════════════════
        public async Task<ExaminationDetailModel> GetExaminationDetailAsync(int ipdId)
        {
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_admission_examination_get", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_ipd_id", ipdId);
            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new ExaminationDetailModel
                {
                    ExamId = Convert.ToInt32(r["exam_id"]),
                    IpdId = Convert.ToInt32(r["ipd_id"]),
                    PatientId = Convert.ToInt32(r["patient_id"]),
                    Conscious = r["conscious"] as string,
                    Orientation = r["orientation"] as string,
                    Pallor = r["pallor"] as string,
                    Icterus = r["icterus"] as string,
                    Cyanosis = r["cyanosis"] as string,
                    Clubbing = r["clubbing"] as string,
                    Edema = r["edema"] as string,
                    LymphNodes = r["lymph_nodes"] as string,
                    Hydration = r["hydration"] as string,
                    BpSystolic = r["bp_systolic"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["bp_systolic"]),
                    BpDiastolic = r["bp_diastolic"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["bp_diastolic"]),
                    Pulse = r["pulse"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["pulse"]),
                    Temperature = r["temperature"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(r["temperature"]),
                    RespiratoryRate = r["respiratory_rate"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["respiratory_rate"]),
                    Spo2 = r["spo2"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["spo2"]),
                    HeightCm = r["height_cm"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(r["height_cm"]),
                    WeightKg = r["weight_kg"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(r["weight_kg"]),
                    Bmi = r["bmi"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(r["bmi"]),
                    Cvs = r["cvs"] as string,
                    Rs = r["rs"] as string,
                    Cns = r["cns"] as string,
                    Abdomen = r["abdomen"] as string,
                    Ent = r["ent"] as string,
                    Skin = r["skin"] as string,
                    Musculoskeletal = r["musculoskeletal"] as string
                };
            }
            return new ExaminationDetailModel { IpdId = ipdId };
        }

        public async Task SaveExaminationDetailAsync(SaveExaminationDetailRequest req, int createdBy, string ipAddress = null, string deviceInfo = null)
        {
            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_admission_examination_save", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@p_ipd_id", req.IpdId);
                cmd.Parameters.AddWithValue("@p_patient_id", req.PatientId);
                cmd.Parameters.AddWithValue("@p_conscious", string.IsNullOrEmpty(req.Conscious) ? (object)DBNull.Value : req.Conscious);
                cmd.Parameters.AddWithValue("@p_orientation", string.IsNullOrEmpty(req.Orientation) ? (object)DBNull.Value : req.Orientation);
                cmd.Parameters.AddWithValue("@p_pallor", string.IsNullOrEmpty(req.Pallor) ? (object)DBNull.Value : req.Pallor);
                cmd.Parameters.AddWithValue("@p_icterus", string.IsNullOrEmpty(req.Icterus) ? (object)DBNull.Value : req.Icterus);
                cmd.Parameters.AddWithValue("@p_cyanosis", string.IsNullOrEmpty(req.Cyanosis) ? (object)DBNull.Value : req.Cyanosis);
                cmd.Parameters.AddWithValue("@p_clubbing", string.IsNullOrEmpty(req.Clubbing) ? (object)DBNull.Value : req.Clubbing);
                cmd.Parameters.AddWithValue("@p_edema", string.IsNullOrEmpty(req.Edema) ? (object)DBNull.Value : req.Edema);
                cmd.Parameters.AddWithValue("@p_lymph_nodes", string.IsNullOrEmpty(req.LymphNodes) ? (object)DBNull.Value : req.LymphNodes);
                cmd.Parameters.AddWithValue("@p_hydration", string.IsNullOrEmpty(req.Hydration) ? (object)DBNull.Value : req.Hydration);
                cmd.Parameters.AddWithValue("@p_bp_systolic", req.BpSystolic.HasValue ? req.BpSystolic.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p_bp_diastolic", req.BpDiastolic.HasValue ? req.BpDiastolic.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p_pulse", req.Pulse.HasValue ? req.Pulse.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p_temperature", req.Temperature.HasValue ? req.Temperature.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p_respiratory_rate", req.RespiratoryRate.HasValue ? req.RespiratoryRate.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p_spo2", req.Spo2.HasValue ? req.Spo2.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p_height_cm", req.HeightCm.HasValue ? req.HeightCm.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p_weight_kg", req.WeightKg.HasValue ? req.WeightKg.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p_cvs", string.IsNullOrEmpty(req.Cvs) ? (object)DBNull.Value : req.Cvs);
                cmd.Parameters.AddWithValue("@p_rs", string.IsNullOrEmpty(req.Rs) ? (object)DBNull.Value : req.Rs);
                cmd.Parameters.AddWithValue("@p_cns", string.IsNullOrEmpty(req.Cns) ? (object)DBNull.Value : req.Cns);
                cmd.Parameters.AddWithValue("@p_abdomen", string.IsNullOrEmpty(req.Abdomen) ? (object)DBNull.Value : req.Abdomen);
                cmd.Parameters.AddWithValue("@p_ent", string.IsNullOrEmpty(req.Ent) ? (object)DBNull.Value : req.Ent);
                cmd.Parameters.AddWithValue("@p_skin", string.IsNullOrEmpty(req.Skin) ? (object)DBNull.Value : req.Skin);
                cmd.Parameters.AddWithValue("@p_musculoskeletal", string.IsNullOrEmpty(req.Musculoskeletal) ? (object)DBNull.Value : req.Musculoskeletal);
                cmd.Parameters.AddWithValue("@p_created_by", createdBy);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            await LogAuditAsync("EXAMINATION", null, req.IpdId, req.PatientId, "UPDATE", "Examination details updated", createdBy, ipAddress, deviceInfo);
        }

        // ══════════════════════════════════════════════════════
        // ONE-CLICK CLINICAL FINDINGS
        // ══════════════════════════════════════════════════════
        public async Task<List<ClinicalFindingModel>> GetClinicalFindingsAsync(string fieldName, int? hospitalId)
        {
            var list = new List<ClinicalFindingModel>();
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_clinical_findings_get", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_field_name", fieldName);
            cmd.Parameters.AddWithValue("@p_hospital_id", hospitalId.HasValue ? (object)hospitalId.Value : DBNull.Value);
            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new ClinicalFindingModel
                {
                    FindingId = Convert.ToInt32(r["finding_id"]),
                    FieldName = r["field_name"] as string,
                    FindingName = r["finding_name"] as string,
                    FindingText = r["finding_text"] as string
                });
            }
            return list;
        }

        public async Task SaveClinicalFindingAsync(SaveClinicalFindingRequest req, int? hospitalId, int createdBy)
        {
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_clinical_finding_save", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_field_name", req.FieldName);
            cmd.Parameters.AddWithValue("@p_finding_name", req.FindingName);
            cmd.Parameters.AddWithValue("@p_finding_text", req.FindingText);
            cmd.Parameters.AddWithValue("@p_hospital_id", hospitalId.HasValue ? (object)hospitalId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@p_created_by", createdBy);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // ══════════════════════════════════════════════════════
        // DEPARTMENT-BASED CLINICAL TEMPLATES
        // ══════════════════════════════════════════════════════
        public async Task<List<DepartmentTemplateModel>> GetDepartmentTemplatesAsync(int createdBy, int? hospitalId)
        {
            var list = new List<DepartmentTemplateModel>();
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_department_templates_get", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_created_by", createdBy);
            cmd.Parameters.AddWithValue("@p_hospital_id", hospitalId.HasValue ? (object)hospitalId.Value : DBNull.Value);
            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new DepartmentTemplateModel
                {
                    DeptTemplateId = Convert.ToInt32(r["dept_template_id"]),
                    DepartmentName = r["department_name"] as string,
                    IsHospitalWide = Convert.ToBoolean(r["is_hospital_wide"]),
                    CommonComplaints = r["common_complaints"] as string,
                    ExaminationFindings = r["examination_findings"] as string,
                    StandardNotes = r["standard_notes"] as string,
                    FrequentDiagnosis = r["frequent_diagnosis"] as string
                });
            }
            return list;
        }

        public async Task SaveDepartmentTemplateAsync(SaveDepartmentTemplateRequest req, int? hospitalId, int createdBy)
        {
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_department_template_save", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_department_name", req.DepartmentName);
            cmd.Parameters.AddWithValue("@p_is_hospital_wide", req.IsHospitalWide ? 1 : 0);
            cmd.Parameters.AddWithValue("@p_common_complaints", string.IsNullOrEmpty(req.CommonComplaints) ? (object)DBNull.Value : req.CommonComplaints);
            cmd.Parameters.AddWithValue("@p_examination_findings", string.IsNullOrEmpty(req.ExaminationFindings) ? (object)DBNull.Value : req.ExaminationFindings);
            cmd.Parameters.AddWithValue("@p_standard_notes", string.IsNullOrEmpty(req.StandardNotes) ? (object)DBNull.Value : req.StandardNotes);
            cmd.Parameters.AddWithValue("@p_frequent_diagnosis", string.IsNullOrEmpty(req.FrequentDiagnosis) ? (object)DBNull.Value : req.FrequentDiagnosis);
            cmd.Parameters.AddWithValue("@p_hospital_id", hospitalId.HasValue ? (object)hospitalId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@p_created_by", createdBy);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // ══════════════════════════════════════════════════════
        // PREVIOUS ADMISSION COMPARISON (read-only across modules)
        // ══════════════════════════════════════════════════════
        public async Task<PreviousAdmissionSummaryModel> GetPreviousAdmissionSummaryAsync(int patientId, int currentIpdId)
        {
            int prevIpdId = 0;
            var summary = new PreviousAdmissionSummaryModel();

            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_previous_admission_get", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@p_patient_id", patientId);
                cmd.Parameters.AddWithValue("@p_current_ipd_id", currentIpdId);
                await conn.OpenAsync();
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    prevIpdId = Convert.ToInt32(r["ipd_id"]);
                    summary.PreviousIpdId = prevIpdId;
                    summary.AdmissionNumber = r["admission_number"] as string;
                    summary.AdmissionDate = r["admission_date"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(r["admission_date"]);
                    summary.DischargeDate = r["discharge_date"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(r["discharge_date"]);
                    summary.Status = r["status"] as string;
                }
            }

            if (prevIpdId <= 0)
                return null; // no previous admission for this patient

            // Previous free-text notes
            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_previous_admission_notes_get", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@p_ipd_id", prevIpdId);
                await conn.OpenAsync();
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    summary.PreviousCurrentComplaint = r["current_complaint"] as string;
                    summary.PreviousExamination = r["examination"] as string;
                }
            }

            // Previous structured chief complaints (reuses existing method)
            summary.PreviousChiefComplaints = await GetChiefComplaintsAsync(prevIpdId);

            // Allergies & Medical History are patient-level, so "previous" = same as current patient record
            summary.PreviousAllergies = await GetAllergiesAsync(patientId, prevIpdId);
            summary.PreviousMedicalHistory = await GetMedicalHistoryAsync(patientId);

            // Previous diagnosis — READ ONLY from ipddiagnosis, never written to
            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_previous_diagnosis_get", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@p_ipd_id", prevIpdId);
                await conn.OpenAsync();
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    summary.PreviousDiagnoses.Add(new PreviousDiagnosisModel
                    {
                        DiagnosisName = r["diagnosis_name"] as string,
                        DiagnosisSubType = r["diagnosis_subtype"] as string,
                        Severity = r["severity"] as string,
                        DiagnosisType = r["diagnosis_type"] as string,
                        Status = r["status"] as string
                    });
                }
            }

            return summary;
        }

        // ══════════════════════════════════════════════════════
        // AUDIT TRAIL
        // ══════════════════════════════════════════════════════
        public async Task<List<AdmissionAuditLogModel>> GetAuditLogAsync(int ipdId)
        {
            var list = new List<AdmissionAuditLogModel>();
            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand("sp_admission_audit_log_get", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@p_ipd_id", ipdId);
            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new AdmissionAuditLogModel
                {
                    LogId = Convert.ToInt64(r["log_id"]),
                    EntityType = r["entity_type"] as string,
                    EntityId = r["entity_id"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["entity_id"]),
                    Action = r["action"] as string,
                    Description = r["description"] as string,
                    ChangedBy = r["changed_by"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["changed_by"]),
                    ChangedDate = Convert.ToDateTime(r["changed_date"]),
                    IpAddress = r["ip_address"] as string,
                    DeviceInfo = r["device_info"] as string
                });
            }
            return list;
        }
    }
}

