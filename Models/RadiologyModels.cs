using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    // ── A single saved Radiology Report against an IPD admission ───────
    public class RadiologyReportModel
    {
        public int Id { get; set; }
        public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }

        [Required]
        public int IPDId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Notes are required.")]
        public string Notes { get; set; }

        [Required(ErrorMessage = "Please select the radiologist.")]
        public int RadiologistDoctorId { get; set; }

        [Required(ErrorMessage = "Report date & time is required.")]
        public DateTime ReportDateTime { get; set; }

        public int? EnteredByUserId { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // Display only (populated by the repository join, never posted back)
        public string RadiologistName { get; set; }
        public string EnteredByName { get; set; }
    }

    // ── Reusable Title+Notes preset ("ANC USG", "CT Brain Plain", ...) ──
    public class RadiologyTemplateModel
    {
        public int TemplateId { get; set; }

        [Required(ErrorMessage = "Template name is required.")]
        [StringLength(150)]
        public string TemplateName { get; set; }

        [Required(ErrorMessage = "Template text is required.")]
        public string TemplateText { get; set; }
    }

    // ── Aggregate ViewModel that powers the Radiology Reports page ──────
    public class RadiologyReportsVM
    {
        public IPDAdmissionModel Admission { get; set; }
        public List<RadiologyReportModel> Reports { get; set; } = new List<RadiologyReportModel>();
    }
}
