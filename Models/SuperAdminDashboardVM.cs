using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    public class SuperAdminDashboardVM
    {
        public int TotalHospitals { get; set; }
        public int TotalSubHospitals { get; set; }
        public int TotalUsers { get; set; }
        public List<HospitalInfo> Hospitals { get; set; } = new List<HospitalInfo>();
    }

    public class HospitalInfo
    {
        public int HospitalId { get; set; }
        public string HospitalName { get; set; }
        public int SubHospitalCount { get; set; }
        public int UserCount { get; set; }
        public List<SubHospitalInfo> SubHospitals { get; set; } = new List<SubHospitalInfo>();
    }

    public class SubHospitalInfo
    {
        public int SubHospitalId { get; set; }
        public string SubHospitalName { get; set; }
        public int UserCount { get; set; }
    }

}
