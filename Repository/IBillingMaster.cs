using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IBillingMaster
    {
        void CreateBilling(BillingMaster billing, int hospitalId, int? subHospitalId);
        int UpdateBilling(BillingMaster billing, int hospitalId, int? subHospitalId);
        void DeleteBilling(int billingId, int hospitalId, int? subHospitalId);
        BillingMaster? GetBillingById(int billingId, int hospitalId, int? subHospitalId);
        List<BillingMaster> GetAllBillings(int hospitalId, int? subHospitalId);
    }
}
