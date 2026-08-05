using System.Collections.Generic;
using WebApplicationSampleTest2.Models;
namespace WebApplicationSampleTest2.Repository
{
    public interface IOperationMaster
    {
        List<OperationMaster> GetAll(int hospitalId, int? subHospitalId);
        OperationMaster GetById(int id);
        void Create(OperationMaster model, int hospitalId, int? subHospitalId);
        int Update(OperationMaster model);
        void Delete(int id);
    }
}