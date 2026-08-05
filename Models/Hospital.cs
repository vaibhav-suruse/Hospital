using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    public class Hospital
    {
        public int Id { get; set; }
        public int? subHospitalId { get; set; }
        [Required(ErrorMessage = "Hospital Name is required")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Hospital Name must contain only letters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Phone number must be 10 digits")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [RegularExpression(@"^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$",
            ErrorMessage = "Email must be in lowercase only")]
        public string EmailId { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Meta Link is required")]
        public string MetaLink { get; set; }

        [Required(ErrorMessage = "Instagram Link is required")]
        public string InstaLink { get; set; }

        public string Logo { get; set; }

        [Required(ErrorMessage = "Registration Number is required")]
        public string RegistrationNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsSubHospital { get; set; }
        public int? ParentHospitalId { get; set; }
        public string ParentHospitalName { get; set; }
        public bool isUpdate { get; set; }
        public IFormFile LogoFile { get; set; } 
    }

   
}
