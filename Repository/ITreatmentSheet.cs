// Repository/ITreatmentSheet.cs
// NEW interface. Does not modify IDoctorRound, IDailyNotes or anything else.
using System;
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface ITreatmentSheet
    {
        // General Orders
        List<GeneralOrderModel> GetGeneralOrders(int ipdId);
        int InsertGeneralOrder(GeneralOrderModel model, DateTime orderDate);
        void UpdateGeneralOrder(GeneralOrderModel model);
        void SetGeneralOrderStatus(int id, string status);
        void DeleteGeneralOrder(int id);

        // General Order Templates
        List<GeneralOrderTemplateModel> GetGeneralOrderTemplates(int hospitalId, string search);
        int SaveGeneralOrderTemplate(int hospitalId, GeneralOrderTemplateModel model, int createdBy);

        // Medications
        List<TreatmentMedicineVM> GetMedications(int ipdId);
        int InsertMedicine(int parentHospitalId, int? subHospitalId, int ipdId, int doctorId, TreatmentMedicineVM model, DateTime orderDate);
        void UpdateMedicine(TreatmentMedicineVM model);
        void UpdateMedicineSortOrder(int id, int sortOrder);

        // Investigations
        List<TreatmentInvestigationVM> GetInvestigations(int ipdId);
        int InsertInvestigation(int parentHospitalId, int? subHospitalId, int ipdId, int doctorId, TreatmentInvestigationVM model, DateTime orderDate);
        void UpdateInvestigation(TreatmentInvestigationVM model);
        void DeleteInvestigation(int id);
    }
}
