
// Models/NursingChargesMaster.cs
namespace WebApplicationSampleTest2.Models
{
    public class NursingChargesMaster
    {
        public int NursingId { get; set; }
        public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public string ChargeName { get; set; }
        public string ChargeType { get; set; }
        public decimal DefaultCharge { get; set; }
        public bool IsActive { get; set; }
        public bool isUpdate { get; set; }
    }
}

