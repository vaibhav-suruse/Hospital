using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IPharmacyQueue
    {
        // ── OPD ──────────────────────────────────────────────────────────────
        List<MedicineNotificationModel> GetOPDQueue(int hospitalId, int? subHospitalId);
        List<PharmacyMedicineLineVM> GetMedicinesForPharmacy(int opdId, int hospitalId, int? subHospitalId);
        int SaveMedicineBill(PharmacyBillVM bill, int hospitalId, int? subHospitalId, int createdBy);

        // ── IPD ──────────────────────────────────────────────────────────────
        List<MedicineNotificationModel> GetIPDQueue(int hospitalId, int? subHospitalId);
        List<PharmacyMedicineLineVM> GetMedicinesForIPDPharmacy(int ipdId, int roundId, int hospitalId, int? subHospitalId);
        int SaveIPDMedicineBill(IPDPharmacyBillVM bill, int hospitalId, int? subHospitalId, int createdBy);

        // ── Shared ───────────────────────────────────────────────────────────
        void MarkDispensed(int notificationId, int hospitalId);
        void MarkBilled(int notificationId, int hospitalId, int billId);
        string GetPatientMobile(int patientId, int hospitalId);
    }
}





//using System;
//using System.Collections.Generic;
//using System.Data;
//using Microsoft.Extensions.Configuration;
//using MySql.Data.MySqlClient;
//using WebApplicationSampleTest2.Models;

//namespace WebApplicationSampleTest2.Repository
//{

//    public interface IPharmacyQueue
//    {

//        List<MedicineNotificationModel> GetOPDQueue(int hospitalId, int? subHospitalId);


//        List<PharmacyMedicineLineVM> GetMedicinesForPharmacy(int opdId, int hospitalId, int? subHospitalId);


//        void MarkDispensed(int notificationId, int hospitalId);


//        void MarkBilled(int notificationId, int hospitalId, int billId);


//        int SaveMedicineBill(PharmacyBillVM bill, int hospitalId, int? subHospitalId, int createdBy);

//        string GetPatientMobile(int patientId, int hospitalId);



//        /// <summary>Returns medicines from an IPD doctor round for the pharmacy counter.</summary>
//        List<PharmacyMedicineLineVM> GetMedicinesForIPDPharmacy(int ipdId, int roundId, int hospitalId, int? subHospitalId);

//        /// <summary>Saves the IPD medicine bill (reuses counter tables) and returns the new BillId.</summary>
//        int SaveIPDMedicineBill(IPDPharmacyBillVM bill, int hospitalId, int? subHospitalId, int createdBy);

//    }
//}
