using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text.Json;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Controllers
{
   
    // ── Controller ───────────────────────────────────────────────────────────
    public class PharmacyDashboardController : Controller
    {
        private readonly string _conn;

        public PharmacyDashboardController(IConfiguration configuration)
        {
            _conn = configuration.GetConnectionString("MySqlConnection");
        }

        public IActionResult Index()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int subHospitalId = HttpContext.Session.GetInt32("SubHospitalId") ?? 0;

            // ── 1. Stat Cards ─────────────────────────────────────────────
            ViewBag.TotalMedicines = GetScalar(
                "SELECT COUNT(*) FROM tbl_medicine WHERE IsActive = 1 AND Hospital_Id = @h",
                hospitalId);

            ViewBag.LowStockAlerts = GetScalar(
                "SELECT COUNT(*) FROM stock WHERE TotalQuantity <= ReorderLevel AND HospitalId = @h",
                hospitalId);

            ViewBag.ExpiryIn30Days = GetScalar(@"
                SELECT COUNT(*) FROM batches
                WHERE HospitalId = @h
                  AND ExpiryDate BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL 30 DAY)",
                hospitalId);

            ViewBag.ActiveSuppliers = GetScalar(
                "SELECT COUNT(*) FROM store_suppliers WHERE HospitalId = @h",
                hospitalId);

            ViewBag.BillsGenerated = GetScalar(
                "SELECT COUNT(*) FROM counter_bill WHERE DATE(BillDate) = CURDATE() AND HospitalId = @h",
                hospitalId);

            //ViewBag.IPDPatientsToday = GetScalar(
            //"SELECT COUNT(*) FROM medicine_notifications WHERE Type = 'IPD' AND HospitalId = @h",
            //hospitalId);

            //ViewBag.OPDPatientsToday = GetScalar(
            //    "SELECT COUNT(*) FROM medicine_notifications WHERE Type = 'OPD' AND HospitalId = @h",
            //    hospitalId);

            ViewBag.OPDPatientsToday = GetScalar(
            "SELECT COUNT(*) FROM medicine_notifications WHERE Type = 'OPD' AND DATE(CreatedAt) = CURDATE() AND HospitalId = @h",
            hospitalId);

            ViewBag.IPDPatientsToday = GetScalar(
                "SELECT COUNT(*) FROM medicine_notifications WHERE Type = 'IPD' AND DATE(CreatedAt) = CURDATE() AND HospitalId = @h",
                hospitalId);

            decimal todaySales = GetDecimal(@"
                SELECT COALESCE(SUM(TotalAmount), 0)
                FROM counter_bill
                WHERE DATE(BillDate) = CURDATE() AND HospitalId = @h",
                hospitalId);
            ViewBag.TodaysSales = "₹" + todaySales.ToString("N0");

            // ── 2. Monthly Sales Chart ────────────────────────────────────
            var monthly = GetMonthlySales(hospitalId);
            ViewBag.ChartLabels = JsonSerializer.Serialize(monthly.Labels);
            ViewBag.IPDSalesData = JsonSerializer.Serialize(monthly.IPD);
            ViewBag.OPDSalesData = JsonSerializer.Serialize(monthly.OPD);
            ViewBag.CounterSalesData = JsonSerializer.Serialize(monthly.Counter);

            // ── 3. Weekly Sales Chart ─────────────────────────────────────
            var weekly = GetWeeklySales(hospitalId);
            ViewBag.WeeklyIPD = JsonSerializer.Serialize(weekly.IPD);
            ViewBag.WeeklyOPD = JsonSerializer.Serialize(weekly.OPD);
            ViewBag.WeeklyCounter = JsonSerializer.Serialize(weekly.Counter);

            // ── 4-6. Tables ───────────────────────────────────────────────
            ViewBag.LowStockItems = GetLowStockItems(hospitalId);
            ViewBag.ExpiryItems = GetExpiryItems(hospitalId);
            ViewBag.RecentBills = GetRecentBills(hospitalId);

            return View("~/Views/PharmacyDashboard/Index.cshtml");
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private int GetScalar(string sql, int hospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                con.Open();
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
            catch { return 0; }
        }

        private decimal GetDecimal(string sql, int hospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                con.Open();
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
            }
            catch { return 0m; }
        }

        private (List<string> Labels, List<decimal> IPD, List<decimal> OPD, List<decimal> Counter)
            GetMonthlySales(int hospitalId)
        {
            var labels = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            var ipd = new List<decimal>(new decimal[12]);
            var opd = new List<decimal>(new decimal[12]);
            var counter = new List<decimal>(new decimal[12]);

            try
            {
                string sql = @"SELECT MONTH(BillDate) AS Mo, BillNumber, COALESCE(TotalAmount,0) AS Amt
                               FROM counter_bill
                               WHERE YEAR(BillDate) = YEAR(CURDATE()) AND HospitalId = @h
                               ORDER BY Mo";
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    int mo = Convert.ToInt32(r["Mo"]) - 1;
                    string billNo = r["BillNumber"]?.ToString() ?? "";
                    decimal amt = Convert.ToDecimal(r["Amt"]);
                    if (billNo.StartsWith("IPD")) ipd[mo] += amt;
                    else if (billNo.StartsWith("PHARM")) opd[mo] += amt;
                    else counter[mo] += amt;
                }
            }
            catch { }
            return (labels, ipd, opd, counter);
        }

        private (List<decimal> IPD, List<decimal> OPD, List<decimal> Counter)
            GetWeeklySales(int hospitalId)
        {
            var ipd = new List<decimal>(new decimal[7]);
            var opd = new List<decimal>(new decimal[7]);
            var counter = new List<decimal>(new decimal[7]);

            try
            {
                string sql = @"SELECT DAYOFWEEK(BillDate) AS DoW, BillNumber, COALESCE(TotalAmount,0) AS Amt
                               FROM counter_bill
                               WHERE BillDate >= DATE_SUB(CURDATE(), INTERVAL 6 DAY) AND HospitalId = @h";
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    int dow = (Convert.ToInt32(r["DoW"]) + 5) % 7;
                    string billNo = r["BillNumber"]?.ToString() ?? "";
                    decimal amt = Convert.ToDecimal(r["Amt"]);
                    if (billNo.StartsWith("IPD")) ipd[dow] += amt;
                    else if (billNo.StartsWith("PHARM")) opd[dow] += amt;
                    else counter[dow] += amt;
                }
            }
            catch { }
            return (ipd, opd, counter);
        }

        private List<PharmLowStockItem> GetLowStockItems(int hospitalId)
        {
            var list = new List<PharmLowStockItem>();
            try
            {
                // NOTE: tbl_medicine uses Hospital_Id (with underscore), stock uses HospitalId
                // Using LEFT JOIN for medicine so a missing medicine record doesn't drop the stock row
                string sql = @"SELECT COALESCE(m.MedicineName,'Unknown') AS MedicineName,
                                      COALESCE(m.Unit,'—') AS Unit,
                                      COALESCE(c.CategoryName,'—') AS CategoryName,
                                      s.TotalQuantity AS Quantity,
                                      COALESCE(s.ReorderLevel, 10) AS ReorderLevel
                               FROM stock s
                               LEFT JOIN tbl_medicine m ON m.MedicineId = s.MedicineId
                               LEFT JOIN categories   c ON c.CategoryId  = m.CategoryId
                               WHERE s.HospitalId = @h
                                 AND s.TotalQuantity <= COALESCE(s.ReorderLevel, 10)
                               ORDER BY s.TotalQuantity ASC LIMIT 10";
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new PharmLowStockItem
                    {
                        MedicineName = r["MedicineName"]?.ToString(),
                        Unit = r["Unit"]?.ToString() ?? "—",
                        CategoryName = r["CategoryName"]?.ToString(),
                        Quantity = Convert.ToInt32(r["Quantity"]),
                        ReorderLevel = r["ReorderLevel"] != DBNull.Value ? Convert.ToInt32(r["ReorderLevel"]) : 10
                    });
                }
            }
            catch { }
            return list;
        }

        private List<PharmExpiryItem> GetExpiryItems(int hospitalId)
        {
            var list = new List<PharmExpiryItem>();
            try
            {
                string sql = @"SELECT m.MedicineName, b.BatchNumber AS BatchNo, b.ExpiryDate
                               FROM batches b
                               INNER JOIN tbl_medicine m ON m.MedicineId = b.MedicineId
                               WHERE b.HospitalId = @h
                                 AND b.ExpiryDate BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL 30 DAY)
                               ORDER BY b.ExpiryDate ASC LIMIT 10";
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new PharmExpiryItem
                    {
                        MedicineName = r["MedicineName"]?.ToString(),
                        BatchNo = r["BatchNo"]?.ToString() ?? "—",
                        ExpiryDate = Convert.ToDateTime(r["ExpiryDate"])
                    });
                }
            }
            catch { }
            return list;
        }

        private List<PharmRecentBill> GetRecentBills(int hospitalId)
        {
            var list = new List<PharmRecentBill>();
            try
            {
                string sql = @"SELECT BillNumber, CustomerName, TotalAmount, PaymentMode, BillDate
                               FROM counter_bill
                               WHERE HospitalId = @h
                               ORDER BY BillDate DESC LIMIT 10";
                using var con = new MySqlConnection(_conn);
                using var cmd = new MySqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@h", hospitalId);
                con.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new PharmRecentBill
                    {
                        BillNumber = r["BillNumber"]?.ToString(),
                        CustomerName = r["CustomerName"]?.ToString() ?? "—",
                        TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
                        PaymentMode = r["PaymentMode"]?.ToString() ?? "Cash",
                        CreatedAt = Convert.ToDateTime(r["BillDate"])
                    });
                }
            }
            catch { }
            return list;
        }
    }
}
