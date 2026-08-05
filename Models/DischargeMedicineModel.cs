// Models/DischargeMedicineModel.cs
namespace WebApplicationSampleTest2.Models
{
    public class DischargeMedicineModel
    {
        public int Id { get; set; }
        public int IPDId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string MedicineType { get; set; }
        public bool Morning { get; set; }
        public bool Afternoon { get; set; }
        public bool Evening { get; set; }
        public int? Days { get; set; }
        public string Route { get; set; }
        public string Dosage { get; set; }
        public string Instructions { get; set; }
    }
}