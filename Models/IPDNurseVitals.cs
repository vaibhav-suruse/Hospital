using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    public class IPDNurseVitals : IValidatableObject
    {
        public int VitalsId { get; set; }

        [Required]
        public int ParentHospitalId { get; set; }

        public int? SubHospitalId { get; set; }

        [Required]
        public int IPDId { get; set; }

        // ===============================
        // Recorded By - Doctor or Nurse
        // ===============================
        [Required(ErrorMessage = "Please select who recorded the vitals.")]
        public string RecordedByRole { get; set; } = "Nurse"; // "Nurse" or "Doctor"

        // Only one of these two will be set, based on RecordedByRole
        public int? NurseId { get; set; }

        public int? DoctorId { get; set; }

        // Not persisted directly - filled by the repository from a join,
        // used to display "Recorded By" in lists without extra lookups
        public string RecordedByName { get; set; }

        [Required(ErrorMessage = "Please select a shift.")]
        public string Shift { get; set; } // "Morning" / "Afternoon" / "Night"

        [Required(ErrorMessage = "Recorded date and time is required.")]
        [DataType(DataType.DateTime)]
        public DateTime RecordedDateTime { get; set; }

        [Range(30, 45, ErrorMessage = "Temperature must be between 30°C and 45°C.")]
        public decimal? Temperature { get; set; }

        [Range(30, 200, ErrorMessage = "Pulse must be between 30 and 200.")]
        public int? Pulse { get; set; }

        [Range(50, 250, ErrorMessage = "Systolic must be between 50 and 250.")]
        public int? Systolic { get; set; }

        [Range(30, 150, ErrorMessage = "Diastolic must be between 30 and 150.")]
        public int? Diastolic { get; set; }

        [Range(5, 60, ErrorMessage = "Respiration rate must be between 5 and 60.")]
        public int? RespirationRate { get; set; }

        [Range(50, 100, ErrorMessage = "Oxygen saturation must be between 50% and 100%.")]
        public int? OxygenSaturation { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string Notes { get; set; }

        public bool IsAbnormal { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        // ===============================
        // Custom Validation Logic
        // ===============================
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // 0️⃣ Recorded By - Doctor or Nurse must be selected consistently
            if (string.Equals(RecordedByRole, "Doctor", StringComparison.OrdinalIgnoreCase))
            {
                if (!DoctorId.HasValue || DoctorId.Value <= 0)
                {
                    yield return new ValidationResult(
                        "Please select the doctor who recorded the vitals.",
                        new[] { nameof(DoctorId) });
                }
            }
            else
            {
                if (!NurseId.HasValue || NurseId.Value <= 0)
                {
                    yield return new ValidationResult(
                        "Please select the nurse who recorded the vitals.",
                        new[] { nameof(NurseId) });
                }
            }

            // 1️⃣ Prevent future date
            if (RecordedDateTime > DateTime.Now)
            {
                yield return new ValidationResult(
                    "Recorded date/time cannot be in the future.",
                    new[] { nameof(RecordedDateTime) });
            }

            // 2️⃣ BP logic: Systolic must be greater than Diastolic
            if (Systolic.HasValue && Diastolic.HasValue)
            {
                if (Systolic <= Diastolic)
                {
                    yield return new ValidationResult(
                        "Systolic must be greater than Diastolic.",
                        new[] { nameof(Systolic), nameof(Diastolic) });
                }
            }

            // 3️⃣ Auto-calculate abnormal flag
            bool abnormal = false;

            if (Temperature.HasValue && (Temperature < 36 || Temperature > 38))
                abnormal = true;

            if (Pulse.HasValue && (Pulse < 60 || Pulse > 100))
                abnormal = true;

            if (Systolic.HasValue && (Systolic < 90 || Systolic > 140))
                abnormal = true;

            if (Diastolic.HasValue && (Diastolic < 60 || Diastolic > 90))
                abnormal = true;

            if (OxygenSaturation.HasValue && OxygenSaturation < 95)
                abnormal = true;

            IsAbnormal = abnormal;
        }
    }
}

