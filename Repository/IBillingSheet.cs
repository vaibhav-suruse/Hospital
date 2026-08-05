using System;
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IBillingSheet
    {
        // billingType: "Doctor" | "Nurse" | null (null = both)
        List<BillingSheetEntryModel> GetEntriesByIPD(int ipdId, string billingType);

        int InsertEntry(
            int parentHospitalId,
            int? subHospitalId,
            BillingSheetEntryModel model,
            int? enteredByUserId);

        void UpdateEntry(BillingSheetEditModel model);

        void DeleteEntry(int id);

        decimal GetTotalByIPD(int ipdId, string billingType);
    }
}
