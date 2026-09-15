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
    public class AdvancedPharmacyController : Controller
    {
        private readonly IAdvancedPharmacy _pharmacy;
        private readonly IInventory _inventory;
        private readonly ISupplier _supplier;
        private readonly ILogger<AdvancedPharmacyController> _logger;

        public AdvancedPharmacyController(
            IAdvancedPharmacy pharmacy,
            IInventory inventory,
            ISupplier supplier,
            ILogger<AdvancedPharmacyController> logger)
        {
            _pharmacy = pharmacy;
            _inventory = inventory;
            _supplier = supplier;
            _logger = logger;
        }

        // =================================================================
        // STOCK ADJUSTMENT
        // =================================================================
        [HttpGet]
        public IActionResult StockAdjustment()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            ViewBag.Inventory = _inventory.GetAllInventory(hospitalId, subHospitalId);
            ViewBag.Log = _pharmacy.GetStockAdjustmentLog(hospitalId, subHospitalId);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StockAdjustment(StockAdjustment model)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            try
            {
                if (model.MedicineId <= 0 || model.QuantityChange == 0)
                {
                    TempData["Error"] = "Select a medicine and enter a non-zero quantity change.";
                    return RedirectToAction("StockAdjustment");
                }
                _pharmacy.AdjustStock(model, hospitalId, subHospitalId, userId);
                TempData["Success"] = "Stock adjusted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StockAdjustment failed");
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("StockAdjustment");
        }

        // =================================================================
        // RETURNS / REFUNDS
        // =================================================================
        [HttpGet]
        public IActionResult Returns()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            ViewBag.Returns = _pharmacy.GetReturns(hospitalId, subHospitalId);
            ViewBag.Inventory = _inventory.GetAllInventory(hospitalId, subHospitalId);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Returns(PharmacyReturn model)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            try
            {
                if (model.Items == null || !model.Items.Any(i => i.Quantity > 0))
                {
                    TempData["Error"] = "Add at least one return item with quantity.";
                    return RedirectToAction("Returns");
                }
                model.TotalRefund = model.Items.Sum(i => i.RefundAmount);
                var rn = _pharmacy.CreateReturn(model, hospitalId, subHospitalId, userId);
                TempData["Success"] = $"Return {rn} processed. Stock restocked.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateReturn failed");
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Returns");
        }

        // =================================================================
        // NARCOTICS / CONTROLLED SUBSTANCE REGISTER
        // =================================================================
        [HttpGet]
        public IActionResult NarcoticsRegister()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            ViewBag.Register = _pharmacy.GetNarcoticsRegister(hospitalId, subHospitalId);
            // Only controlled substances should appear here
            ViewBag.Inventory = _inventory.GetAllInventory(hospitalId, subHospitalId);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NarcoticsRegister(NarcoticsRegisterEntry model, int authorizedBy)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            int dispensedBy = HttpContext.Session.GetInt32("UserId") ?? 0;

            try
            {
                if (dispensedBy == authorizedBy)
                {
                    TempData["Error"] = "Dispenser and Authorizer must be different users (dual signatory).";
                    return RedirectToAction("NarcoticsRegister");
                }
                if (model.MedicineId <= 0 || model.QuantityOut <= 0)
                {
                    TempData["Error"] = "Select a medicine and enter quantity out.";
                    return RedirectToAction("NarcoticsRegister");
                }
                _pharmacy.AddNarcoticsEntry(model, hospitalId, subHospitalId, dispensedBy, authorizedBy);
                TempData["Success"] = "Narcotics register entry added.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddNarcoticsEntry failed");
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("NarcoticsRegister");
        }

        // =================================================================
        // DRUG INTERACTIONS
        // =================================================================
        [HttpGet]
        public IActionResult DrugInteractions()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            ViewBag.Interactions = _pharmacy.GetDrugInteractions(hospitalId);
            ViewBag.Medicines = _inventory.GetAllInventory(hospitalId, subHospitalId);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DrugInteractions(DrugInteraction model)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            try
            {
                if (model.MedicineA <= 0 || model.MedicineB <= 0 || model.MedicineA == model.MedicineB)
                {
                    TempData["Error"] = "Select two different medicines.";
                    return RedirectToAction("DrugInteractions");
                }
                _pharmacy.AddDrugInteraction(model, hospitalId);
                TempData["Success"] = "Drug interaction added.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddDrugInteraction failed");
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("DrugInteractions");
        }

        // =================================================================
        // PATIENT ALLERGIES
        // =================================================================
        [HttpGet]
        public IActionResult PatientAllergies(int patientId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            ViewBag.Allergies = _pharmacy.GetPatientAllergies(patientId, hospitalId);
            ViewBag.PatientId = patientId;
            ViewBag.Medicines = _inventory.GetAllInventory(hospitalId, subHospitalId);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PatientAllergies(PatientAllergyRecord model)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            try
            {
                if (model.PatientId <= 0 || string.IsNullOrEmpty(model.AllergyName))
                {
                    TempData["Error"] = "Patient and allergy name are required.";
                    return RedirectToAction("PatientAllergies", new { patientId = model.PatientId });
                }
                _pharmacy.AddPatientAllergy(model, hospitalId);
                TempData["Success"] = "Allergy recorded.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddPatientAllergy failed");
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("PatientAllergies", new { patientId = model.PatientId });
        }
    }
}
