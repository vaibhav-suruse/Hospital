using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
   


    public class DoctorNoteModel
    {
        public int NoteId { get; set; }
        public int? RoundId { get; set; }
        public int IpId { get; set; }
        public decimal? Temperature { get; set; }
        public int? Pulse { get; set; }
        public int? RespRate { get; set; }
        public int? BpSystolic { get; set; }
        public int? BpDiastolic { get; set; }
        public decimal? SpO2 { get; set; }
        public string? Notes { get; set; }
        public DateTime NoteDate { get; set; } = DateTime.Today;
        public TimeSpan NoteTime { get; set; } = DateTime.Now.TimeOfDay;
        public int? TemplateId { get; set; }
        public string? TemplateName { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? DisplayDatetime { get; set; }

        // NEW: Doctor name for display
        public string? DoctorName { get; set; }

        // Display helpers
        public string NoteDateStr => NoteDate.ToString("dd/MM/yyyy");
        public string NoteTimeStr => DateTime.Today.Add(NoteTime).ToString("hh:mm tt");
        public string BpDisplay => (BpSystolic.HasValue && BpDiastolic.HasValue)
            ? $"{BpSystolic}/{BpDiastolic} mmHg" : "-";
    }

    public class NurseNoteModel
    {
        public int NoteId { get; set; }
        public int IpId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime NoteDate { get; set; } = DateTime.Today;
        public TimeSpan NoteTime { get; set; } = DateTime.Now.TimeOfDay;
        public int? TemplateId { get; set; }
        public string? TemplateName { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? DisplayDatetime { get; set; }


        public string? NurseName { get; set; }

    }
    public class NoteTemplateModel
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty; // "doctor" or "nurse"
        public string TemplateText { get; set; } = string.Empty;
    }
    public class DailyNotesViewModel
    {
        public int IpId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string ActiveTab { get; set; } = "doctor"; // "doctor" or "nurse"

        public List<DoctorNoteModel> DoctorNotes { get; set; } = new List<DoctorNoteModel>();

        public List<NurseNoteModel> NurseNotes { get; set; } = new List<NurseNoteModel>();

        public List<NoteTemplateModel> Templates { get; set; } = new List<NoteTemplateModel>();

        // Dropdown lists for Doctor & Nurse selection
        public List<DoctorModel> Doctors { get; set; } = new List<DoctorModel>();
        public List<NurseDropdownModel> Nurses { get; set; } = new List<NurseDropdownModel>();

    }
    public class DailyNotesVM
    {
        public int IpdId { get; set; }
        public int PatientId { get; set; }

        public List<DoctorNoteModel> DoctorNotes { get; set; }
        public List<NurseNoteModel> NurseNotes { get; set; }

        public List<NoteTemplateModel> DoctorTemplates { get; set; }
        public List<NoteTemplateModel> NurseTemplates { get; set; }
    }





    public class NoteHistoryModel
    {
        public int HistoryId { get; set; }
        public int NoteId { get; set; }
        public string NoteType { get; set; } = string.Empty;
        public string? PreviousNotes { get; set; }
        public decimal? PreviousTemperature { get; set; }
        public int? PreviousPulse { get; set; }
        public int? PreviousRespRate { get; set; }
        public int? PreviousBpSystolic { get; set; }
        public int? PreviousBpDiastolic { get; set; }
        public decimal? PreviousSpO2 { get; set; }
        public int ModifiedBy { get; set; }
        public string? ModifiedByName { get; set; }
        public DateTime ModifiedAt { get; set; }
        public string? ChangeSummary { get; set; }
    }
}
