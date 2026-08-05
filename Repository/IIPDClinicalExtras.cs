using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IIPDClinicalExtras
    {
        List<IPDExamination> GetExaminations(int ipdId, int hospitalId, int? subHospitalId);
        void AddExamination(IPDExamination model);
        void DeleteExamination(int id);

        List<IPDInputOutput> GetInputOutputs(int ipdId, int hospitalId, int? subHospitalId);
        void AddInputOutput(IPDInputOutput model);
        void DeleteInputOutput(int id);

        List<IPDDailyWellbeing> GetDailyWellbeings(int ipdId, int hospitalId, int? subHospitalId);
        void AddDailyWellbeing(IPDDailyWellbeing model);
        void DeleteDailyWellbeing(int id);

        List<IPDBodyComposition> GetBodyCompositions(int ipdId, int hospitalId, int? subHospitalId);
        void AddBodyComposition(IPDBodyComposition model);
        void DeleteBodyComposition(int id);
    }
}
