using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IPatientPortal
    {
        // Dashboard summary data
        PatientDashboardVM GetDashboardSummary(int patientId);

        // Appointments
        List<PatientAppointmentVM> GetAppointmentsByPatientId(int patientId);
        bool CancelAppointment(int appointmentId, int patientId);

        // OPD History
        List<PatientOPDHistoryVM> GetOPDHistoryByPatientId(int patientId);

        // IPD History (with billing info)
        List<PatientIPDHistoryVM> GetIPDHistoryByPatientId(int patientId);

        // Billing
        List<PatientOPDBillVM> GetOPDBillsByPatientId(int patientId);
        List<PatientIPDRoundVM> GetIPDRoundsByIPDId(
           int ipdId,
           int patientId,
           int hospitalId);
       
        List<PatientVitalsVM> GetVitalsByIPDId(
            int ipdId,
            int patientId,
            int hospitalId,
            int? subHospitalId);
       
    
}
}
