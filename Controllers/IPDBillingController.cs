// Controllers/IPDBillingController.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class IPDBillingController : Controller
    {
        private readonly IIPDBilling _billingRepo;
        private readonly IBillingMaster _billingMaster;
        private readonly IHospital _IHospital;
        private readonly ILogger<IPDBillingController> _logger;

        public IPDBillingController(
            IIPDBilling billingRepo,
            IBillingMaster billingMaster,
            IHospital hospital,
            ILogger<IPDBillingController> logger)
        {
            _billingRepo = billingRepo;
            _billingMaster = billingMaster;
            _IHospital = hospital;
            _logger = logger;
        }

        private (int hospitalId, int? subHospitalId, int userId) CurrentContext()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            return (hospitalId, subHospitalId, userId);
        }

        // ── GENERATE BILL (real-time item grid) — GET ─────────────────────
        [HttpGet]
        public IActionResult GenerateBill(int ipdId)
        {
            try
            {
                var (hospitalId, subHospitalId, _) = CurrentContext();

                var vm = _billingRepo.GetBillSummary(ipdId, hospitalId, subHospitalId);

                var masterItems = _billingMaster
                    .GetAllBillings(hospitalId, subHospitalId)
                    .Where(b => b.IsActive == 1)
                    .OrderBy(b => b.Category).ThenBy(b => b.Name)
                    .ToList();

                ViewBag.BillingMasterItems = masterItems;
                ViewBag.BillNumber = vm.BillNumber ?? "";

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateBill. IPDId={IPDId}", ipdId);
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", "IPDAdmission", new { id = ipdId });
            }
        }

        // ── SAVE BILL (Draft or Finalize) — POST ───────────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult SaveBill([FromBody] SaveIPDBillRequest request)
        {
            try
            {
                if (request == null || request.IPDId <= 0)
                    return Json(new { success = false, message = "Invalid request." });

                var (hospitalId, subHospitalId, userId) = CurrentContext();

                var bill = new IPDBill
                {
                    IPDId = request.IPDId,
                    ParentHospitalId = hospitalId,
                    SubHospitalId = subHospitalId,
                    BillDate = request.BillDate ?? DateTime.Now,
                    BedCharges = SumByType(request.Items, "Bed"),
                    DoctorVisitCharges = SumByType(request.Items, "DoctorVisit"),
                    MedicineCharges = SumByType(request.Items, "Medicine"),
                    InvestigationCharges = SumByType(request.Items, "Investigation"),
                    DischargeMedicineCharges = SumByType(request.Items, "DischargeMedicine"),
                    NursingCharges = 0,   // pulled live from the Nursing module, not part of the editable grid
                    OperationCharges = 0, // pulled live from OT Management, not part of the editable grid
                    ProcedureCharges = SumByType(request.Items, "Procedure"),
                    OtherCharges = request.Items?
                        .Where(i => i.ItemType != "Bed" && i.ItemType != "DoctorVisit"
                                 && i.ItemType != "Medicine" && i.ItemType != "Investigation"
                                 && i.ItemType != "DischargeMedicine" && i.ItemType != "Procedure")
                        .Sum(i => i.TotalPrice) ?? 0,
                    SubTotal = request.SubTotal,
                    LineDiscountAmount = request.LineDiscountAmt,
                    TaxAmount = request.TaxAmount,
                    DiscountPercent = request.DiscountPercent,
                    DiscountAmount = request.DiscountAmount,
                    TotalAmount = request.TotalAmount,
                    Notes = request.Notes,
                    CreatedBy = userId
                };

                var items = (request.Items ?? new List<SaveIPDBillItemDto>())
                    .Select(i => new IPDBillItem
                    {
                        IPDId = request.IPDId,
                        ItemType = i.ItemType,
                        BillingMasterId = i.BillingMasterId,
                        ItemName = i.ItemName,
                        Quantity = i.Quantity <= 0 ? 1 : i.Quantity,
                        UnitPrice = i.UnitPrice,
                        DiscountValue = i.DiscountValue,
                        DiscountIsPercent = i.DiscountIsPercent,
                        GstPercent = i.GstPercent,
                        TotalPrice = i.TotalPrice
                    }).ToList();

                int billId = _billingRepo.SaveBill(bill, items, hospitalId, subHospitalId, request.IsDraft);

                return Json(new { success = true, billId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveBill. IPDId={IPDId}", request?.IPDId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        private static decimal SumByType(List<SaveIPDBillItemDto> items, string type)
            => items?.Where(i => i.ItemType == type).Sum(i => i.TotalPrice) ?? 0;

        // ── COLLECT PAYMENT — POST ──────────────────────────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult CollectPayment([FromBody] PayIPDBillRequest request)
        {
            try
            {
                if (request == null || request.BillId <= 0 || request.Amount <= 0)
                    return Json(new { success = false, message = "Invalid payment request." });

                var (hospitalId, subHospitalId, userId) = CurrentContext();

                var bill = _billingRepo.CollectPayment(
                    request.BillId, request.IPDId, hospitalId, subHospitalId,
                    request.Amount, request.PaymentMode, request.TransactionRef, request.Notes, userId);

                return Json(new
                {
                    success = true,
                    paidAmount = bill.PaidAmount,
                    dueAmount = bill.DueAmount,
                    paymentStatus = bill.PaymentStatus,
                    billStatus = bill.BillStatus
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CollectPayment. BillId={BillId}", request?.BillId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── CANCEL BILL — POST ──────────────────────────────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult CancelBill([FromBody] CancelIPDBillRequest request)
        {
            try
            {
                if (request == null || request.BillId <= 0)
                    return Json(new { success = false, message = "Invalid request." });

                var (hospitalId, subHospitalId, userId) = CurrentContext();
                var bill = _billingRepo.CancelBill(request.BillId, hospitalId, subHospitalId, request.Reason, userId);

                return Json(new { success = true, billStatus = bill.BillStatus });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelBill. BillId={BillId}", request?.BillId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── PRINT BILL — GET ────────────────────────────────────────────────
        [HttpGet]
        public IActionResult PrintBill(int ipdId)
        {
            try
            {
                var (hospitalId, subHospitalId, _) = CurrentContext();
                var vm = _billingRepo.GetBillSummary(ipdId, hospitalId, subHospitalId);

                var hospital = _IHospital.GetsubandMainHospitalById(hospitalId, subHospitalId);
                ViewBag.HospitalName = hospital?.Name;
                ViewBag.HospitalAddress = hospital?.Address;
                ViewBag.HospitalPhone = hospital?.PhoneNumber;
                ViewBag.HospitalEmail = hospital?.EmailId;
                ViewBag.HospitalLogo = hospital?.Logo;
                ViewBag.HospitalRegNo = hospital?.RegistrationNumber;
                ViewBag.BillNumber = vm.BillNumber ?? "N/A";

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PrintBill. IPDId={IPDId}", ipdId);
                TempData["Error"] = ex.Message;
                return RedirectToAction("GenerateBill", new { ipdId });
            }
        }

        // ── PATIENT BILLING SUMMARY — unchanged, still used by Discharge Planning ──
        [HttpGet]
        public IActionResult PatientBillingSummary(int ipdId)
        {
            try
            {
                var vm = _billingRepo.GetPatientBillingSummary(ipdId);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PatientBillingSummary. IPDId={IPDId}", ipdId);
                TempData["Error"] = ex.Message;
                return RedirectToAction("GenerateBill", new { ipdId });
            }
        }

        // ── LIVE STATUS BANNER (used inside the IPD dashboard) ──────────────
        [HttpGet]
        public IActionResult GetBillBanner(int ipdId)
        {
            var (hospitalId, subHospitalId, _) = CurrentContext();
            var bill = _billingRepo.GetBillByIPDId(ipdId, hospitalId, subHospitalId);
            if (bill == null) return Content("");

            var statusClass = bill.PaymentStatus == "Paid" ? "alert-success"
                             : bill.PaymentStatus == "Partial" ? "alert-warning"
                             : "alert-secondary";

            var html = $@"<div class='alert {statusClass} py-2 mb-3'>
        <i class='fas fa-receipt me-1'></i>
        <strong>Bill No: {bill.BillNumber}</strong>
        &nbsp;|&nbsp; Status: <strong>{bill.PaymentStatus}</strong> ({bill.BillStatus})
        &nbsp;|&nbsp; Total: <strong>₹{bill.TotalAmount:0.00}</strong>
        &nbsp;|&nbsp; Paid: <strong>₹{bill.PaidAmount:0.00}</strong>
        &nbsp;|&nbsp; Due: <strong class='text-danger'>₹{bill.DueAmount:0.00}</strong>
    </div>";
            return Content(html, "text/html");
        }
    }
}
