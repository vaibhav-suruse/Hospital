using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    public class LabInvestigationModel
    {
        public int Id { get; set; }
        public int RoundId { get; set; }
        public int IPDId { get; set; }
        public string InvestigationType { get; set; }
        public string TestName { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string Instructions { get; set; }
        public DateTime OrderedDateTime { get; set; }
        public DateTime? CollectedDateTime { get; set; }
        public DateTime? CompletedDateTime { get; set; }
        public string Result { get; set; }
        public string ResultFilePath { get; set; }
        public bool IsAbnormal { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // Patient + IPD + Doctor Info
        public string PatientName { get; set; }
        public string PatientPhone { get; set; }
        public string AdmissionNumber { get; set; }
        public string DoctorName { get; set; }
        public string Specialization { get; set; }

        // For file upload
        public IFormFile ReportFile { get; set; }

        // For update form
        [Required(ErrorMessage = "Please select status")]
        public string NewStatus { get; set; }
    }
}