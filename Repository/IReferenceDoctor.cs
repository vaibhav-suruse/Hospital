using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IReferenceDoctor
    {
        int AddReferenceDoctor(ReferenceDoctorModel model, int hospitalId, int? subHospitalId);

        List<ReferenceDoctorModel> GetAllReferenceDoctor(int hospitalId, int? subHospitalId);

        ReferenceDoctorModel GetReferenceDoctorById(int referenceDoctorId, int hospitalId, int? subHospitalId);

        int UpdateReferenceDoctor(ReferenceDoctorModel model, int hospitalId, int? subHospitalId);

        int DeleteReferenceDoctor(int referenceDoctorId, int hospitalId, int? subHospitalId);
    }
}
