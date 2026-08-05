// Models/DoctorRoundModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    public class IPDDoctorRound
    {
        public int RoundId { get; set; }
        [Required]
        public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        [Required]
        public int IPDId { get; set; }
        [Required(ErrorMessage = "Please select a doctor.")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid doctor selection.")]
        public int DoctorId { get; set; }
        [Required(ErrorMessage = "Round type is required.")]
        public string RoundType { get; set; }
        [Required(ErrorMessage = "Round date and time is required.")]
        public DateTime RoundDateTime { get; set; }
        public string PatientCondition { get; set; }
        public string Diagnosis { get; set; }
        public string Instructions { get; set; }
        public string Notes { get; set; }
        public bool IsAbnormal { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // For display
        public string DoctorName { get; set; }
        public string Specialization { get; set; }

        // Sub lists
        public List<IPDRoundSymptom> Symptoms { get; set; } = new List<IPDRoundSymptom>();
        public List<IPDRoundPrescription> Prescriptions { get; set; } = new List<IPDRoundPrescription>();
        public List<IPDRoundInvestigation> Investigations { get; set; } = new List<IPDRoundInvestigation>();
    }

    public class IPDRoundSymptom
    {
        public int Id { get; set; }
        public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public int RoundId { get; set; }
        public int IPDId { get; set; }
        [Required(ErrorMessage = "Please select a symptom.")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid symptom.")]
        public int SymptomId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }

        // For display
        public string SymptomName { get; set; }
        public string SubName { get; set; }
    }

    public class IPDRoundPrescription
    {
        public int Id { get; set; }
        public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public int RoundId { get; set; }
        public int IPDId { get; set; }
        [Required(ErrorMessage = "Please select a medicine.")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid medicine.")]
        public int MedicineId { get; set; }
        public bool Morning { get; set; }
        public bool Afternoon { get; set; }
        public bool Evening { get; set; }
        [Range(1, 365, ErrorMessage = "Days must be between 1 and 365.")]
        public int? Days { get; set; }
        public string Route { get; set; } = "Oral";
        [StringLength(100)]
        public string Dosage { get; set; }
        public string Instructions { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // For display
        public string MedicineName { get; set; }
        public string MedicineType { get; set; }
    }

    public class IPDRoundInvestigation
    {
        public int Id { get; set; }
        public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public int RoundId { get; set; }
        public int IPDId { get; set; }
        [Required(ErrorMessage = "Investigation type is required.")]
        public string InvestigationType { get; set; }
        [Required(ErrorMessage = "Test name is required.")]
        [StringLength(255)]
        public string TestName { get; set; }
        public string Priority { get; set; } = "Routine";
        public DateTime OrderedDateTime { get; set; }
        public DateTime? CollectedDateTime { get; set; }
        public DateTime? CompletedDateTime { get; set; }
        public string Result { get; set; }
        public string ResultFilePath { get; set; }
        public string Instructions { get; set; }
        public string Status { get; set; } = "Ordered";
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
      
    }
}