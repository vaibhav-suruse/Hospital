// Models/BillingSummaryVM.cs
using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    public class BillingSummaryVM
    {
        // ── Patient Info ─────────────────────────────────────────────────
        public int IPDId { get; set; }
        public string AdmissionNumber { get; set; }
        public string PatientName { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public DateTime AdmissionDateTime { get; set; }
        public DateTime? DischargeDateTime { get; set; }
        public int TotalDays { get; set; }
        public string AdmissionStatus { get; set; }
        public string WardName { get; set; }
        public string BedNumber { get; set; }
        public string DoctorName { get; set; }

        // ── Bill Info ────────────────────────────────────────────────────
        public int? BillId { get; set; }
        public string BillNumber { get; set; }
        public DateTime? BillDate { get; set; }
        public string PaymentStatus { get; set; }

        // ── Charge Breakdown ─────────────────────────────────────────────
        public decimal BedCharges { get; set; }
        public decimal DoctorCharges { get; set; }
        public decimal MedicineCharges { get; set; }
        public decimal InvestigationCharges { get; set; }
        public decimal DischargeMedCharges { get; set; }
        public decimal NursingCharges { get; set; }
        public decimal OperationCharges { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }

        // ── Nursing Detail ───────────────────────────────────────────────
        public List<IPDNursingCharge> NursingDetails { get; set; }
            = new List<IPDNursingCharge>();

        // ── Operation Detail ─────────────────────────────────────────────
        public List<IPDOperationModel> OperationDetails { get; set; }
            = new List<IPDOperationModel>();

        // ── Payment History ──────────────────────────────────────────────
        public List<IPDPayment> Payments { get; set; }
            = new List<IPDPayment>();
    }
}