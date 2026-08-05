using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IBed
    {
        List<Bed> GetAllBeds(int hospitalId, int? subHospitalId, int wardId = 0, int roomId = 0);
        Bed GetBedById(int bedId, int hospitalId, int? subHospitalId);
        void CreateBed(Bed bed);
        void UpdateBed(Bed bed);
        void DeleteBed(int bedId, int hospitalId, int? subHospitalId);
        List<Bed> GetAllBed(int hospitalId, int? subHospitalId);
        Bed GetActiveBedByNumber(string bedNumber, int wardId, int roomId, int hospitalId, int? subHospitalId, int excludeBedId = 0);
    }
}
