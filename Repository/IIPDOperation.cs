using System.Collections.Generic;
using WebApplicationSampleTest2.Models;
namespace WebApplicationSampleTest2.Repository
{
    public interface IIPDOperation
    {
        List<IPDOperationModel> GetByIPDId(int ipdId);
        int SaveAndReturnId(IPDOperationModel model,
                               int hospitalId, int? subHospitalId);
        void SaveStaff(List<IPDOperationStaff> staffList);
        List<IPDOperationStaff> GetStaffByOperationId(int ipdOperationId);
        void Delete(int ipdOperationId);
        decimal GetTotalByIPDId(int ipdId);
    }
}