using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    public class PrescriptionVM
    { // Hospital
        public string HospitalName { get; set; }
        public string HospitalAddress { get; set; }
        public string HospitalLogo { get; set; }
        public string HospitalRegistrationNo { get; set; }
        public string HospitalEmail { get; set; }
        // Patient
        public string PatientName { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }

        // Doctor
        public string DoctorName { get; set; }
        public string Specialization { get; set; }
        public string Education { get; set; }

        // Appointment
        public DateTime AppointmentDate { get; set; }

        // OPD Clinical Data
        public string BP { get; set; }
        public string Pulse { get; set; }
        public string Investigation { get; set; }

        // Symptoms
        public List<OPDSymptomVM> Symptoms { get; set; }

        // Medicines
    

        // Medicines
        public List<OPDMedicineVM> Medicines { get; set; }
    }
    public class OPDSymptomVM
    {
        public int Id { get; set; }
        public int OPD_Id { get; set; }
        public int Symptom_Id { get; set; }
        public string SymptomName { get; set; } // matches SP alias
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}

