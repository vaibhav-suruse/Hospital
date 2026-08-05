using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    public class IPDPharmacyBillVM
    {
        public int NotificationId { get; set; }
        public int IPDId { get; set; }
        public string PatientName { get; set; }
        public string MobileNumber { get; set; }
        public decimal SubTotal { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string PaymentMode { get; set; }

        // Reuse existing PharmacyBillItemVM
        public List<PharmacyBillItemVM> Items { get; set; }
    }
}
