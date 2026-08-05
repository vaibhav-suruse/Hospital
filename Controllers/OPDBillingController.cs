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
    public class OPDBillingController : Controller
    {
        private readonly IOPDBilling _billingRepo;
        private readonly IBillingMaster _billingMaster;
        private readonly IOPDAppointment _appointmentRepo;
        private readonly IHospital _hospitalRepo;
        private readonly ILogger<OPDBillingController> _logger;

        public OPDBillingController(
            IOPDBilling billingRepo,
            IBillingMaster billingMaster,
            IOPDAppointment appointmentRepo,
            IHospital hospitalRepo,
            ILogger<OPDBillingController> logger)
        {
            _billingRepo = billingRepo;
            _billingMaster = billingMaster;
            _appointmentRepo = appointmentRepo;
            _hospitalRepo = hospitalRepo;
            _logger = logger;
        }

        // ── GENERATE BILL (GET) ──────────────────────────────────────────
        [HttpGet]
        public IActionResult GenerateBill(int appointmentId)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                // Track where the user came from so "Back" can return to the
                // correct list (WalkInConsultation for walk-ins, otherwise OPD).
                var appointment = _appointmentRepo.GetAppointmentById(appointmentId, hospitalId, subHospitalId);
                ViewBag.IsWalkIn = appointment?.IsWalkIn == true;

                var vm = _billingRepo.GetBillSummary(appointmentId, hospitalId, subHospitalId);

                // Check if an active bill already exists for this appointment.
                // If it does, hydrate EVERYTHING from it — this is what stops
                // the "reopen a bill, see a blank form, re-enter items, get a
                // duplicate bill" bug. The screen now always reflects the true
                // saved state, never a blank slate.
                var existingBill = _billingRepo.GetBillByAppointmentId(appointmentId, hospitalId, subHospitalId);
                if (existingBill != null)
                {
                    vm.BillId = existingBill.BillId;
                    vm.BillNumber = existingBill.BillNumber;
                    vm.BillDate = existingBill.BillDate;
                    vm.ConsultationFee = existingBill.ConsultationFee;
                    vm.MedicineCharges = existingBill.MedicineCharges;
                    vm.ProcedureCharges = existingBill.ProcedureCharges;
                    vm.OtherCharges = existingBill.OtherCharges;
                    vm.SubTotal = existingBill.SubTotal;
                    vm.LineDiscountAmount = existingBill.LineDiscountAmount;
                    vm.GstAmount = existingBill.GstAmount;
                    vm.ExtraDiscountValue = existingBill.ExtraDiscountValue;
                    vm.ExtraDiscountIsPercent = existingBill.ExtraDiscountIsPercent;
                    vm.ExtraDiscountAmount = existingBill.ExtraDiscountAmount;
                    vm.TotalAmount = existingBill.TotalAmount;
                    vm.PaidAmount = existingBill.PaidAmount;
                    vm.DueAmount = existingBill.DueAmount;
                    vm.PaymentStatus = existingBill.PaymentStatus;
                    vm.BillStatus = existingBill.BillStatus;
                    vm.CancelledReason = existingBill.CancelledReason;
                    vm.CancelledDate = existingBill.CancelledDate;
                    vm.PaymentMode = existingBill.PaymentMode;
                    vm.Notes = existingBill.Notes;
                    vm.OtherItems = _billingRepo.GetBillItems(existingBill.BillId, hospitalId, subHospitalId);
                    vm.Payments = _billingRepo.GetPayments(existingBill.BillId, hospitalId, subHospitalId);
                }

                // Billing master items for the "Add from Billing Master" search —
                // full fields, not just a label, so selecting one can auto-fill
                // price/GST/discount/category on the client.
                var masterItems = _billingMaster
                    .GetAllBillings(hospitalId, subHospitalId)
                    .Where(b => b.IsActive == 1)
                    .Select(b => new
                    {
                        id = b.Id,
                        name = b.Name,
                        category = b.BillingType,
                        amount = b.Amount,
                        gstPercent = b.GstPercent,
                        discountPercent = b.DefaultDiscountPercent
                    }).ToList();

                ViewBag.BillingMasterItemsJson = System.Text.Json.JsonSerializer.Serialize(masterItems);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateBill. AppointmentId={Id}", appointmentId);
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "OPDAppointment");
            }
        }

        // ── PRINT BILL (GET) — standalone professional A4 print page ─────
        // Renders the saved bill as a clean, branded, printable document
        // (modeled on the PrintPrescription page) instead of printing the
        // whole editing form. Hospital branding is resolved exactly the same
        // way PrintPriscription does it.
        [HttpGet]
        public IActionResult PrintBill(int appointmentId)
        {
            try
            {
                if (appointmentId <= 0)
                    return BadRequest("Appointment identifier missing");

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var appointment = _appointmentRepo.GetAppointmentById(appointmentId, hospitalId, subHospitalId);
                if (appointment == null)
                    return NotFound("Appointment not found");

                ViewBag.IsWalkIn = appointment.IsWalkIn;

                var vm = _billingRepo.GetBillSummary(appointmentId, hospitalId, subHospitalId);

                // Hydrate from the saved bill (same as GenerateBill) so the
                // printed document always reflects the true saved state.
                var existingBill = _billingRepo.GetBillByAppointmentId(appointmentId, hospitalId, subHospitalId);
                if (existingBill != null)
                {
                    vm.BillId = existingBill.BillId;
                    vm.BillNumber = existingBill.BillNumber;
                    vm.BillDate = existingBill.BillDate;
                    vm.ConsultationFee = existingBill.ConsultationFee;
                    vm.MedicineCharges = existingBill.MedicineCharges;
                    vm.ProcedureCharges = existingBill.ProcedureCharges;
                    vm.OtherCharges = existingBill.OtherCharges;
                    vm.SubTotal = existingBill.SubTotal;
                    vm.LineDiscountAmount = existingBill.LineDiscountAmount;
                    vm.GstAmount = existingBill.GstAmount;
                    vm.ExtraDiscountValue = existingBill.ExtraDiscountValue;
                    vm.ExtraDiscountIsPercent = existingBill.ExtraDiscountIsPercent;
                    vm.ExtraDiscountAmount = existingBill.ExtraDiscountAmount;
                    vm.TotalAmount = existingBill.TotalAmount;
                    vm.PaidAmount = existingBill.PaidAmount;
                    vm.DueAmount = existingBill.DueAmount;
                    vm.PaymentStatus = existingBill.PaymentStatus;
                    vm.BillStatus = existingBill.BillStatus;
                    vm.CancelledReason = existingBill.CancelledReason;
                    vm.CancelledDate = existingBill.CancelledDate;
                    vm.PaymentMode = existingBill.PaymentMode;
                    vm.Notes = existingBill.Notes;
                    vm.OtherItems = _billingRepo.GetBillItems(existingBill.BillId, hospitalId, subHospitalId);
                    vm.Payments = _billingRepo.GetPayments(existingBill.BillId, hospitalId, subHospitalId);
                }

                // Hospital branding for the print header.
                var hospital = _hospitalRepo.GetsubandMainHospitalById(hospitalId, subHospitalId);
                ViewBag.HospitalName = hospital?.Name;
                ViewBag.HospitalAddress = hospital?.Address;
                ViewBag.HospitalLogo = hospital?.Logo;
                ViewBag.HospitalEmail = hospital?.EmailId;
                ViewBag.HospitalPhone = hospital?.PhoneNumber;
                ViewBag.HospitalRegNo = hospital?.RegistrationNumber;

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PrintBill. AppointmentId={Id}", appointmentId);
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "OPDAppointment");
            }
        }

        // ── SAVE BILL (POST) — true upsert ───────────────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult SaveBill([FromBody] SaveOPDBillRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { success = false, message = "Empty request." });

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

                // Consultation / Medicine / Procedure charge subtotals are
                // derived from the items actually submitted, not trusted as
                // separate numbers from the client (that split is only used
                // for the summary panel's category rows).
                var itemDtos = request.Items ?? new List<SaveOPDBillItemDto>();
                decimal consultationFee = itemDtos.Where(i => string.Equals(i.ItemType, "Consultation", StringComparison.OrdinalIgnoreCase)).Sum(i => i.TotalPrice);
                decimal medicineCharges = itemDtos.Where(i => string.Equals(i.ItemType, "Medicine", StringComparison.OrdinalIgnoreCase)).Sum(i => i.TotalPrice);
                decimal procedureCharges = itemDtos.Where(i => string.Equals(i.ItemType, "Procedure", StringComparison.OrdinalIgnoreCase)).Sum(i => i.TotalPrice);
                decimal otherCharges = itemDtos.Where(i =>
                        !string.Equals(i.ItemType, "Consultation", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(i.ItemType, "Medicine", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(i.ItemType, "Procedure", StringComparison.OrdinalIgnoreCase))
                    .Sum(i => i.TotalPrice);

                var bill = new OPDBill
                {
                    AppointmentId = request.AppointmentId,
                    OPDId = request.OPDId,
                    PatientId = request.PatientId,
                    HospitalId = hospitalId,
                    SubHospitalId = subHospitalId,
                    BillDate = request.BillDate ?? DateTime.Now,
                    ConsultationFee = consultationFee,
                    MedicineCharges = medicineCharges,
                    ProcedureCharges = procedureCharges,
                    OtherCharges = otherCharges,
                    SubTotal = request.SubTotal,
                    LineDiscountAmount = request.LineDiscountAmt,
                    GstAmount = request.GstAmount,
                    ExtraDiscountValue = request.ExtraDiscountValue,
                    ExtraDiscountIsPercent = request.ExtraDiscountIsPercent,
                    ExtraDiscountAmount = request.ExtraDiscountAmount,
                    TotalAmount = request.TotalAmount,
                    Notes = request.Notes,
                    CreatedBy = userId
                };

                var items = itemDtos.Select(i => new OPDBillItem
                {
                    ItemType = i.ItemType,
                    BillingMasterId = i.BillingMasterId,
                    ItemName = i.ItemName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    DiscountValue = i.DiscountValue,
                    DiscountIsPercent = i.DiscountIsPercent,
                    GstPercent = i.GstPercent,
                    TotalPrice = i.TotalPrice
                }).ToList();

                int billId = _billingRepo.SaveBill(bill, items, hospitalId, subHospitalId, request.IsDraft);

                // Any payment collected at the moment of generating/editing the
                // bill goes through the same append-only ledger as a normal
                // "Collect Payment" — nothing here silently drops to 0 anymore.
                OPDBill finalBill = null;
                var paymentEntries = request.PaymentEntries ?? new List<PaymentEntryDto>();
                foreach (var pe in paymentEntries.Where(p => p.Amount > 0))
                {
                    finalBill = _billingRepo.CollectPayment(
                        billId, request.AppointmentId, hospitalId, subHospitalId,
                        pe.Amount, pe.Mode, transactionRef: null, notes: request.Notes, receivedBy: userId);
                }

                return Json(new
                {
                    success = true,
                    billId = billId,
                    paidAmount = finalBill?.PaidAmount,
                    dueAmount = finalBill?.DueAmount,
                    paymentStatus = finalBill?.PaymentStatus,
                    billStatus = finalBill?.BillStatus ?? (request.IsDraft ? "Draft" : "Finalized")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveBill");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── COLLECT PAYMENT (POST) — appends to the ledger ───────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult PayBill([FromBody] PayOPDBillRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { success = false, message = "Empty request." });

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

                var updatedBill = _billingRepo.CollectPayment(
                    request.BillId,
                    request.AppointmentId,
                    hospitalId,
                    subHospitalId,
                    request.Amount,
                    request.PaymentMode,
                    request.TransactionRef,
                    request.Notes,
                    userId);

                return Json(new
                {
                    success = true,
                    paidAmount = updatedBill?.PaidAmount,
                    dueAmount = updatedBill?.DueAmount,
                    paymentStatus = updatedBill?.PaymentStatus
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PayBill. BillId={Id}", request?.BillId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── CANCEL BILL (POST) ────────────────────────────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult CancelBill([FromBody] CancelOPDBillRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { success = false, message = "Empty request." });

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

                var updated = _billingRepo.CancelBill(request.BillId, hospitalId, subHospitalId, request.Reason, userId);
                return Json(new { success = true, billStatus = updated?.BillStatus });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelBill. BillId={Id}", request?.BillId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── GET BILL PANEL (AJAX partial reload) ─────────────────────────
        [HttpGet]
        public IActionResult GetBillPanel(int appointmentId)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var vm = _billingRepo.GetBillSummary(appointmentId, hospitalId, subHospitalId);
                var existingBill = _billingRepo.GetBillByAppointmentId(appointmentId, hospitalId, subHospitalId);
                if (existingBill != null)
                {
                    vm.BillId = existingBill.BillId;
                    vm.BillNumber = existingBill.BillNumber;
                    vm.ConsultationFee = existingBill.ConsultationFee;
                    vm.MedicineCharges = existingBill.MedicineCharges;
                    vm.ProcedureCharges = existingBill.ProcedureCharges;
                    vm.OtherCharges = existingBill.OtherCharges;
                    vm.SubTotal = existingBill.SubTotal;
                    vm.TotalAmount = existingBill.TotalAmount;
                    vm.PaidAmount = existingBill.PaidAmount;
                    vm.DueAmount = existingBill.DueAmount;
                    vm.PaymentStatus = existingBill.PaymentStatus;
                    vm.PaymentMode = existingBill.PaymentMode;
                }
                return PartialView("_OPDBillPanel", vm);
            }
            catch (Exception ex)
            {
                return Content("<div class='alert alert-danger'>Error: " + ex.Message + "</div>");
            }
        }
    }
}
