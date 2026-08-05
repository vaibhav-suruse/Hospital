using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class IPDPharmacyQueueController : Controller
    {
        private readonly IPharmacyQueue _pharmacy;

        public IPDPharmacyQueueController(IPharmacyQueue pharmacy)
        {
            _pharmacy = pharmacy;
        }

        // ── 1. QUEUE PAGE ─────────────────────────────────────────────────────
        public IActionResult Index()
        {
            return View();
        }

        // ── 2. AJAX — GET IPD QUEUE DATA (polled every 8s) ───────────────────
        [HttpGet]
        public JsonResult GetQueueData()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            // Uses GetIPDQueue — calls GetIPDMedicineNotifications proc (IPD only)
            var list = _pharmacy.GetIPDQueue(hospitalId, subHospitalId);

            var result = list.Select(n => new
            {
                notificationId = n.NotificationId,
                patientName = n.PatientName,
                doctorName = n.DoctorName,
                medicineCount = n.MedicineCount,
                medicinesSummary = n.MedicinesSummary,
                status = n.Status,
                ipdId = n.IPDId,
                roundId = n.RoundId,
                wardName = n.WardName,
                roomNo = n.RoomNo,
                bedNo = n.BedNo,
                createdAt = n.CreatedAt.ToString("dd-MM-yyyy hh:mm tt"),
                dispensedAt = n.DispensedAt.HasValue ? n.DispensedAt.Value.ToString("hh:mm tt") : null,
                timeAgo = n.TimeAgo
            }).ToList();

            return Json(result);
        }

        // ── 3. AJAX — GET PRESCRIPTION DETAIL for modal ──────────────────────
        [HttpGet]
        public JsonResult GetPrescription(int notificationId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            try
            {
                var allNotifs = _pharmacy.GetIPDQueue(hospitalId, subHospitalId);
                var notif = allNotifs.FirstOrDefault(n => n.NotificationId == notificationId);

                if (notif == null || notif.IPDId == null)
                    return Json(new { success = false, message = "Notification not found" });

                var medicines = _pharmacy.GetMedicinesForIPDPharmacy(
                    notif.IPDId.Value, notif.RoundId ?? 0, hospitalId, subHospitalId);

                return Json(new
                {
                    success = true,
                    notificationId = notif.NotificationId,
                    patientName = notif.PatientName,
                    doctorName = notif.DoctorName,
                    ipdId = notif.IPDId,
                    roundId = notif.RoundId,
                    wardName = notif.WardName,
                    roomNo = notif.RoomNo,
                    bedNo = notif.BedNo,
                    status = notif.Status,
                    createdAt = notif.CreatedAt.ToString("dd-MM-yyyy hh:mm tt"),
                    medicines = medicines.Select(m => new
                    {
                        medicineId = m.MedicineId,
                        medicineName = m.MedicineName,
                        medicineType = m.MedicineType,
                        route = m.Route,
                        morning = m.Morning,
                        afternoon = m.Afternoon,
                        evening = m.Evening,
                        days = m.Days,
                        qtyPerDay = m.QtyPerDay,
                        totalQty = m.TotalQty,
                        sellingPrice = m.SellingPrice,
                        lineTotal = m.TotalQty * m.SellingPrice,
                        availableStock = m.AvailableStock,
                        stockStatus = m.StockStatus
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── 4. AJAX — MARK DISPENSED ─────────────────────────────────────────
        [HttpPost]
        public JsonResult MarkDispensed(int notificationId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            try
            {
                _pharmacy.MarkDispensed(notificationId, hospitalId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── 5. MEDICINE BILLING PAGE ──────────────────────────────────────────
        [HttpGet]
        public IActionResult MedicineBill(int notificationId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var allNotifs = _pharmacy.GetIPDQueue(hospitalId, subHospitalId);
            var notif = allNotifs.FirstOrDefault(n => n.NotificationId == notificationId);

            if (notif == null)
                return RedirectToAction("Index");

            if (notif.Status == "Billed")
            {
                TempData["Info"] = "This prescription has already been billed.";
                return RedirectToAction("Index");
            }

            var medicines = new List<PharmacyMedicineLineVM>();
            if (notif.IPDId.HasValue)
                medicines = _pharmacy.GetMedicinesForIPDPharmacy(
                    notif.IPDId.Value, notif.RoundId ?? 0, hospitalId, subHospitalId);

            ViewBag.MobileNumber = _pharmacy.GetPatientMobile(notif.PatientId, hospitalId);
            ViewBag.Notification = notif;
            ViewBag.Medicines = medicines;
            return View();
        }

        // ── 6. SAVE IPD MEDICINE BILL ─────────────────────────────────────────
        [HttpPost]
        public JsonResult SaveMedicineBill([FromBody] IPDPharmacyBillVM bill)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            int createdBy = HttpContext.Session.GetInt32("UserId") ?? 0;

            try
            {
                if (bill == null || bill.Items == null || !bill.Items.Any())
                    return Json(new { success = false, message = "No items in bill." });

                int billId = _pharmacy.SaveIPDMedicineBill(bill, hospitalId, subHospitalId, createdBy);

                if (billId > 0)
                {
                    _pharmacy.MarkBilled(bill.NotificationId, hospitalId, billId);
                    return Json(new { success = true, billId });
                }

                return Json(new { success = false, message = "Bill could not be saved." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
