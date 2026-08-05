using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface ISupplier
    {
        List<SupplierModel> GetAllSuppliers(int hospitalId, int? subHospitalId);
        SupplierModel GetSupplierById(int supplierId, int hospitalId, int? subHospitalId);
        void AddSupplier(SupplierModel model);
        void UpdateSupplier(SupplierModel model);
        void DeleteSupplier(int supplierId, int hospitalId, int? subHospitalId);
    }
}
