using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    public class IPDAdmissionModel
    {
        public int IPDId { get; set; }
        public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public string AdmissionNumber { get; set; } = string.Empty;  // REMOVED ?
        public int PatientId { get; set; }
        public int? OPDVisitId { get; set; }
        public int PrimaryDoctorId { get; set; }
        public int? ReferringDoctorId { get; set; }
        public string AdmissionType { get; set; } = string.Empty;  // REMOVED ?
        public DateTime AdmissionDateTime { get; set; }
        public DateTime? ExpectedDischargeDateTime { get; set; }
        public DateTime? ActualDischargeDateTime { get; set; }
        public string DischargeType { get; set; } = string.Empty;  // REMOVED ?
        public string Status { get; set; } = string.Empty;  // REMOVED ?
        public string ReasonForAdmission { get; set; } = string.Empty;  // REMOVED ?
        public string CancellationReason { get; set; } = string.Empty;  // REMOVED ?
        public bool IsActiveAdmission { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public bool IsFromAppointment { get; set; }

        public string PatientName { get; set; } = string.Empty;  // REMOVED ?
        public string DoctorName { get; set; } = string.Empty;  // REMOVED ?
        public bool isUpdate { get; set; }
    }








    public class IPDDetailsViewModel
    {
        public IPDAdmissionModel Admission { get; set; }
        public Patient Patient { get; set; }
        public List<IPDNurseVitals> VitalsList { get; set; }
        public List<IPDDoctorRound> RoundsList { get; set; }
        public List<IPDNursingCharge> NursingCharges { get; set; } = new List<IPDNursingCharge>();
    }


    public class IPDAdmissionWithBedVM
    {
        // 🔹 IPD Info
        public int IPDId { get; set; }
        public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public int PatientId { get; set; }
        public DateTime AdmissionDate { get; set; }
        public string AdmissionType { get; set; }
        public DateTime? DischargeDate { get; set; }

        // 🔹 Doctor Info
        public int? PrimaryDoctorId { get; set; }

        // 🔹 Bed Allocation
        public int? AllocationId { get; set; }
        public int? BedId { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public bool? IsCurrent { get; set; }
        public int? AllocatedBy { get; set; }
        public int? Days { get; set; }

        public string BedNumber { get; set; }
        public int? RoomId { get; set; }
        public int? WardId { get; set; }

        // 🔹 Display / UI Properties
        public int DischargeId { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public string AdmissionNumber { get; set; }
        public string Status { get; set; }
        public string ReferenceDoctorName { get; set; }
        public string BedFullLocation { get; set; }   // optional
    }




}
