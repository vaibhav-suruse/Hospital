using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    public class PharmLowStockItem
    {
        public string MedicineName { get; set; }
        public string Unit { get; set; }
        public string CategoryName { get; set; }
        public int Quantity { get; set; }
        public int ReorderLevel { get; set; }
    }

    public class PharmExpiryItem
    {
        public string MedicineName { get; set; }
        public string BatchNo { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class PharmRecentBill
    {
        public string BillNumber { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMode { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
