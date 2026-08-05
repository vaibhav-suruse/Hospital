using System;

namespace WebApplicationSampleTest2.Models
{
    public class MedicineNotificationModel
    {
        public int NotificationId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int? OPDId { get; set; }
        public int? AppointmentId { get; set; }
        public string DoctorName { get; set; }
        public int MedicineCount { get; set; }
        public string MedicinesSummary { get; set; }
        public string Type { get; set; }   // OPD | IPD
        public string Status { get; set; }   // Pending | Dispensed | Cancelled
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DispensedAt { get; set; }
        public int? IPDId { get; set; }
        public int? RoundId { get; set; }
        public string WardName { get; set; }
        public string RoomNo { get; set; }
        public string BedNo { get; set; }

        // Computed — how long ago
        public string TimeAgo
        {
            get
            {
                var diff = DateTime.Now - CreatedAt;
                if (diff.TotalMinutes < 1) return "Just now";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                return CreatedAt.ToString("dd MMM, hh:mm tt");
            }
        }
    }
}
