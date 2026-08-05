using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    public class Room
    {
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Ward is required")]
        public int WardId { get; set; }

        [Required]
        public int HospitalId { get; set; }

        // SubHospitalId can be null
        public int? SubHospitalId { get; set; }

        [Required(ErrorMessage = "Room number is required")]
        [StringLength(50, ErrorMessage = "Room number cannot exceed 50 characters")]
        public string RoomNumber { get; set; }

        [Required(ErrorMessage = "Room type is required")]
        [RegularExpression("General|ICU|Private|SemiPrivate", ErrorMessage = "Invalid room type")]
        public string RoomType { get; set; }


        [StringLength(255)]
        public string Description { get; set; } // <-- Add this

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        public int SelectedWardId { get; set; }
    }

    public class RoomListVM
    {
        public int RoomId { get; set; }
        public int RoomNumber { get; set; }
        public string RoomType { get; set; }
        public int FloorNumber { get; set; }
        public int WardId { get; set; }
        public string WardName { get; set; }
        public bool IsActive { get; set; }
        public int TotalBeds { get; set; }
    }
}
