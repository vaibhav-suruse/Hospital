using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IIPDBedAllocation
    {
        void AllocateOrTransferBed(int ipdId, int newBedId, int allocatedBy);
        void UpdateBedAllocation(IPDBedAllocationModel model);
        void DeleteBedAllocation(int allocationId);
        IPDBedAllocationModel GetById(int allocationId);
        List<IPDBedAllocationModel> GetAll();
        void AllocateBed(int ipdId, int newBedId);
        void UpdateBedStatus(int bedId, string status);
        void ShiftBed(int ipdId, int newBedId, int allocatedBy, string reason, int hospitalId, int? subHospitalId);
        BedShiftVM GetCurrentBedInfo(int ipdId, int hospitalId, int? subHospitalId);
    }
}
