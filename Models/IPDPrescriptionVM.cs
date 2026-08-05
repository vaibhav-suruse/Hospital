// Models/IPDPrescriptionVM.cs
using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    public class IPDPrescriptionVM
    {
        // Hospital
        public string HospitalName { get; set; }
        public string HospitalAddress { get; set; }
        public string HospitalPhone { get; set; }
        public string HospitalEmail { get; set; }
        public string HospitalLogo { get; set; }
        public string HospitalRegNo { get; set; }

        // Patient
        public string PatientName { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; }
        public string PatientPhone { get; set; }
        public string PatientAddress { get; set; }

        // IPD
        public string AdmissionNumber { get; set; }
        public DateTime AdmissionDateTime { get; set; }
        public string BedNumber { get; set; }
        public string WardName { get; set; }
        public string RoomNumber { get; set; }

        // For single round print
        public IPDRoundPrintVM Round { get; set; }

        // For all rounds print
        public List<IPDRoundPrintVM> Rounds { get; set; }
    }

    public class IPDRoundPrintVM
    {
        public int RoundId { get; set; }
        public string RoundType { get; set; }
        public DateTime RoundDateTime { get; set; }
        public string DoctorName { get; set; }
        public string Specialization { get; set; }
        public string Education { get; set; }
        public string PatientCondition { get; set; }
        public string Diagnosis { get; set; }
        public string Instructions { get; set; }
        public string Notes { get; set; }

        public List<IPDSymptomPrintVM> Symptoms { get; set; } = new List<IPDSymptomPrintVM>();
        public List<IPDMedicinePrintVM> Medicines { get; set; } = new List<IPDMedicinePrintVM>();
        public List<IPDInvestigationPrintVM> Investigations { get; set; } = new List<IPDInvestigationPrintVM>();
    }

    public class IPDSymptomPrintVM
    {
        public string SymptomName { get; set; }
        public string SubName { get; set; }
    }

    public class IPDMedicinePrintVM
    {
        public string MedicineName { get; set; }
        public string MedicineType { get; set; }
        public bool Morning { get; set; }
        public bool Afternoon { get; set; }
        public bool Evening { get; set; }
        public int? Days { get; set; }
        public string Route { get; set; }
        public string Dosage { get; set; }
        public string MedicineInstructions { get; set; }
        public string Status { get; set; }
    }

    public class IPDInvestigationPrintVM
    {
        public string InvestigationType { get; set; }
        public string TestName { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string InvInstructions { get; set; }
    }
}