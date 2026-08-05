using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class DischargePlanningController : Controller
    {
        private readonly IDischargePlanning _planningRepo;
        private readonly IIPDAdmission _admissionRepo;
        private readonly IIPDBilling _billingRepo;

        public DischargePlanningController(
            IDischargePlanning planningRepo,
            IIPDAdmission admissionRepo,
            IIPDBilling billingRepo)
        {
            _planningRepo = planningRepo;
            _admissionRepo = admissionRepo;
            _billingRepo = billingRepo;
        }

        private int HospitalId => HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        private int? SubHospitalId => HttpContext.Session.GetInt32("SubHospitalId");
        private int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 1;

        [HttpGet]
        public IActionResult Index(int ipdId)
        {
            var admission = _admissionRepo.GetIPDAdmissionById(ipdId, HospitalId, SubHospitalId);
            if (admission == null) return NotFound();

            var model = _planningRepo.GetByIPD(ipdId, HospitalId, SubHospitalId)
                ?? new DischargePlanningModel { IPDId = ipdId };

            model.PatientName = admission.PatientName;
            model.AdmissionNumber = admission.AdmissionNumber;
            model.AdmissionDateTime = admission.AdmissionDateTime;
            model.Status = admission.Status;

            // Live billing snapshot - never stored, always read fresh so it
            // can't go stale relative to the actual billing module.
            try
            {
                var billing = _billingRepo.GetPatientBillingSummary(ipdId);
                model.BillingDueAmount = billing?.DueAmount;
            }
            catch
            {
                model.BillingDueAmount = null; // no bill created yet - not an error
            }

            ComputeProgress(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(DischargePlanningModel model)
        {
            try
            {
                var admission = _admissionRepo.GetIPDAdmissionById(model.IPDId, HospitalId, SubHospitalId);
                if (admission == null) return Json(new { success = false, message = "Admission not found." });

                _planningRepo.SavePlanning(model, CurrentUserId, HospitalId, SubHospitalId);

                var saved = _planningRepo.GetByIPD(model.IPDId, HospitalId, SubHospitalId);
                ComputeProgress(saved);

                return Json(new
                {
                    success = true,
                    message = "Discharge plan saved.",
                    progressPercent = saved.ProgressPercent,
                    completed = saved.ChecklistCompleted,
                    total = saved.ChecklistTotal,
                    updatedByName = saved.UpdatedByName,
                    updatedDate = saved.UpdatedDate?.ToString("dd MMM yyyy, hh:mm tt")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private void ComputeProgress(DischargePlanningModel model)
        {
            int completed = 0;
            if (model.EstimatedDischargeDate.HasValue) completed++;
            if (!string.IsNullOrWhiteSpace(model.Disposition)) completed++;
            if (model.EquipmentArranged) completed++;
            if (model.ReferralsArranged) completed++;
            if (model.PatientEducationStatus == "Completed") completed++;
            if (model.FollowUpBooked) completed++;
            if (model.PendingReportsCollected) completed++;

            model.ChecklistCompleted = completed;
            model.ChecklistTotal = 7;
        }
    }
}
