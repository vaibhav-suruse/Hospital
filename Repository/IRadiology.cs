// Repository/IRadiology.cs
// NEW interface. Independent of ILabReports / ITreatmentSheet / IExtraOrders.
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IRadiology
    {
        // Patient radiology reports
        List<RadiologyReportModel> GetReportsByIPD(int ipdId);
        int InsertReport(RadiologyReportModel model);
        void UpdateReport(RadiologyReportModel model);
        void DeleteReport(int id);

        // Reusable Title+Notes templates (hospital-wide)
        List<RadiologyTemplateModel> GetTemplates(int hospitalId, string search);
        int SaveTemplate(int hospitalId, RadiologyTemplateModel model, int createdBy);
    }
}
