using System;

namespace WebApplicationSampleTest2.Models
{
    public class BillingMaster
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string BillingType { get; set; } = string.Empty;
        public string? Category { get; set; }
        public decimal Amount { get; set; }
        public decimal GstPercent { get; set; }
        public decimal DefaultDiscountPercent { get; set; }
        public int Hospital_Id { get; set; }
        public int? SubHospital_Id { get; set; }
        public int IsActive { get; set; } = 1;
        public DateTime CreatedDate { get; set; }
        public bool isUpdate { get; set; }

    }
    public class BillingDetailModel
    {
        public int BillingId { get; set; }
        public string BillName { get; set; }
        public decimal Amount { get; set; }
        public int AppointmentId { get; set; }
    }

}
