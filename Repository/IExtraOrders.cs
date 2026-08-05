using System;
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IExtraOrders
    {
        // Extra Medication
        List<ExtraMedicationModel> GetExtraMedications(int ipdId);
        int InsertExtraMedication(int parentHospitalId, int? subHospitalId, ExtraMedicationModel model, DateTime timeGiven);
        void UpdateExtraMedication(ExtraMedicationModel model);
        void DeleteExtraMedication(int id);

        // Extra Orders
        List<ExtraOrderModel> GetExtraOrders(int ipdId);
        int InsertExtraOrder(int parentHospitalId, int? subHospitalId, ExtraOrderModel model, DateTime timeGiven);
        void UpdateExtraOrder(ExtraOrderModel model);
        void DeleteExtraOrder(int id);
    }
}
