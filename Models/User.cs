using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Login Name is required")]
        public string LoginName { get; set; }
        [Required(ErrorMessage = "First Name is required")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "First Name must contain only letters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Last Name must contain only letters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone Number must be 10 digits")]
        public string PhoneNo { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email format")]
        [RegularExpression(@"^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$",
           ErrorMessage = "Email must be lowercase")]
        public string EmailId { get; set; }
        public string Password { get; set; }


        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; } // Nullable for optional sub-hospital
        public string Type { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Role must contain only letters")]
        public string Role { get; set; }
        public bool isUpdate { get; set; }
        public string HospitalName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? MainHospitalId { get; set; }
      
    }
}
