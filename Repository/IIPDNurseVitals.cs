using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IIPDNurseVitals
    {
        List<IPDNurseVitals> GetVitalsByIPDId(int ipdId, int hospitalId, int? subHospitalId);
        IPDNurseVitals GetVitalsById(int vitalsId);
        void CreateVitals(IPDNurseVitals model);
        void UpdateVitals(IPDNurseVitals model);
        void DeleteVitals(int vitalsId);
        List<IPDNurseVitals> GetVitalsByHospital(int parentHospitalId, int? subHospitalId);
        IPDNurseVitals GetLastVitalsByIPDId(int ipdId); // used for "Copy Previous" quick entry
    }
}