using System;

namespace WebApplicationSampleTest2.Models
{
    public class MARRowModel
    {
        public int MarId { get; set; }
        public int PrescriptionId { get; set; }
        public string MedicineName { get; set; }
        public string Dosage { get; set; }
        public string Route { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string ScheduledSlot { get; set; }   // Morning/Afternoon/Evening
        public string Status { get; set; }          // Pending/Given/Missed/Refused
        public DateTime? GivenAt { get; set; }
        public string GivenByName { get; set; }
        public string Notes { get; set; }
    }

    public class RecordAdministrationModel
    {
        public int MarId { get; set; }
        public string Status { get; set; }   // Given/Missed/Refused
        public int GivenBy { get; set; }
        public string Notes { get; set; }
    }
}
