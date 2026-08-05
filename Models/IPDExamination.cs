using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    public class IPDExamination
    {
        public int Id { get; set; }
        [Required] public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        [Required] public int IPDId { get; set; }
        [Required] public DateTime RecordedDateTime { get; set; } = DateTime.Now;

        public string CNS { get; set; }
        public string CVS { get; set; }
        public string RS { get; set; }
        public string PA { get; set; }

        public string RecordedByRole { get; set; } = "Nurse";
        public int? NurseId { get; set; }
        public int? DoctorId { get; set; }
        public string RecordedByName { get; set; } // display only, filled by repository
    }

    public class IPDInputOutput
    {
        public int Id { get; set; }
        [Required] public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        [Required] public int IPDId { get; set; }
        [Required] public DateTime RecordedDateTime { get; set; } = DateTime.Now;

        [Range(0, 20000)] public decimal? Input { get; set; }
        [Range(0, 20000)] public decimal? Output { get; set; }
        [Range(0, 20000)] public decimal? DrainOutput { get; set; }

        public string RecordedByRole { get; set; } = "Nurse";
        public int? NurseId { get; set; }
        public int? DoctorId { get; set; }
        public string RecordedByName { get; set; }

        // Convenience, computed on the fly - never persisted
        public decimal TotalOutput => (Output ?? 0) + (DrainOutput ?? 0);
        public decimal FluidBalance => (Input ?? 0) - TotalOutput;
    }

    public class IPDDailyWellbeing
    {
        public int Id { get; set; }
        [Required] public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        [Required] public int IPDId { get; set; }
        [Required] public DateTime RecordedDateTime { get; set; } = DateTime.Now;

        public string Sleep { get; set; }
        public string Bowel { get; set; }
        public string Bladder { get; set; }
        public string Appetite { get; set; }
        public string Ambulation { get; set; }
        public string Eating { get; set; }

        public string RecordedByRole { get; set; } = "Nurse";
        public int? NurseId { get; set; }
        public int? DoctorId { get; set; }
        public string RecordedByName { get; set; }
    }

    public class IPDBodyComposition
    {
        public int Id { get; set; }
        [Required] public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        [Required] public int IPDId { get; set; }
        [Required] public DateTime RecordedDateTime { get; set; } = DateTime.Now;

        [Range(30, 250, ErrorMessage = "Height must be between 30cm and 250cm.")]
        public decimal? HeightCm { get; set; }

        [Range(1, 400, ErrorMessage = "Weight must be between 1kg and 400kg.")]
        public decimal? WeightKg { get; set; }

        // Server-computed - see BodyCompositionCalculator. Never trust a
        // client-supplied value for these three.
        public decimal? BMI { get; set; }
        public decimal? BMR { get; set; }
        public decimal? BSA { get; set; }

        public string RecordedByRole { get; set; } = "Nurse";
        public int? NurseId { get; set; }
        public int? DoctorId { get; set; }
        public string RecordedByName { get; set; }
    }

    // Pure calculation helper - no DB/IO. Same formulas the reference app's
    // numbers matched: BMI = kg/m^2, Mifflin-St Jeor for BMR, Mosteller for BSA.
    public static class BodyCompositionCalculator
    {
        public static (decimal? bmi, decimal? bmr, decimal? bsa) Calculate(
            decimal? heightCm, decimal? weightKg, int? ageYears, string gender)
        {
            if (!heightCm.HasValue || !weightKg.HasValue || heightCm <= 0 || weightKg <= 0)
                return (null, null, null);

            decimal heightM = heightCm.Value / 100m;
            decimal bmi = Math.Round(weightKg.Value / (heightM * heightM), 2);

            decimal bsa = Math.Round((decimal)Math.Sqrt((double)((heightCm.Value * weightKg.Value) / 3600m)), 2);

            decimal? bmr = null;
            if (ageYears.HasValue && ageYears.Value > 0)
            {
                bool isFemale = !string.IsNullOrEmpty(gender) &&
                                 gender.Trim().StartsWith("F", StringComparison.OrdinalIgnoreCase);

                decimal baseVal = 10m * weightKg.Value + 6.25m * heightCm.Value - 5m * ageYears.Value;
                bmr = Math.Round(isFemale ? baseVal - 161m : baseVal + 5m, 2);
            }

            return (bmi, bmr, bsa);
        }
    }
}
