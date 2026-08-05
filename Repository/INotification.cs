using System;
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface INotification
    {
        void InsertNotification(MedicineNotificationModel model);
        List<MedicineNotificationModel> GetAllNotifications(int hospitalId, int? subHospitalId);
        List<MedicineNotificationModel> GetNewNotificationsSince(int hospitalId, int? subHospitalId, DateTime since);
        int GetPendingCount(int hospitalId, int? subHospitalId);
        void MarkDispensed(int notificationId, int hospitalId);
        void MarkCancelled(int notificationId, int hospitalId);
    }
}
