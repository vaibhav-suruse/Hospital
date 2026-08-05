using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    public class ProcedureNote
    {
        public int ProcedureId { get; set; }

        public int IPDId { get; set; }

        public int PatientId { get; set; }

        public string ProcedureName { get; set; }

        public int? DoneByDoctorId { get; set; }

        public int? AnaesthetistId { get; set; }

        public DateTime? ProcedureDate { get; set; }

        public string AnaesthesiaType { get; set; }

        public string PreOperativeOrder { get; set; }

        public string OperativeNote { get; set; }

        public string PostOperativeOrder { get; set; }

        public string TemplateName { get; set; }

        public int CreatedBy { get; set; }
        public string DoneByDoctorName { get; set; }
        public string AnaesthetistName { get; set; }
        public List<Doctor> Doctors { get; set; }

        public List<ProcedureNote> ProcedureList { get; set; }
    }
    public class ProcedurePageVM
    {
        public int IPDId { get; set; }

        public int PatientId { get; set; }

        public List<Doctor> Doctors { get; set; }

        public List<ProcedureNote> ProcedureList { get; set; }
    }
}
