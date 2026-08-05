using System.Collections.Generic;
using WebApplicationSampleTest2.Models;
namespace WebApplicationSampleTest2.Repository
{
    public interface INursingChargesMaster
    {
        List<NursingChargesMaster> GetAll(int hospitalId, int? subHospitalId);
        NursingChargesMaster GetById(int id);
        void Create(NursingChargesMaster model, int hospitalId, int? subHospitalId);
        int Update(NursingChargesMaster model);
        void Delete(int id);
    }
}