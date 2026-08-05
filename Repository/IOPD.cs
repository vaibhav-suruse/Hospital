using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IOPD
    {
        // ✅ FIX: Changed return type from bool to int
        // Returns the new OPDId on success, 0 on failure
        int AddOPD(OPD model, int hospitalId, int? subHospitalId);
        OPD GetOPDById(int opdId, int hospitalId, int? subHospitalId);
        int GetOPDIdByAppointmentId(int appointmentId, int hospitalId, int? subHospitalId);
        List<OPDSymptomVM> GetOPDSymptomsByOPDId(int opdId);
        int UpdateOPD(OPD opd);
        List<DiagnosisMaster> GetAllDiagnoses(int hospitalId);
    }
}

//using System.Collections.Generic;
//using System.Threading.Tasks;
//using WebApplicationSampleTest2.Models;

//namespace WebApplicationSampleTest2.Repository
//{
//    public interface IOPD
//    {
//        bool AddOPD(OPD model, int hospitalId, int? subHospitalId);
//        OPD GetOPDById(int opdId, int hospitalId, int? subHospitalId);
//        int GetOPDIdByAppointmentId(int appointmentId, int hospitalId, int? subHospitalId);
//        List<OPDSymptomVM> GetOPDSymptomsByOPDId(int opdId);
//        int UpdateOPD(OPD opd);
//    }
//}
