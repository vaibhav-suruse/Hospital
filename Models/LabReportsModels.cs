// Models/LabReportsModels.cs
// NEW FILE. Independent of Treatment Sheet / Extra Orders models.
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    // ── Master catalog (hospital-wide, shared across every patient) ────
    public class LabTestCategoryModel
    {
        public int CategoryId { get; set; }
        [Required(ErrorMessage = "Category name is required.")]
        public string CategoryName { get; set; }
    }

    public class LabTestParameterModel
    {
        public int ParameterId { get; set; }
        public int CategoryId { get; set; }
        [Required(ErrorMessage = "Parameter name is required.")]
        public string ParameterName { get; set; }
        public string Unit { get; set; }
        public decimal? NormalRangeLow { get; set; }
        public decimal? NormalRangeHigh { get; set; }
        public string NormalRangeText { get; set; } // for non-numeric parameters, e.g. "Negative"

        // Critical thresholds - tighter than the normal range, and distinct
        // from it: a value can be "High" (routine abnormal) without being
        // "Critical" (immediately life-threatening, must be phoned to the
        // doctor right away per standard lab-accreditation practice).
        public decimal? CriticalLow { get; set; }
        public decimal? CriticalHigh { get; set; }
        public string CriticalText { get; set; }

        public string RangeDisplay =>
            !string.IsNullOrWhiteSpace(NormalRangeText) ? NormalRangeText
            : (NormalRangeLow.HasValue && NormalRangeHigh.HasValue) ? $"{NormalRangeLow} - {NormalRangeHigh}"
            : "-";
    }

    // A single parameterId + typed value pair, used when submitting several
    // results at once from the "Add Lab Value" modal.
    public class LabValueEntryModel
    {
        public int ParameterId { get; set; }
        public string ResultValue { get; set; }
    }

    // A critical result that was just saved, returned to the client so it
    // can show a hard "notify the doctor" prompt right away.
    public class CriticalResultAlert
    {
        public int LabValueId { get; set; }
        public string ParameterName { get; set; }
        public string ResultValue { get; set; }
        public string Unit { get; set; }
    }

    // ── Per-patient, day-wise result row, as displayed on screen/print ──
    public class LabReportValueVM
    {
        public int Id { get; set; }
        public int IPDId { get; set; }
        public int ParameterId { get; set; }
        public string ParameterName { get; set; }
        public string CategoryName { get; set; }
        public string Unit { get; set; }
        public decimal? NormalRangeLow { get; set; }
        public decimal? NormalRangeHigh { get; set; }
        public string NormalRangeText { get; set; }
        public decimal? CriticalLow { get; set; }
        public decimal? CriticalHigh { get; set; }
        public string CriticalText { get; set; }
        public string ResultValue { get; set; }
        public DateTime ReportDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? EnteredByDoctorId { get; set; }
        public string EnteredByName { get; set; }

        // Critical-value notification (who was told, when) - null until
        // someone acknowledges notifying the doctor.
        public int? NotificationId { get; set; }
        public DateTime? NotifiedAt { get; set; }
        public string NotifiedToDoctorName { get; set; }

        public string RangeDisplay =>
            !string.IsNullOrWhiteSpace(NormalRangeText) ? NormalRangeText
            : (NormalRangeLow.HasValue && NormalRangeHigh.HasValue) ? $"{NormalRangeLow} - {NormalRangeHigh}"
            : "-";

        // True if this result crosses the CRITICAL threshold - i.e.
        // immediately life-threatening, not just routinely out of range.
        public bool IsCritical
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(CriticalText))
                    return string.Equals(ResultValue?.Trim(), CriticalText.Trim(), StringComparison.OrdinalIgnoreCase);

                if (decimal.TryParse(ResultValue, out var numeric))
                {
                    if (CriticalLow.HasValue && numeric <= CriticalLow.Value) return true;
                    if (CriticalHigh.HasValue && numeric >= CriticalHigh.Value) return true;
                }
                return false;
            }
        }

        public bool IsNotified => NotificationId.HasValue;

        // "Critical" / "Normal" / "High" / "Low" / "Abnormal" / null (can't be judged)
        public string AbnormalFlag
        {
            get
            {
                if (IsCritical) return "Critical";

                if (!string.IsNullOrWhiteSpace(NormalRangeText))
                    return string.Equals(ResultValue?.Trim(), NormalRangeText.Trim(), StringComparison.OrdinalIgnoreCase)
                        ? "Normal" : "Abnormal";

                if (NormalRangeLow.HasValue && NormalRangeHigh.HasValue &&
                    decimal.TryParse(ResultValue, out var numeric))
                {
                    if (numeric < NormalRangeLow.Value) return "Low";
                    if (numeric > NormalRangeHigh.Value) return "High";
                    return "Normal";
                }
                return null; // no range configured - can't judge, so don't flag
            }
        }
    }

    // ── Aggregate ViewModel that powers the Lab Reports page ────────────
    public class LabReportsVM
    {
        public IPDAdmissionModel Admission { get; set; }
        public List<LabReportValueVM> Values { get; set; } = new List<LabReportValueVM>();

        public int TotalDays { get; set; } = 1;
        public int ActiveDay { get; set; } = 1;
    }
}
