// Models/OperationMaster.cs
using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    public class OperationMaster
    {
        public int OperationId { get; set; }
        public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public string OperationName { get; set; }
        public string OperationCode { get; set; }
        public string Category { get; set; }
        public decimal DefaultCharge { get; set; }
        public decimal AnesthesiaCharge { get; set; }
        public decimal SurgeonCharge { get; set; }
        public decimal OTCharge { get; set; }
        public decimal TotalCharge =>
            DefaultCharge + AnesthesiaCharge + SurgeonCharge + OTCharge;
        public bool IsActive { get; set; }
        public bool isUpdate { get; set; }
    }
}

