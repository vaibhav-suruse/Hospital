using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IMedicine
    {
        int AddMedicine(Medicine model, int hospitalId, int? subHospitalId);
        List<Medicine> GetAllMedicine(int hospitalId, int? subHospitalId);
        Medicine GetMedicineById(int medicineId, int hospitalId, int? subHospitalId);
        int UpdateMedicine(Medicine model, int hospitalId, int? subHospitalId);
        int DeleteMedicine(int medicineId, int hospitalId, int? subHospitalId);
    }
}
