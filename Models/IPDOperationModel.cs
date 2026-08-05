// Models/IPDOperationModel.cs
using System;
using System.Collections.Generic;
namespace WebApplicationSampleTest2.Models
{
    public class IPDOperationModel
    {
        public int IPDOperationId { get; set; }
        public int IPDId { get; set; }
        public int OperationId { get; set; }
        public string OperationName { get; set; }
        public DateTime OperationDate { get; set; }
        public int? SurgeonId { get; set; }
        public string SurgeonName { get; set; }
        public int? AnesthesistId { get; set; }
        public string AnesthesistName { get; set; }
        public decimal ActualCharge { get; set; }
        public decimal AnesthesiaCharge { get; set; }
        public decimal SurgeonCharge { get; set; }
        public decimal OTCharge { get; set; }
        public string Notes { get; set; }
        // Staff list loaded separately
        public List<IPDOperationStaff> Staff { get; set; }
            = new List<IPDOperationStaff>();
        public decimal TotalStaffCharge { get; set; }
        // Grand total of everything
        public decimal GrandTotal =>
            ActualCharge + AnesthesiaCharge +
            SurgeonCharge + OTCharge + TotalStaffCharge;
    }


    public class IPDOperationStaff
    {
        public int Id { get; set; }
        public int IPDOperationId { get; set; }
        public string StaffType { get; set; }
        public int? DoctorId { get; set; }
        public int? NurseId { get; set; }
        public string StaffName { get; set; }
        public decimal Charge { get; set; }
    }

}


   

