using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IOPDBilling
    {
        // Every read/write is hospital-scoped (defense in depth against
        // cross-hospital access, enforced again at the stored-procedure level).
        OPDBillVM GetBillSummary(int appointmentId, int hospitalId, int? subHospitalId);
        OPDBill GetBillByAppointmentId(int appointmentId, int hospitalId, int? subHospitalId);
        List<OPDBillItem> GetBillItems(int billId, int hospitalId, int? subHospitalId);
        List<OPDPayment> GetPayments(int billId, int hospitalId, int? subHospitalId);

        // True upsert: one active bill per appointment. isDraft controls
        // whether the bill lands in Draft or Finalized status (a bill that
        // already has a payment against it can never go back to Draft —
        // enforced at the stored-procedure level).
        int SaveBill(OPDBill bill, List<OPDBillItem> items, int hospitalId, int? subHospitalId, bool isDraft);

        // Appends to the payment ledger and returns the recalculated bill.
        OPDBill CollectPayment(
            int billId, int appointmentId, int hospitalId, int? subHospitalId,
            decimal amount, string paymentMode, string transactionRef, string notes, int receivedBy);

        // Cancels a bill. Refuses if any payment has already been collected
        // against it (refund the payments first) — enforced at the DB layer.
        OPDBill CancelBill(int billId, int hospitalId, int? subHospitalId, string reason, int cancelledBy);
    }
}
