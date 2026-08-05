using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    public class PatientIPDDetailVM
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

        public string PaymentStatus { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }

        public List<PatientIPDRoundVM> DoctorRounds { get; set; }
            = new List<PatientIPDRoundVM>();

        public List<PatientVitalsVM> Vitals { get; set; }
            = new List<PatientVitalsVM>();
    }

    public class PatientIPDRoundVM
    {
        public int RoundId { get; set; }

        public DateTime RoundDateTime { get; set; }

        public string RoundType { get; set; }

        public string DoctorName { get; set; }

        public string Specialization { get; set; }

        public string PatientCondition { get; set; }

        public string Diagnosis { get; set; }

        public string Notes { get; set; }

        public string Instructions { get; set; }

        public bool IsAbnormal { get; set; }

        public List<PatientRoundSymptomVM> Symptoms { get; set; }
            = new List<PatientRoundSymptomVM>();

        public List<PatientRoundMedicineVM> Medicines { get; set; }
            = new List<PatientRoundMedicineVM>();

        public List<PatientRoundInvestigationVM> Investigations { get; set; }
            = new List<PatientRoundInvestigationVM>();
    }

    public class PatientRoundSymptomVM
    {
        public string SymptomName { get; set; }

        public string SubName { get; set; }
    }

    public class PatientRoundMedicineVM
    {
        public string MedicineName { get; set; }

        public string MedicineType { get; set; }

        public bool Morning { get; set; }

        public bool Afternoon { get; set; }

        public bool Evening { get; set; }

        public int? Days { get; set; }

        public string Route { get; set; }

        public string Dosage { get; set; }

        public string Instructions { get; set; }

        public string Status { get; set; }

        public string TimingDisplay
        {
            get
            {
                var timings = new List<string>();

                if (Morning) timings.Add("Morning");
                if (Afternoon) timings.Add("Afternoon");
                if (Evening) timings.Add("Evening");

                return timings.Count > 0
                    ? string.Join(" | ", timings)
                    : "As directed";
            }
        }
    }

    public class PatientRoundInvestigationVM
    {
        public string InvestigationType { get; set; }

        public string TestName { get; set; }

        public string Priority { get; set; }

        public string Status { get; set; }

        public string Result { get; set; }

        public string Instructions { get; set; }
    }

    public class PatientVitalsVM
    {
        public DateTime RecordedDateTime { get; set; }

        public decimal? Temperature { get; set; }

        public int? Pulse { get; set; }

        public int? Systolic { get; set; }

        public int? Diastolic { get; set; }

        public int? OxygenSaturation { get; set; }

        public int? RespirationRate { get; set; }

        public bool IsAbnormal { get; set; }

        public string Notes { get; set; }

        public string BPDisplay
        {
            get
            {
                if (Systolic.HasValue && Diastolic.HasValue)
                    return $"{Systolic}/{Diastolic} mmHg";

                if (Systolic.HasValue)
                    return $"{Systolic}/- mmHg";

                return "Not recorded";
            }
        }

        public string TemperatureDisplay
        {
            get
            {
                return Temperature.HasValue
                    ? $"{Temperature}°C"
                    : "Not recorded";
            }
        }

        public string PulseDisplay
        {
            get
            {
                return Pulse.HasValue
                    ? $"{Pulse} bpm"
                    : "Not recorded";
            }
        }

        public string SpO2Display
        {
            get
            {
                return OxygenSaturation.HasValue
                    ? $"{OxygenSaturation}%"
                    : "Not recorded";
            }
        }
    }
}