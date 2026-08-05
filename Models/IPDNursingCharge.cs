//// Models/IPDNursingCharge.cs
//using System;
//namespace WebApplicationSampleTest2.Models
//{
//    public class IPDNursingCharge
//    {
//        public int Id { get; set; }
//        public int IPDId { get; set; }
//        public int ParentHospitalId { get; set; }
//        public int? SubHospitalId { get; set; }
//        public int? VitalsId { get; set; }
//        public int? NursingMasterId { get; set; }
//        public string ChargeName { get; set; }
//        public string ChargeType { get; set; }
//        public DateTime ChargeDate { get; set; }
//        public decimal Quantity { get; set; }
//        public decimal UnitCharge { get; set; }
//        public decimal TotalCharge { get; set; }
//        public int? NurseId { get; set; }
//        public string NurseName { get; set; }
//        public string Notes { get; set; }
//    }
//}





// Models/IPDNursingCharge.cs
using System;

namespace WebApplicationSampleTest2.Models
{
    public class IPDNursingCharge
    {
        public int Id { get; set; }
        public int IPDId { get; set; }
        public int ParentHospitalId { get; set; }  // ← must exist
        public int? SubHospitalId { get; set; }  // ← must exist
        public int? VitalsId { get; set; }
        public int? NursingMasterId { get; set; }
        public string ChargeName { get; set; }
        public string ChargeType { get; set; } = "PerProcedure";
        public DateTime ChargeDate { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal UnitCharge { get; set; }
        public decimal TotalCharge { get; set; }
        public int? NurseId { get; set; }

        // Not mapped — display only
        public string NurseName { get; set; }
        public string Notes { get; set; }
    }
}