using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    public class Ward
    {
        public int WardId { get; set; }

        [Required]
        public int HospitalId { get; set; }

        public int? SubHospitalId { get; set; }

        [Required(ErrorMessage = "Ward name is required")]
        [StringLength(100)]
        public string WardName { get; set; }

        [Required(ErrorMessage = "Ward type is required")]
        public string WardType { get; set; }

        [Required(ErrorMessage = "Floor number is required")]
        [Range(0, 100)]
        public int FloorNumber { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

    }

    public class WardListVM
    {
        public int WardId { get; set; }
        public string WardName { get; set; }
        public string WardType { get; set; }
        public int FloorNumber { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }

        public int TotalRooms { get; set; }
        public int TotalBeds { get; set; }
    }
}
