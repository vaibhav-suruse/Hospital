// Repository/IDischarge.cs
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IDischarge
    {
        DischargeModel GetAdmissionForDischarge(int ipdId, int hospitalId, int? subHospitalId);
        void DischargePatient(DischargeModel model, int updatedBy, int hospitalId, int? subHospitalId);
        DischargeModel GetDischargeSummary(int ipdId, int hospitalId, int? subHospitalId);


        List<DischargeMedicineModel> GetDischargeMedicines(int ipdId, int hospitalId, int? subHospitalId);
        void SaveDischargeMedicine(DischargeMedicineModel model, int createdBy, int hospitalId, int? subHospitalId);
        void DeleteDischargeMedicine(int id, int hospitalId, int? subHospitalId);
    }
}