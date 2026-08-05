using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    public class ReferenceDoctorModel
    {
        public int ReferenceDoctorId { get; set; }

        [Required]
        public int ParentHospitalId { get; set; }

        public int? SubHospitalId { get; set; }

        [Required(ErrorMessage = "Doctor Name is required")]
        [StringLength(150)]
        public string DoctorName { get; set; }

        [StringLength(150)]
        public string ClinicName { get; set; }

        [Phone]
        [StringLength(15)]
        public string MobileNumber { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [Required(ErrorMessage = "Percentage is required")]
        [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100")]
        public decimal Percentage { get; set; }

        public bool IsActive { get; set; } 

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public bool isUpdate { get; set; }

    }
}
