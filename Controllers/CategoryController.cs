using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategory _categoryRepository;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ICategory categoryRepository, ILogger<CategoryController> logger)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        // ─────────────────────────────────────────
        // INDEX — List with Search + Pagination
        // ─────────────────────────────────────────
        public IActionResult Index(string search, int page = 1)
        {
            int pageSize = 10;

            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                _logger.LogInformation("Loading categories. HospitalId: {HospitalId}", hospitalId);

                var data = _categoryRepository
                                .GetAllCategories(hospitalId, subHospitalId)
                                .AsQueryable();

                // Search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    data = data.Where(x =>
                        !string.IsNullOrEmpty(x.CategoryName) &&
                        x.CategoryName.ToLower().Contains(search)
                    );
                }

                // Pagination
                int totalRecords = data.Count();
                int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                var pagedData = data
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.Search = search;

                return View(pagedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                TempData["Error"] = "Something went wrong while loading categories!";
                return View(new List<CategoryModel>());
            }
        }

        // ─────────────────────────────────────────
        // CREATE — GET
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            var model = new CategoryModel { IsUpdate = false };
            return View("Create", model);
        }

        // ─────────────────────────────────────────
        // CREATE / UPDATE — POST
        // ─────────────────────────────────────────
        [HttpPost]
        public IActionResult Create(CategoryModel model)
        {
            if (!ModelState.IsValid)
                return View("Create", model);

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            model.HospitalId = hospitalId;
            model.SubHospitalId = subHospitalId;

            try
            {
                if (model.IsUpdate && model.CategoryId > 0)
                {
                    _categoryRepository.UpdateCategory(model);
                    TempData["Success"] = "Category updated successfully!";
                }
                else
                {
                    _categoryRepository.AddCategory(model);
                    TempData["Success"] = "Category added successfully!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving category");
                TempData["Error"] = "Something went wrong while saving category!";
                return View("Create", model);
            }
        }

        // ─────────────────────────────────────────
        // EDIT — GET
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (id <= 0)
                return NotFound();

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var data = _categoryRepository.GetCategoryById(id, hospitalId, subHospitalId);

            if (data == null)
                return NotFound();

            data.IsUpdate = true;

            return View("Create", data);   // shared view for Create & Edit
        }

        // ─────────────────────────────────────────
        // DELETE — GET (via modal confirmation)
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Delete(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid category ID.";
                return RedirectToAction("Index");
            }

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            try
            {
                _categoryRepository.DeleteCategory(id, hospitalId, subHospitalId);
                TempData["Success"] = "Category deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category. Id: {Id}", id);
                TempData["Error"] = "Cannot delete this category — it may be linked to existing medicines.";
            }

            return RedirectToAction("Index");
        }
    }
}
