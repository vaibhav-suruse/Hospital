namespace WebApplicationSampleTest2.Models
{
    public class SupplierModel
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string ContactNo { get; set; }
        public string Address { get; set; }

        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }

        public bool IsUpdate { get; set; }
    }
}
