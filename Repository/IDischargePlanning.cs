using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IDischargePlanning
    {
        DischargePlanningModel GetByIPD(int ipdId, int hospitalId, int? subHospitalId);
        void SavePlanning(DischargePlanningModel model, int updatedBy, int hospitalId, int? subHospitalId);
    }
}
