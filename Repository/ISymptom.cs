using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface ISymptom
    {

        int CreateSymptom(Symptom model, int hospitalId, int? subHospitalId);
        int UpdateSymptom(Symptom model, int hospitalId, int? subHospitalId);
        int DeleteSymptom(int symptomId, int hospitalId, int? subHospitalId);

        List<Symptom> GetAllSymptoms(int hospitalId, int? subHospitalId);
        Symptom GetSymptomById(int symptomId, int hospitalId, int? subHospitalId);
    }
}
