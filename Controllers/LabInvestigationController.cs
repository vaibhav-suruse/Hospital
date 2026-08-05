// Controllers/LabInvestigationController.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class LabInvestigationController : Controller
    {
        private readonly ILabInvestigation _labRepo;
        private readonly IWebHostEnvironment _env;

        public LabInvestigationController(
            ILabInvestigation labRepo,
            IWebHostEnvironment env)
        {
            _labRepo = labRepo;
            _env = env;
        }

        // ===============================
        // INDEX - All Investigations
        // ===============================






        public IActionResult Index(string status = "", string priority = "", int page = 1)
        {
            try
            {
                int pageSize = 5;

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var data = _labRepo.GetAllInvestigations(
                    hospitalId,
                    subHospitalId,
                    status,
                    priority
                ).AsQueryable();

                // 🔹 Pagination logic
                int totalRecords = data.Count();
                int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                var pagedData = data
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                ViewBag.CurrentStatus = status;
                ViewBag.CurrentPriority = priority;

                // Counts (UNCHANGED)
                var all = _labRepo.GetAllInvestigations(hospitalId, subHospitalId, null, null);
                ViewBag.CountAll = all.Count;
                ViewBag.CountOrdered = all.FindAll(x => x.Status == "Ordered").Count;
                ViewBag.CountCollected = all.FindAll(x => x.Status == "Collected").Count;
                ViewBag.CountCompleted = all.FindAll(x => x.Status == "Completed").Count;
                ViewBag.CountEmergency = all.FindAll(x => x.Priority == "Emergency").Count;

                return View(pagedData);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;

                return View(new System.Collections.Generic.List<LabInvestigationModel>());
            }
        }






        //public IActionResult Index(string status = "", string priority = "")
        //{
        //    try
        //    {
        //        int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //        int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

        //        var list = _labRepo.GetAllInvestigations(
        //            hospitalId,
        //            subHospitalId,
        //            status,
        //            priority);

        //        ViewBag.CurrentStatus = status;
        //        ViewBag.CurrentPriority = priority;

        //        // Counts for tab badges
        //        var all = _labRepo.GetAllInvestigations(hospitalId, subHospitalId, null, null);
        //        ViewBag.CountAll = all.Count;
        //        ViewBag.CountOrdered = all.FindAll(x => x.Status == "Ordered").Count;
        //        ViewBag.CountCollected = all.FindAll(x => x.Status == "Collected").Count;
        //        ViewBag.CountCompleted = all.FindAll(x => x.Status == "Completed").Count;
        //        ViewBag.CountEmergency = all.FindAll(x => x.Priority == "Emergency").Count;

        //        return View(list);
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["Error"] = ex.Message;
        //        return View(new System.Collections.Generic.List<LabInvestigationModel>());
        //    }
        //}

        // ===============================
        // UPDATE (GET)
        // ===============================
        [HttpGet]
        public IActionResult Update(int id)
        {
            try
            {
                var model = _labRepo.GetInvestigationById(id);

                if (model == null)
                    return NotFound();

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        // ===============================
        // UPDATE (POST)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(LabInvestigationModel model)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            try
            {
                string filePath = null;

                // ===== FILE UPLOAD =====
                if (model.ReportFile != null && model.ReportFile.Length > 0)
                {
                    // Validate file type
                    var ext = Path.GetExtension(model.ReportFile.FileName).ToLower();
                    if (ext != ".pdf" && ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                        throw new Exception("Only PDF, JPG, JPEG, PNG files are allowed.");

                    // Validate file size (max 5MB)
                    if (model.ReportFile.Length > 5 * 1024 * 1024)
                        throw new Exception("File size must be less than 5MB.");

                    // Create folder if not exists
                    var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "investigations");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    // Generate unique filename
                    var fileName = $"investigation_{DateTime.Now:yyyyMMddHHmmssfff}{ext}";
                    var fullPath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        model.ReportFile.CopyTo(stream);
                    }

                    filePath = $"/uploads/investigations/{fileName}";
                }

                // ===== UPDATE IN DB =====
                _labRepo.UpdateInvestigation(
                    model.Id,
                    model.NewStatus,
                    model.Result,
                    filePath,
                    userId,
                    model.IsAbnormal);

                TempData["Success"] = "Investigation updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }
    }
}