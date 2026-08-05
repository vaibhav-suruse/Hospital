using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
   
    public class ExtraMedicationModel
    {
        public int Id { get; set; }
        public int IPDId { get; set; }
        public int? MedicineId { get; set; }
        [Required(ErrorMessage = "Medicine name is required.")]
        public string MedicineName { get; set; }
        public string Reason { get; set; }
        public string Remark { get; set; }
        public int? EnteredByDoctorId { get; set; }
        public DateTime TimeGiven { get; set; }
        public DateTime CreatedDate { get; set; }

        // Display only
        public string EnteredByName { get; set; }
    }

    // ── Extra Orders: any other one-off nursing/doctor instruction that
    // isn't a medicine (e.g. "apply ice pack", "call doctor if BP < 90/60")
    public class ExtraOrderModel
    {
        public int Id { get; set; }
        public int IPDId { get; set; }
        [Required(ErrorMessage = "Order details are required.")]
        public string OrderDetails { get; set; }
        public string Remark { get; set; }
        public int? EnteredByDoctorId { get; set; }
        public DateTime TimeGiven { get; set; }
        public DateTime CreatedDate { get; set; }

        // Display only
        public string EnteredByName { get; set; }
    }

    // ── Aggregate ViewModel that powers the Extra Orders page ───────────
    public class ExtraOrdersVM
    {
        public IPDAdmissionModel Admission { get; set; }
        public List<ExtraMedicationModel> ExtraMedications { get; set; } = new List<ExtraMedicationModel>();
        public List<ExtraOrderModel> ExtraOrders { get; set; } = new List<ExtraOrderModel>();

        public int TotalDays { get; set; } = 1;
        public int ActiveDay { get; set; } = 1;
    }
}
