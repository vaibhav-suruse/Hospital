using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class BedController : Controller
    {
        private readonly IBed _bedService;
        private readonly IWard _wardService;
        private readonly IRoom _roomService;

        public BedController(IBed bedService, IWard wardService, IRoom roomService)
        {
            _bedService = bedService;
            _wardService = wardService;
            _roomService = roomService;
        }

        // ================= INDEX =================
        public IActionResult Index(int wardId = 0, int roomId = 0)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                ViewBag.SelectedWardId = wardId;
                ViewBag.SelectedRoomId = roomId;

                var beds = _bedService.GetAllBeds(hospitalId, subHospitalId, wardId, roomId) ?? new List<Bed>();

                var wards = _wardService.GetAllWards(hospitalId, subHospitalId) ?? new List<Ward>();
                ViewBag.Wards = new SelectList(wards, "WardId", "WardName", wardId);

                var rooms = (wardId > 0)
                    ? _roomService.GetAllRooms(hospitalId, subHospitalId, wardId)
                    : new List<Room>();

                ViewBag.Rooms = new SelectList(rooms, "RoomId", "RoomNumber", roomId);

                return View(beds);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading beds: " + ex.Message;
                return View(new List<Bed>());
            }
        }

        // ================= CREATE GET =================
        public IActionResult Create()
        {
            try
            {
                PopulateDropdowns();
                return View(new Bed());
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading create bed page: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // ================= CREATE POST =================
        [HttpPost]
        public IActionResult Create(Bed model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.HospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                    model.SubHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                    var existingBed = _bedService.GetActiveBedByNumber(
                        model.BedNumber,
                        model.WardId,
                        model.RoomId,
                        model.HospitalId,
                        model.SubHospitalId
                    );

                    if (existingBed != null)
                    {
                        ModelState.AddModelError(nameof(model.BedNumber), "This bed number already exists in this room.");
                        PopulateDropdowns(model.WardId, model.RoomId);
                        return View(model);
                    }

                    _bedService.CreateBed(model);

                    TempData["ToastMessage"] = "Bed Added successfully.";
                    TempData["ToastType"] = "success";

                    return RedirectToAction("Index", new { wardId = model.WardId, roomId = model.RoomId });
                }

                PopulateDropdowns(model.WardId, model.RoomId);
                return View(model);
            }
            catch
            {
                ModelState.AddModelError("", "Could not create bed. Please try again.");
                PopulateDropdowns(model.WardId, model.RoomId);
                return View(model);
            }
        }

        // ================= EDIT GET =================
        public IActionResult Edit(int id)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var bed = _bedService.GetBedById(id, hospitalId, subHospitalId);
                if (bed == null) return RedirectToAction("Index");

                PopulateDropdowns(bed.WardId, bed.RoomId);

                return View(bed);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading edit bed page: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // ================= EDIT POST =================
        [HttpPost]
        public IActionResult Edit(Bed model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.HospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                    model.SubHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                    var existingBed = _bedService.GetActiveBedByNumber(
                        model.BedNumber,
                        model.WardId,
                        model.RoomId,
                        model.HospitalId,
                        model.SubHospitalId,
                        model.BedId
                    );

                    if (existingBed != null)
                    {
                        ModelState.AddModelError(nameof(model.BedNumber), "This bed number already exists in this room.");
                        PopulateDropdowns(model.WardId, model.RoomId);
                        return View(model);
                    }

                    _bedService.UpdateBed(model);
                    TempData["ToastMessage"] = "Bed Updated successfully.";
                    TempData["ToastType"] = "success";
                    return RedirectToAction("Index", new { wardId = model.WardId, roomId = model.RoomId });
                }

                PopulateDropdowns(model.WardId, model.RoomId);
                return View(model);
            }
            catch
            {
                ModelState.AddModelError("", "Could not update bed. Please try again.");
                PopulateDropdowns(model.WardId, model.RoomId);
                return View(model);
            }
        }

        // ================= DELETE =================
        public IActionResult Delete(int id, int wardId, int roomId)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                _bedService.DeleteBed(id, hospitalId, subHospitalId);
                TempData["ToastMessage"] = "Bed deleted successfully.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index", new { wardId = wardId, roomId = roomId });
            }
            catch (Exception ex)
            {
                //ViewBag.Error = "Error deleting bed: " + ex.Message;

                TempData["ToastMessage"] = "Delete failed.";
                TempData["ToastType"] = "warning";

                return RedirectToAction("Index", new { wardId = wardId, roomId = roomId });
            }
        }

        // ================= POPULATE DROPDOWNS =================
        private void PopulateDropdowns(int selectedWardId = 0, int selectedRoomId = 0)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var wards = _wardService.GetAllWards(hospitalId, subHospitalId) ?? new List<Ward>();
            ViewBag.Wards = new SelectList(wards, "WardId", "WardName", selectedWardId);

            var rooms = (selectedWardId > 0)
                ? _roomService.GetAllRooms(hospitalId, subHospitalId, selectedWardId)
                : new List<Room>();

            ViewBag.Rooms = new SelectList(rooms, "RoomId", "RoomNumber", selectedRoomId);

            ViewBag.StatusList = new List<string>
            {
                "Active",
                "Maintenance",
                "Cleaning",
                "Blocked"
            };
        }

        // ================= AJAX ROOM LOADER =================
        public IActionResult GetRoomsByWard(int wardId)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var rooms = _roomService.GetRoomsByWardId(wardId, hospitalId, subHospitalId);

                var result = rooms.Select(r => new
                {
                    roomId = r.RoomId,
                    roomNumber = r.RoomNumber
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

    }
}
