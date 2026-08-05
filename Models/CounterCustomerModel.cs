using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    public class CounterCustomerModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string MobileNumber { get; set; }
        public string Address { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int PendingBillCount { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public bool IsNew { get; set; }
    }

    public class CounterMedicineSearchModel
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public decimal MRP { get; set; }
        public decimal SellingPrice { get; set; }
        public int StockQuantity { get; set; }
        public string SupplierName { get; set; }
    }

    public class CounterCartItem
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
        public int MaxStock { get; set; }
    }

    public class CounterBillModel
    {
        public int BillId { get; set; }
        public string BillNumber { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string MobileNumber { get; set; }
        public DateTime BillDate { get; set; }
        public decimal SubTotal { get; set; }
        public string DiscountType { get; set; } // Percentage or Fixed
        public decimal DiscountValue { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMode { get; set; }
        public string PaymentStatus { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public int CreatedBy { get; set; }

        // For view
        public List<CounterBillItemModel> Items { get; set; }
        public string HospitalName { get; set; }
        public string HospitalAddress { get; set; }
        public string HospitalPhone { get; set; }
        public string HospitalEmail { get; set; }
        public string HospitalLogo { get; set; }
        public string CreatedByName { get; set; }
    }

    public class CounterBillItemModel
    {
        public int ItemId { get; set; }
        public int BillId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string BillNumber { get; set; }
        public DateTime BillDate { get; set; }

    }

    public class CounterPendingBillModel
    {
        public int BillId { get; set; }
        public string BillNumber { get; set; }
        public DateTime BillDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string PaymentStatus { get; set; }
    }

    public class CounterTodaySummaryModel
    {
        public int TotalBills { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalPending { get; set; }
    }
}