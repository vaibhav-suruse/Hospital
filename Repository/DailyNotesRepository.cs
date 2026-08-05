using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class DailyNotesRepository : IDailyNotes
    {
        private readonly string _connectionString;

        public DailyNotesRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // ============================================================
        // DOCTOR NOTES
        // ============================================================

        // GET ALL Doctor Notes for Inpatient
        public List<DoctorNoteModel> GetDoctorNotes(int ipId)
        {
            var list = new List<DoctorNoteModel>();

            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetDoctorNotes", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_ip_id", ipId);

                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(MapDoctorNote(reader));
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while fetching doctor notes.", ex);
            }

            return list;
        }

        // GET Single Doctor Note by ID
        public DoctorNoteModel GetDoctorNoteById(int noteId)
        {
            DoctorNoteModel model = null;

            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetDoctorNoteById", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_note_id", noteId);

                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            model = MapDoctorNote(reader);
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while fetching doctor note by id.", ex);
            }

            return model;
        }

        // INSERT Doctor Note
        public int InsertDoctorNote(DoctorNoteModel model)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_InsertDoctorNote", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_ip_id", model.IpId);
                    cmd.Parameters.AddWithValue("p_temperature", model.Temperature.HasValue ? (object)model.Temperature.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_pulse", model.Pulse.HasValue ? (object)model.Pulse.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_resp_rate", model.RespRate.HasValue ? (object)model.RespRate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_bp_systolic", model.BpSystolic.HasValue ? (object)model.BpSystolic.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_bp_diastolic", model.BpDiastolic.HasValue ? (object)model.BpDiastolic.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_spo2", model.SpO2.HasValue ? (object)model.SpO2.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_notes", string.IsNullOrEmpty(model.Notes) ? DBNull.Value : (object)model.Notes);
                    cmd.Parameters.AddWithValue("p_note_date", model.NoteDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("p_note_time", model.NoteTime.ToString(@"hh\:mm\:ss"));
                    cmd.Parameters.AddWithValue("p_template_id", model.TemplateId.HasValue ? (object)model.TemplateId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_template_name", string.IsNullOrEmpty(model.TemplateName) ? DBNull.Value : (object)model.TemplateName);
                    cmd.Parameters.AddWithValue("p_created_by", model.CreatedBy);

                    var outParam = new MySqlParameter("p_new_id", MySqlDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outParam);

                    con.Open();
                    cmd.ExecuteNonQuery();

                    return Convert.ToInt32(outParam.Value);
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while inserting doctor note.", ex);
            }
        }

        // UPDATE Doctor Note
        public void UpdateDoctorNote(DoctorNoteModel model)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_UpdateDoctorNote", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_note_id", model.NoteId);
                    cmd.Parameters.AddWithValue("p_temperature", model.Temperature.HasValue ? (object)model.Temperature.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_pulse", model.Pulse.HasValue ? (object)model.Pulse.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_resp_rate", model.RespRate.HasValue ? (object)model.RespRate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_bp_systolic", model.BpSystolic.HasValue ? (object)model.BpSystolic.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_bp_diastolic", model.BpDiastolic.HasValue ? (object)model.BpDiastolic.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_spo2", model.SpO2.HasValue ? (object)model.SpO2.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_notes", string.IsNullOrEmpty(model.Notes) ? DBNull.Value : (object)model.Notes);
                    cmd.Parameters.AddWithValue("p_note_date", model.NoteDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("p_note_time", model.NoteTime.ToString(@"hh\:mm\:ss"));
                    cmd.Parameters.AddWithValue("p_template_id", model.TemplateId.HasValue ? (object)model.TemplateId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_template_name", string.IsNullOrEmpty(model.TemplateName) ? DBNull.Value : (object)model.TemplateName);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while updating doctor note.", ex);
            }
        }

        // DELETE Doctor Note (Soft Delete)
        public void DeleteDoctorNote(int noteId)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_DeleteDoctorNote", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_note_id", noteId);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while deleting doctor note.", ex);
            }
        }

        // ============================================================
        // NURSE NOTES
        // ============================================================

        // GET ALL Nurse Notes for Inpatient
        public List<NurseNoteModel> GetNurseNotes(int ipId)
        {
            var list = new List<NurseNoteModel>();

            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetNurseNotes", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_ip_id", ipId);

                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(MapNurseNote(reader));
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while fetching nurse notes.", ex);
            }

            return list;
        }

        // GET Single Nurse Note by ID
        public NurseNoteModel GetNurseNoteById(int noteId)
        {
            NurseNoteModel model = null;

            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetNurseNoteById", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_note_id", noteId);

                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            model = MapNurseNote(reader);
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while fetching nurse note by id.", ex);
            }

            return model;
        }

        // INSERT Nurse Note
        public int InsertNurseNote(NurseNoteModel model)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_InsertNurseNote", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_ip_id", model.IpId);
                    cmd.Parameters.AddWithValue("p_notes", model.Notes);
                    cmd.Parameters.AddWithValue("p_note_date", model.NoteDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("p_note_time", model.NoteTime.ToString(@"hh\:mm\:ss"));
                    cmd.Parameters.AddWithValue("p_template_id", model.TemplateId.HasValue ? (object)model.TemplateId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_template_name", string.IsNullOrEmpty(model.TemplateName) ? DBNull.Value : (object)model.TemplateName);
                    cmd.Parameters.AddWithValue("p_created_by", model.CreatedBy);

                    var outParam = new MySqlParameter("p_new_id", MySqlDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outParam);

                    con.Open();
                    cmd.ExecuteNonQuery();

                    return Convert.ToInt32(outParam.Value);
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while inserting nurse note.", ex);
            }
        }

        // UPDATE Nurse Note
        public void UpdateNurseNote(NurseNoteModel model)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_UpdateNurseNote", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_note_id", model.NoteId);
                    cmd.Parameters.AddWithValue("p_notes", model.Notes);
                    cmd.Parameters.AddWithValue("p_note_date", model.NoteDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("p_note_time", model.NoteTime.ToString(@"hh\:mm\:ss"));
                    cmd.Parameters.AddWithValue("p_template_id", model.TemplateId.HasValue ? (object)model.TemplateId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_template_name", string.IsNullOrEmpty(model.TemplateName) ? DBNull.Value : (object)model.TemplateName);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while updating nurse note.", ex);
            }
        }

        // DELETE Nurse Note (Soft Delete)
        public void DeleteNurseNote(int noteId)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_DeleteNurseNote", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_note_id", noteId);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while deleting nurse note.", ex);
            }
        }

        // ============================================================
        // TEMPLATES
        // ============================================================

        // GET Templates by type with optional search
        public List<NoteTemplateModel> GetTemplates(string type, string search = "")
        {
            var list = new List<NoteTemplateModel>();

            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetTemplates", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_type", type);
                    cmd.Parameters.AddWithValue("p_search", string.IsNullOrEmpty(search) ? DBNull.Value : (object)search);

                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new NoteTemplateModel
                            {
                                TemplateId = reader.GetInt32("template_id"),
                                TemplateName = reader.GetString("template_name"),
                                TemplateType = reader.GetString("template_type"),
                                TemplateText = reader.GetString("template_text")
                            });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while fetching templates.", ex);
            }

            return list;
        }

        // INSERT new Template
        public int InsertTemplate(NoteTemplateModel model, int createdBy)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_InsertTemplate", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_template_name", model.TemplateName);
                    cmd.Parameters.AddWithValue("p_template_type", model.TemplateType);
                    cmd.Parameters.AddWithValue("p_template_text", model.TemplateText);
                    cmd.Parameters.AddWithValue("p_created_by", createdBy);

                    var outParam = new MySqlParameter("p_new_id", MySqlDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outParam);

                    con.Open();
                    cmd.ExecuteNonQuery();

                    return Convert.ToInt32(outParam.Value);
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while inserting template.", ex);
            }
        }

        // ============================================================
        // PRIVATE: Reader Mappers
        // ============================================================

        private static DoctorNoteModel MapDoctorNote(MySqlDataReader r)
        {
            var model = new DoctorNoteModel();

            model.NoteId = r.GetInt32("note_id");
            model.IpId = r.GetInt32("ip_id");
            model.NoteDate = r.GetDateTime("note_date");
            model.NoteTime = r.GetTimeSpan("note_time");
            model.CreatedBy = r.GetInt32("created_by");
            model.CreatedAt = r.GetDateTime("created_at");

            // round_id was added to link this note to the medicines/labs/
            // symptoms ordered alongside it (see add_round_link_to_daily_notes.sql)
            int roundIdOrdinal = r.GetOrdinal("round_id");
            model.RoundId = r.IsDBNull(roundIdOrdinal) ? (int?)null : r.GetInt32(roundIdOrdinal);

            if (r.IsDBNull(r.GetOrdinal("temperature"))) model.Temperature = null;
            else model.Temperature = r.GetDecimal("temperature");

            if (r.IsDBNull(r.GetOrdinal("pulse"))) model.Pulse = null;
            else model.Pulse = r.GetInt32("pulse");

            if (r.IsDBNull(r.GetOrdinal("resp_rate"))) model.RespRate = null;
            else model.RespRate = r.GetInt32("resp_rate");

            if (r.IsDBNull(r.GetOrdinal("bp_systolic"))) model.BpSystolic = null;
            else model.BpSystolic = r.GetInt32("bp_systolic");

            if (r.IsDBNull(r.GetOrdinal("bp_diastolic"))) model.BpDiastolic = null;
            else model.BpDiastolic = r.GetInt32("bp_diastolic");

            if (r.IsDBNull(r.GetOrdinal("spo2"))) model.SpO2 = null;
            else model.SpO2 = r.GetDecimal("spo2");

            if (r.IsDBNull(r.GetOrdinal("template_id"))) model.TemplateId = null;
            else model.TemplateId = r.GetInt32("template_id");

            model.Notes = r.IsDBNull(r.GetOrdinal("notes")) ? string.Empty : r.GetString("notes");
            model.TemplateName = r.IsDBNull(r.GetOrdinal("template_name")) ? string.Empty : r.GetString("template_name");
            model.DisplayDatetime = r.IsDBNull(r.GetOrdinal("display_datetime")) ? string.Empty : r.GetString("display_datetime");

            // NEW: Read doctor name
            model.DoctorName = r.IsDBNull(r.GetOrdinal("doctor_name")) ? string.Empty : r.GetString("doctor_name");

            return model;
        }

        private static NurseNoteModel MapNurseNote(MySqlDataReader r)
        {
            var model = new NurseNoteModel();

            model.NoteId = r.GetInt32("note_id");
            model.IpId = r.GetInt32("ip_id");
            model.Notes = r.GetString("notes");
            model.NoteDate = r.GetDateTime("note_date");
            model.NoteTime = r.GetTimeSpan("note_time");
            model.CreatedBy = r.GetInt32("created_by");
            model.CreatedAt = r.GetDateTime("created_at");

            if (r.IsDBNull(r.GetOrdinal("template_id"))) model.TemplateId = null;
            else model.TemplateId = r.GetInt32("template_id");

            model.TemplateName = r.IsDBNull(r.GetOrdinal("template_name")) ? string.Empty : r.GetString("template_name");
            model.DisplayDatetime = r.IsDBNull(r.GetOrdinal("display_datetime")) ? string.Empty : r.GetString("display_datetime");

            // NEW: Read nurse name
            model.NurseName = r.IsDBNull(r.GetOrdinal("nurse_name")) ? string.Empty : r.GetString("nurse_name");

            return model;
        }


        // ============================================================
        // AUDIT TRAIL
        // ============================================================

        public void InsertNoteHistory(NoteHistoryModel history)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_InsertNoteHistory", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_note_id", history.NoteId);
                    cmd.Parameters.AddWithValue("p_note_type", history.NoteType);
                    cmd.Parameters.AddWithValue("p_previous_notes", string.IsNullOrEmpty(history.PreviousNotes) ? DBNull.Value : (object)history.PreviousNotes);
                    cmd.Parameters.AddWithValue("p_previous_temperature", history.PreviousTemperature.HasValue ? (object)history.PreviousTemperature.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_previous_pulse", history.PreviousPulse.HasValue ? (object)history.PreviousPulse.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_previous_resp_rate", history.PreviousRespRate.HasValue ? (object)history.PreviousRespRate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_previous_bp_systolic", history.PreviousBpSystolic.HasValue ? (object)history.PreviousBpSystolic.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_previous_bp_diastolic", history.PreviousBpDiastolic.HasValue ? (object)history.PreviousBpDiastolic.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_previous_spo2", history.PreviousSpO2.HasValue ? (object)history.PreviousSpO2.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_modified_by", history.ModifiedBy);
                    cmd.Parameters.AddWithValue("p_change_summary", string.IsNullOrEmpty(history.ChangeSummary) ? DBNull.Value : (object)history.ChangeSummary);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while inserting note history.", ex);
            }
        }

        public List<NoteHistoryModel> GetNoteHistory(int noteId, string noteType)
        {
            var list = new List<NoteHistoryModel>();

            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetNoteHistory", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_note_id", noteId);
                    cmd.Parameters.AddWithValue("p_note_type", noteType);

                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new NoteHistoryModel
                            {
                                HistoryId = reader.GetInt32("history_id"),
                                NoteId = reader.GetInt32("note_id"),
                                NoteType = reader.GetString("note_type"),
                                PreviousNotes = reader.IsDBNull(reader.GetOrdinal("previous_notes")) ? null : reader.GetString("previous_notes"),
                                PreviousTemperature = reader.IsDBNull(reader.GetOrdinal("previous_temperature")) ? (decimal?)null : reader.GetDecimal("previous_temperature"),
                                PreviousPulse = reader.IsDBNull(reader.GetOrdinal("previous_pulse")) ? (int?)null : reader.GetInt32("previous_pulse"),
                                PreviousRespRate = reader.IsDBNull(reader.GetOrdinal("previous_resp_rate")) ? (int?)null : reader.GetInt32("previous_resp_rate"),
                                PreviousBpSystolic = reader.IsDBNull(reader.GetOrdinal("previous_bp_systolic")) ? (int?)null : reader.GetInt32("previous_bp_systolic"),
                                PreviousBpDiastolic = reader.IsDBNull(reader.GetOrdinal("previous_bp_diastolic")) ? (int?)null : reader.GetInt32("previous_bp_diastolic"),
                                PreviousSpO2 = reader.IsDBNull(reader.GetOrdinal("previous_spo2")) ? (decimal?)null : reader.GetDecimal("previous_spo2"),
                                ModifiedBy = reader.GetInt32("modified_by"),
                                ModifiedByName = reader.IsDBNull(reader.GetOrdinal("modified_by_name")) ? string.Empty : reader.GetString("modified_by_name"),
                                ModifiedAt = reader.GetDateTime("modified_at"),
                                ChangeSummary = reader.IsDBNull(reader.GetOrdinal("change_summary")) ? string.Empty : reader.GetString("change_summary")
                            });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while fetching note history.", ex);
            }

            return list;
        }



        // ============================================================
        // MEDICINE ORDERS
        // ============================================================

        public void InsertMedicineOrder(MedicineOrderModel model)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_InsertMedicineOrder", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_ipd_id", model.IPDId);
                    cmd.Parameters.AddWithValue("p_medicine_id", model.MedicineId);
                    cmd.Parameters.AddWithValue("p_dosage", model.Dosage);
                    cmd.Parameters.AddWithValue("p_frequency", model.Frequency);
                    cmd.Parameters.AddWithValue("p_duration", model.Duration);
                    cmd.Parameters.AddWithValue("p_quantity", model.Quantity);
                    cmd.Parameters.AddWithValue("p_route", model.Route);
                    cmd.Parameters.AddWithValue("p_instructions", string.IsNullOrEmpty(model.Instructions) ? DBNull.Value : (object)model.Instructions);
                    cmd.Parameters.AddWithValue("p_ordered_by", model.OrderedBy);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while inserting medicine order.", ex);
            }
        }

        public List<MedicineOrderModel> GetPendingMedicineOrders(int ipId)
        {
            var list = new List<MedicineOrderModel>();

            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetPendingMedicineOrders", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_ipd_id", ipId);

                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new MedicineOrderModel
                            {
                                OrderId = reader.GetInt32("order_id"),
                                IPDId = reader.GetInt32("ipd_id"),
                                MedicineId = reader.GetInt32("medicine_id"),
                                MedicineName = reader.GetString("medicine_name"),
                                Dosage = reader.GetString("dosage"),
                                Frequency = reader.GetString("frequency"),
                                Duration = reader.GetString("duration"),
                                Quantity = reader.GetInt32("quantity"),
                                Route = reader.GetString("route"),
                                Instructions = reader.IsDBNull(reader.GetOrdinal("instructions")) ? string.Empty : reader.GetString("instructions"),
                                OrderedBy = reader.GetInt32("ordered_by"),
                                OrderedByName = reader.IsDBNull(reader.GetOrdinal("ordered_by_name")) ? string.Empty : reader.GetString("ordered_by_name"),
                                OrderedDateTime = reader.GetDateTime("ordered_datetime"),
                                Status = reader.GetString("status")
                            });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while fetching pending medicine orders.", ex);
            }

            return list;
        }



        // ============================================================
        // LAB ORDERS
        // ============================================================

        public void InsertLabOrder(LabOrderModel model)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_InsertLabOrder", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_ipd_id", model.IPDId);
                    cmd.Parameters.AddWithValue("p_test_name", model.TestName);
                    cmd.Parameters.AddWithValue("p_category", string.IsNullOrEmpty(model.Category) ? DBNull.Value : (object)model.Category);
                    cmd.Parameters.AddWithValue("p_priority", model.Priority);
                    cmd.Parameters.AddWithValue("p_instructions", string.IsNullOrEmpty(model.Instructions) ? DBNull.Value : (object)model.Instructions);
                    cmd.Parameters.AddWithValue("p_ordered_by", model.OrderedBy);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while inserting lab order.", ex);
            }
        }

        public List<LabOrderModel> GetPendingLabOrders(int ipId)
        {
            var list = new List<LabOrderModel>();

            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetPendingLabOrders", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_ipd_id", ipId);

                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new LabOrderModel
                            {
                                OrderId = reader.GetInt32("order_id"),
                                IPDId = reader.GetInt32("ipd_id"),
                                TestName = reader.GetString("test_name"),
                                Category = reader.IsDBNull(reader.GetOrdinal("category")) ? string.Empty : reader.GetString("category"),
                                Priority = reader.GetString("priority"),
                                Instructions = reader.IsDBNull(reader.GetOrdinal("instructions")) ? string.Empty : reader.GetString("instructions"),
                                OrderedBy = reader.GetInt32("ordered_by"),
                                OrderedByName = reader.IsDBNull(reader.GetOrdinal("ordered_by_name")) ? string.Empty : reader.GetString("ordered_by_name"),
                                OrderedDateTime = reader.GetDateTime("ordered_datetime"),
                                Status = reader.GetString("status")
                            });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while fetching pending lab orders.", ex);
            }

            return list;
        }




        // ===================== LAB TEST MASTER =====================
        public List<LabTestMasterModel> SearchLabTests(int parentHospitalId, int? subHospitalId, string search)
        {
            var list = new List<LabTestMasterModel>();

            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_SearchLabTests", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_parent_hospital_id", parentHospitalId);
                cmd.Parameters.AddWithValue("p_sub_hospital_id", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : (object)search);

                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new LabTestMasterModel
                        {
                            TestId = System.Convert.ToInt32(r["TestId"]),
                            TestName = r["TestName"].ToString(),
                            InvestigationType = r["InvestigationType"].ToString(),
                            Department = r["Department"] == DBNull.Value ? "" : r["Department"].ToString(),
                            SampleType = r["SampleType"] == DBNull.Value ? "" : r["SampleType"].ToString(),
                            PreparationInstructions = r["PreparationInstructions"] == DBNull.Value ? "" : r["PreparationInstructions"].ToString()
                        });
                    }
                }
            }

            return list;
        }

        // ===================== LAB ORDERS =====================



        public int InsertDailyNotesLabOrder(int parentHospitalId, int? subHospitalId, LabOrderModel model, int doctorId)
        {
            using (var con = new MySqlConnection(_connectionString))
            {
                con.Open();

                // BUGFIX: previously delegated to sp_InsertDailyNotesLabOrder,
                // which ALWAYS created its own new, disconnected ipd_doctor_round
                // row - same issue as medicine had. Now reuses the round already
                // linked to the note being saved, so this lab order can be
                // traced back to exactly that note (History view).
                int roundId = model.RoundId;
                if (roundId <= 0)
                {
                    string roundSql = @"INSERT INTO ipd_doctor_round (ParentHospitalId, SubHospitalId, IPDId, DoctorId, RoundType, RoundDateTime, Notes, IsAbnormal, IsActive, CreatedDate)
                               VALUES (@ph, @sh, @ipd, @doc, 'Emergency', NOW(), 'Lab order created from Daily Notes', 0, 1, NOW());
                               SELECT LAST_INSERT_ID();";
                    using (var roundCmd = new MySqlCommand(roundSql, con))
                    {
                        roundCmd.Parameters.AddWithValue("@ph", parentHospitalId);
                        roundCmd.Parameters.AddWithValue("@sh", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                        roundCmd.Parameters.AddWithValue("@ipd", model.IPDId);
                        roundCmd.Parameters.AddWithValue("@doc", doctorId);
                        roundId = Convert.ToInt32(roundCmd.ExecuteScalar());
                    }
                }

                string invSql = @"INSERT INTO ipd_round_investigation (ParentHospitalId, SubHospitalId, RoundId, IPDId, InvestigationType, TestName, Priority, OrderedDateTime, Instructions, Status, IsActive, CreatedDate)
                           VALUES (@ph, @sh, @rid, @ipd, @type, @name, @pri, NOW(), @ins, 'Ordered', 1, NOW())";

                using (var invCmd = new MySqlCommand(invSql, con))
                {
                    invCmd.Parameters.AddWithValue("@ph", parentHospitalId);
                    invCmd.Parameters.AddWithValue("@sh", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                    invCmd.Parameters.AddWithValue("@rid", roundId);
                    invCmd.Parameters.AddWithValue("@ipd", model.IPDId);
                    invCmd.Parameters.AddWithValue("@type", model.InvestigationType);
                    invCmd.Parameters.AddWithValue("@name", model.TestName);
                    invCmd.Parameters.AddWithValue("@pri", model.Priority);
                    invCmd.Parameters.AddWithValue("@ins", string.IsNullOrWhiteSpace(model.Instructions) ? DBNull.Value : (object)model.Instructions);
                    invCmd.ExecuteNonQuery();
                }

                return roundId;
            }
        }



        public List<LabOrderModel> GetPendingLabOrdersByIPD(int ipdId)
        {
            var list = new List<LabOrderModel>();

            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_GetPendingLabOrdersByIPD", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_ipd_id", ipdId);

                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new LabOrderModel
                        {
                            IPDId = System.Convert.ToInt32(r["IPDId"]),
                            InvestigationType = r["InvestigationType"].ToString(),
                            TestName = r["TestName"].ToString(),
                            Priority = r["Priority"] == DBNull.Value ? "Routine" : r["Priority"].ToString(),
                            Instructions = r["Instructions"] == DBNull.Value ? "" : r["Instructions"].ToString(),
                            Status = r["Status"] == DBNull.Value ? "" : r["Status"].ToString(),
                            OrderedDateTime = System.Convert.ToDateTime(r["OrderedDateTime"])
                        });
                    }
                }
            }

            return list;
        }

        // ===================== MEDICINE ORDERS =====================
        //public void InsertDailyNotesMedicine(int parentHospitalId, int? subHospitalId, MedicineOrderModel model, int doctorId)
        //{
        //    // 1) Create Round (FK requirement)
        //    int roundId = CreateDailyNotesRound(parentHospitalId, subHospitalId, model.IPDId, doctorId);

        //    // 2) Insert prescription using real RoundId
        //    using (var con = new MySqlConnection(_connectionString))
        //    using (var cmd = new MySqlCommand("sp_InsertDailyNotesMedicine", con))
        //    {
        //        cmd.CommandType = CommandType.StoredProcedure;

        //        cmd.Parameters.AddWithValue("p_parent_hospital_id", parentHospitalId);
        //        cmd.Parameters.AddWithValue("p_sub_hospital_id", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
        //        cmd.Parameters.AddWithValue("p_round_id", roundId);
        //        cmd.Parameters.AddWithValue("p_ipd_id", model.IPDId);
        //        cmd.Parameters.AddWithValue("p_medicine_id", model.MedicineId);
        //        cmd.Parameters.AddWithValue("p_morning", model.Morning ? 1 : 0);
        //        cmd.Parameters.AddWithValue("p_afternoon", model.Afternoon ? 1 : 0);
        //        cmd.Parameters.AddWithValue("p_evening", model.Evening ? 1 : 0);
        //        cmd.Parameters.AddWithValue("p_days", model.Days.HasValue ? (object)model.Days.Value : DBNull.Value);
        //        cmd.Parameters.AddWithValue("p_route", string.IsNullOrWhiteSpace(model.Route) ? "Oral" : model.Route);
        //        cmd.Parameters.AddWithValue("p_dosage", string.IsNullOrWhiteSpace(model.Dosage) ? DBNull.Value : (object)model.Dosage);
        //        cmd.Parameters.AddWithValue("p_instructions", string.IsNullOrWhiteSpace(model.Instructions) ? DBNull.Value : (object)model.Instructions);

        //        con.Open();
        //        cmd.ExecuteNonQuery();
        //    }
        //}




        public int InsertDailyNotesMedicine(int parentHospitalId, int? subHospitalId, MedicineOrderModel model, int doctorId)
        {
            using (var con = new MySqlConnection(_connectionString))
            {
                con.Open();

                // BUGFIX: previously this ALWAYS created a brand new, disconnected
                // ipd_doctor_round row for every single medicine order - meaning
                // there was no way to ever tell which note a medicine belonged to.
                // Now it reuses the round already created (and linked) when the
                // note itself was saved. Falls back to creating one only if
                // somehow called without a round (defensive, shouldn't happen
                // via the normal Daily Notes flow).
                int roundId = model.RoundId;
                if (roundId <= 0)
                {
                    string roundSql = @"INSERT INTO ipd_doctor_round (ParentHospitalId, SubHospitalId, IPDId, DoctorId, RoundType, RoundDateTime, Notes, IsActive, CreatedDate)
                               VALUES (@ph, @sh, @ipd, @doc, 'Emergency', NOW(), 'Medicine order from Daily Notes', 1, NOW());
                               SELECT LAST_INSERT_ID();";
                    using (var roundCmd = new MySqlCommand(roundSql, con))
                    {
                        roundCmd.Parameters.AddWithValue("@ph", parentHospitalId);
                        roundCmd.Parameters.AddWithValue("@sh", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                        roundCmd.Parameters.AddWithValue("@ipd", model.IPDId);
                        roundCmd.Parameters.AddWithValue("@doc", doctorId);
                        roundId = Convert.ToInt32(roundCmd.ExecuteScalar());
                    }
                }

                // Insert prescription
                string prescSql = @"INSERT INTO ipd_round_prescription (ParentHospitalId, SubHospitalId, RoundId, IPDId, MedicineId, Morning, Afternoon, Evening, Days, Route, Dosage, Instructions, Status, IsActive, CreatedDate)
                           VALUES (@ph, @sh, @rid, @ipd, @med, @m, @a, @e, @d, @r, @dos, @ins, 'Active', 1, NOW())";

                using (var prescCmd = new MySqlCommand(prescSql, con))
                {
                    prescCmd.Parameters.AddWithValue("@ph", parentHospitalId);
                    prescCmd.Parameters.AddWithValue("@sh", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                    prescCmd.Parameters.AddWithValue("@rid", roundId);
                    prescCmd.Parameters.AddWithValue("@ipd", model.IPDId);
                    prescCmd.Parameters.AddWithValue("@med", model.MedicineId);
                    prescCmd.Parameters.AddWithValue("@m", model.Morning);
                    prescCmd.Parameters.AddWithValue("@a", model.Afternoon);
                    prescCmd.Parameters.AddWithValue("@e", model.Evening);
                    prescCmd.Parameters.AddWithValue("@d", model.Days);
                    prescCmd.Parameters.AddWithValue("@r", model.Route ?? "");
                    prescCmd.Parameters.AddWithValue("@dos", model.Dosage ?? "");
                    prescCmd.Parameters.AddWithValue("@ins", model.Instructions ?? "");
                    prescCmd.ExecuteNonQuery();
                }

                return roundId;
            }
        }

        // ============================================================
        // SYMPTOMS (Daily Notes) — same pattern as InsertDailyNotesMedicine:
        // creates its own ipd_doctor_round row so ipd_round_symptom's FK to
        // RoundId is satisfied.
        // ============================================================
        public int InsertDailyNotesSymptom(int parentHospitalId, int? subHospitalId, int ipdId, int symptomId, int doctorId, int roundIdIn = 0)
        {
            using (var con = new MySqlConnection(_connectionString))
            {
                con.Open();

                int roundId = roundIdIn;
                if (roundId <= 0)
                {
                    string roundSql = @"INSERT INTO ipd_doctor_round (ParentHospitalId, SubHospitalId, IPDId, DoctorId, RoundType, RoundDateTime, Notes, IsActive, CreatedDate)
                               VALUES (@ph, @sh, @ipd, @doc, 'Emergency', NOW(), 'Symptom added from Daily Notes', 1, NOW());
                               SELECT LAST_INSERT_ID();";
                    using (var roundCmd = new MySqlCommand(roundSql, con))
                    {
                        roundCmd.Parameters.AddWithValue("@ph", parentHospitalId);
                        roundCmd.Parameters.AddWithValue("@sh", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                        roundCmd.Parameters.AddWithValue("@ipd", ipdId);
                        roundCmd.Parameters.AddWithValue("@doc", doctorId);
                        roundId = Convert.ToInt32(roundCmd.ExecuteScalar());
                    }
                }

                string symSql = @"INSERT INTO ipd_round_symptom (ParentHospitalId, SubHospitalId, RoundId, IPDId, SymptomId, IsActive, CreatedDate)
                           VALUES (@ph, @sh, @rid, @ipd, @sym, 1, NOW())";

                using (var symCmd = new MySqlCommand(symSql, con))
                {
                    symCmd.Parameters.AddWithValue("@ph", parentHospitalId);
                    symCmd.Parameters.AddWithValue("@sh", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                    symCmd.Parameters.AddWithValue("@rid", roundId);
                    symCmd.Parameters.AddWithValue("@ipd", ipdId);
                    symCmd.Parameters.AddWithValue("@sym", symptomId);
                    symCmd.ExecuteNonQuery();
                }

                return roundId;
            }
        }

        // ============================================================
        // NOTE <-> ROUND LINKAGE
        // Creates exactly one round per saved note and links the note to
        // it, so every medicine/lab/symptom added alongside that note can
        // be traced back to precisely that note later (History view).
        // ============================================================
        public int CreateRoundForNote(int parentHospitalId, int? subHospitalId, int ipdId, int doctorId, int noteId)
        {
            using (var con = new MySqlConnection(_connectionString))
            {
                con.Open();

                string roundSql = @"INSERT INTO ipd_doctor_round (ParentHospitalId, SubHospitalId, IPDId, DoctorId, RoundType, RoundDateTime, Notes, IsActive, CreatedDate)
                           VALUES (@ph, @sh, @ipd, @doc, 'Follow-up', NOW(), 'Daily Note round', 1, NOW());
                           SELECT LAST_INSERT_ID();";
                int roundId;
                using (var roundCmd = new MySqlCommand(roundSql, con))
                {
                    roundCmd.Parameters.AddWithValue("@ph", parentHospitalId);
                    roundCmd.Parameters.AddWithValue("@sh", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                    roundCmd.Parameters.AddWithValue("@ipd", ipdId);
                    roundCmd.Parameters.AddWithValue("@doc", doctorId);
                    roundId = Convert.ToInt32(roundCmd.ExecuteScalar());
                }

                using (var updCmd = new MySqlCommand("UPDATE ip_daily_doctor_notes SET round_id = @rid WHERE note_id = @nid", con))
                {
                    updCmd.Parameters.AddWithValue("@rid", roundId);
                    updCmd.Parameters.AddWithValue("@nid", noteId);
                    updCmd.ExecuteNonQuery();
                }

                return roundId;
            }
        }

        // ============================================================
        // ROUND-SCOPED FETCHES (for the per-note History view)
        // ============================================================
        public List<MedicineOrderModel> GetMedicinesByRoundId(int roundId)
        {
            var list = new List<MedicineOrderModel>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(@"
                SELECT rp.Id, rp.MedicineId, m.MedicineName, rp.Morning, rp.Afternoon, rp.Evening,
                       rp.Days, rp.Route, rp.Dosage, rp.Instructions, rp.Status
                FROM ipd_round_prescription rp
                INNER JOIN tbl_medicine m ON m.MedicineId = rp.MedicineId
                WHERE rp.RoundId = @rid AND rp.IsActive = 1
                ", con))
            {
                cmd.Parameters.AddWithValue("@rid", roundId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new MedicineOrderModel
                        {
                            OrderId = Convert.ToInt32(r["Id"]),
                            MedicineId = Convert.ToInt32(r["MedicineId"]),
                            MedicineName = r["MedicineName"].ToString(),
                            Morning = Convert.ToBoolean(r["Morning"]),
                            Afternoon = Convert.ToBoolean(r["Afternoon"]),
                            Evening = Convert.ToBoolean(r["Evening"]),
                            Days = r["Days"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Days"]),
                            Route = r["Route"].ToString(),
                            Dosage = r["Dosage"].ToString(),
                            Instructions = r["Instructions"] == DBNull.Value ? null : r["Instructions"].ToString(),
                            Status = r["Status"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public List<LabInvestigationModel> GetLabOrdersByRoundId(int roundId)
        {
            var list = new List<LabInvestigationModel>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(@"
                SELECT Id, IPDId, InvestigationType, TestName, Priority, Instructions, Status,
                       OrderedDateTime, CollectedDateTime, CompletedDateTime, Result, ResultFilePath, IsAbnormal
                FROM ipd_round_investigation
                WHERE RoundId = @rid AND IsActive = 1
                ", con))
            {
                cmd.Parameters.AddWithValue("@rid", roundId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new LabInvestigationModel
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            IPDId = Convert.ToInt32(r["IPDId"]),
                            InvestigationType = r["InvestigationType"].ToString(),
                            TestName = r["TestName"].ToString(),
                            Priority = r["Priority"].ToString(),
                            Instructions = r["Instructions"] == DBNull.Value ? null : r["Instructions"].ToString(),
                            Status = r["Status"].ToString(),
                            OrderedDateTime = Convert.ToDateTime(r["OrderedDateTime"]),
                            CollectedDateTime = r["CollectedDateTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CollectedDateTime"]),
                            CompletedDateTime = r["CompletedDateTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CompletedDateTime"]),
                            Result = r["Result"] == DBNull.Value ? null : r["Result"].ToString(),
                            ResultFilePath = r["ResultFilePath"] == DBNull.Value ? null : r["ResultFilePath"].ToString(),
                            IsAbnormal = Convert.ToBoolean(r["IsAbnormal"])
                        });
                    }
                }
            }
            return list;
        }

        // ============================================================
        // MAR (Medication Administration Record)
        // ============================================================
        public List<MARRowModel> GetOrCreateMARForDate(int ipdId, DateTime date)
        {
            using (var con = new MySqlConnection(_connectionString))
            {
                con.Open();

                // Step 1: for every ACTIVE prescription's ordered time slots,
                // ensure a Pending MAR row exists for this date. INSERT IGNORE
                // relies on the UNIQUE (PrescriptionId, ScheduledDate,
                // ScheduledSlot) key so this is safe to call repeatedly.
                string ensureSql = @"
                    INSERT IGNORE INTO ipd_medication_administration (PrescriptionId, IPDId, ScheduledDate, ScheduledSlot, Status)
                    SELECT rp.Id, rp.IPDId, @date, 'Morning', 'Pending'
                    FROM ipd_round_prescription rp
                    WHERE rp.IPDId = @ipd AND rp.Status = 'Active' AND rp.IsActive = 1 AND rp.Morning = 1
                    UNION ALL
                    SELECT rp.Id, rp.IPDId, @date, 'Afternoon', 'Pending'
                    FROM ipd_round_prescription rp
                    WHERE rp.IPDId = @ipd AND rp.Status = 'Active' AND rp.IsActive = 1 AND rp.Afternoon = 1
                    UNION ALL
                    SELECT rp.Id, rp.IPDId, @date, 'Evening', 'Pending'
                    FROM ipd_round_prescription rp
                    WHERE rp.IPDId = @ipd AND rp.Status = 'Active' AND rp.IsActive = 1 AND rp.Evening = 1";

                using (var cmd = new MySqlCommand(ensureSql, con))
                {
                    cmd.Parameters.AddWithValue("@ipd", ipdId);
                    cmd.Parameters.AddWithValue("@date", date.Date);
                    cmd.ExecuteNonQuery();
                }

                var list = new List<MARRowModel>();
                string selSql = @"
                    SELECT mar.Id AS MarId, mar.PrescriptionId, m.MedicineName, rp.Dosage, rp.Route,
                           mar.ScheduledDate, mar.ScheduledSlot, mar.Status, mar.GivenAt, mar.Notes,
                           CONCAT(n.FirstName, ' ', n.LastName) AS GivenByName
                    FROM ipd_medication_administration mar
                    INNER JOIN ipd_round_prescription rp ON rp.Id = mar.PrescriptionId
                    INNER JOIN tbl_medicine m ON m.MedicineId = rp.MedicineId
                    LEFT JOIN nurse n ON n.NurseId = mar.GivenBy
                    WHERE mar.IPDId = @ipd AND mar.ScheduledDate = @date
                    ORDER BY m.MedicineName, FIELD(mar.ScheduledSlot, 'Morning','Afternoon','Evening')";

                using (var cmd = new MySqlCommand(selSql, con))
                {
                    cmd.Parameters.AddWithValue("@ipd", ipdId);
                    cmd.Parameters.AddWithValue("@date", date.Date);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new MARRowModel
                            {
                                MarId = Convert.ToInt32(r["MarId"]),
                                PrescriptionId = Convert.ToInt32(r["PrescriptionId"]),
                                MedicineName = r["MedicineName"].ToString(),
                                Dosage = r["Dosage"] == DBNull.Value ? null : r["Dosage"].ToString(),
                                Route = r["Route"] == DBNull.Value ? null : r["Route"].ToString(),
                                ScheduledDate = Convert.ToDateTime(r["ScheduledDate"]),
                                ScheduledSlot = r["ScheduledSlot"].ToString(),
                                Status = r["Status"].ToString(),
                                GivenAt = r["GivenAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["GivenAt"]),
                                Notes = r["Notes"] == DBNull.Value ? null : r["Notes"].ToString(),
                                GivenByName = r["GivenByName"] == DBNull.Value ? null : r["GivenByName"].ToString()
                            });
                        }
                    }
                }
                return list;
            }
        }

        public void RecordAdministration(RecordAdministrationModel model)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(
                "UPDATE ipd_medication_administration SET Status = @status, GivenBy = @givenBy, GivenAt = NOW(), Notes = @notes WHERE Id = @id", con))
            {
                cmd.Parameters.AddWithValue("@status", model.Status);
                cmd.Parameters.AddWithValue("@givenBy", model.GivenBy);
                cmd.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(model.Notes) ? DBNull.Value : (object)model.Notes);
                cmd.Parameters.AddWithValue("@id", model.MarId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public MedicineOrderModel DiscontinueMedicine(int prescriptionId, int hospitalId, int? subHospitalId)
        {
            using (var con = new MySqlConnection(_connectionString))
            {
                con.Open();

                // Status is a strict ENUM('Active','Stopped','Completed') -
                // 'Stopped' is the correct discontinue value.
                using (var cmd = new MySqlCommand(
                    "UPDATE ipd_round_prescription SET Status = 'Stopped' WHERE Id = @id AND ParentHospitalId = @ph", con))
                {
                    cmd.Parameters.AddWithValue("@id", prescriptionId);
                    cmd.Parameters.AddWithValue("@ph", hospitalId);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new MySqlCommand(@"
                    SELECT rp.Id, rp.IPDId, rp.MedicineId, m.MedicineName, rp.Dosage
                    FROM ipd_round_prescription rp
                    INNER JOIN tbl_medicine m ON m.MedicineId = rp.MedicineId
                    WHERE rp.Id = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", prescriptionId);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            return new MedicineOrderModel
                            {
                                OrderId = Convert.ToInt32(r["Id"]),
                                IPDId = Convert.ToInt32(r["IPDId"]),
                                MedicineId = Convert.ToInt32(r["MedicineId"]),
                                MedicineName = r["MedicineName"].ToString(),
                                Dosage = r["Dosage"] == DBNull.Value ? null : r["Dosage"].ToString()
                            };
                        }
                    }
                }
                return null;
            }
        }
        public List<Symptom> GetSymptomsByRoundId(int roundId)
        {
            var list = new List<Symptom>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(@"
                SELECT s.SymptomId, s.SymptomName, s.SubName, s.Description
                FROM ipd_round_symptom rs
                INNER JOIN symptoms s ON s.SymptomId = rs.SymptomId
                WHERE rs.RoundId = @rid AND rs.IsActive = 1
                ", con))
            {
                cmd.Parameters.AddWithValue("@rid", roundId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new Symptom
                        {
                            SymptomId = Convert.ToInt32(r["SymptomId"]),
                            SymptomName = r["SymptomName"].ToString(),
                            SubName = r["SubName"] == DBNull.Value ? null : r["SubName"].ToString(),
                            Description = r["Description"] == DBNull.Value ? null : r["Description"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public List<Symptom> GetActiveSymptomsByIPD(int ipdId)
        {
            var list = new List<Symptom>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(@"
                SELECT s.SymptomId, s.SymptomName, s.SubName, s.Description
                FROM ipd_round_symptom rs
                INNER JOIN symptoms s ON s.SymptomId = rs.SymptomId
                WHERE rs.IPDId = @ipd AND rs.IsActive = 1
                GROUP BY s.SymptomId, s.SymptomName, s.SubName, s.Description
                ORDER BY MAX(rs.CreatedDate) DESC
                ", con))
            {
                cmd.Parameters.AddWithValue("@ipd", ipdId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new Symptom
                        {
                            SymptomId = Convert.ToInt32(r["SymptomId"]),
                            SymptomName = r["SymptomName"].ToString(),
                            SubName = r["SubName"] == DBNull.Value ? null : r["SubName"].ToString(),
                            Description = r["Description"] == DBNull.Value ? null : r["Description"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        // ============================================================
        // COMPLETED LAB REPORTS — visible to the doctor once the lab
        // uploads a result. sp_GetPendingLabOrdersByIPD only ever returns
        // Status IN ('Ordered','Collected'), so once LabInvestigationController
        // marks a test 'Completed' it silently disappears from Daily Notes
        // entirely. This is the missing counterpart.
        // ============================================================
        public List<LabInvestigationModel> GetCompletedLabReportsByIPD(int ipdId)
        {
            var list = new List<LabInvestigationModel>();
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(@"
                SELECT Id, IPDId, InvestigationType, TestName, Priority, Instructions,
                       Status, OrderedDateTime, CollectedDateTime, CompletedDateTime,
                       Result, ResultFilePath, UpdatedDate
                FROM ipd_round_investigation
                WHERE IPDId = @ipd AND IsActive = 1 AND Status = 'Completed'
                ORDER BY CompletedDateTime DESC
                ", con))
            {
                cmd.Parameters.AddWithValue("@ipd", ipdId);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new LabInvestigationModel
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            IPDId = Convert.ToInt32(r["IPDId"]),
                            InvestigationType = r["InvestigationType"].ToString(),
                            TestName = r["TestName"].ToString(),
                            Priority = r["Priority"].ToString(),
                            Instructions = r["Instructions"] == DBNull.Value ? null : r["Instructions"].ToString(),
                            Status = r["Status"].ToString(),
                            OrderedDateTime = Convert.ToDateTime(r["OrderedDateTime"]),
                            CollectedDateTime = r["CollectedDateTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CollectedDateTime"]),
                            CompletedDateTime = r["CompletedDateTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CompletedDateTime"]),
                            Result = r["Result"] == DBNull.Value ? null : r["Result"].ToString(),
                            ResultFilePath = r["ResultFilePath"] == DBNull.Value ? null : r["ResultFilePath"].ToString(),
                            UpdatedDate = r["UpdatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["UpdatedDate"])
                        });
                    }
                }
            }
            return list;
        }






        public List<MedicineOrderModel> GetPendingMedicinesByIPD(int ipdId)
        {
            var list = new List<MedicineOrderModel>();

            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_GetPendingMedicinesByIPD", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_ipd_id", ipdId);

                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new MedicineOrderModel
                        {
                            OrderId = System.Convert.ToInt32(r["Id"]),
                            IPDId = System.Convert.ToInt32(r["IPDId"]),
                            MedicineId = System.Convert.ToInt32(r["MedicineId"]),
                            MedicineName = r["MedicineName"].ToString(),
                            Morning = System.Convert.ToBoolean(r["Morning"]),
                            Afternoon = System.Convert.ToBoolean(r["Afternoon"]),
                            Evening = System.Convert.ToBoolean(r["Evening"]),
                            Days = r["Days"] == DBNull.Value ? (int?)null : System.Convert.ToInt32(r["Days"]),
                            Route = r["Route"] == DBNull.Value ? "Oral" : r["Route"].ToString(),
                            Dosage = r["Dosage"] == DBNull.Value ? "" : r["Dosage"].ToString(),
                            Instructions = r["Instructions"] == DBNull.Value ? "" : r["Instructions"].ToString(),
                            Status = r["Status"] == DBNull.Value ? "" : r["Status"].ToString(),
                            CreatedDate = System.Convert.ToDateTime(r["CreatedDate"])
                        });
                    }
                }
            }

            return list;
        }


        private int CreateDailyNotesRound(int parentHospitalId, int? subHospitalId, int ipdId, int doctorId)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_CreateDailyNotesRound", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_parent_hospital_id", parentHospitalId);
                cmd.Parameters.AddWithValue("p_sub_hospital_id", subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_ipd_id", ipdId);
                cmd.Parameters.AddWithValue("p_doctor_id", doctorId);

                var outParam = new MySqlParameter("p_round_id", MySqlDbType.Int32)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(outParam);

                con.Open();
                cmd.ExecuteNonQuery();

                return Convert.ToInt32(outParam.Value);
            }
        }


        public List<MedicineOrderModel> SearchMedicinesForDailyNotes(string searchTerm)
        {
            var list = new List<MedicineOrderModel>();

            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_SearchMedicinesForDailyNotes", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_search", string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : (object)searchTerm);

                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new MedicineOrderModel
                        {
                            MedicineId = Convert.ToInt32(r["MedicineId"]),
                            MedicineName = r["MedicineName"].ToString()
                        });
                    }
                }
            }

            return list;
        }




        // In your DailyNotesRepository
        public List<DoctorModel> GetDoctors()
        {
            var list = new List<DoctorModel>();
            using (var con = new MySqlConnection(_connectionString))
            {
                string sql = "SELECT Doctor_Id, CONCAT(FirstName, ' ', LastName) AS Name FROM doctor WHERE IsActive = 1";
                using (var cmd = new MySqlCommand(sql, con))
                {
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new DoctorModel
                            {
                                Doctor_Id = Convert.ToInt32(reader["Doctor_Id"]),
                                Name = reader["Name"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        // Get active nurses for Daily Notes dropdown
        public List<NurseDropdownModel> GetNurses()
        {
            var list = new List<NurseDropdownModel>();
            using (var con = new MySqlConnection(_connectionString))
            {
                string sql = "SELECT NurseId, CONCAT(FirstName, ' ', LastName) AS Name FROM nurse WHERE IsActive = 1";
                using (var cmd = new MySqlCommand(sql, con))
                {
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new NurseDropdownModel
                            {
                                NurseId = Convert.ToInt32(reader["NurseId"]),
                                Name = reader["Name"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }





    }
}