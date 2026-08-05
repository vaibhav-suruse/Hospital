using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Data;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class UserController : Controller
    {
        private readonly IUser _IUser;
        private readonly IHospital _IHospital;

        public UserController(IUser User, IHospital hospital)
        {
            _IUser = User;
            _IHospital = hospital;
        }
        public IActionResult Index(string search, int page = 1)
        {

            int pageSize = 5;   // ek page var kiti records pahije
            var users = _IUser.GetAllUsers(); // full list

            // ========== SEARCH ==========
            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u =>
                    u.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    u.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    u.LoginName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    u.EmailId.Contains(search, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // ========== PAGINATION ==========
            int totalRecords = users.Count;
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var pagedUsers = users
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

            // ========== VIEWBAG ==========
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchTerm = search;

            return View(pagedUsers);
        }

        // GET: Create User
        public IActionResult Create()
        {
            var model = new User { isUpdate = false };

            // ONLY MAIN HOSPITALS
            var mainHospitals = _IHospital.GetAllHospitals()
                                          .Where(x => !x.IsSubHospital)
                                          .ToList();

            ViewBag.MainHospitals = new SelectList(mainHospitals, "Id", "Name");

            return View("Create", model);
        }

        [HttpPost]
        public IActionResult Save(User model)
        {
            model.isUpdate = model.Id > 0;

            if (!ModelState.IsValid)
            {
                ViewBag.MainHospitals = new SelectList(
                    _IHospital.GetAllHospitals().Where(x => !x.IsSubHospital),
                    "Id", "Name"
                );
                return View("Create", model);
            }

            // CORRECT LOGIC (IMPORTANT)

            // 1️⃣ Main hospital mandatory
            if (model.Id == 0)
            {

                if (!model.MainHospitalId.HasValue)
                {
                    ModelState.AddModelError("", "Please select Main Hospital");
                    ViewBag.MainHospitals = new SelectList(
                        _IHospital.GetAllHospitals().Where(x => !x.IsSubHospital),
                        "Id", "Name"
                    );
                    return View("Create", model);
                }
                model.HospitalId = model.MainHospitalId.Value;
            }


           
            

            // 3️⃣ SubHospitalId optional → already bind via dropdown
            // (nothing to do here)

            if (model.Id > 0)
                _IUser.UpdateUser(model);
            else
                _IUser.InsertUser(model);

            return RedirectToAction("Index");
        }




        // GET: Edit User
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = _IUser.GetUserById(id);
            if (model == null)
                return NotFound();

            model.isUpdate = true;

            var allHospitals = _IHospital.GetAllHospitals();
            ViewBag.Hospitals = new SelectList(allHospitals, "Id", "Name", model.HospitalId);

            return View("Create", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid user id";
                return RedirectToAction("Index");
            }

            var user = _IUser.GetUserById(id);
            if (user == null)
            {
                TempData["Error"] = "User not found";
                return RedirectToAction("Index");
            }

            _IUser.DeleteUser(id);
            TempData["Success"] = "User deleted successfully";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Login()
        {
            ViewBag.MainHospitals = _IHospital.GetMainHospitals();
            return View();
        }

        // 🔹 IDENTIFY USER (AJAX)
        [HttpGet]
        public IActionResult IdentifyUser(string username)
        {
            var role = _IUser.GetUserRoleByUsername(username);

            return Json(new
            {
                role = role ?? ""
            });
        }

        // 🔹 GET SUB HOSPITALS (AJAX)
        [HttpGet]
        public IActionResult GetSubHospitals(int mainHospitalId)
        {
            var dt = _IUser.GetSubHospitals(mainHospitalId);

            var list = dt.AsEnumerable().Select(r => new
            {
                id = r.Field<int>("Id"),
                name = r.Field<string>("Name")
            }).ToList();

            return Json(list);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult LoginClick(Login model)
        //{
        //    var username = model.username?.Trim();
        //    var password = model.password?.Trim();

        //    var user = _IUser.LoginUser(
        //        model.username,
        //        model.password,
        //        model.MainHospitalId,
        //        model.SubHospitalId
        //    );

        //    if (user == null)
        //    {
        //        ViewBag.Error = "Invalid Username or Password";
        //        ViewBag.MainHospitals = _IHospital.GetMainHospitals();
        //        return View("Login", model);
        //    }

        //    // 🚀 SuperAdmin not  hospital validation 
        //    if (user.Role != "SuperAdmin" && !model.MainHospitalId.HasValue)
        //    {
        //        ViewBag.Error = "Please select Main Hospital";
        //        ViewBag.MainHospitals = _IHospital.GetMainHospitals();
        //        return View("Login", model);
        //    }

        //    //  SESSION
        //    HttpContext.Session.SetInt32("UserId", user.Id);
        //    HttpContext.Session.SetString("Username", user.LoginName);
        //    HttpContext.Session.SetString("UserRole", user.Role);

        //    if (user.Role != "SuperAdmin")
        //    {
        //        HttpContext.Session.SetInt32("MainHospitalId", user.HospitalId);

        //        if (user.SubHospitalId.HasValue)
        //            HttpContext.Session.SetInt32("SubHospitalId", user.SubHospitalId.Value);
        //    }

        //    if (user.Role == "SuperAdmin")
        //    {
        //        return RedirectToAction("Dash", "DashBord");
        //    }
        //    else
        //    {
        //        return RedirectToAction("Index", "DashBord");
        //    }
        //}


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LoginClick(Login model)
        {
            try
            {
                var username = model.username?.Trim();
                var password = model.password?.Trim();

                var user = _IUser.LoginUser(
                    model.username,
                    model.password,
                    model.MainHospitalId,
                    model.SubHospitalId
                );

                if (user == null)
                {
                    ViewBag.Error = "Invalid Username or Password";
                    ViewBag.MainHospitals = _IHospital.GetMainHospitals();
                    return View("Login", model);
                }


                if (user.Role != "SuperAdmin" && !model.MainHospitalId.HasValue)
                {
                    ViewBag.Error = "Please select Main Hospital";
                    ViewBag.MainHospitals = _IHospital.GetMainHospitals();
                    return View("Login", model);
                }

                // SESSION
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.LoginName);
                HttpContext.Session.SetString("UserRole", user.Role);

                if (user.Role != "SuperAdmin")
                {
                    HttpContext.Session.SetInt32("MainHospitalId", user.HospitalId);
                    if (user.SubHospitalId.HasValue)
                        HttpContext.Session.SetInt32("SubHospitalId", user.SubHospitalId.Value);
                }

                if (user.Role == "SuperAdmin")
                    return RedirectToAction("Dash", "DashBord");
                else
                    return RedirectToAction("Index", "DashBord");
            }
catch (Exception ex)
            {
                // Security: do not surface raw exception text (which can leak
                // DB/SP internals) to the end user. The stored procedure that
                // throws already carries a friendly message like "Invalid
                // Username" / "Invalid Password" / "Account is Inactive", but
                // we deliberately keep the message generic here and log the
                // real exception server-side instead.
                ViewBag.Error = "Unable to log in. Please check your credentials and try again.";
                ViewBag.MainHospitals = _IHospital.GetMainHospitals();
                return View("Login", model);
            }
        }

    }
}
