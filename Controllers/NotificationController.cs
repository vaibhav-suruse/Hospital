using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class NotificationController : Controller
    {
        private readonly INotification _notifRepo;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotification notifRepo, ILogger<NotificationController> logger)
        {
            _notifRepo = notifRepo;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────
        // GET /Notification/GetAll
        // Returns all notifications (last 50) for the panel list
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetAll()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var list = _notifRepo.GetAllNotifications(hospitalId, subHospitalId);
            return Json(list);
        }

        // ─────────────────────────────────────────────────────────
        // GET /Notification/GetPendingCount
        // Lightweight — polled every 8s just for the badge number
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetPendingCount()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            int count = _notifRepo.GetPendingCount(hospitalId, subHospitalId);
            return Json(new { count });
        }

        // ─────────────────────────────────────────────────────────
        // GET /Notification/GetNewSince?since=2024-04-05T10:30:00
        // Returns only notifications newer than given timestamp
        // Used for toast popups — avoids re-showing old ones
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetNewSince(string since)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            DateTime sinceDate = DateTime.TryParse(since, out var parsed)
                ? parsed
                : DateTime.Now.AddSeconds(-10);

            var list = _notifRepo.GetNewNotificationsSince(hospitalId, subHospitalId, sinceDate);
            return Json(list);
        }

        // ─────────────────────────────────────────────────────────
        // GET /Notification/MarkDispensed?id=5
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult MarkDispensed(int id)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;

            try
            {
                _notifRepo.MarkDispensed(id, hospitalId);
                return Json(new { success = true, message = "Marked as dispensed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking dispensed. Id: {Id}", id);
                return Json(new { success = false, message = "Error updating status" });
            }
        }

        // ─────────────────────────────────────────────────────────
        // GET /Notification/MarkCancelled?id=5
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult MarkCancelled(int id)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;

            try
            {
                _notifRepo.MarkCancelled(id, hospitalId);
                return Json(new { success = true, message = "Marked as cancelled" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking cancelled. Id: {Id}", id);
                return Json(new { success = false, message = "Error updating status" });
            }
        }
    }
}
