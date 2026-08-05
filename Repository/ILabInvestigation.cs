using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface ILabInvestigation
    {
        List<LabInvestigationModel> GetAllInvestigations(
            int parentHospitalId,
            int? subHospitalId,
            string status,
            string priority);

        LabInvestigationModel GetInvestigationById(int id);

        void UpdateInvestigation(
            int investigationId,
            string status,
            string result,
            string resultFilePath,
            int updatedBy,
            bool isAbnormal = false);
    }
}