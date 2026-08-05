// Controllers/IPDOperationController.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class IPDOperationController : Controller
    {
        private readonly IIPDOperation _opRepo;
        private readonly IOperationMaster _masterRepo;
        private readonly IDoctor _doctorRepo;
        private readonly INurse _nurseRepo;
        private readonly ILogger<IPDOperationController> _logger;

        public IPDOperationController(
            IIPDOperation opRepo,
            IOperationMaster masterRepo,
            IDoctor doctorRepo,
            INurse nurseRepo,
            ILogger<IPDOperationController> logger)
        {
            _opRepo = opRepo;
            _masterRepo = masterRepo;
            _doctorRepo = doctorRepo;
            _nurseRepo = nurseRepo;
            _logger = logger;
        }

        // ── GET all operations for IPD (AJAX) ────────────────────────────
        [HttpGet]
        public IActionResult GetByIPDId(int ipdId)
        {
            try
            {
                var list = _opRepo.GetByIPDId(ipdId);
                foreach (var op in list)
                {
                    op.Staff = _opRepo.GetStaffByOperationId(
                        op.IPDOperationId);
                    op.TotalStaffCharge =
                        op.Staff != null
                        ? (decimal)new System.Collections.Generic
                            .List<IPDOperationStaff>(op.Staff)
                            .ConvertAll(s => (double)s.Charge)
                            .ToArray()
                            .Sum()
                        : 0;
                }
                return Json(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in GetByIPDId. IPDId={IPDId}", ipdId);
                return Json(new List<IPDOperationModel>());
            }
        }

        // ── GET master data for dropdowns (AJAX) ─────────────────────────
        [HttpGet]
        public IActionResult GetMasterData()
        {
            try
            {
                int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subid = HttpContext.Session.GetInt32("SubHospitalId");

                var operations = _masterRepo.GetAll(hid, subid);
                var doctors = _doctorRepo.GetAllDoctor(hid, subid);
                var nurses = _nurseRepo.GetAll(hid, subid);

                return Json(new
                {
                    operations = operations,
                    doctors = doctors,
                    nurses = nurses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMasterData");
                return Json(new { });
            }
        }

        // ── SAVE operation + staff (AJAX POST) ───────────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult SaveOperation(
            [FromBody] SaveOperationRequest req)
        {
            try
            {
                int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subid = HttpContext.Session.GetInt32("SubHospitalId");

                var model = new IPDOperationModel
                {
                    IPDId = req.IPDId,
                    OperationId = req.OperationId,
                    OperationDate = req.OperationDate,
                    SurgeonId = req.SurgeonId,
                    AnesthesistId = req.AnesthesistId,
                    ActualCharge = req.ActualCharge,
                    AnesthesiaCharge = req.AnesthesiaCharge,
                    SurgeonCharge = req.SurgeonCharge,
                    OTCharge = req.OTCharge,
                    Notes = req.Notes,
                    Staff = req.Staff
                                       ?? new List<IPDOperationStaff>()
                };

                int opId = _opRepo.SaveAndReturnId(model, hid, subid);
                return Json(new { success = true, operationId = opId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in SaveOperation. IPDId={IPDId}", req?.IPDId);
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public IActionResult DeleteOperation([FromBody] DeleteOperationRequest req)
        {
            try
            {
                _opRepo.Delete(req.Id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteOperation. Id={Id}", req?.Id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class SaveOperationRequest
        {
            public int IPDId { get; set; }
            public int OperationId { get; set; }
            public DateTime OperationDate { get; set; }
            public int? SurgeonId { get; set; }
            public int? AnesthesistId { get; set; }
            public decimal ActualCharge { get; set; }
            public decimal AnesthesiaCharge { get; set; }
            public decimal SurgeonCharge { get; set; }
            public decimal OTCharge { get; set; }
            public string Notes { get; set; }
            public List<IPDOperationStaff> Staff { get; set; }
        }

        public class DeleteOperationRequest
        {
            public int Id { get; set; }
            public int IpdId { get; set; }
        }
    }
}