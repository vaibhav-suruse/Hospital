using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    // ── Login account (tbl_patient_account) ─────────────
    public class PatientAccount
    {
        public int AccountId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; }
    }

    // ── Profile selector item ────────────────────────────
    public class PatientProfileVM
    {
        public int PatientId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => FirstName + " " + LastName;
        public string Gender { get; set; }
        public string Age { get; set; }
        public string Relation { get; set; }
        public int Hospital_Id { get; set; }
        public int? SubHospital_Id { get; set; }
        public string HospitalName { get; set; }
        public string Initials =>
            (FirstName?.Length > 0 ? FirstName[0].ToString() : "") +
            (LastName?.Length > 0 ? LastName[0].ToString() : "");
    }

    // ── OTP request model ────────────────────────────────
    public class OTPModel
    {
        public string Email { get; set; }
        public string OTP { get; set; }
        public string Purpose { get; set; }
    }

    // ── Registration view model ──────────────────────────
    public class PatientRegisterVM
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
    }

    // ── Forgot password view model ───────────────────────
    public class PatientForgotVM
    {
        public string Email { get; set; }
        public string OTP { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}