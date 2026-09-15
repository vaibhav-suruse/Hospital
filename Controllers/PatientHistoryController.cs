using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;
using Microsoft.Extensions.Logging;

namespace WebApplicationSampleTest2.Controllers
{
    public class PatientHistoryController : Controller
    {
        private readonly ILogger<PatientHistoryController> _logger;
        private readonly IPatientHistory _repo;

        public PatientHistoryController(IPatientHistory repo, ILogger<PatientHistoryController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        private int HospitalId => HttpContext.Session.GetInt32("MainHospitalId") ?? 0;

        [HttpGet]
        public JsonResult GetFullHistory(int patientId)
        {
            var vm = _repo.GetFullHistory(patientId, HospitalId);
            return Json(vm);
        }

        // ═══════════════════════════════════════════════════════════════
        //  VITALS
        // ═══════════════════════════════════════════════════════════════
        [HttpPost]
        public JsonResult SaveVitals([FromBody] PatientVitals model)

        {
            try
            {
                if (model == null)
                    return Json(new { success = false, message = "No data received." });

                model.HospitalId = HospitalId;
                int id = _repo.SaveVitals(model);
                return Json(new { success = true, id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveVitals");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  MEDICAL CONDITIONS  ← YOUR FIX HERE
        // ═══════════════════════════════════════════════════════════════
        [HttpPost]
        public JsonResult SaveCondition([FromBody] PatientMedicalCondition model)

        {
            try
            {
                if (model == null)
                    return Json(new { success = false, message = "No data received." });

                model.HospitalId = HospitalId;
                int id = _repo.SaveMedicalCondition(model);
                return Json(new { success = true, id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveCondition");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteCondition(int id)
        {
            try
            {
                _repo.DeleteMedicalCondition(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteCondition");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  ALLERGIES
        // ═══════════════════════════════════════════════════════════════
        [HttpPost]
        public JsonResult SaveAllergy([FromBody] PatientAllergy model)

        {
            try
            {
                if (model == null)
                    return Json(new { success = false, message = "No data received." });

                model.HospitalId = HospitalId;
                if (model.SinceValue.HasValue)
                {
                    model.SinceHours = model.SinceUnit switch
                    {
                        "Days" => model.SinceValue * 24,
                        "Months" => model.SinceValue * 24 * 30,
                        "Years" => model.SinceValue * 24 * 365,
                        _ => model.SinceValue
                    };
                }
                int id = _repo.SaveAllergy(model);
                return Json(new { success = true, id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveAllergy");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteAllergy(int id)
        {
            try
            {
                _repo.DeleteAllergy(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteAllergy");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  SURGICAL HISTORY
        // ═══════════════════════════════════════════════════════════════
        [HttpPost]
        public JsonResult SaveSurgery([FromBody] PatientSurgicalHistory model)

        {
            try
            {
                if (model == null)
                    return Json(new { success = false, message = "No data received." });

                model.HospitalId = HospitalId;
                int id = _repo.SaveSurgery(model);
                return Json(new { success = true, id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveSurgery");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteSurgery(int id)
        {
            try
            {
                _repo.DeleteSurgery(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteSurgery");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  FAMILY HISTORY
        // ═══════════════════════════════════════════════════════════════
        [HttpPost]
        public JsonResult SaveFamilyHistory([FromBody] PatientFamilyHistory model)
        {
            try
            {
                if (model == null)
                    return Json(new { success = false, message = "No data received." });

                model.HospitalId = HospitalId;
                int id = _repo.SaveFamilyHistory(model);
                return Json(new { success = true, id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveFamilyHistory");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteFamilyHistory(int id)
        {
            try
            {
                _repo.DeleteFamilyHistory(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteFamilyHistory");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  LAB RESULTS
        // ═══════════════════════════════════════════════════════════════
        [HttpPost]
        public JsonResult SaveLabResult(PatientLabResult model)
        {
            try
            {
                if (model == null)
                    return Json(new { success = false, message = "No data received." });

                model.HospitalId = HospitalId;

                var file = Request.Form.Files["LabReportFile"];
                if (file != null && file.Length > 0)
                {
                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/LabReports");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                    string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
                    file.CopyTo(stream);
                    model.FilePath = "/LabReports/" + fileName;
                }

                int id = _repo.SaveLabResult(model);
                return Json(new { success = true, id, filePath = model.FilePath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveLabResult");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteLabResult(int id)
        {
            try
            {
                _repo.DeleteLabResult(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteLabResult");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}


//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using System;
//using System.IO;
//using WebApplicationSampleTest2.Models;
//using WebApplicationSampleTest2.Repository;

//namespace WebApplicationSampleTest2.Controllers
//{
//    // ═══════════════════════════════════════════════════════════════════
//    //  PatientHistoryController
//    //  Add this as a NEW controller file: Controllers/PatientHistoryController.cs
//    //  Register IPatientHistory in Program.cs / Startup.cs:
//    //      builder.Services.AddScoped<IPatientHistory, PatientHistoryRepository>();
//    // ═══════════════════════════════════════════════════════════════════
//    public class PatientHistoryController : Controller
//    {
//        private readonly IPatientHistory _repo;

//        public PatientHistoryController(IPatientHistory repo)
//        {
//            _repo = repo;
//        }

//        // ── Helpers ──────────────────────────────────────────────────────
//        private int HospitalId => HttpContext.Session.GetInt32("MainHospitalId") ?? 0;

//        // ── Full History (all sections, single AJAX call from left panel) ─
//        [HttpGet]
//        public JsonResult GetFullHistory(int patientId)
//        {
//            var vm = _repo.GetFullHistory(patientId, HospitalId);
//            return Json(vm);
//        }

//        // ═══════════════════════════════════════════════════════════════
//        //  VITALS
//        // ═══════════════════════════════════════════════════════════════
//        [HttpPost]
//        public JsonResult SaveVitals(PatientVitals model)
//        {
//            try
//            {
//                model.HospitalId = HospitalId;
//                int id = _repo.SaveVitals(model);
//                return Json(new { success = true, id });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        // ═══════════════════════════════════════════════════════════════
//        //  MEDICAL CONDITIONS
//        // ═══════════════════════════════════════════════════════════════
//        [HttpPost]
//        public JsonResult SaveCondition(PatientMedicalCondition model)
//        {
//            try
//            {
//                model.HospitalId = HospitalId;
//                int id = _repo.SaveMedicalCondition(model);
//                return Json(new { success = true, id });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        [HttpPost]
//        public JsonResult DeleteCondition(int id)
//        {
//            try
//            {
//                _repo.DeleteMedicalCondition(id);
//                return Json(new { success = true });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        // ═══════════════════════════════════════════════════════════════
//        //  ALLERGIES
//        // ═══════════════════════════════════════════════════════════════
//        [HttpPost]
//        public JsonResult SaveAllergy(PatientAllergy model)
//        {
//            try
//            {
//                model.HospitalId = HospitalId;
//                int id = _repo.SaveAllergy(model);
//                return Json(new { success = true, id });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        [HttpPost]
//        public JsonResult DeleteAllergy(int id)
//        {
//            try
//            {
//                _repo.DeleteAllergy(id);
//                return Json(new { success = true });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        // ═══════════════════════════════════════════════════════════════
//        //  SURGICAL HISTORY
//        // ═══════════════════════════════════════════════════════════════
//        [HttpPost]
//        public JsonResult SaveSurgery(PatientSurgicalHistory model)
//        {
//            try
//            {
//                model.HospitalId = HospitalId;
//                int id = _repo.SaveSurgery(model);
//                return Json(new { success = true, id });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        [HttpPost]
//        public JsonResult DeleteSurgery(int id)
//        {
//            try
//            {
//                _repo.DeleteSurgery(id);
//                return Json(new { success = true });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        // ═══════════════════════════════════════════════════════════════
//        //  FAMILY HISTORY
//        // ═══════════════════════════════════════════════════════════════
//        [HttpPost]
//        public JsonResult SaveFamilyHistory(PatientFamilyHistory model)
//        {
//            try
//            {
//                model.HospitalId = HospitalId;
//                int id = _repo.SaveFamilyHistory(model);
//                return Json(new { success = true, id });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        [HttpPost]
//        public JsonResult DeleteFamilyHistory(int id)
//        {
//            try
//            {
//                _repo.DeleteFamilyHistory(id);
//                return Json(new { success = true });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        // ═══════════════════════════════════════════════════════════════
//        //  LAB RESULTS
//        // ═══════════════════════════════════════════════════════════════
//        [HttpPost]
//        public JsonResult SaveLabResult(PatientLabResult model)
//        {
//            try
//            {
//                model.HospitalId = HospitalId;

//                // Handle file upload
//                var file = Request.Form.Files["LabReportFile"];
//                if (file != null && file.Length > 0)
//                {
//                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/LabReports");
//                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
//                    string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
//                    using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
//                    file.CopyTo(stream);
//                    model.FilePath = "/LabReports/" + fileName;
//                }

//                int id = _repo.SaveLabResult(model);
//                return Json(new { success = true, id, filePath = model.FilePath });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }

//        [HttpPost]
//        public JsonResult DeleteLabResult(int id)
//        {
//            try
//            {
//                _repo.DeleteLabResult(id);
//                return Json(new { success = true });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }
//    }
//}
