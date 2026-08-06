﻿using System;
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IOPDAppointment
    {
        void CreateAppointment(OPDAppointmentModel appointment, int hospitalId, int? subHospitalId);
        int UpdateAppointment(OPDAppointmentModel appointment, int hospitalId, int? subHospitalId);
        void DeleteAppointment(int appointmentId, int hospitalId, int? subHospitalId);
        OPDAppointmentModel? GetAppointmentById(int appointmentId, int hospitalId, int? subHospitalId);
List<OPDAppointmentModel> GetAllAppointments(int hospitalId, int? subHospitalId);

        /// <summary>
        /// Fetch a single page of appointments (JOINed with patient + doctor) in SQL.
        /// Filters by date + search and applies LIMIT/OFFSET so the whole table is
        /// never loaded into memory. Returns today's count via out param.
        /// </summary>
        List<OPDAppointmentModel> GetAppointmentsPaged(
            int hospitalId, int? subHospitalId, DateTime date, string search,
            int page, int pageSize, out int totalRecords, out int todayAppointmentCount);

        void UpdateStatus(int appointmentId, int hospitalId, int? subHospitalId, string status);
        List<OPDMedicineVM> GetMedicinesByOPDId(int opdId);

List<OPD> GetPatientFullHistory(int patientId, int hospitalId, int? subHospitalId);

        /// <summary>
        /// Batch-fetch OPD ID + IPD admission status for a list of appointment IDs.
        /// Replaces N+1 calls to GetOPDIdByAppointmentId + GetIPDAdmissionById.
        /// </summary>
        Dictionary<int, (int opdId, string ipdStatus)> GetOPDWithIPDStatusBatch(List<int> appointmentIds, int hospitalId, int? subHospitalId);

        /// <summary>
        /// Batch-fetch symptoms for multiple OPD IDs.
        /// Returns a dictionary: OPD_Id -> comma-separated symptom names.
        /// </summary>
        Dictionary<int, string> GetSymptomsByOPDIds(List<int> opdIds);

        /// <summary>
        /// Get today's token number for a given appointment via a single SQL row_number().
        /// </summary>
        int GetTodayTokenNumber(int appointmentId, int hospitalId, int? subHospitalId);
    }
}
