using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplicationSampleTest2.Models
{
    public class OPD
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int AppointmentId { get; set; }
        public string BP { get; set; }
        public string Pulse { get; set; }
        public string Investigation { get; set; }
        public string ReportDetail { get; set; }
        public string ReportFilePath { get; set; }  // DB sathi path
        [NotMapped]
        public IFormFile ReportFile { get; set; }   // Upload sathi
        public DateTime? NextAppointmentDate { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public DateTime AppointmentDate { get; set; }

        public List<string> Symptom { get; set; } = new List<string>();

        public List<int> Symptoms { get; set; } = new List<int>();
        // changed to use OPDMedicine (string flags for morning/afternoon/evening)
        public List<OPDMedicine> Medicines { get; set; } = new List<OPDMedicine>();
        public List<OPDDiagnosis> Diagnoses { get; set; } = new List<OPDDiagnosis>();
    }
    public class OPDMedicineVM
    {
        public string MedicineName { get; set; }
        public int MedicineId { get; set; }
        public int Morning { get; set; }
        public int Afternoon { get; set; }
        public int Evening { get; set; }
        public int Days { get; set; }
        public string Frequency { get; set; } = "TDS";
        public string WhenToTake { get; set; } = "After Food";
        public string Dose { get; set; } = "1";
        public string Unit { get; set; } = "Tablet(s)";
        public int Quantity { get; set; }
    }


    public class OPDMedicine
    {
        public string MedicineName { get; set; }
        public int MedicineId { get; set; }
        public string Morning { get; set; }   // "1" or "0"
        public string Afternoon { get; set; }   // "1" or "0"
        public string Evening { get; set; }   // "1" or "0"
        public int Days { get; set; }
        public string Frequency { get; set; } = "TDS";          // OD/BD/TDS/QID/PRN
        public string WhenToTake { get; set; } = "After Food";   // timing instruction
        public string Dose { get; set; } = "1";            // 1, 2, 0.5 etc.
        public string Unit { get; set; } = "Tablet(s)";    // Tablet(s)/ml/Puffs etc.
        public int Quantity { get; set; }                   // auto = freq × days
    }

    public class OPDDetailVM
    {
        public OPD OPD { get; set; }
        public List<Symptom> Symptoms { get; set; } 
        public List<tablet> Medicines { get; set; } 
    }


    public class OPDDiagnosis
    {
        public int Id { get; set; }
        public int OPDId { get; set; }
        public string DiagnosisName { get; set; }
        public string Type { get; set; } = "Final";  // Final / Provisional
        public string Notes { get; set; }
    }

    // ── DiagnosisMaster (NEW) ─────────────────────────────────────────
    public class DiagnosisMaster
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IcdCode { get; set; }
        public string Category { get; set; }
    }

}
