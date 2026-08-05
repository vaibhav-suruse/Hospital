using System.Collections.Generic;
using System.Numerics;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IDoctor
    {
        int AddDoctor(Doctor model, int hospitalId, int? subHospitalId);
        List<Doctor> GetAllDoctor(int hospitalId, int? subHospitalId);
        Doctor GetDoctorById(int doctorId, int hospitalId, int? subHospitalId);
        int UpdateDoctor(Doctor model, int hospitalId, int? subHospitalId);
        int DeleteDoctor(int doctorId, int hospitalId, int? subHospitalId);
    }
}
