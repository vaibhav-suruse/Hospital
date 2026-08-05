using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    public class DischargeModel
    {
        public int IPDId { get; set; }
        public int PatientId { get; set; }

        // Auto filled — display only
        public string AdmissionNumber { get; set; }
        public string PatientName { get; set; }
        public string Gender { get; set; }
        public int? Age { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public DateTime AdmissionDateTime { get; set; }
        public string ReasonForAdmission { get; set; }
        public string PrimaryDoctorName { get; set; }
        public string PrimaryDoctorSpecialization { get; set; }
        public string BedNumber { get; set; }
        public string WardName { get; set; }
        public string RoomNumber { get; set; }
        public int TotalDaysStayed { get; set; }
        public string Status { get; set; }

        // Discharge fields
        [Required(ErrorMessage = "Please select discharging doctor")]
        public int DischargeDoctorId { get; set; }

        [Required(ErrorMessage = "Please select discharge type")]
        public string DischargeType { get; set; }

        [Required(ErrorMessage = "Please select discharge condition")]
        public string DischargeCondition { get; set; }

        [Required(ErrorMessage = "Please enter final diagnosis")]
        public string FinalDiagnosis { get; set; }

        public string TreatmentSummary { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public int? FollowUpDoctorId { get; set; }
        public string DischargeInstructions { get; set; }
        public string DietInstructions { get; set; }
        public string ActivityRestrictions { get; set; }
        public string SpecialNotes { get; set; }

        // Summary display
        public DateTime? ActualDischargeDateTime { get; set; }
        public string DischargeDoctorName { get; set; }
        public string FollowUpDoctorName { get; set; }


        public List<DischargeMedicineModel> DischargeMedicines { get; set; }
            = new List<DischargeMedicineModel>();
        public List<Doctor> DoctorsList { get; set; }
            = new List<Doctor>();
    }
}