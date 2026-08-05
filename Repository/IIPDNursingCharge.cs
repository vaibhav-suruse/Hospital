using System.Collections.Generic;
using WebApplicationSampleTest2.Models;
namespace WebApplicationSampleTest2.Repository
{
    public interface IIPDNursingCharge
    {
        List<IPDNursingCharge> GetByIPDId(int ipdId);
        void SaveCharges(List<IPDNursingCharge> charges);
        void Delete(int id);
        decimal GetTotalByIPDId(int ipdId);
    }
}