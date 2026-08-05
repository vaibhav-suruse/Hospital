using Microsoft.AspNetCore.Http;

namespace WebApplicationSampleTest2.Models
{
    public class Medicine
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int Quantity { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }  // Tablet, Syrup, etc.
        public bool Morning { get; set; }
        public bool Afternoon { get; set; }
        public bool Evening { get; set; }
        public bool isUpdate { get; set; }
        public IFormFile LogoFile { get; set; }
    }
}
