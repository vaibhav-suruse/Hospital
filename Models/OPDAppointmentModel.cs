using System;

namespace WebApplicationSampleTest2.Models
{
    public class OPDAppointmentModel
    {
        public int Id { get; set; }

        public int PatientId { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }

        public DateTime CreatedOn { get; set; }
        public bool IsActive { get; set; } = true;

        // UI purpose
        public string Status { get; set; }
        public string PatientName { get; set; }
        public string MobileNo { get; set; }
        public bool isUpdate { get; set; }
        public int OPDId { get; set; }
        public string IPDStatus { get; set; }
        public bool IsIPDAdmitted { get; set; }

        // Distinguishes walk-in (1) from regular OPD appointment (0)
        public bool IsWalkIn { get; set; }
    }
}
