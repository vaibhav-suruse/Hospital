// Controllers/OperationMasterController.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class OperationMasterController : Controller
    {
        private readonly IOperationMaster _repo;
        private readonly ILogger<OperationMasterController> _logger;

        public OperationMasterController(
            IOperationMaster repo,
            ILogger<OperationMasterController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        // ── LIST ─────────────────────────────────────────────────────────
        public IActionResult Index()
        {
            try
            {
                int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subid = HttpContext.Session.GetInt32("SubHospitalId");
                var list = _repo.GetAll(hid, subid);
                return View(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OperationMaster Index");
                TempData["Error"] = "Error loading operations.";
                return View(new System.Collections.Generic.List<OperationMaster>());
            }
        }

        // ── CREATE GET ───────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            return View(new OperationMaster { IsActive = true });
        }

        // ── CREATE / UPDATE POST ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OperationMaster model)
        {
            try
            {
                int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subid = HttpContext.Session.GetInt32("SubHospitalId");

                if (model.isUpdate)
                {
                    _repo.Update(model);
                    TempData["Success"] = "Operation updated successfully.";
                }
                else
                {
                    _repo.Create(model, hid, subid);
                    TempData["Success"] = "Operation created successfully.";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving OperationMaster");
                TempData["Error"] = "Error saving operation.";
                return View(model);
            }
        }

        // ── EDIT GET ─────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Edit(int id)
        {
            try
            {
                var data = _repo.GetById(id);
                if (data == null)
                {
                    TempData["Error"] = "Operation not found.";
                    return RedirectToAction("Index");
                }
                data.isUpdate = true;
                return View("Create", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in OperationMaster Edit. Id={Id}", id);
                TempData["Error"] = "Error loading operation.";
                return RedirectToAction("Index");
            }
        }

        // ── DELETE ───────────────────────────────────────────────────────
        public IActionResult Delete(int id)
        {
            try
            {
                _repo.Delete(id);
                TempData["Success"] = "Operation deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in OperationMaster Delete. Id={Id}", id);
                TempData["Error"] = "Error deleting operation.";
            }
            return RedirectToAction("Index");
        }

        // ── JSON for billing/IPD details dropdowns ───────────────────────
        [HttpGet]
        public IActionResult GetAllJson()
        {
            try
            {
                int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subid = HttpContext.Session.GetInt32("SubHospitalId");
                var data = _repo.GetAll(hid, subid);
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllJson OperationMaster");
                return Json(new System.Collections.Generic.List<OperationMaster>());
            }
        }
    }
}