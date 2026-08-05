using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class InpatientDocumentController : Controller
    {

private readonly IInpatientDocument _repository;
            private readonly IWebHostEnvironment _environment;
            private readonly IIPDAdmission _ipdRepo; // NEW — hospital-scoped IPD admission check

            public InpatientDocumentController(
                IInpatientDocument repository,
                IIPDAdmission ipdRepo,
                IWebHostEnvironment environment)
            {
                _repository = repository;
                _ipdRepo = ipdRepo;
                _environment = environment;
            }

            // ── Multi-hospital IPD data isolation helper ──────────────
            // The documents attached to an IPD admission must only be visible
            // to users whose session hospital/sub-hospital owns that admission.
            private bool IsIpdAuthorized(int ipdId)
            {
                if (ipdId <= 0) return false;
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
                return _ipdRepo.GetIPDAdmissionById(ipdId, hospitalId, subHospitalId) != null;
            }

            [HttpGet]
            public IActionResult Index(int ipId, int patientId)
            {
                // NEW — a user typing another hospital's ipdId directly into the
                // URL gets "Not Found" instead of that hospital's documents.
                if (!IsIpdAuthorized(ipId))
                    return NotFound();

                OtherDocumentsVM model = new OtherDocumentsVM
                {
                    IpdId = ipId,
                    PatientId = patientId,
                    Documents = _repository.GetDocuments(ipId)
                };

                return View(model);
            }

            [HttpPost]
            public IActionResult Upload(InpatientDocumentModel model)
            {
                try
                {
                    // NEW — reject uploads to an IPD that belongs to another hospital.
                    if (!IsIpdAuthorized(model.IP_ID))
                        return NotFound();

                    if (model.DocumentFile == null || model.DocumentFile.Length == 0)
                    {
                        TempData["Error"] = "Please select a file.";
                        return RedirectToAction("Index", new
                        {
                            ipId = model.IP_ID,
                            patientId = model.PatientId
                        });
                    }

                    string folderPath = Path.Combine(_environment.WebRootPath, "Uploads", "InpatientDocuments");

                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string newFileName = Guid.NewGuid() + Path.GetExtension(model.DocumentFile.FileName);

                    string fullPath = Path.Combine(folderPath, newFileName);

                    using (FileStream fs = new FileStream(fullPath, FileMode.Create))
                    {
                        model.DocumentFile.CopyTo(fs);
                    }

                    model.FileName = model.DocumentFile.FileName;
                    model.FilePath = "/Uploads/InpatientDocuments/" + newFileName;
                    model.FileType = model.DocumentFile.ContentType;
                    model.FileSize = model.DocumentFile.Length;
                    model.UploadedBy = 1;

                    _repository.InsertDocument(model);

                    TempData["Success"] = "Document Uploaded Successfully.";

                    return RedirectToAction("Index", new
                    {
                        ipId = model.IP_ID,
                        patientId = model.PatientId
                    });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;

                    return RedirectToAction("Index", new
                    {
                        ipId = model.IP_ID,
                        patientId = model.PatientId
                    });
                }
            }

            [HttpGet]
            public IActionResult Download(int id)
            {
                var document = _repository.GetDocumentById(id);

                if (document == null)
                    return NotFound();

                string filePath = Path.Combine(
                    _environment.WebRootPath,
                    document.FilePath.TrimStart('/').Replace("/", "\\"));

                if (!System.IO.File.Exists(filePath))
                    return NotFound();

                byte[] bytes = System.IO.File.ReadAllBytes(filePath);

                return File(bytes, document.FileType, document.FileName);
            }

            [HttpPost]
            public IActionResult Delete(int id, int ipId, int patientId)
            {
                try
                {
                    _repository.DeleteDocument(id);

                    TempData["Success"] = "Document Deleted Successfully.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                }

                return RedirectToAction("Index", new
                {
                    ipId = ipId,
                    patientId = patientId
                });
            }
        }
}
