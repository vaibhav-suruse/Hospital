using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class CounterController : Controller
    {
        private readonly ICounter _counterRepo;

        public CounterController(ICounter counterRepo)
        {
            _counterRepo = counterRepo;
        }

        // ─── MAIN COUNTER PAGE ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Index()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var summary = _counterRepo.GetTodaySummary(hospitalId, subHospitalId);
            ViewBag.TodaySummary = summary;

            return View();
        }

        // ─── SEARCH CUSTOMER (AJAX) ────────────────────────────────────
        [HttpGet]
        public JsonResult SearchCustomer(string search)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            if (string.IsNullOrWhiteSpace(search) || search.Length < 3)
                return Json(new List<CounterCustomerModel>());

            var customers = _counterRepo.SearchCustomer(search, hospitalId, subHospitalId);
            return Json(customers);
        }

        // ─── GET PENDING BILLS FOR CUSTOMER (AJAX) ─────────────────────
        [HttpGet]
        public JsonResult GetPendingBills(int customerId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            var bills = _counterRepo.GetPendingBills(customerId, hospitalId);
            return Json(bills);
        }

        // ─── SEARCH MEDICINE (AJAX) ────────────────────────────────────
        [HttpGet]
        public JsonResult SearchMedicine(string search)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            if (string.IsNullOrWhiteSpace(search))
                return Json(new List<CounterMedicineSearchModel>());

            var medicines = _counterRepo.SearchMedicine(search, hospitalId, subHospitalId);
            return Json(medicines);
        }

        // ─── CREATE NEW CUSTOMER (AJAX) ────────────────────────────────
        [HttpPost]
        public JsonResult CreateCustomer(string customerName, string mobileNumber, string address)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            if (string.IsNullOrWhiteSpace(mobileNumber) || string.IsNullOrWhiteSpace(customerName))
                return Json(new { success = false, message = "Name and Mobile are required" });

            try
            {
                var customer = _counterRepo.GetOrCreateCustomer(mobileNumber, customerName, address, hospitalId, subHospitalId);
                return Json(new { success = true, customerId = customer.CustomerId, customerName = customer.CustomerName, isNew = customer.IsNew });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ─── SAVE BILL (POST) ──────────────────────────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult SaveBill([FromBody] SaveBillRequest request)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            try
            {
                // Generate bill number
                string billNumber = $"CTR-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";

                var bill = new CounterBillModel
                {
                    BillNumber = billNumber,
                    CustomerId = request.CustomerId,
                    CustomerName = request.CustomerName,
                    MobileNumber = request.MobileNumber,
                    SubTotal = request.SubTotal,
                    DiscountType = request.DiscountType,
                    DiscountValue = request.DiscountValue,
                    DiscountAmount = request.DiscountAmount,
                    TotalAmount = request.TotalAmount,
                    PaymentMode = request.PaymentMode,
                    PaidAmount = request.PaidAmount,
                    DueAmount = request.DueAmount,
                    CreatedBy = userId
                };

                var cartItems = request.CartItems.Select(c => new CounterCartItem
                {
                    MedicineId = c.MedicineId,
                    MedicineName = c.MedicineName,
                    Quantity = c.Quantity,
                    UnitPrice = c.UnitPrice,
                    MaxStock = c.MaxStock
                }).ToList();

                int billId = _counterRepo.SaveBill(bill, cartItems, hospitalId, subHospitalId);

                return Json(new { success = true, billId = billId, billNumber = billNumber });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ─── PRINT BILL ─────────────────────────────────────────────────
        [HttpGet]
        public IActionResult PrintBill(int id)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            var bill = _counterRepo.GetBillDetails(id, hospitalId);

            if (bill == null)
                return NotFound();

            return View(bill);
        }

        // ─── REQUEST MODELS ────────────────────────────────────────────
        public class SaveBillRequest
        {
            public int CustomerId { get; set; }
            public string CustomerName { get; set; }
            public string MobileNumber { get; set; }
            public decimal SubTotal { get; set; }
            public string DiscountType { get; set; }
            public decimal DiscountValue { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal TotalAmount { get; set; }
            public string PaymentMode { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal DueAmount { get; set; }
            public List<CartItemRequest> CartItems { get; set; }
        }

        public class CartItemRequest
        {
            public int MedicineId { get; set; }
            public string MedicineName { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public int MaxStock { get; set; }
        }

        // ─── COLLECT PENDING PAYMENT ─────────────────────────────────────────
        [HttpPost]
        public JsonResult CollectPayment(int billId, decimal amount, string paymentMode)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            try
            {
                // Validate inputs
                if (billId <= 0)
                    return Json(new { success = false, message = "Invalid bill ID" });

                if (amount <= 0)
                    return Json(new { success = false, message = "Amount must be greater than 0" });

                if (string.IsNullOrEmpty(paymentMode))
                    return Json(new { success = false, message = "Please select payment mode" });

                bool result = _counterRepo.CollectPayment(billId, amount, paymentMode, userId, hospitalId);

                if (result)
                    return Json(new { success = true, message = "Payment collected successfully" });
                else
                    return Json(new { success = false, message = "Failed to collect payment" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ─── CUSTOMER PURCHASE HISTORY ─────────────────────────────────────────
        [HttpGet]
        public IActionResult CustomerHistory(int id)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            // Get customer details directly by ID
            var customer = _counterRepo.GetCustomerById(id, hospitalId, subHospitalId);

            if (customer == null)
            {
                return NotFound("Customer not found");
            }

            var bills = _counterRepo.GetCustomerHistory(id, hospitalId, subHospitalId);
            var items = _counterRepo.GetCustomerPurchaseItems(id, hospitalId, subHospitalId);

            ViewBag.Customer = customer;
            ViewBag.Bills = bills;
            ViewBag.Items = items;

            return View();
        }

        // ─── GET CUSTOMER HISTORY (AJAX for modal) ─────────────────────────────
        [HttpGet]
        public JsonResult GetCustomerHistory(int customerId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var bills = _counterRepo.GetCustomerHistory(customerId, hospitalId, subHospitalId);
            var items = _counterRepo.GetCustomerPurchaseItems(customerId, hospitalId, subHospitalId);

            return Json(new { bills = bills, items = items });
        }

    }
}