using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IWard
    {
        int CreateWard(Ward ward);
        int UpdateWard(Ward ward);
        Ward GetWardById(int wardId, int hospitalId, int? subHospitalId);
        List<Ward> GetAllWards(int hospitalId, int? subHospitalId);
        int DeleteWard(int wardId, int hospitalId, int? subHospitalId);
        public List<WardListVM> GetWardsWithCounts(int hospitalId, int? subHospitalId);
    }
}
