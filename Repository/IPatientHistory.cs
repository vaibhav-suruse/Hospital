using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IPatientHistory
    {
        // Vitals
        int SaveVitals(PatientVitals v);
        PatientVitals GetLatestVitals(int patientId, int hospitalId);

        // Medical Conditions
        int SaveMedicalCondition(PatientMedicalCondition c);
        void DeleteMedicalCondition(int id);
        List<PatientMedicalCondition> GetMedicalConditions(int patientId, int hospitalId);

        // Allergies
        int SaveAllergy(PatientAllergy a);
        void DeleteAllergy(int id);
        List<PatientAllergy> GetAllergies(int patientId, int hospitalId);

        // Surgical History
        int SaveSurgery(PatientSurgicalHistory s);
        void DeleteSurgery(int id);
        List<PatientSurgicalHistory> GetSurgeries(int patientId, int hospitalId);

        // Family History
        int SaveFamilyHistory(PatientFamilyHistory f);
        void DeleteFamilyHistory(int id);
        List<PatientFamilyHistory> GetFamilyHistory(int patientId, int hospitalId);

        // Lab Results
        int SaveLabResult(PatientLabResult l);
        void DeleteLabResult(int id);
        List<PatientLabResult> GetLabResults(int patientId, int hospitalId);

        // Combined (all sections in one call)
        PatientHistoryVM GetFullHistory(int patientId, int hospitalId);
    }

}
