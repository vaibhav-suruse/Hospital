using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    public class Doctor
    {
        public int Doctor_Id { get; set; }

        public string DoctorCode { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Only characters allowed")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Only characters allowed")]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Education is required")]
        [StringLength(100)]
        public string Education { get; set; }

        [Required(ErrorMessage = "Specialization is required")]
        [StringLength(100)]
        public string Specialization { get; set; }

        [Required(ErrorMessage = "Experience is required")]
        [Range(1, 60, ErrorMessage = "Experience must be between 1 and 60 years")]
        public int ExperienceYears { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mobile number must be 10 digits")]
        public string MobileNo { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [RegularExpression(@"^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$",
       ErrorMessage = "Email must be lowercase only")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Address is required")]
        [StringLength(250)]
        public string Address { get; set; }

        public int Hospital_Id { get; set; }
        public int? Sub_Hospital_Id { get; set; }

        public bool isUpdate { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
