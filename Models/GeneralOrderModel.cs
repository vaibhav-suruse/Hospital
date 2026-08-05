// Models/TreatmentSheetModels.cs
// NEW FILE. Does not modify IPDRoundPrescription, MedicineOrderModel,
// LabOrderModel, MARRowModel or any other existing model.
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WebApplicationSampleTest2.Models
{
    // ── General Order (IV fluids / nursing instructions) ───────────────
    public class GeneralOrderModel
    {
        public int Id { get; set; }
        public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        [Required]
        public int IPDId { get; set; }
        [Required(ErrorMessage = "Please select the ordering doctor.")]
        public int DoctorId { get; set; }
        [Required(ErrorMessage = "Order details are required.")]
        public string OrderDetails { get; set; }
        public string Priority { get; set; } = "Routine";   // Routine / Urgent / STAT
        public string Status { get; set; } = "Active";       // Active / Completed / Discontinued
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // Display only
        public string OrderByName { get; set; }
    }

    public class GeneralOrderTemplateModel
    {
        public int TemplateId { get; set; }
        [Required(ErrorMessage = "Template name is required.")]
        [StringLength(150)]
        public string TemplateName { get; set; }
        [Required(ErrorMessage = "Template text is required.")]
        public string TemplateText { get; set; }
    }

    // ── Medication row, as displayed/edited on the Treatment Sheet ──────
    // Deliberately separate from MedicineOrderModel (used by Daily Notes)
    // so nothing here can ever affect that existing screen.
    public class TreatmentMedicineVM
    {
        public int Id { get; set; }               // ipd_round_prescription.Id
        public int IPDId { get; set; }
        [Required(ErrorMessage = "Please select a medicine.")]
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string MedicineType { get; set; }

        public bool Morning { get; set; }
        public bool Afternoon { get; set; }
        public bool Evening { get; set; }
        public string FrequencyText { get; set; }   // e.g. "BD", "TDS", custom
        public int? Days { get; set; }
        public string Route { get; set; } = "Oral";
        public string Dosage { get; set; }
        public string FoodInstruction { get; set; } // Before Food / After Food / None
        public string DurationType { get; set; } = "Duration"; // Duration / Add Till Discontinued / SOS
        public int? Qty { get; set; }
        public string Instructions { get; set; }
        public string Status { get; set; } = "Active";
        public int SortOrder { get; set; }
        public DateTime CreatedDate { get; set; }

        public int DoctorId { get; set; }
        public string OrderByName { get; set; }

        // Convenience for the view - human readable frequency summary
        public string FrequencySummary
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(FrequencyText)) return FrequencyText;
                var slots = new List<string>();
                if (Morning) slots.Add("Morning");
                if (Afternoon) slots.Add("Afternoon");
                if (Evening) slots.Add("Evening");
                return slots.Count > 0 ? string.Join(", ", slots) : "-";
            }
        }
    }

    // ── Investigation row, as displayed/edited on the Treatment Sheet ──
    public class TreatmentInvestigationVM
    {
        public int Id { get; set; }
        public int IPDId { get; set; }
        [Required(ErrorMessage = "Investigation type is required.")]
        public string InvestigationType { get; set; } = "Lab";
        [Required(ErrorMessage = "Test name is required.")]
        public string TestName { get; set; }
        public string Priority { get; set; } = "Routine";
        public string Instructions { get; set; }
        public string Status { get; set; }
        public DateTime OrderedDateTime { get; set; }
        public DateTime? CollectedDateTime { get; set; }
        public DateTime? CompletedDateTime { get; set; }
        public string Result { get; set; }
        public string ResultFilePath { get; set; }
        public bool ReviewedByDoctor { get; set; }

        public int DoctorId { get; set; }
        public string OrderByName { get; set; }
    }

    // ── Aggregate ViewModel that powers the Treatment Sheet page ────────
    public class TreatmentSheetVM
    {
        public IPDAdmissionModel Admission { get; set; }

        public List<GeneralOrderModel> GeneralOrders { get; set; } = new List<GeneralOrderModel>();
        public List<TreatmentMedicineVM> Medications { get; set; } = new List<TreatmentMedicineVM>();
        public List<TreatmentInvestigationVM> Investigations { get; set; } = new List<TreatmentInvestigationVM>();

        // Allergy safety banner (reuses the existing Admission Notes allergy
        // system - PatientAllergyModel - so a critical drug allergy is
        // impossible to miss while prescribing from this screen)
        public List<PatientAllergyModel> Allergies { get; set; } = new List<PatientAllergyModel>();

        // Day tabs - drive only the MAR (Medication Administration Record)
        // grid, since orders/medications/investigations are cumulative
        // "active list" views, not day-partitioned.
        public int TotalDays { get; set; } = 1;
        public int ActiveDay { get; set; } = 1;

        public int ActiveGeneralOrderCount => GeneralOrders?.FindAll(g => g.Status == "Active").Count ?? 0;
        public int ActiveMedicationCount => Medications?.FindAll(m => m.Status == "Active").Count ?? 0;
        public int PendingInvestigationCount => Investigations?.FindAll(i => i.Status != "Completed" && i.Status != "Cancelled").Count ?? 0;
    }
}
