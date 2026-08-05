using System;

namespace WebApplicationSampleTest2.Models
{
    public class NurseModel
    {
        public int NurseId { get; set; }                     
        public int ParentHospitalId { get; set; }           
        public int? SubHospitalId { get; set; }            
        public string FirstName { get; set; }               
        public string LastName { get; set; }
        public string Email { get; set; }

        public string Gender { get; set; }            
        public string PhoneNumber { get; set; }             
        public string Qualification { get; set; }           
        public string Department { get; set; }              
        public bool IsActive { get; set; } = true;          
        public DateTime CreatedDate { get; set; }
    }
}
