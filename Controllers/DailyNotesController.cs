using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    [WebApplicationSampleTest2.Filters.RequireLogin]
    public class DailyNotesController : Controller
    {
        private readonly IDailyNotes _repo;
        // BUGFIX: needed so OrderMedicine can raise an IPD Pharmacy Queue
        // notification the same way DoctorRoundController does — without
        // this, medicines ordered from Daily Notes were saved correctly
        // but never appeared in the pharmacy queue.
        private readonly IDoctorRound _roundRepo;
        private readonly IDoctor _doctorRepo;
        private readonly IMedicine _medicineRepo;
        private readonly IIPDAdmission _ipdRepo;
        private readonly ISymptom _symptomRepo;

        public DailyNotesController(IDailyNotes repo, IDoctorRound roundRepo, IDoctor doctorRepo, IMedicine medicineRepo, IIPDAdmission ipdRepo, ISymptom symptomRepo)
        {
            _repo = repo;
            _roundRepo = roundRepo;
            _doctorRepo = doctorRepo;
            _medicineRepo = medicineRepo;
            _ipdRepo = ipdRepo;
            _symptomRepo = symptomRepo;
        }

        // --------------------------------------------------------
        // GET: /DailyNotes/DailyNotes?ipId=5&tab=doctor
        // --------------------------------------------------------
public IActionResult Index(int ipdId, int ipId, string tab = "doctor")
        {
            int idToUse = ipdId > 0 ? ipdId : ipId;

            // BUGFIX: previously, opening this page without a valid IPD
            // context (e.g. the tab bar losing route values) silently
            // rendered with idToUse = 0. Every downstream save then failed
            // its foreign-key constraint against ipdadmission and looked
            // like "medicine/lab isn't saving", with no clear indication
            // why. Fail fast and visibly instead.
            if (idToUse <= 0)
            {
                // Go back to wherever the user came from rather than guessing
                // a page name; fall back to a plain error message if there's
                // no referer to bounce to (e.g. page opened directly by URL).
                string referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    TempData["ErrorMessage"] = "No patient/admission was selected. Please open Daily Notes from a patient's IPD record.";
                    return Redirect(referer);
                }

                return Content("No patient/admission (IPD) was selected. Please open Daily Notes from a patient's IPD record, not directly by URL.");
            }

            // NEW — multi-hospital IPD data isolation. The IPD admission this
            // page reads/writes must belong to the current session's
            // hospital/sub-hospital. A user typing another hospital's ipdId
            // directly into the URL gets "Not Found" instead of that hospital's
            // notes (same as the Admission Notes tab).
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            if (_ipdRepo.GetIPDAdmissionById(idToUse, hospitalId, subHospitalId) == null)
                return NotFound();

            var vm = new DailyNotesViewModel
            {
                IpId = idToUse,
                ActiveTab = tab,
                DoctorNotes = _repo.GetDoctorNotes(idToUse),
                NurseNotes = _repo.GetNurseNotes(idToUse),
                Templates = _repo.GetTemplates(tab, string.Empty),
                Doctors = _repo.GetDoctors(),
                Nurses = _repo.GetNurses()
            };
            return View(vm);
        }

        // ============================================================
        // DOCTOR NOTES AJAX ENDPOINTS
        // ============================================================

        [HttpGet]
        public IActionResult GetDoctorNoteById(int noteId)
        {
            try
            {
                var note = _repo.GetDoctorNoteById(noteId);
                if (note == null) return NotFound();
                return Json(new { success = true, data = note });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SaveDoctorNote([FromBody] DoctorNoteModel model)
        {
            try
            {
                // ============================================
                // SERVER-SIDE VALIDATION
                // ============================================
                string validationError = ValidateDoctorNote(model);
                if (validationError != null)
                    return Json(new { success = false, message = validationError });

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                // BUGFIX: this used to be Session UserId (login account), not a
                // real Doctor_Id - which is why the "Doctor:" field on the Shift
                // Handover report was always blank (the join to the doctor table
                // never matched). Now it's the doctor actually selected in the
                // "Select Doctor" dropdown, sent from the client.
                if (model.CreatedBy <= 0)
                    return Json(new { success = false, message = "Please select a doctor." });

                if (model.NoteId == 0)
                {
                    // INSERT
                    int newId = _repo.InsertDoctorNote(model);

                    // BUGFIX: create (and link) exactly one round for this note,
                    // so any medicines/labs/symptoms added alongside it can be
                    // traced back to precisely this note later (History view) -
                    // instead of each one creating its own disconnected round.
                    int roundId = _repo.CreateRoundForNote(hospitalId, subHospitalId, model.IpId, model.CreatedBy, newId);

                    // Save template if checkbox checked
                    if (!string.IsNullOrEmpty(model.TemplateName))
                    {
                        _repo.InsertTemplate(new NoteTemplateModel
                        {
                            TemplateName = model.TemplateName,
                            TemplateType = "doctor",
                            TemplateText = model.Notes ?? ""
                        }, model.CreatedBy);
                    }

                    return Json(new { success = true, message = "Doctor note saved successfully.", noteId = newId, roundId = roundId });
                }
                else
                {
                    // UPDATE - Save audit trail BEFORE updating
                    var existingNote = _repo.GetDoctorNoteById(model.NoteId);
                    if (existingNote != null)
                    {
                        var history = new NoteHistoryModel
                        {
                            NoteId = model.NoteId,
                            NoteType = "doctor",
                            PreviousNotes = existingNote.Notes,
                            PreviousTemperature = existingNote.Temperature,
                            PreviousPulse = existingNote.Pulse,
                            PreviousRespRate = existingNote.RespRate,
                            PreviousBpSystolic = existingNote.BpSystolic,
                            PreviousBpDiastolic = existingNote.BpDiastolic,
                            PreviousSpO2 = existingNote.SpO2,
                            ModifiedBy = model.CreatedBy,
                            ChangeSummary = GenerateChangeSummary(existingNote, model)
                        };
                        _repo.InsertNoteHistory(history);
                    }

                    // Preserve the note's original author/round - editing text
                    // shouldn't reassign either.
                    model.CreatedBy = existingNote?.CreatedBy ?? model.CreatedBy;
                    int? roundIdForEdit = existingNote?.RoundId;
                    if (roundIdForEdit == null || roundIdForEdit <= 0)
                    {
                        // Older note saved before this fix - back-fill a round now.
                        roundIdForEdit = _repo.CreateRoundForNote(hospitalId, subHospitalId, model.IpId, model.CreatedBy, model.NoteId);
                    }

                    _repo.UpdateDoctorNote(model);
                    return Json(new { success = true, message = "Doctor note updated successfully.", noteId = model.NoteId, roundId = roundIdForEdit });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteDoctorNote(int noteId)
        {
            try
            {
                // Role check - only doctors can delete doctor notes
                string userRole = HttpContext.Session.GetString("UserRole") ?? "";
                if (userRole != "Doctor" && userRole != "Admin")
                    return Json(new { success = false, message = "Unauthorized: Only doctors can delete doctor notes." });

                // BUGFIX: deleting a note previously left its linked round's
                // medicines/labs/symptoms in the database but permanently
                // unreachable from the UI (no note left to view them
                // through), while they'd keep affecting Pharmacy Queue etc.
                // Block deletion instead if anything is still active.
                var note = _repo.GetDoctorNoteById(noteId);
                if (note?.RoundId.HasValue == true && note.RoundId.Value > 0)
                {
                    var meds = _repo.GetMedicinesByRoundId(note.RoundId.Value).Where(m => m.Status == "Active").ToList();
                    var labs = _repo.GetLabOrdersByRoundId(note.RoundId.Value).Where(l => l.Status != "Completed" && l.Status != "Stopped").ToList();
                    if (meds.Count > 0 || labs.Count > 0)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Can't delete: this note has {meds.Count} active medicine(s) and {labs.Count} pending lab order(s) linked to it. Discontinue/complete them first, or they'll become unreachable."
                        });
                    }
                }

                _repo.DeleteDoctorNote(noteId);
                return Json(new { success = true, message = "Doctor note deleted." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetDoctorNotesList(int ipId)
        {
            try
            {
                var notes = _repo.GetDoctorNotes(ipId);
                return Json(new { success = true, data = notes });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // NURSE NOTES AJAX ENDPOINTS
        // ============================================================

        [HttpGet]
        public IActionResult GetNurseNoteById(int noteId)
        {
            try
            {
                var note = _repo.GetNurseNoteById(noteId);
                if (note == null) return NotFound();
                return Json(new { success = true, data = note });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SaveNurseNote([FromBody] NurseNoteModel model)
        {
            try
            {
                // ============================================
                // SERVER-SIDE VALIDATION
                // ============================================
                string validationError = ValidateNurseNote(model);
                if (validationError != null)
                    return Json(new { success = false, message = validationError });

                model.CreatedBy = GetCurrentUserId();

                if (model.NoteId == 0)
                {
                    // INSERT
                    int newId = _repo.InsertNurseNote(model);

                    // Save template if checkbox checked
                    if (!string.IsNullOrEmpty(model.TemplateName))
                    {
                        _repo.InsertTemplate(new NoteTemplateModel
                        {
                            TemplateName = model.TemplateName,
                            TemplateType = "nurse",
                            TemplateText = model.Notes
                        }, model.CreatedBy);
                    }

                    return Json(new { success = true, message = "Nurse note saved successfully.", noteId = newId });
                }
                else
                {
                    // UPDATE - Save audit trail BEFORE updating
                    var existingNote = _repo.GetNurseNoteById(model.NoteId);
                    if (existingNote != null)
                    {
                        var history = new NoteHistoryModel
                        {
                            NoteId = model.NoteId,
                            NoteType = "nurse",
                            PreviousNotes = existingNote.Notes,
                            ModifiedBy = model.CreatedBy,
                            ChangeSummary = "Nurse note content updated"
                        };
                        _repo.InsertNoteHistory(history);
                    }

                    _repo.UpdateNurseNote(model);
                    return Json(new { success = true, message = "Nurse note updated successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteNurseNote(int noteId)
        {
            try
            {
                // Role check - only nurses can delete nurse notes
                string userRole = HttpContext.Session.GetString("UserRole") ?? "";
                if (userRole != "Nurse" && userRole != "Admin")
                    return Json(new { success = false, message = "Unauthorized: Only nurses can delete nurse notes." });

                _repo.DeleteNurseNote(noteId);
                return Json(new { success = true, message = "Nurse note deleted." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetNurseNotesList(int ipId)
        {
            try
            {
                var notes = _repo.GetNurseNotes(ipId);
                return Json(new { success = true, data = notes });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // TEMPLATES
        // ============================================================

        [HttpGet]
        public IActionResult GetTemplates(string type, string search = null)
        {
            try
            {
                var templates = _repo.GetTemplates(type, search);
                return Json(new { success = true, data = templates });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SaveTemplate([FromBody] NoteTemplateModel model)
        {
            try
            {
                int newId = _repo.InsertTemplate(model, GetCurrentUserId());
                return Json(new { success = true, message = "Template saved.", templateId = newId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // AUDIT TRAIL ENDPOINT
        // ============================================================

        [HttpGet]
        public IActionResult GetNoteHistory(int noteId, string noteType)
        {
            try
            {
                var history = _repo.GetNoteHistory(noteId, noteType);
                return Json(new { success = true, data = history });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // PRIVATE HELPERS
        // ============================================================

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 1;
        }

        private string ValidateDoctorNote(DoctorNoteModel model)
        {
            // Required notes
            if (string.IsNullOrWhiteSpace(model.Notes))
                return "Doctor notes cannot be empty.";

            // Temperature range (94-110°F)
            if (model.Temperature.HasValue && (model.Temperature < 94 || model.Temperature > 110))
                return "Temperature must be between 94°F and 110°F.";

            // Pulse range (30-250)
            if (model.Pulse.HasValue && (model.Pulse < 30 || model.Pulse > 250))
                return "Pulse must be between 30 and 250.";

            // Resp Rate range (5-60)
            if (model.RespRate.HasValue && (model.RespRate < 5 || model.RespRate > 60))
                return "Respiratory rate must be between 5 and 60.";

            // BP range
            if (model.BpSystolic.HasValue && (model.BpSystolic < 50 || model.BpSystolic > 300))
                return "Systolic BP must be between 50 and 300.";
            if (model.BpDiastolic.HasValue && (model.BpDiastolic < 30 || model.BpDiastolic > 200))
                return "Diastolic BP must be between 30 and 200.";

            // SpO2 range (50-100)
            if (model.SpO2.HasValue && (model.SpO2 < 50 || model.SpO2 > 100))
                return "SpO2 must be between 50% and 100%.";

            // Date validation - cannot be in the future
            if (model.NoteDate > DateTime.Today)
                return "Note date cannot be in the future.";

            return null; // No validation error
        }

        private string ValidateNurseNote(NurseNoteModel model)
        {
            // Required notes
            if (string.IsNullOrWhiteSpace(model.Notes))
                return "Nurse notes cannot be empty.";

            // Date validation - cannot be in the future
            if (model.NoteDate > DateTime.Today)
                return "Note date cannot be in the future.";

            return null; // No validation error
        }

        private string GenerateChangeSummary(DoctorNoteModel old, DoctorNoteModel updated)
        {
            var changes = new System.Collections.Generic.List<string>();

            if (old.Temperature != updated.Temperature)
                changes.Add($"Temperature: {old.Temperature}°F → {updated.Temperature}°F");
            if (old.Pulse != updated.Pulse)
                changes.Add($"Pulse: {old.Pulse} → {updated.Pulse}");
            if (old.RespRate != updated.RespRate)
                changes.Add($"RR: {old.RespRate} → {updated.RespRate}");
            if (old.BpSystolic != updated.BpSystolic || old.BpDiastolic != updated.BpDiastolic)
                changes.Add($"BP: {old.BpSystolic}/{old.BpDiastolic} → {updated.BpSystolic}/{updated.BpDiastolic}");
            if (old.SpO2 != updated.SpO2)
                changes.Add($"SpO2: {old.SpO2}% → {updated.SpO2}%");
            if (old.Notes != updated.Notes)
                changes.Add("Notes content updated");

            return changes.Count > 0 ? string.Join("; ", changes) : "No significant changes detected";
        }

        // ============================================================
        // VITALS TREND & ALERTS
        // ============================================================

        [HttpGet]
        public IActionResult GetVitalsTrend(int ipId)
        {
            try
            {
                var vitalsRepo = HttpContext.RequestServices.GetService<IIPDNurseVitals>();
                // BUGFIX: same wrong session key as GetHandoverReport had -
                // "HospitalId" is never set anywhere else in this controller.
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var nurseVitals = vitalsRepo.GetVitalsByIPDId(ipId, hospitalId, subHospitalId)
                    .Where(v => v.RecordedDateTime >= DateTime.Now.AddDays(-7))
                    .Select(v => new
                    {
                        recordedAt = v.RecordedDateTime,
                        temperature = v.Temperature,
                        pulse = v.Pulse,
                        systolic = v.Systolic,
                        diastolic = v.Diastolic,
                        oxygenSaturation = v.OxygenSaturation,
                        respirationRate = v.RespirationRate,
                        isAbnormal = v.IsAbnormal
                    });

                // BUGFIX: this chart previously ONLY looked at the separate
                // nurse-vitals table, so a patient whose vitals were only
                // ever recorded on the doctor note itself showed "No vitals
                // data available" even with vitals sitting right there.
                // Merge both sources into one chronological trend.
                var doctorVitals = _repo.GetDoctorNotes(ipId)
                    .Where(n => n.NoteDate >= DateTime.Now.AddDays(-7) &&
                                (n.Temperature.HasValue || n.Pulse.HasValue || n.BpSystolic.HasValue || n.SpO2.HasValue || n.RespRate.HasValue))
                    .Select(n => new
                    {
                        recordedAt = n.NoteDate.Date.Add(n.NoteTime),
                        temperature = n.Temperature.HasValue ? (decimal?)Math.Round((n.Temperature.Value - 32) * 5 / 9, 1) : null,
                        pulse = n.Pulse,
                        systolic = n.BpSystolic,
                        diastolic = n.BpDiastolic,
                        oxygenSaturation = n.SpO2.HasValue ? (int?)Math.Round(n.SpO2.Value) : null,
                        respirationRate = n.RespRate,
                        isAbnormal = false
                    });

                var merged = nurseVitals
                    .Select(v => new { v.recordedAt, v.temperature, v.pulse, v.systolic, v.diastolic, v.oxygenSaturation, v.respirationRate, v.isAbnormal })
                    .Concat(doctorVitals.Select(v => new { v.recordedAt, v.temperature, v.pulse, v.systolic, v.diastolic, v.oxygenSaturation, v.respirationRate, v.isAbnormal }))
                    .OrderBy(v => v.recordedAt)
                    .ToList();

                var chartData = new
                {
                    labels = merged.Select(v => v.recordedAt.ToString("dd/MM HH:mm")).ToList(),
                    temperature = merged.Select(v => v.temperature).ToList(),
                    pulse = merged.Select(v => v.pulse).ToList(),
                    systolic = merged.Select(v => v.systolic).ToList(),
                    diastolic = merged.Select(v => v.diastolic).ToList(),
                    spo2 = merged.Select(v => v.oxygenSaturation).ToList(),
                    respiration = merged.Select(v => v.respirationRate).ToList(),

                    alerts = merged
                        .Where(v => v.isAbnormal)
                        .Select(v => new
                        {
                            time = v.recordedAt.ToString("dd/MM HH:mm"),
                            message = GetAbnormalMessageFromValues(v.temperature, v.pulse, v.systolic, v.diastolic, v.oxygenSaturation)
                        }).ToList(),

                    latestVitals = merged.LastOrDefault()
                };

                return Json(new { success = true, data = chartData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GetAbnormalMessageFromValues(decimal? temperature, int? pulse, int? systolic, int? diastolic, int? spo2)
        {
            var messages = new List<string>();
            if (temperature.HasValue && (temperature < 36 || temperature > 38))
                messages.Add($"Temperature: {temperature}°C {(temperature > 38 ? "High" : "Low")}");
            if (pulse.HasValue && (pulse < 60 || pulse > 100))
                messages.Add($"Pulse: {pulse} bpm {(pulse > 100 ? "High" : "Low")}");
            if (systolic.HasValue && systolic > 140)
                messages.Add($"BP: {systolic}/{diastolic} mmHg High");
            if (spo2.HasValue && spo2 < 95)
                messages.Add($"SpO2: {spo2}% Low");
            return string.Join(", ", messages);
        }

        private string GetAbnormalMessage(IPDNurseVitals v)
        {
            var messages = new List<string>();

            if (v.Temperature.HasValue && (v.Temperature < 36 || v.Temperature > 38))
                messages.Add($"Temperature: {v.Temperature}°C {(v.Temperature > 38 ? "🔥 High" : "❄️ Low")}");

            if (v.Pulse.HasValue && (v.Pulse < 60 || v.Pulse > 100))
                messages.Add($"Pulse: {v.Pulse} bpm {(v.Pulse > 100 ? "⚡ High" : "🐢 Low")}");

            if (v.Systolic.HasValue && v.Systolic > 140)
                messages.Add($"BP: {v.Systolic}/{v.Diastolic} mmHg ⚠️ High");

            if (v.Diastolic.HasValue && v.Diastolic > 90)
                messages.Add($"BP: {v.Systolic}/{v.Diastolic} mmHg ⚠️ High Diastolic");

            if (v.OxygenSaturation.HasValue && v.OxygenSaturation < 95)
                messages.Add($"SpO2: {v.OxygenSaturation}% 🫁 Low Oxygen");

            return messages.Count > 0 ? string.Join(" | ", messages) : "Abnormal vitals detected";
        }



        // ============================================================
        // MEDICATION ORDERS FROM NOTES
        // ============================================================

        [HttpGet]
        public IActionResult SearchMedicines(string searchTerm)
        {
            try
            {
                var data = _repo.SearchMedicinesForDailyNotes(searchTerm);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }







        // ============================================================
        // SHIFT HANDOVER REPORT
        // ============================================================
        [HttpGet]
        public IActionResult GetHandoverReport(int ipId)
        {
            try
            {
                // Get services
                var admissionRepo = HttpContext.RequestServices.GetService<IIPDAdmission>();
                var vitalsRepo = HttpContext.RequestServices.GetService<IIPDNurseVitals>();

                // BUGFIX: was reading Session["HospitalId"], which is never set
                // anywhere else in this controller - every other action uses
                // "MainHospitalId". Fixed for consistency.
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var admission = admissionRepo.GetIPDAdmissionById(ipId, hospitalId, subHospitalId);

                // Get today's doctor notes
                var doctorNotes = _repo.GetDoctorNotes(ipId)
                    .Where(n => n.NoteDate.Date == DateTime.Today)
                    .ToList();
                var latestDoctorNote = doctorNotes.OrderByDescending(n => n.NoteDate).ThenByDescending(n => n.NoteTime).FirstOrDefault();

                // BUGFIX: this pulled ONLY from the separate nurse-vitals
                // table, ignoring the vitals recorded directly on the doctor
                // note itself - so "No vitals" showed even when the note
                // clearly had Temp/Pulse/RR/BP/SpO2 on it. Fall back to the
                // most recent doctor note's own vitals if nurse vitals are empty.
                var vitals = vitalsRepo.GetVitalsByIPDId(ipId, hospitalId, subHospitalId);
                var latestNurseVitals = vitals.OrderByDescending(v => v.RecordedDateTime).FirstOrDefault();

                object latestVitalsForReport = null;
                if (latestNurseVitals != null)
                {
                    latestVitalsForReport = new
                    {
                        temperature = latestNurseVitals.Temperature,
                        pulse = latestNurseVitals.Pulse,
                        bp = latestNurseVitals.Systolic.HasValue ? $"{latestNurseVitals.Systolic}/{latestNurseVitals.Diastolic}" : "",
                        spo2 = latestNurseVitals.OxygenSaturation,
                        respiration = latestNurseVitals.RespirationRate,
                        recordedTime = latestNurseVitals.RecordedDateTime.ToString("HH:mm")
                    };
                }
                else if (latestDoctorNote != null && (latestDoctorNote.Temperature.HasValue || latestDoctorNote.Pulse.HasValue || latestDoctorNote.BpSystolic.HasValue || latestDoctorNote.SpO2.HasValue))
                {
                    decimal? tempInCelsius = latestDoctorNote.Temperature.HasValue
                        ? (decimal?)Math.Round((latestDoctorNote.Temperature.Value - 32) * 5 / 9, 1)
                        : null;
                    latestVitalsForReport = new
                    {
                        temperature = tempInCelsius,
                        pulse = latestDoctorNote.Pulse,
                        bp = latestDoctorNote.BpSystolic.HasValue ? $"{latestDoctorNote.BpSystolic}/{latestDoctorNote.BpDiastolic}" : "",
                        spo2 = latestDoctorNote.SpO2,
                        respiration = latestDoctorNote.RespRate,
                        recordedTime = latestDoctorNote.NoteTime.ToString(@"hh\:mm")
                    };
                }

                // Get today's nurse notes
                var nurseNotes = _repo.GetNurseNotes(ipId)
                    .Where(n => n.NoteDate.Date == DateTime.Today)
                    .ToList();

                // Build report
                var report = new
                {
                    // S - Situation
                    patientName = admission?.PatientName ?? "Unknown",
                    admissionNumber = admission?.AdmissionNumber ?? "",
                    admissionDate = admission?.AdmissionDateTime.ToString("dd/MM/yyyy HH:mm"),
                    diagnosis = admission?.ReasonForAdmission ?? "",
                    // BUGFIX: was ONLY admission.DoctorName (the admission
                    // record's assigned primary doctor field) which is often
                    // left blank at admission time. Fall back to whoever
                    // actually wrote today's most recent doctor note.
                    primaryDoctor = !string.IsNullOrWhiteSpace(admission?.DoctorName)
                        ? admission.DoctorName
                        : (latestDoctorNote?.DoctorName ?? ""),

                    // B - Background
                    latestVitals = latestVitalsForReport,

                    // A - Assessment
                    recentDoctorNotes = doctorNotes.Select(n => new
                    {
                        time = n.DisplayDatetime,
                        doctor = n.DoctorName,
                        notes = n.Notes
                    }),
                    recentNurseNotes = nurseNotes.Select(n => new
                    {
                        time = n.DisplayDatetime,
                        nurse = n.NurseName,
                        notes = n.Notes
                    }),
                    abnormalVitalsCount = vitals.Count(v => v.IsAbnormal),

                    // R - Recommendation
                    pendingMedicines = new List<object>(),
                    pendingLabs = new List<object>()
                };

                return Json(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        // ===================== LAB SEARCH =====================
        [HttpGet]
        public IActionResult SearchLabTests(string searchTerm)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                if (hospitalId <= 0)
                    return Json(new { success = false, message = "HospitalId missing in session." });

                var data = _repo.SearchLabTests(hospitalId, subHospitalId, searchTerm);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===================== LAB ORDER =====================



        [HttpPost]
        public IActionResult OrderLab([FromBody] LabOrderModel model)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                // BUGFIX: this previously used Session["UserId"] (the logged-in
                // account's ID), which is NOT a Doctor_Id. ipd_doctor_round.DoctorId
                // has a foreign key to the doctor table, so every lab order failed
                // with a silent FK constraint error. Use the doctor actually
                // selected in the UI instead - same as OrderMedicine already does.
                int doctorId = model.OrderedBy;

                if (hospitalId <= 0) return Json(new { success = false, message = "MainHospitalId missing." });
                if (doctorId <= 0) return Json(new { success = false, message = "Please select a doctor." });
                if (model == null) return Json(new { success = false, message = "Invalid model." });

                _repo.InsertDailyNotesLabOrder(hospitalId, subHospitalId, model, doctorId);

                return Json(new { success = true, message = "Lab order placed." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        // ===================== PENDING LAB ORDERS =====================
        [HttpGet]
        public IActionResult GetPendingLabOrders(int ipId)
        {
            try
            {
                var data = _repo.GetPendingLabOrdersByIPD(ipId);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===================== COMPLETED LAB REPORTS =====================
        // Shows results/uploaded reports once the lab marks a test 'Completed'.
        // Without this, a doctor has no way to see a finished report from
        // inside Daily Notes at all - it just silently drops off the
        // "Pending" list with nothing to replace it.
        [HttpGet]
        public IActionResult GetCompletedLabReports(int ipId)
        {
            try
            {
                var data = _repo.GetCompletedLabReportsByIPD(ipId);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===================== NOTE HISTORY (per-note detail) =====================
        // This is what the "History" button opens - everything tied to ONE
        // specific note/round, not the IPD-wide "everything ever ordered" view.
        [HttpGet]
        public IActionResult GetNoteDetail(int noteId)
        {
            try
            {
                var note = _repo.GetDoctorNoteById(noteId);
                if (note == null) return Json(new { success = false, message = "Note not found." });

                var medicines = new List<MedicineOrderModel>();
                var labs = new List<LabInvestigationModel>();
                var symptoms = new List<Symptom>();

                if (note.RoundId.HasValue && note.RoundId.Value > 0)
                {
                    medicines = _repo.GetMedicinesByRoundId(note.RoundId.Value);
                    labs = _repo.GetLabOrdersByRoundId(note.RoundId.Value);
                    symptoms = _repo.GetSymptomsByRoundId(note.RoundId.Value);
                }

                return Json(new { success = true, note = note, medicines = medicines, labs = labs, symptoms = symptoms });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public IActionResult SearchSymptoms(string searchTerm)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var all = _symptomRepo.GetAllSymptoms(hospitalId, subHospitalId) ?? new List<Symptom>();
                var filtered = string.IsNullOrWhiteSpace(searchTerm)
                    ? all.Take(20).ToList()
                    : all.Where(s => s.SymptomName != null &&
                                      s.SymptomName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                         .Take(20).ToList();

                return Json(new { success = true, data = filtered });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult OrderSymptom([FromBody] SymptomOrderModel model)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                if (hospitalId <= 0) return Json(new { success = false, message = "MainHospitalId missing." });
                if (model == null || model.SymptomId <= 0) return Json(new { success = false, message = "Please select a symptom." });
                if (model.DoctorId <= 0) return Json(new { success = false, message = "Please select a doctor." });

                _repo.InsertDailyNotesSymptom(hospitalId, subHospitalId, model.IPDId, model.SymptomId, model.DoctorId, model.RoundId);

                return Json(new { success = true, message = "Symptom added." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetActiveSymptoms(int ipId)
        {
            try
            {
                var data = _repo.GetActiveSymptomsByIPD(ipId);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===================== MEDICINE ORDER =====================
        //[HttpPost]
        //public IActionResult OrderMedicine([FromBody] MedicineOrderModel model)
        //{
        //    try
        //    {
        //        int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //        int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

        //        // You must have DoctorId in session for FK round creation:
        //        int doctorId = HttpContext.Session.GetInt32("DoctorId") ?? 0;

        //        if (hospitalId <= 0) return Json(new { success = false, message = "MainHospitalId missing in session." });
        //        if (doctorId <= 0) return Json(new { success = false, message = "DoctorId missing in session." });
        //        if (model == null || model.MedicineId <= 0) return Json(new { success = false, message = "MedicineId is required." });

        //        _repo.InsertDailyNotesMedicine(hospitalId, subHospitalId, model, doctorId);

        //        return Json(new { success = true, message = "Medicine order placed." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}



        [HttpPost]
        public IActionResult OrderMedicine([FromBody] MedicineOrderModel model)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                // Use DoctorId from the model (sent from JavaScript dropdown)
                int doctorId = model.DoctorId;

                if (hospitalId <= 0) return Json(new { success = false, message = "MainHospitalId missing." });
                if (doctorId <= 0) return Json(new { success = false, message = "Please select a doctor." });
                if (model == null || model.MedicineId <= 0) return Json(new { success = false, message = "MedicineId required." });

                int roundId = _repo.InsertDailyNotesMedicine(hospitalId, subHospitalId, model, doctorId);

                // BUGFIX: this is the piece that was missing. Saving into
                // ipd_round_prescription alone does NOT make a medicine show
                // up in IPD Pharmacy Queue — that screen reads from a
                // separate `medicine_notifications` table that has to be
                // explicitly populated. The old Doctor Round module did this;
                // Daily Notes never did, so pharmacy never saw these orders.
                try
                {
                    var admission = _ipdRepo.GetIPDAdmissionById(model.IPDId, hospitalId, subHospitalId);
                    var doctor = _doctorRepo.GetAllDoctor(hospitalId, subHospitalId)
                                             ?.FirstOrDefault(d => d.Doctor_Id == doctorId);
                    string doctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}".Trim() : "";

                    var medicine = _medicineRepo.GetAllMedicine(hospitalId, subHospitalId)
                                                 ?.FirstOrDefault(m => m.MedicineId == model.MedicineId);

                    _roundRepo.InsertIPDPharmacyNotification(new MedicineNotificationModel
                    {
                        PatientId = admission?.PatientId ?? 0,
                        PatientName = admission?.PatientName ?? "",
                        IPDId = model.IPDId,
                        RoundId = roundId,
                        DoctorName = doctorName,
                        MedicineCount = 1,
                        MedicinesSummary = medicine?.MedicineName ?? "",
                        Type = "IPD",
                        HospitalId = hospitalId,
                        SubHospitalId = subHospitalId
                    });
                }
                catch
                {
                    // Non-blocking: the prescription itself already saved
                    // successfully above; a notification failure shouldn't
                    // make the whole order look like it failed.
                }

                return Json(new { success = true, message = "Medicine order placed." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===================== CURRENT MEDICATIONS (aggregate, across all notes) =====================
        // Reinstated by request: per-note History answers "what did THIS note
        // add", but a real round also needs "what is this patient on RIGHT
        // NOW, across everything" in one place - which is what this is for.
        [HttpGet]
        public IActionResult GetCurrentMedications(int ipId)
        {
            try
            {
                var data = _repo.GetPendingMedicinesByIPD(ipId);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===================== DISCONTINUE MEDICINE =====================
        [HttpPost]
        public IActionResult DiscontinueMedicine([FromBody] DiscontinueMedicineModel model)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
                int doctorId = model?.DoctorId ?? 0;

                if (hospitalId <= 0) return Json(new { success = false, message = "MainHospitalId missing." });
                if (model == null || model.PrescriptionId <= 0) return Json(new { success = false, message = "PrescriptionId required." });
                if (doctorId <= 0) return Json(new { success = false, message = "Please select a doctor." });

                var stopped = _repo.DiscontinueMedicine(model.PrescriptionId, hospitalId, subHospitalId);

                // Tell pharmacy this was stopped too, not just ordered -
                // otherwise a discontinued medicine keeps getting dispensed.
                try
                {
                    if (stopped != null)
                    {
                        var admission = _ipdRepo.GetIPDAdmissionById(stopped.IPDId, hospitalId, subHospitalId);
                        var doctor = _doctorRepo.GetAllDoctor(hospitalId, subHospitalId)
                                                 ?.FirstOrDefault(d => d.Doctor_Id == doctorId);
                        string doctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}".Trim() : "";

                        _roundRepo.InsertIPDPharmacyNotification(new MedicineNotificationModel
                        {
                            PatientId = admission?.PatientId ?? 0,
                            PatientName = admission?.PatientName ?? "",
                            IPDId = stopped.IPDId,
                            DoctorName = doctorName,
                            MedicineCount = 1,
                            MedicinesSummary = "DISCONTINUE: " + stopped.MedicineName,
                            Type = "IPD",
                            HospitalId = hospitalId,
                            SubHospitalId = subHospitalId
                        });
                    }
                }
                catch
                {
                    // Non-blocking - the discontinue itself already succeeded.
                }

                return Json(new { success = true, message = "Medicine discontinued." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===================== MAR (Medication Administration Record) =====================
        [HttpGet]
        public IActionResult GetMAR(int ipId, string date)
        {
            try
            {
                DateTime scheduledDate = string.IsNullOrEmpty(date) ? DateTime.Today : DateTime.Parse(date);
                var data = _repo.GetOrCreateMARForDate(ipId, scheduledDate);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult RecordAdministration([FromBody] RecordAdministrationModel model)
        {
            try
            {
                if (model == null || model.MarId <= 0) return Json(new { success = false, message = "MarId required." });
                if (model.GivenBy <= 0) return Json(new { success = false, message = "Please select who is recording this." });
                if (model.Status != "Given" && model.Status != "Missed" && model.Status != "Refused")
                    return Json(new { success = false, message = "Invalid status." });

                _repo.RecordAdministration(model);
                return Json(new { success = true, message = "Recorded." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }





        // ===================== PENDING MEDICINES =====================
        [HttpGet]
        public IActionResult GetPendingMedicines(int ipId)
        {
            try
            {
                var data = _repo.GetPendingMedicinesByIPD(ipId);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // Add this action to load the Daily Notes page
        public IActionResult DailyNotes(int ipdId)
        {
            // Get doctors
            var doctors = _repo.GetDoctors(); // You need to add this method
            ViewBag.Doctors = doctors;



            ViewBag.IPDId = ipdId;
            return View();
        }



    }
}
