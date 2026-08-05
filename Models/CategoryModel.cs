namespace WebApplicationSampleTest2.Models
{
    public class CategoryModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }

        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }

       
        public bool IsUpdate { get; set; }
    }
}
