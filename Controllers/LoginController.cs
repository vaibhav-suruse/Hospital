using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Models.Classs;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class LoginController : Controller
    {
        private readonly Ipatient _patientRepo;
        private readonly IEmailService _emailService;

        public LoginController(
            Ipatient patientRepo,
            IEmailService emailService)
        {
            _patientRepo = patientRepo;
            _emailService = emailService;
        }

        // ── HOSPITAL STAFF LOGIN ─────────────────────────
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult LoginClick([Bind] Users _users)
        {
            if (!string.IsNullOrEmpty(_users.username) &&
                !string.IsNullOrEmpty(_users.password))
            {
                var patient = _patientRepo
                    .Login(_users.username, _users.password);

                if (patient != null)
                    return RedirectToAction("Index", "DashBord");

                ViewBag.Error = "Invalid Username or Password";
                return View("Index");
            }

            ViewBag.Error =
                "Please enter both Username and Password";
            return View("Index");
        }

        // ── PATIENT LOGIN GET ────────────────────────────
        [HttpGet]
        public IActionResult PatientLogin()
        {
            // ── FIXED: use HasValue to avoid null error ──
            int? patientId = HttpContext.Session
                             .GetInt32("PatientId");

            if (patientId.HasValue && patientId.Value > 0)
                return RedirectToAction(
                    "Dashboard", "PatientPortal");

            return View();
        }

        // ── PATIENT LOGIN POST ───────────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult PatientLogin(
     string email, string password)
        {
            // ── Step 1: Basic validation ─────────────────
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error =
                    "Please enter email and password.";
                return View();
            }

            // ── Step 2: Login from tbl_patient_account ───
            PatientAccount account = null;

try
            {
                account = _patientRepo
                    .LoginAccount(email, password);
            }
            catch (Exception ex)
            {
                // Security: never surface DB/exception details or echo the
                // user's credentials back to the browser.
                ViewBag.Error =
                    "Unable to log in right now. " +
                    "Please try again.";
                return View();
            }

            // ── Step 3: Check if account found ───────────
            if (account == null)
            {
                ViewBag.Error =
                    "Invalid email or password.";
                return View();
            }

            // ── Step 4: Store AccountId in session ───────
            HttpContext.Session.SetInt32(
                "AccountId", account.AccountId);
            HttpContext.Session.SetString(
                "AccountName",
                (account.FirstName ?? "") + " " +
                (account.LastName ?? ""));
            HttpContext.Session.SetString(
"AccountEmail", account.Email ?? "");

            // ── Step 5: Get all profiles ─────────────────
            var profiles = _patientRepo
                .GetProfilesByAccountId(account.AccountId);

            // ── Step 6: Decide where to go ───────────────
            if (profiles == null || profiles.Count == 0)
            {
                // No hospital records yet
                HttpContext.Session.SetInt32("PatientId", 0);
                HttpContext.Session.SetInt32(
                    "PatientHospitalId", 0);
                HttpContext.Session.SetString(
                    "PatientName",
                    (account.FirstName ?? "") + " " +
                    (account.LastName ?? ""));

                // ── Go to SelectProfile page ──────────────
                // Even with 0 profiles show this page
                // It has Add Family Member button
                return RedirectToAction("SelectProfile");
            }
            else if (profiles.Count == 1)
            {
                // Only 1 record — go directly to dashboard
                SetPatientSession(profiles.First());
                return RedirectToAction(
                    "Dashboard", "PatientPortal");
            }
            else
            {
                // Multiple records — show profile selector
                return RedirectToAction("SelectProfile");
            }
        }

        // ── PROFILE SELECTOR GET ─────────────────────────
        [HttpGet]
        public IActionResult SelectProfile()
        {
            int? accountId = HttpContext.Session
                             .GetInt32("AccountId");

            if (!accountId.HasValue ||
                 accountId.Value == 0)
                return RedirectToAction("PatientLogin");

            var profiles = _patientRepo
                .GetProfilesByAccountId(accountId.Value);

           

            return View(profiles);
        }

        // ── PROFILE SELECTOR POST ────────────────────────
        [HttpPost]
        public IActionResult SelectProfile(int patientId)
        {
            int? accountId = HttpContext.Session
                             .GetInt32("AccountId");

            if (!accountId.HasValue || accountId.Value == 0)
                return RedirectToAction("PatientLogin");

            var profiles = _patientRepo
                .GetProfilesByAccountId(accountId.Value);

            var selected = profiles?
                .FirstOrDefault(p => p.PatientId == patientId);

            if (selected == null)
            {
                TempData["Error"] = "Invalid profile selected.";
                return View(profiles);
            }

            SetPatientSession(selected);
            return RedirectToAction(
                "Dashboard", "PatientPortal");
        }

        // ── PATIENT REGISTRATION GET ─────────────────────
        [HttpGet]
        public IActionResult PatientRegister()
        {
            return View();
        }

        // ── PATIENT REGISTRATION POST ────────────────────
        [HttpPost]
        public IActionResult PatientRegister(
            PatientRegisterVM model)
        {
            if (string.IsNullOrWhiteSpace(model.FirstName) ||
                string.IsNullOrWhiteSpace(model.LastName) ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.Error = "All fields are required.";
                return View(model);
            }

            // Check email already exists
            if (_patientRepo.EmailExistsInAccount(model.Email))
            {
                ViewBag.Error =
                    "This email is already registered. " +
                    "Please login instead.";
                return View(model);
            }

            // Store in session temporarily
            HttpContext.Session.SetString(
                "Reg_FirstName", model.FirstName);
            HttpContext.Session.SetString(
                "Reg_LastName", model.LastName);
            HttpContext.Session.SetString(
                "Reg_Email", model.Email);
            HttpContext.Session.SetString(
                "Reg_Password", model.Password);
            HttpContext.Session.SetString(
                "Reg_PhoneNumber", model.PhoneNumber ?? "");

            // Generate OTP
            string otp = GenerateOTP();
            _patientRepo.GenerateOTP(
                model.Email, "REGISTER", otp);

            try
            {
                _emailService.SendOTP(
                    model.Email, otp, "REGISTER");
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Failed to send OTP: " + ex.Message;
                return View(model);
            }

            TempData["RegisterEmail"] = model.Email;
            return RedirectToAction("VerifyRegisterOTP");
        }

        // ── VERIFY REGISTER OTP GET ──────────────────────
        [HttpGet]
        public IActionResult VerifyRegisterOTP()
        {
            ViewBag.Email =
                TempData["RegisterEmail"]?.ToString()
                ?? HttpContext.Session.GetString("Reg_Email");
            return View();
        }

        // ── VERIFY REGISTER OTP POST ─────────────────────
        [HttpPost]
        public IActionResult VerifyRegisterOTP(string otp)
        {
            string email = HttpContext.Session
                           .GetString("Reg_Email");

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("PatientRegister");

            bool valid = _patientRepo
                .VerifyOTP(email, otp, "REGISTER");

            if (!valid)
            {
                ViewBag.Error =
                    "Invalid or expired OTP. " +
                    "Please try again.";
                ViewBag.Email = email;
                return View();
            }

            // OTP correct → create account
            var account = new PatientAccount
            {
                FirstName = HttpContext.Session
                              .GetString("Reg_FirstName"),
                LastName = HttpContext.Session
                              .GetString("Reg_LastName"),
                Email = email,
                Password = HttpContext.Session
                              .GetString("Reg_Password"),
                PhoneNumber = HttpContext.Session
                              .GetString("Reg_PhoneNumber")
            };

            int accountId = _patientRepo
                .SignupAccount(account, out string message);

            if (accountId <= 0)
            {
                ViewBag.Error = message;
                ViewBag.Email = email;
                return View();
            }

            // Clear session
            HttpContext.Session.Remove("Reg_FirstName");
            HttpContext.Session.Remove("Reg_LastName");
            HttpContext.Session.Remove("Reg_Email");
            HttpContext.Session.Remove("Reg_Password");
            HttpContext.Session.Remove("Reg_PhoneNumber");

            // Set account session
            HttpContext.Session.SetInt32(
                "AccountId", accountId);
            HttpContext.Session.SetString(
                "AccountName",
                account.FirstName + " " + account.LastName);

            return RedirectToAction("RegisterSuccess");
        }

        // ── REGISTER SUCCESS ─────────────────────────────
        [HttpGet]
        public IActionResult RegisterSuccess()
        {
            int? accountId = HttpContext.Session
                             .GetInt32("AccountId");

            if (!accountId.HasValue || accountId.Value == 0)
                return RedirectToAction("PatientLogin");

            var profiles = _patientRepo
                .GetProfilesByAccountId(accountId.Value);

            return View(profiles);
        }

        // ── RESEND OTP ───────────────────────────────────
        [HttpPost]
        public IActionResult ResendOTP(string purpose)
        {
            string email = purpose == "REGISTER"
                ? HttpContext.Session.GetString("Reg_Email")
                : HttpContext.Session.GetString("ForgotEmail");

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("PatientLogin");

            string otp = GenerateOTP();
            _patientRepo.GenerateOTP(email, purpose, otp);

            try
            {
                _emailService.SendOTP(email, otp, purpose);
                TempData["Success"] = "OTP resent successfully.";
            }
            catch
            {
                TempData["Error"] = "Failed to resend OTP.";
            }

            return purpose == "REGISTER"
                ? RedirectToAction("VerifyRegisterOTP")
                : RedirectToAction("VerifyForgotOTP");
        }

        // ── FORGOT PASSWORD GET ──────────────────────────
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // ── FORGOT PASSWORD POST ─────────────────────────
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter your email.";
                return View();
            }

            if (!_patientRepo.EmailExistsInAccount(email))
            {
                ViewBag.Error =
                    "No account found with this email. " +
                    "Please register first.";
                return View();
            }

            string otp = GenerateOTP();
            _patientRepo.GenerateOTP(email, "FORGOT", otp);

            try
            {
                _emailService.SendOTP(email, otp, "FORGOT");
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Failed to send OTP: " + ex.Message;
                return View();
            }

            HttpContext.Session.SetString("ForgotEmail", email);
            TempData["ForgotEmail"] = email;
            return RedirectToAction("VerifyForgotOTP");
        }

        // ── VERIFY FORGOT OTP GET ────────────────────────
        [HttpGet]
        public IActionResult VerifyForgotOTP()
        {
            ViewBag.Email =
                TempData["ForgotEmail"]?.ToString()
                ?? HttpContext.Session.GetString("ForgotEmail");
            return View();
        }

        // ── VERIFY FORGOT OTP POST ───────────────────────
        [HttpPost]
        public IActionResult VerifyForgotOTP(string otp)
        {
            string email = HttpContext.Session
                           .GetString("ForgotEmail");

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("ForgotPassword");

            bool valid = _patientRepo
                .VerifyOTP(email, otp, "FORGOT");

            if (!valid)
            {
                ViewBag.Error =
                    "Invalid or expired OTP. " +
                    "Please try again.";
                ViewBag.Email = email;
                return View();
            }

            HttpContext.Session.SetString(
                "ForgotVerified", "yes");
            return RedirectToAction("ResetPassword");
        }

        // ── RESET PASSWORD GET ───────────────────────────
        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (HttpContext.Session
                .GetString("ForgotVerified") != "yes")
                return RedirectToAction("ForgotPassword");

            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(PatientForgotVM model)
        {
            if (HttpContext.Session.GetString("ForgotVerified") != "yes")
                return RedirectToAction("ForgotPassword");

            if (model.NewPassword != model.ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View(model);
            }

            string email = HttpContext.Session.GetString("ForgotEmail");

            // ── NEW: Check if new password is same as old ──
            bool isSamePassword = _patientRepo.IsSamePassword(email, model.NewPassword);
            if (isSamePassword)
            {
                ViewBag.Error = "New password cannot be the same as your current password.";
                return View(model);
            }

            _patientRepo.ResetPasswordAccount(email, model.NewPassword);

            HttpContext.Session.Remove("ForgotEmail");
            HttpContext.Session.Remove("ForgotVerified");

            TempData["Success"] = "Password reset successfully. Please login.";
            return RedirectToAction("PatientLogin");
        }

        // ── EXISTING SIGNUP (hospital side) ─────────────
        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Signup(Patient model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "Please fill all required fields";
                return View(model);
            }

            bool result = _patientRepo
                .SignupPatient(model, out string message);

            if (result)
            {
                TempData["Success"] = message;
                return RedirectToAction("Signup", "Login");
            }

            TempData["Error"] = message;
            return View(model);
        }

        // ── PRIVATE HELPERS ──────────────────────────────
        private void SetPatientSession(PatientProfileVM p)
        {
            HttpContext.Session.SetInt32(
                "PatientId", p.PatientId);
            HttpContext.Session.SetInt32(
                "PatientHospitalId", p.Hospital_Id);
            HttpContext.Session.SetString(
                "PatientName", p.FullName);
            HttpContext.Session.SetString(
                "PatientRelation", p.Relation ?? "Self");
            HttpContext.Session.SetString(
                "PatientHospitalName", p.HospitalName ?? "");

            if (p.SubHospital_Id.HasValue)
                HttpContext.Session.SetInt32(
                    "PatientSubHospitalId",
                    p.SubHospital_Id.Value);
            else
                HttpContext.Session.Remove(
                    "PatientSubHospitalId");
        }

        private string GenerateOTP()
        {
            return new Random()
                .Next(100000, 999999)
                .ToString();
        }



        // ── ADD FAMILY MEMBER GET ────────────────────────
        [HttpGet]
        public IActionResult AddFamilyMember()
        {
            int? accountId = HttpContext.Session
                             .GetInt32("AccountId");

            if (!accountId.HasValue || accountId.Value == 0)
                return RedirectToAction("PatientLogin");

            return View();
        }

        // ── ADD FAMILY MEMBER POST ───────────────────────
        [HttpPost]
        public IActionResult AddFamilyMember(
     string firstName,
     string lastName,
     string relation,
     int age,
     string gender)
        {
            int? accountId = HttpContext.Session
                             .GetInt32("AccountId");

            if (!accountId.HasValue || accountId.Value == 0)
                return RedirectToAction("PatientLogin");

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(relation))
            {
                TempData["Error"] =
                    "First name, last name and " +
                    "relation are required.";
                return View();
            }

            try
            {
                // ── Get email from account session ────────
                string accountEmail = HttpContext.Session
                    .GetString("AccountEmail") ?? "";

                _patientRepo.AddFamilyMember(
                    accountId.Value,
                    firstName,
                    lastName,
                    relation,
                    age,
                    gender,
                    accountEmail); // ← pass email

                TempData["Success"] =
                    firstName + " added successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Error: " + ex.Message;
                return View();
            }

            return RedirectToAction("SelectProfile");
        }




    }
}






















//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using WebApplicationSampleTest2.Models;
//using WebApplicationSampleTest2.Models.Classs;
//using WebApplicationSampleTest2.Repository;

//namespace WebApplicationSampleTest2.Controllers
//{
//    public class LoginController : Controller
//    {
//        private readonly Ipatient _patientRepo;

//        public LoginController(Ipatient patientRepo)
//        {
//            _patientRepo = patientRepo;
//        }
//        public IActionResult Index()
//        {
//            return View();
//        }

//        public IActionResult ForgotPassword()
//        {
//            return View();
//        }
//        public IActionResult Signup()
//        {
//            return View();
//        }

//        public IActionResult LoginClick([Bind] Users _users)
//        {

//            ViewBag.Username = _users.username;

//            // Check if username and password entered
//            if (!string.IsNullOrEmpty(_users.username) && !string.IsNullOrEmpty(_users.password))
//            {
//                // Repository call to validate user
//                var patient = _patientRepo.Login(_users.username, _users.password);

//                if (patient != null)
//                {
//                    // Valid user → redirect to dashboard
//                    return RedirectToAction("Index", "DashBord");
//                }
//                else
//                {
//                    // Invalid user → show login page with error message
//                    ViewBag.Error = "Invalid Username or Password";
//                    return View("Index"); // explicitly return Login page
//                }
//            }
//            else
//            {
//                // Empty fields → show login page with message
//                ViewBag.Error = "Please enter both Username and Password";
//                return View("Index"); // explicitly return Login page
//            }
//        }

//        [HttpPost]
//        public IActionResult ForgotPassword(ForgotPasswordModel model)
//        {
//            if (string.IsNullOrEmpty(model.Email))
//            {
//                ViewBag.Error = "Please enter email";
//                return View(model);
//            }

//            bool emailExists = _patientRepo.CheckEmail(model.Email);

//            if (!emailExists)
//            {
//                ViewBag.Error = "Email not found";
//                return View(model);
//            }

//            // ✅ Email verified here
//            ViewBag.ShowModal = true;
//            return View(model);
//        }

//        [HttpPost]
//        public IActionResult ResetPassword(ForgotPasswordModel model)
//        {
//            if (model.NewPassword != model.ConfirmPassword)
//            {
//                ViewBag.Error = "Password mismatch";
//                ViewBag.ShowModal = true;
//                return View("ForgotPassword", model);
//            }

//            _patientRepo.UpdatePassword(model.Email, model.NewPassword);
//            return RedirectToAction("ForgotPassword");
//        }

//        //[HttpPost]
//        //public IActionResult Signup(Patient patient)
//        //{

//        //    if (!ModelState.IsValid)
//        //    {
//        //        TempData["Error"] = "Please fill all required fields";
//        //        return View(patient);
//        //    }

//        //    bool result = _patientRepo.RegisterPatient(patient);

//        //    if (result)
//        //    {
//        //        TempData["Success"] = "Registration successful";
//        //        return RedirectToAction("Signup", "Login");
//        //    }

//        //    TempData["Error"] = "Registration failed";
//        //    return View(patient);
//        //}

//        [HttpPost]
//        public IActionResult Signup(Patient model)
//        {
//            // 4️⃣ Server-side basic validation
//            if (!ModelState.IsValid)
//            {
//                TempData["Error"] = "Please fill all required fields";
//                return View(model);
//            }

//            // 5️⃣ Call Repository method
//            bool result = _patientRepo.SignupPatient(model, out string message);

//            // 6️⃣ Success / Failure handling
//            if (result)
//            {
//                TempData["Success"] = message; // "Patient registered successfully"
//                return RedirectToAction("Signup", "Login"); // back to login page
//            }
//            else
//            {
//                TempData["Error"] = message; // Email/Mobile already exists
//                return View(model);
//            }
//        }




//        [HttpGet]
//        public IActionResult PatientLogin()
//        {
//            return View();
//        }

//        [HttpPost]
//        public IActionResult PatientLogin(string email, string password)
//        {
//            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
//            {
//                ViewBag.Error = "Please enter email and password.";
//                return View();
//            }

//            // Reuse your existing Login method from Ipatient — it takes email + password
//            var patient = _patientRepo.Login(email, password);

//            if (patient != null)
//            {
//                // ✅ Store patient session keys
//                HttpContext.Session.SetInt32("PatientId", patient.Id);
//                HttpContext.Session.SetString("PatientName", patient.FirstName + " " + patient.LastName);
//                HttpContext.Session.SetString("UserType", "Patient");

//                // Store hospital info — patient's Hospital_Id from tbl_patient
//                // You need to fetch Hospital_Id from the patient record
//                // Add Hospital_Id and SubHospital_Id to your Login SP and Patient model if not already there
//                // For now using 0 as fallback — update once SP returns these fields
//                HttpContext.Session.SetInt32("PatientHospitalId", patient.Hospital_Id > 0 ? patient.Hospital_Id : 0);
//                if (patient.SubHospital_Id.HasValue)
//                    HttpContext.Session.SetInt32("PatientSubHospitalId", patient.SubHospital_Id.Value);

//                return RedirectToAction("Dashboard", "PatientPortal");
//            }

//            ViewBag.Error = "Invalid email or password.";
//            return View();
//        }



//    }
//}
