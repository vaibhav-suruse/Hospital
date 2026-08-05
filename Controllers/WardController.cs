using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class WardController : Controller
    {
        private readonly IWard _wardService;

        public WardController(IWard wardService)
        {
            _wardService = wardService;
        }

        // GET: Index
        public IActionResult Index()
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                //var wards = _wardService.GetAllWards(hospitalId, subHospitalId);

                var wards = _wardService.GetWardsWithCounts(hospitalId, subHospitalId);
                return View(wards);

               
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
        }

        // GET: Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        // POST: Create
        [HttpPost]
        public IActionResult Create(Ward model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.HospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                    model.SubHospitalId = HttpContext.Session.GetInt32("SubHospitalId");



                    _wardService.CreateWard(model);
                    return RedirectToAction("Index");
                }

                PopulateDropdowns(model);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                PopulateDropdowns(model);
                return View(model);
            }
        }

        // GET: Edit
        public IActionResult Edit(int id)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var ward = _wardService.GetWardById(id, hospitalId, subHospitalId);
                if (ward == null) return RedirectToAction("Index");

                PopulateDropdowns(ward);
                return View(ward);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return RedirectToAction("Index");
            }
        }



        [HttpPost]
        public IActionResult Edit(Ward model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.HospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                    model.SubHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                    // Convert string to bool if needed
                    if (Request.Form["IsActive"].Count > 0)
                        model.IsActive = Request.Form["IsActive"] == "true";

                    _wardService.UpdateWard(model);
                    return RedirectToAction("Index");
                }

                PopulateDropdowns(model);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                PopulateDropdowns(model);
                return View(model);
            }
        }







        // GET: Delete
        public IActionResult Delete(int id)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                _wardService.DeleteWard(id, hospitalId, subHospitalId);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return RedirectToAction("Index");
            }
        }

        // Populate dropdowns for WardType and Status
        private void PopulateDropdowns(Ward model = null)
        {
            ViewBag.WardTypes = new List<SelectListItem>
            {
                new SelectListItem { Text = "General", Value = "General", Selected = model?.WardType == "General" },
                new SelectListItem { Text = "ICU", Value = "ICU", Selected = model?.WardType == "ICU" },
                new SelectListItem { Text = "Private", Value = "Private", Selected = model?.WardType == "Private" },
                new SelectListItem { Text = "SemiPrivate", Value = "SemiPrivate", Selected = model?.WardType == "SemiPrivate" },
                new SelectListItem { Text = "Emergency", Value = "Emergency", Selected = model?.WardType == "Emergency" }
            };
            ViewBag.StatusList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Active", Value = "true", Selected = model?.IsActive == true },
                new SelectListItem { Text = "Inactive", Value = "false", Selected = model?.IsActive == false }
            };






        }

    }
}
