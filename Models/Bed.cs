using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationSampleTest2.Models
{
    public class Bed
    {
        public int BedId { get; set; }

        [Required(ErrorMessage = "Ward is required")]
        public int WardId { get; set; }

        public string WardName { get; set; }

        [Required(ErrorMessage = "Room is required")]
        public int RoomId { get; set; }


        public string RoomNumber { get; set; }

        [Required(ErrorMessage = "Bed number is required")]
        [StringLength(50)]
        public string BedNumber { get; set; }

        [Required]
        public string OperationalStatus { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public int Floor { get; set; }
        [Required(ErrorMessage = "Room is required")]
        public decimal ChargesPerDay { get; set; }
    }
}
