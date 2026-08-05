using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface ICategory
    {
        List<CategoryModel> GetAllCategories(int hospitalId, int? subHospitalId);
        CategoryModel GetCategoryById(int categoryId, int hospitalId, int? subHospitalId);
        void AddCategory(CategoryModel model);
        void UpdateCategory(CategoryModel model);
        void DeleteCategory(int categoryId, int hospitalId, int? subHospitalId);
    }
}
