using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    // ViewModel that powers the "Vital Parameters" day-wise timeline (Clinical Details > Vital Parameters).
    // Purely additive - does not change IPDNurseVitals or IPDAdmissionModel.
    public class NurseVitalsTimelineVM
    {
        public IPDAdmissionModel Admission { get; set; }

        // Day number -> vitals recorded that day (most recent first)
        public Dictionary<int, List<IPDNurseVitals>> VitalsByDay { get; set; } = new Dictionary<int, List<IPDNurseVitals>>();

        // Total number of day-tabs to render (from admission date to today, minimum 1)
        public int TotalDays { get; set; } = 1;

        // Which day tab should be active on first load (defaults to "today"/latest)
        public int ActiveDay { get; set; } = 1;
    }

    // Lightweight status used to color-code a single reading. Kept in its own
    // class so both the Timeline partial (server-rendered) and any future
    // screen can reuse identical thresholds - no duplicated "magic numbers".
    public class VitalFieldStatus
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public string Level { get; set; } // "normal" | "warning" | "critical" | "empty"
    }



    public static class VitalsStatusHelper
    {
        public static List<VitalFieldStatus> BuildStatusList(IPDNurseVitals v)
        {
            var list = new List<VitalFieldStatus>
            {
                Evaluate("Temp", v.Temperature.HasValue ? v.Temperature.Value + "°C" : null,
                    v.Temperature, 36m, 38m),

                Evaluate("Pulse", v.Pulse.HasValue ? v.Pulse.Value + " bpm" : null,
                    (decimal?)v.Pulse, 60m, 100m),

                Evaluate("BP", (v.Systolic.HasValue && v.Diastolic.HasValue)
                        ? $"{v.Systolic}/{v.Diastolic}" : null,
                    null, null, null, IsBpAbnormal(v)),

                Evaluate("RR", v.RespirationRate.HasValue ? v.RespirationRate.Value + "/min" : null,
                    (decimal?)v.RespirationRate, 12m, 20m),

                Evaluate("SpO2", v.OxygenSaturation.HasValue ? v.OxygenSaturation.Value + "%" : null,
                    (decimal?)v.OxygenSaturation, 95m, 100m)
            };

            return list;
        }

        private static bool? IsBpAbnormal(IPDNurseVitals v)
        {
            if (!v.Systolic.HasValue || !v.Diastolic.HasValue) return null;
            return v.Systolic < 90 || v.Systolic > 140 || v.Diastolic < 60 || v.Diastolic > 90;
        }

        private static VitalFieldStatus Evaluate(string label, string display, decimal? numericValue,
            decimal? min, decimal? max, bool? overrideAbnormal = null)
        {
            string level;

            if (display == null)
            {
                level = "empty";
            }
            else if (overrideAbnormal.HasValue)
            {
                level = overrideAbnormal.Value ? "critical" : "normal";
            }
            else if (numericValue.HasValue && min.HasValue && max.HasValue)
            {
                level = (numericValue.Value < min.Value || numericValue.Value > max.Value) ? "critical" : "normal";
            }
            else
            {
                level = "normal";
            }

            return new VitalFieldStatus { Label = label, Value = display ?? "—", Level = level };
        }
    }


}
