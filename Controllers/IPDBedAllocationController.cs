using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class IPDBedAllocationController : Controller
    {
        private readonly IIPDBedAllocation _repository;
        private readonly ILogger<IPDBedAllocationController> _logger;
        private readonly IWard _IWard;
        private readonly IRoom _IRoom;
        private readonly IBed _IBed;
        public IPDBedAllocationController(IWard ward, IBed bed, IRoom room,
            IIPDBedAllocation repository,
            ILogger<IPDBedAllocationController> logger)
        {
            _repository = repository;
            _logger = logger;
            _IWard = ward;
            _IBed = bed;
            _IRoom = room;
        }

        // ✅ LIST ALL
        public IActionResult Index()
        {
            try
            {
                var data = _repository.GetAll();
                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Index method of IPDBedAllocationController");
                return View("Error");
            }
        }
        [HttpGet]
        public IActionResult Create(int ipdId)
        {
            if (ipdId <= 0)
                return BadRequest("IPDId is required"); // safeguard

            var model = new IPDBedAllocationModel
            {
                IPDId = ipdId
            };
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            if (subHospitalId == 0)
                subHospitalId = null;

            var wards = _IWard.GetAllWards(hospitalId, subHospitalId);
            var rooms = _IRoom.GetAllRoom(hospitalId, subHospitalId);
            var beds = _IBed.GetAllBeds(hospitalId, subHospitalId);

            var bedsByRoom = beds
     .GroupBy(b => b.RoomId)
     .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.Wards = wards;
            ViewBag.Rooms = rooms;
            ViewBag.BedsByRoom = bedsByRoom;



            return View(model);
        }

        // ✅ POST: Allocate / Transfer BedSSS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(IPDBedAllocationModel model)
        {
            try
            {
                int allocatedBy = HttpContext.Session.GetInt32("UserId") ?? 0;

                if (ModelState.IsValid && model.BedId > 0 && allocatedBy > 0)
                {
                    _repository.AllocateOrTransferBed(
                        model.IPDId,
                        model.BedId,
                        allocatedBy
                    );
                    //var oldBedId = _repository.GetCurrentBedIdByIPD(model.IPDId); // Method to fetch old bed
                    //if (oldBedId.HasValue)
                    //{
                    //    _repository.UpdateBedStatus(oldBedId.Value, "Active"); // Free old bed
                    //}

                    _repository.UpdateBedStatus(model.BedId, "Occupied");
                    TempData["Success"] = "Bed allocated / transferred successfully!";
                    return RedirectToAction("Index", "IPDAdmission");
                }


                ModelState.AddModelError("", "Please select a bed or ensure you are logged in.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Allocate POST");
                ModelState.AddModelError("", "Something went wrong!");
                return View(model);
            }
        }

        // ✅ GET: Edit
        public IActionResult Edit(int id)
        {
            try
            {
                var data = _repository.GetById(id);
                if (data == null)
                    return NotFound();

                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Edit GET");
                return View("Error");
            }
        }

        // ✅ POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(IPDBedAllocationModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _repository.UpdateBedAllocation(model);
                    TempData["Success"] = "Bed allocation updated successfully!";
                    return RedirectToAction("Index");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Edit POST");
                ModelState.AddModelError("", "Update failed!");
                return View(model);
            }
        }

        // ✅ Delete
        public IActionResult Delete(int id)
        {
            try
            {
                _repository.DeleteBedAllocation(id);
                TempData["Success"] = "Bed allocation deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Delete");
                return View("Error");
            }
        }

        [HttpGet]
        public JsonResult GetWards()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var wards = _IWard.GetAllWards(hospitalId, subHospitalId)
                               .Select(w => new
                               {
                                   w.WardId,
                                   w.WardName
                               }).ToList();

            return Json(wards);
        }
        [HttpGet]
        public JsonResult GetRoomsByWard(int wardId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var rooms = _IRoom.GetAllRoom(hospitalId, subHospitalId)
                              .Where(r => r.WardId == wardId)
                              .Select(r => new
                              {
                                  r.RoomId,
                                  r.RoomNumber,
                                  r.WardId
                              }).ToList();

            return Json(rooms);
        }

        [HttpGet]
        public JsonResult GetBedsByRoom(int roomId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var beds = _IBed.GetAllBeds(hospitalId, subHospitalId)
                            .Where(b => b.RoomId == roomId)
                            .Select(b => new
                            {
                                b.BedId,
                                b.BedNumber,
                                b.OperationalStatus
                            }).ToList();

            return Json(beds);
        }





        [HttpGet]
        public IActionResult ShiftBed(int ipdId, string returnUrl = null)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
                if (subHospitalId == 0) subHospitalId = null;

                var model = _repository.GetCurrentBedInfo(ipdId, hospitalId, subHospitalId);
                if (model == null) return NotFound();

                if (model.Status == "Discharged")
                {
                    TempData["Error"] = "This patient has already been discharged - bed cannot be shifted.";
                    return RedirectToAction("Details", "IPDAdmission", new { id = ipdId });
                }

                model.ReturnUrl = returnUrl;
                ViewBag.Wards = _IWard.GetAllWards(hospitalId, subHospitalId);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShiftBed GET");
                TempData["Error"] = "Error loading shift bed page.";
                return RedirectToAction("Details", "IPDAdmission", new { id = ipdId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ShiftBed(BedShiftVM model)
        {
            try
            {
                if (model.NewBedId <= 0)
                {
                    TempData["Error"] = "Please select a bed.";
                    return RedirectToAction("ShiftBed", new { ipdId = model.IPDId, returnUrl = model.ReturnUrl });
                }

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
                if (subHospitalId == 0) subHospitalId = null;

                int allocatedBy = HttpContext.Session.GetInt32("UserId") ?? 0;
                _repository.ShiftBed(model.IPDId, model.NewBedId, allocatedBy, model.Reason, hospitalId, subHospitalId);

                TempData["Success"] = "Bed shifted successfully!";
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Details", "IPDAdmission", new { id = model.IPDId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShiftBed POST");
                TempData["Error"] = "Error shifting bed: " + ex.Message;
                return RedirectToAction("ShiftBed", new { ipdId = model.IPDId, returnUrl = model.ReturnUrl });
            }
        }






    }
}
