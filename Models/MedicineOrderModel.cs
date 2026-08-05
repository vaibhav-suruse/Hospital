using System;

namespace WebApplicationSampleTest2.Models
{
    public class MedicineOrderModel
    {
        public int OrderId { get; set; }
        public string Frequency { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int OrderedBy { get; set; }
        public string OrderedByName { get; set; } = string.Empty;
        public DateTime OrderedDateTime { get; set; }
        public bool IsActive { get; set; } = true;

        public int IPDId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public bool Morning { get; set; }
        public bool Afternoon { get; set; }
        public bool Evening { get; set; }
        public int? Days { get; set; }
        public string Route { get; set; }
        public string Dosage { get; set; }
        public string Instructions { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }

        // ADD THIS
        public int DoctorId { get; set; }
        // Round this order belongs to - links it back to the note it was added with
        public int RoundId { get; set; }
    }


    public class DiscontinueMedicineModel
    {
        public int PrescriptionId { get; set; }
        public int DoctorId { get; set; }
    }

}
