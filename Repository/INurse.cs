using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface INurse
    {
        // Returns a list of nurses
        List<NurseModel> GetAll(int hospitalId, int? subHospitalId = null);

        // Returns a single nurse by Id
        NurseModel GetById(int nurseId, int hospitalId, int? subHospitalId = null);

        // Insert new nurse
        void Insert(NurseModel nurse);

        // Update existing nurse
        void Update(NurseModel nurse);

        // Delete nurse
        void Delete(int nurseId, int hospitalId, int? subHospitalId = null);

    }
}
