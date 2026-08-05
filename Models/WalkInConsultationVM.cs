using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
// ── View Model for the Walk-In Consultation page ──────────────
    public class WalkInConsultationVM
    {
        public List<WalkInConsultRow> TodayConsults { get; set; } = new List<WalkInConsultRow>();

        // ── Date-filter support (mirrors OPD Appointment Index) ─────
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public int TodayCount { get; set; }          // walk-ins today
        public int SelectedDateCount { get; set; }   // walk-ins on the filtered date
        public List<WalkInConsultRow> SelectedDateConsults { get; set; } = new List<WalkInConsultRow>();
    }

    // ── One row in today's consult table ──────────────────────────
    public class WalkInConsultRow
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string MobileNo { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; }
        public int OPDId { get; set; }   // 0 = no OPD yet
    }

    // ── Result returned by SearchPatient AJAX ─────────────────────
    public class PatientSearchResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
    }
}
