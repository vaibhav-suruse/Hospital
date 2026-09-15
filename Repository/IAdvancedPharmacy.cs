using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IAdvancedPharmacy
    {
        // ── Purchase Order ──────────────────────────────────────────────────
        List<PurchaseOrder> GetPurchaseOrders(int hospitalId, int? subHospitalId);
        PurchaseOrder GetPurchaseOrder(string poNumber, int hospitalId);
        string CreatePurchaseOrder(PurchaseOrder po, int hospitalId, int? subHospitalId, int createdBy);
        void UpdatePurchaseOrderStatus(string poNumber, string status, int hospitalId);

        // ── Goods Receipt (GRN) ─────────────────────────────────────────────
        List<GoodsReceipt> GetGoodsReceipts(int hospitalId, int? subHospitalId);
        string CreateGoodsReceipt(GoodsReceipt grn, int hospitalId, int? subHospitalId, int createdBy);

        // ── Stock Adjustment ────────────────────────────────────────────────
        void AdjustStock(StockAdjustment adjustment, int hospitalId, int? subHospitalId, int changedBy);
        List<StockAdjustment> GetStockAdjustmentLog(int hospitalId, int? subHospitalId);

        // ── Returns / Refunds ───────────────────────────────────────────────
        List<PharmacyReturn> GetReturns(int hospitalId, int? subHospitalId);
        string CreateReturn(PharmacyReturn pr, int hospitalId, int? subHospitalId, int createdBy);

        // ── Narcotics / controlled substance register ───────────────────────
        List<NarcoticsRegisterEntry> GetNarcoticsRegister(int hospitalId, int? subHospitalId);
        void AddNarcoticsEntry(NarcoticsRegisterEntry entry, int hospitalId, int? subHospitalId, int dispensedBy, int authorizedBy);

        // ── Clinical Safety ─────────────────────────────────────────────────
        ClinicalSafetyCheckVM CheckClinicalSafety(List<int> medicineIds, int patientId, int hospitalId);
        List<DrugInteraction> GetDrugInteractions(int hospitalId);
        void AddDrugInteraction(DrugInteraction interaction, int hospitalId);
        List<PatientAllergyRecord> GetPatientAllergies(int patientId, int hospitalId);
        void AddPatientAllergy(PatientAllergyRecord allergy, int hospitalId);
    }
}
