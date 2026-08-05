// Controllers/IPDNursingChargeController.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class IPDNursingChargeController : Controller
    {
        private readonly IIPDNursingCharge _repo;
        private readonly ILogger<IPDNursingChargeController> _logger;

        public IPDNursingChargeController(
            IIPDNursingCharge repo,
            ILogger<IPDNursingChargeController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        // ── GET nursing charges for IPD (AJAX) ──────────────────────────
        [HttpGet]
        public IActionResult GetByIPDId(int ipdId)
        {
            try
            {
                return Json(_repo.GetByIPDId(ipdId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in GetByIPDId NursingCharges. IPDId={IPDId}",
                    ipdId);
                return Json(new List<IPDNursingCharge>());
            }
        }

        // ── SAVE charges (called from vitals form) ───────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult SaveCharges(
            [FromBody] SaveNursingRequest req)
        {
            try
            {
                int hid =
                    HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subid =
                    HttpContext.Session.GetInt32("SubHospitalId");

                if (req?.Charges == null || req.Charges.Count == 0)
                    return Json(new { success = true });

                foreach (var c in req.Charges)
                {
                    c.ParentHospitalId = hid;
                    c.SubHospitalId = subid;
                }

                _repo.SaveCharges(req.Charges);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveCharges");
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // ── DELETE single charge ─────────────────────────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult DeleteCharge(int id)
        {
            try
            {
                _repo.Delete(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in DeleteCharge. Id={Id}", id);
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // ── Request model ────────────────────────────────────────────────
        public class SaveNursingRequest
        {
            public List<IPDNursingCharge> Charges { get; set; }
        }
    }
}