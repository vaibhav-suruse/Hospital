// Repository/IDoctorRound.cs
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IDoctorRound
    {
        List<IPDDoctorRound> GetRoundsByIPD(int ipdId, int parentHospitalId);
        IPDDoctorRound GetRoundDetail(int roundId);
        int CreateRound(IPDDoctorRound model);
        void InsertSymptom(IPDRoundSymptom model);
        void InsertPrescription(IPDRoundPrescription model);
        void InsertInvestigation(IPDRoundInvestigation model);
        void DeleteRound(int roundId);

        
        IPDPrescriptionVM GetRoundPrescriptionPrint(int roundId);
        IPDPrescriptionVM GetAllRoundsPrescriptionPrint(int ipdId);

        void InsertIPDPharmacyNotification(MedicineNotificationModel model);
    }
}