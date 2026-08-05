// Repository/IIPDBilling.cs
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IIPDBilling
    {
        // Everything needed to (re)build the real-time item grid: bed charges
        // (auto Days × rate), doctor round charges (auto-priced from Billing
        // Master), medicine/discharge-medicine suggestions, investigations
        // (reference only), nursing + operation charges (already priced by
        // their own modules), and — if a bill already exists — its saved
        // items and payment history.
        IPDBillVM GetBillSummary(int ipdId, int hospitalId, int? subHospitalId);

        // Latest active bill for this admission, or null if none yet.
        IPDBill GetBillByIPDId(int ipdId, int hospitalId, int? subHospitalId);

        List<IPDBillItem> GetBillItems(int billId, int hospitalId, int? subHospitalId);

        List<IPDPayment> GetPayments(int billId, int hospitalId, int? subHospitalId);

        // Idempotent upsert: first call creates the bill, every later call
        // for the same admission updates it in place (replacing its items).
        int SaveBill(IPDBill bill, List<IPDBillItem> items, int hospitalId, int? subHospitalId, bool isDraft);

        IPDBill CollectPayment(
            int billId, int ipdId, int hospitalId, int? subHospitalId,
            decimal amount, string paymentMode, string transactionRef, string notes, int receivedBy);

        IPDBill CancelBill(int billId, int hospitalId, int? subHospitalId, string reason, int cancelledBy);

        // Unchanged — still used by DischargePlanningController.
        BillingSummaryVM GetPatientBillingSummary(int ipdId);
    }
}
