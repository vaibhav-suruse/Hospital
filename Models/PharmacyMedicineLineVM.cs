using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    public class PharmacyMedicineLineVM
    {
        public int OPDMedicineId { get; set; }
        public int MedicineId { get; set; }        // tbl_medicine ID (prescription)
        public int StoreMedicineId { get; set; }   // ← ADD THIS: store_medicines ID (for billing/stock)
        public string MedicineName { get; set; }

        public string MedicineType { get; set; }   // Tablet / Syrup / Capsule
        public bool Morning { get; set; }
        public bool Afternoon { get; set; }
        public bool Evening { get; set; }
        public int Days { get; set; }
        public int QtyPerDay { get; set; }
        public int TotalQty { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal LineTotal => TotalQty * SellingPrice;
        public string Route { get; set; }

        // Stock availability (fetched from stock table at prescription load time)
        public int AvailableStock { get; set; }   // -1 = not tracked / not found
        public string StockStatus { get; set; }  // "sufficient" | "partial" | "outofstock"
    }

    // Bill header + lines submitted from the billing form
    public class PharmacyBillVM
    {
        public int NotificationId { get; set; }
        public int OPDId { get; set; }
        public string PatientName { get; set; }
        public string MobileNumber { get; set; }
        public decimal SubTotal { get; set; }
        public string DiscountType { get; set; }   // Percentage / Fixed
        public decimal DiscountValue { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string PaymentMode { get; set; }   // Cash / UPI / Card
        public List<PharmacyBillItemVM> Items { get; set; } = new List<PharmacyBillItemVM>();
    }

    public class PharmacyBillItemVM
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

}
