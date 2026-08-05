namespace WebApplicationSampleTest2.Models
{
    public class BedShiftVM
    {
        public int IPDId { get; set; }
        public int NewBedId { get; set; }
        public string Reason { get; set; }
        public string ReturnUrl { get; set; }

        // Current bed info (for display)
        public string CurrentBedNumber { get; set; }
        public string CurrentWardName { get; set; }
        public string CurrentRoomNumber { get; set; }
        public int CurrentBedId { get; set; }
        public string Status { get; set; }

        // Patient info (for display)
        public string PatientName { get; set; }
        public string AdmissionNumber { get; set; }
    }
}