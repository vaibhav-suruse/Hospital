using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IInventory
    {
        List<InventoryModel> GetAllInventory(int hospitalId, int? subHospitalId);
            void AddInventory(InventoryModel model);
        void UpdateInventory(InventoryModel model);

        void DeleteInventory(int batchId, int medicineId, int hospitalId, int? subHospitalId);

        InventoryModel GetInventoryById(int batchId, int medicineId, int hospitalId, int? subHospitalId);

        // Dropdowns
        List<SupplierModel> GetSuppliers(int hospitalId, int? subHospitalId);
        List<CategoryModel> GetCategories(int hospitalId, int? subHospitalId);
    }
}
