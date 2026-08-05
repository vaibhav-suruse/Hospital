
namespace WebApplicationSampleTest2.Models
{
  
    public class IPDPharmacyMedicineLineVM
    {
        public int IPDPrescriptionId { get; set; }  
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string MedicineType { get; set; } 
        public string Route { get; set; }   
        public bool Morning { get; set; }
        public bool Afternoon { get; set; }
        public bool Evening { get; set; }
        public int Days { get; set; }
        public int QtyPerDay { get; set; }
        public int TotalQty { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal LineTotal { get; set; }   // TotalQty × SellingPrice

    }
}

