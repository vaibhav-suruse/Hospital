using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplicationSampleTest2.Models
{
    // ── Vitals ────────────────────────────────────────────────────────
    public class PatientVitals
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int HospitalId { get; set; }
        public int? AppointmentId { get; set; }
        public decimal? Temperature { get; set; }
        public int? PulseRate { get; set; }
        public int? BPSystolic { get; set; }
        public int? BPDiastolic { get; set; }
        public decimal? SPO2 { get; set; }
        public decimal? RBS { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public decimal? BMI { get; set; }
        public int? Respiration { get; set; }
        public decimal? WaistCircumference { get; set; }
        public decimal? FIB4 { get; set; }
        public DateTime RecordedDate { get; set; }
    }

    // ── Medical Condition ─────────────────────────────────────────────
    public class PatientMedicalCondition
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int HospitalId { get; set; }
        public string ConditionName { get; set; }
        public int? SinceDays { get; set; }         // numeric value entered by user
        public string SinceUnit { get; set; } = "Days"; // Hours / Days / Months / Years
        public string Status { get; set; } = "Active";   // Active / Resolved
        public bool OnMedication { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // ── Allergy ───────────────────────────────────────────────────────
    public class PatientAllergy
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int HospitalId { get; set; }
        public string AllergyTo { get; set; }
        public int? SinceHours { get; set; }        // stored in hours (computed from SinceValue+SinceUnit)
        public int? SinceValue { get; set; }        // number the user typed
        public string SinceUnit { get; set; } = "Hours"; // Hours / Days / Months / Years
        public string Status { get; set; } = "Active";
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // ── Surgical History ──────────────────────────────────────────────
    public class PatientSurgicalHistory
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int HospitalId { get; set; }
        public string ProcedureName { get; set; }
        public DateTime? SurgeryDate { get; set; }
        public string Location { get; set; }
        public string Notes { get; set; }
    }

    // ── Family History ────────────────────────────────────────────────
    public class PatientFamilyHistory
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int HospitalId { get; set; }
        public string Relation { get; set; }
        public string ConditionName { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // ── Lab Result ────────────────────────────────────────────────────
    public class PatientLabResult
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int HospitalId { get; set; }
        public int? AppointmentId { get; set; }
        public string TestName { get; set; }
        public string ResultValue { get; set; }
        public string Unit { get; set; }
        public string NormalRange { get; set; }
        public bool IsAbnormal { get; set; }
        public DateTime ResultDate { get; set; }
        public string FilePath { get; set; }
        public string Notes { get; set; }
    }

    // ── Full Patient History (left panel) ─────────────────────────────
    public class PatientHistoryVM
    {
        public List<PatientMedicalCondition> Conditions { get; set; } = new List<PatientMedicalCondition>();
        public List<PatientAllergy> Allergies { get; set; } = new List<PatientAllergy>();
        public List<PatientSurgicalHistory> Surgeries { get; set; } = new List<PatientSurgicalHistory>();
        public List<PatientFamilyHistory> FamilyHistory { get; set; } = new List<PatientFamilyHistory>();
        public PatientVitals LatestVitals { get; set; }
        public List<PatientLabResult> LabResults { get; set; } = new List<PatientLabResult>();
    }

    // ── FULL PATIENT HISTORY DASHBOARD ──────────────────────────────
    // Aggregates patient demographics + every OPD visit + clinical history.
    public class PatientFullHistoryVM
    {
        // Patient
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        public string PhoneNumber { get; set; }
        public string BloodGroup { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string MaritalStatus { get; set; }
        public string Occupation { get; set; }

        // Visits (latest first)
        public List<OPD> Visits { get; set; } = new List<OPD>();

        // Summary counts
        public int TotalVisits => Visits.Count;
        public DateTime? LastVisitDate => Visits.Count > 0 ? (DateTime?)Visits.Max(v => v.AppointmentDate) : null;
        public DateTime? NextAppointmentDate => Visits.Count > 0 ? Visits.Where(v => v.NextAppointmentDate.HasValue).Max(v => v.NextAppointmentDate) : null;

        // Clinical history
        public PatientHistoryVM Clinical { get; set; } = new PatientHistoryVM();
    }


}




//using System;
//using System.Collections.Generic;

//namespace WebApplicationSampleTest2.Models
//{
//    // ── Vitals ────────────────────────────────────────────────────────
//    public class PatientVitals
//    {
//        public int Id { get; set; }
//        public int PatientId { get; set; }
//        public int HospitalId { get; set; }
//        public int? AppointmentId { get; set; }
//        public decimal? Temperature { get; set; }
//        public int? PulseRate { get; set; }
//        public int? BPSystolic { get; set; }
//        public int? BPDiastolic { get; set; }
//        public decimal? SPO2 { get; set; }
//        public decimal? RBS { get; set; }
//        public decimal? Weight { get; set; }
//        public decimal? Height { get; set; }
//        public decimal? BMI { get; set; }
//        public int? Respiration { get; set; }
//        public decimal? WaistCircumference { get; set; }
//        public decimal? FIB4 { get; set; }
//        public DateTime RecordedDate { get; set; }
//    }

//    // ── Medical Condition ─────────────────────────────────────────────
//    public class PatientMedicalCondition
//    {
//        public int Id { get; set; }
//        public int PatientId { get; set; }
//        public int HospitalId { get; set; }
//        public string ConditionName { get; set; }
//        public int? SinceDays { get; set; }
//        public string Status { get; set; } = "Active";   // Active / Resolved
//        public bool OnMedication { get; set; }
//        public string Notes { get; set; }
//        public DateTime CreatedDate { get; set; }
//    }

//    // ── Allergy ───────────────────────────────────────────────────────
//    public class PatientAllergy
//    {
//        public int Id { get; set; }
//        public int PatientId { get; set; }
//        public int HospitalId { get; set; }
//        public string AllergyTo { get; set; }
//        public int? SinceHours { get; set; }
//        public string Status { get; set; } = "Active";
//        public string Notes { get; set; }
//        public DateTime CreatedDate { get; set; }
//    }

//    // ── Surgical History ──────────────────────────────────────────────
//    public class PatientSurgicalHistory
//    {
//        public int Id { get; set; }
//        public int PatientId { get; set; }
//        public int HospitalId { get; set; }
//        public string ProcedureName { get; set; }
//        public DateTime? SurgeryDate { get; set; }
//        public string Location { get; set; }
//        public string Notes { get; set; }
//    }

//    // ── Family History ────────────────────────────────────────────────
//    public class PatientFamilyHistory
//    {
//        public int Id { get; set; }
//        public int PatientId { get; set; }
//        public int HospitalId { get; set; }
//        public string Relation { get; set; }
//        public string ConditionName { get; set; }
//        public string Notes { get; set; }
//        public DateTime CreatedDate { get; set; }
//    }

//    // ── Lab Result ────────────────────────────────────────────────────
//    public class PatientLabResult
//    {
//        public int Id { get; set; }
//        public int PatientId { get; set; }
//        public int HospitalId { get; set; }
//        public int? AppointmentId { get; set; }
//        public string TestName { get; set; }
//        public string ResultValue { get; set; }
//        public string Unit { get; set; }
//        public string NormalRange { get; set; }
//        public bool IsAbnormal { get; set; }
//        public DateTime ResultDate { get; set; }
//        public string FilePath { get; set; }
//        public string Notes { get; set; }
//    }

//    // ── Full Patient History (left panel) ─────────────────────────────
//    public class PatientHistoryVM
//    {
//        public List<PatientMedicalCondition> Conditions { get; set; } = new List<PatientMedicalCondition>();
//        public List<PatientAllergy> Allergies { get; set; } = new List<PatientAllergy>();
//        public List<PatientSurgicalHistory> Surgeries { get; set; } = new List<PatientSurgicalHistory>();
//        public List<PatientFamilyHistory> FamilyHistory { get; set; } = new List<PatientFamilyHistory>();
//        public PatientVitals LatestVitals { get; set; }
//        public List<PatientLabResult> LabResults { get; set; } = new List<PatientLabResult>();
//    }


//}
