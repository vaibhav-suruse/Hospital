using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class RoomController : Controller
    {
        private readonly IRoom _roomService;
        private readonly IWard _wardService;

        public RoomController(IRoom roomService, IWard wardService)
        {
            _roomService = roomService;
            _wardService = wardService;
        }



        //public IActionResult Index(int wardId = 0)
        //{
        //    try
        //    {
        //        int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //        int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

        //        // Populate ward dropdown
        //        var wards = _wardService.GetAllWards(hospitalId, subHospitalId) ?? new List<Ward>();
        //        ViewBag.Wards = new SelectList(wards, "WardId", "WardName", wardId);
        //        ViewBag.WardId = wardId; // track selected ward

        //        // Get rooms for the selected ward, or empty list if none selected
        //        var rooms = wardId > 0
        //            ? _roomService.GetAllRooms(hospitalId, subHospitalId, wardId)
        //            : new List<Room>();

        //        return View(rooms);
        //    }
        //    catch (Exception ex)
        //    {
        //        ViewBag.Error = "Error fetching rooms: " + ex.Message;
        //        return View(new List<Room>());
        //    }
        //}





        public IActionResult Index(int wardId = 0)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var wards = _wardService.GetAllWards(hospitalId, subHospitalId) ?? new List<Ward>();
                ViewBag.Wards = new SelectList(wards, "WardId", "WardName", wardId);
                ViewBag.WardId = wardId;

                var rooms = _roomService.GetRoomsWithBedCount(hospitalId, subHospitalId, wardId);

                return View(rooms);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error fetching rooms: " + ex.Message;
                return View(new List<RoomListVM>());
            }
        }











        // GET: Create
        public IActionResult Create(int wardId = 0)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var wards = _wardService.GetAllWards(hospitalId, subHospitalId) ?? new List<Ward>();
                if (wards.Count == 0)
                {
                    ViewBag.Error = "No wards available. Please create a ward first.";
                    return RedirectToAction("Index", "Ward");
                }

                if (!wards.Exists(w => w.WardId == wardId))
                    wardId = wards[0].WardId;

                PopulateDropdowns(wardId);
                return View(new Room { WardId = wardId });
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading create room page: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Create
        [HttpPost]
        public IActionResult Create(Room model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.HospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                    model.SubHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                    // Check if room number already exists in this ward/hospital/subhospital
                    var existingRoom = _roomService.GetRoomByNumber(model.RoomNumber, model.WardId, model.HospitalId, model.SubHospitalId);
                    if (existingRoom != null)
                    {
                        ModelState.AddModelError("RoomNumber", "This room number already exists in the selected ward.");
                        PopulateDropdowns(model.WardId);
                        return View(model);
                    }

                    _roomService.CreateRoom(model);
                    return RedirectToAction("Index", new { wardId = model.WardId });
                }

                PopulateDropdowns(model.WardId);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error creating room: " + ex.Message;
                PopulateDropdowns(model.WardId);
                return View(model);
            }
        }


        // GET: Edit
        public IActionResult Edit(int id)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var room = _roomService.GetRoomById(id, hospitalId, subHospitalId);
                if (room == null) return RedirectToAction("Index");




                PopulateDropdowns(room.WardId);
                return View(room);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading edit page: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Edit
        [HttpPost]
        public IActionResult Edit(Room model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    int HospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                    int? SubHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                    var existingRoom = _roomService.GetRoomById(model.RoomId, HospitalId, SubHospitalId);
                    if (existingRoom == null)
                        return NotFound();

                    // ✅ Force original WardId (prevents modification)
                    model.WardId = existingRoom.WardId;

                    model.HospitalId = HospitalId;
                    model.SubHospitalId = SubHospitalId;

                    // ✅ ADD: Duplicate check (exclude current room)
                    var duplicateRoom = _roomService.GetRoomByNumber(
                        model.RoomNumber,
                        model.WardId,
                        model.HospitalId,
                        model.SubHospitalId
                    );

                    if (duplicateRoom != null && duplicateRoom.RoomId != model.RoomId)
                    {
                        ModelState.AddModelError("RoomNumber",
                            "This room number already exists in the selected ward.");

                        PopulateDropdowns(model.WardId);
                        return View(model);
                    }

                    if (Request.Form["IsActive"].Count > 0)
                        model.IsActive = Request.Form["IsActive"] == "true";

                    _roomService.UpdateRoom(model);

                    return RedirectToAction("Index", new { wardId = model.WardId });
                }

                PopulateDropdowns(model.WardId);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error updating room: " + ex.Message;
                PopulateDropdowns(model.WardId);
                return View(model);
            }
        }


        // GET: Delete
        public IActionResult Delete(int id, int wardId)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                _roomService.DeleteRoom(id, hospitalId, subHospitalId);
                return RedirectToAction("Index", new { wardId = wardId });
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error deleting room: " + ex.Message;
                return RedirectToAction("Index", new { wardId = wardId });
            }
        }

        // Populate dropdowns for Ward selection and RoomType
        private void PopulateDropdowns(int selectedWardId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var wards = _wardService.GetAllWards(hospitalId, subHospitalId) ?? new List<Ward>();

            // Safety: if no wards exist, create empty SelectList to prevent nulls
            ViewBag.Wards = wards.Count > 0
                ? new SelectList(wards, "WardId", "WardName", selectedWardId)
                : new SelectList(new List<Ward>(), "WardId", "WardName");

            // RoomType dropdown
            ViewBag.RoomTypes = new List<SelectListItem>
            {
                new SelectListItem { Text = "General", Value = "General" },
                new SelectListItem { Text = "ICU", Value = "ICU" },
                new SelectListItem { Text = "Private", Value = "Private" },
                new SelectListItem { Text = "SemiPrivate", Value = "SemiPrivate" }
            };
        }

    }
}
