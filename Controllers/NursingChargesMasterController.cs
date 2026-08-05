// Controllers/NursingChargesMasterController.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class NursingChargesMasterController : Controller
    {
        private readonly INursingChargesMaster _repo;
        private readonly ILogger<NursingChargesMasterController> _logger;

        public NursingChargesMasterController(
            INursingChargesMaster repo,
            ILogger<NursingChargesMasterController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public IActionResult Index()
        {
            try
            {
                int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subid = HttpContext.Session.GetInt32("SubHospitalId");
                return View(_repo.GetAll(hid, subid));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NursingChargesMaster Index");
                TempData["Error"] = "Error loading nursing charges.";
                return View(
                    new System.Collections.Generic.List<NursingChargesMaster>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new NursingChargesMaster
            {
                IsActive = true,
                ChargeType = "PerProcedure"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(NursingChargesMaster model)
        {
            try
            {
                int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subid = HttpContext.Session.GetInt32("SubHospitalId");

                if (model.isUpdate)
                {
                    _repo.Update(model);
                    TempData["Success"] = "Updated successfully.";
                }
                else
                {
                    _repo.Create(model, hid, subid);
                    TempData["Success"] = "Created successfully.";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving NursingChargesMaster");
                TempData["Error"] = "Error saving nursing charge.";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            try
            {
                var data = _repo.GetById(id);
                if (data == null)
                {
                    TempData["Error"] = "Record not found.";
                    return RedirectToAction("Index");
                }
                data.isUpdate = true;
                return View("Create", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in NursingChargesMaster Edit. Id={Id}", id);
                TempData["Error"] = "Error loading record.";
                return RedirectToAction("Index");
            }
        }

        public IActionResult Delete(int id)
        {
            try
            {
                _repo.Delete(id);
                TempData["Success"] = "Deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in NursingChargesMaster Delete. Id={Id}", id);
                TempData["Error"] = "Error deleting record.";
            }
            return RedirectToAction("Index");
        }

        // JSON for vitals form dropdown
        [HttpGet]
        public IActionResult GetAllJson()
        {
            try
            {
                int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subid = HttpContext.Session.GetInt32("SubHospitalId");
                return Json(_repo.GetAll(hid, subid));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllJson NursingChargesMaster");
                return Json(
                    new System.Collections.Generic.List<NursingChargesMaster>());
            }
        }
    }
}