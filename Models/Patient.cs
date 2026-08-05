using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    public class Patient
    {
        // ── Core / existing ──────────────────────────────
        public int Id { get; set; }
        public bool isUpdate { get; set; }
        public int Hospital_Id { get; set; }
        public int? SubHospital_Id { get; set; }
        public int? AccountId { get; set; }
        public string Relation { get; set; }

        // ── Personal Details (new + existing) ────────────
        public string Salutation { get; set; }   // NEW — Mr / Mrs / Ms / Dr

        /// <summary>
        /// Single field shown on the form.
        /// On save: splits into FirstName + LastName automatically.
        /// </summary>
        public string FullName { get; set; }   // NEW — used by form only

        public string FirstName { get; set; }   // kept — used by OPD/prescription/notifications
        public string LastName { get; set; }   // kept — used by OPD/prescription/notifications

        public string FatherName { get; set; }   // NEW

        public string Gender { get; set; }
        public string PhoneNumber { get; set; }

        public string Age { get; set; }   // years
        public int AgeMonths { get; set; }   // NEW — months part

        public DateTime? DateOfBirth { get; set; }  // NEW
        public string ReferenceId { get; set; }  // NEW — Patient Reference ID

        // ── Secondary Details ─────────────────────────────
        public string BloodGroup { get; set; }  // NEW
        public string MaritalStatus { get; set; }  // NEW
        public string Occupation { get; set; }  // NEW
        public string Email { get; set; }
        public string ProfilePhoto { get; set; }  // NEW — stores file path

        public string Address { get; set; }

        // ── Legacy fields (keep — used elsewhere) ─────────
        public string Password { get; set; }
        public string pulse { get; set; }
        public string BP { get; set; }
        public string Status { get; set; }
        public string Investigation { get; set; }
        public string ReportDetail { get; set; }
        public string TotalBill { get; set; }

        public List<tablet> Medicineslist { get; set; }
        public Dictionary<string, string> BillingDetails { get; set; }
        public List<string> symptoms { get; set; }
        public List<string> symptomsmain { get; set; }
    }
}
