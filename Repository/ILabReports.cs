// Repository/ILabReports.cs
// NEW interface. Independent of ITreatmentSheet / IExtraOrders / ILabInvestigation.
using System;
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface ILabReports
    {
        // Master catalog - Categories
        List<LabTestCategoryModel> GetCategories(int hospitalId);
        int InsertCategory(int hospitalId, string categoryName);
        void DeleteCategory(int categoryId);

        // Master catalog - Parameters
        List<LabTestParameterModel> GetParametersByCategory(int categoryId);
        int InsertParameter(LabTestParameterModel model);
        void UpdateParameter(LabTestParameterModel model);
        void DeleteParameter(int parameterId);

        // Patient lab values
        List<LabReportValueVM> GetLabValues(int ipdId);
        int InsertLabValue(int parentHospitalId, int? subHospitalId, int ipdId, int parameterId, string resultValue, DateTime reportDate, int? doctorId);
        void UpdateLabValue(int id, string resultValue);
        void DeleteLabValue(int id);

        // Critical-value notification (who was told about a critical result, and when)
        int InsertCriticalNotification(int labValueId, int notifiedToDoctorId, int? notifiedByUserId, string remarks);
    }
}
