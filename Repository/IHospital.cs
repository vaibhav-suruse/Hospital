using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IHospital
    {
        List<Hospital> GetAllHospitals();
        Hospital GetHospitalById(int id);
        void InsertHospital(Hospital hospital);
        void UpdateHospital(Hospital hospital);
        void DeleteHospital(int id);
        List<Hospital> GetMainHospitals();
        List<Hospital> GetSubHospitalsByMainId(int mainHospitalId);
        Hospital GetsubandMainHospitalById(int hospitalId, int? subHospitalId = null);

        DataTable GetMainHospitalDashboard();
        DataTable GetSubHospitalDashboard();
    }
}
