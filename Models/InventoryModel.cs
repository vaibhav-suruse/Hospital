using System;

namespace WebApplicationSampleTest2.Models
{
    public class InventoryModel
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string CategoryName { get; set; }
        public int? SubHospitalId { get; set; }
        public int HospitalId { get; set; }
        public string BatchNumber { get; set; }
        public int BatchId { get; set; }
        public int Quantity { get; set; }
        public string SupplierName { get; set; }
        public string Unit { get; set; }
        public decimal MRP { get; set; }    
        public int Stock { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsUpdate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; }
        public int CategoryId { get; set; }
        public int SupplierId { get; set; }
        public decimal SellingPrice { get; set; }
    }
}
