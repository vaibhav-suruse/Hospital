using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    // ── Dashboard Summary ────────────────────────────────
    public class PatientDashboardVM
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public int UpcomingAppointmentsCount { get; set; }
        public int TotalOPDVisits { get; set; }
        public int TotalIPDAdmissions { get; set; }
        public decimal TotalDueAmount { get; set; }
        public DateTime? NextAppointmentDate { get; set; }
        public string NextDoctorName { get; set; }
        public TimeSpan? NextAppointmentTime { get; set; }
        public List<PatientAppointmentVM> RecentAppointments { get; set; }
            = new List<PatientAppointmentVM>();
    }

    // ── Appointment ──────────────────────────────────────
    public class PatientAppointmentVM
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string DoctorSpecialization { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; }
        public int? OPDId { get; set; }
        public bool CanCancel =>
            Status == "Pending" &&
            AppointmentDate.Date >= DateTime.Today;
    }

    // ── OPD History ──────────────────────────────────────
    public class PatientOPDHistoryVM
    {
        public int OPDId { get; set; }
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string DoctorName { get; set; }
        public string DoctorSpecialization { get; set; }
        public string BP { get; set; }
        public string Pulse { get; set; }
        public string Investigation { get; set; }
        public string ReportDetail { get; set; }
        public string ReportFilePath { get; set; }
        public DateTime? NextAppointmentDate { get; set; }
        public List<string> Symptoms { get; set; }
            = new List<string>();
        public List<OPDMedicineVM> Medicines { get; set; }
            = new List<OPDMedicineVM>();
    }

    // ── IPD History ──────────────────────────────────────
    public class PatientIPDHistoryVM
    {
        public int IPDId { get; set; }
        public string AdmissionNumber { get; set; }
        public DateTime AdmissionDateTime { get; set; }
        public DateTime? DischargeDateTime { get; set; }
        public string Status { get; set; }
        public string ReasonForAdmission { get; set; }
        public string DoctorName { get; set; }
        public string WardName { get; set; }
        public string BedNumber { get; set; }
        public int TotalDays { get; set; }
        public bool HasBill { get; set; }
        public int? BillId { get; set; }
        public string PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
    }

    // ── Billing ──────────────────────────────────────────
    public class PatientBillingVM
    {
        public List<PatientOPDBillVM> OPDBills { get; set; }
            = new List<PatientOPDBillVM>();
        public List<PatientIPDHistoryVM> IPDBills { get; set; }
            = new List<PatientIPDHistoryVM>();
        public decimal TotalOPDAmount { get; set; }
        public decimal TotalIPDAmount { get; set; }
        public decimal TotalDue { get; set; }
    }

    public class PatientOPDBillVM
    {
        public int BillId { get; set; }
        public int AppointmentId { get; set; }
        public string BillNumber { get; set; }
        public DateTime BillDate { get; set; }
        public string DoctorName { get; set; }
        public decimal ConsultationFee { get; set; }
        public decimal MedicineCharges { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string PaymentStatus { get; set; }
    }

    // ── Book Appointment ─────────────────────────────────
    public class PatientBookAppointmentVM
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public DateTime AppointmentDate { get; set; }
            = DateTime.Today.AddDays(1);
        public TimeSpan AppointmentTime { get; set; }
        public string Relation { get; set; }
        public List<Doctor> AvailableDoctors { get; set; }
            = new List<Doctor>();
    }
}
