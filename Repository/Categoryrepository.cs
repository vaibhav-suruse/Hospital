using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class CategoryRepository : ICategory
    {
        private readonly string _connectionString;
        private readonly ILogger<CategoryRepository> _logger;

        public CategoryRepository(IConfiguration configuration, ILogger<CategoryRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        // ─────────────────────────────────────────
        // GET ALL
        // ─────────────────────────────────────────
        public List<CategoryModel> GetAllCategories(int hospitalId, int? subHospitalId)
        {
            var list = new List<CategoryModel>();

            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("GetAllCategoriesMaster", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new CategoryModel
                            {
                                CategoryId = Convert.ToInt32(reader["CategoryId"]),
                                CategoryName = reader["CategoryName"]?.ToString(),
                                HospitalId = reader["HospitalId"] != DBNull.Value ? Convert.ToInt32(reader["HospitalId"]) : 0,
                                SubHospitalId = reader["SubHospitalId"] != DBNull.Value ? Convert.ToInt32(reader["SubHospitalId"]) : (int?)null
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllCategories");
                throw;
            }

            return list;
        }

        // ─────────────────────────────────────────
        // GET BY ID
        // ─────────────────────────────────────────
        public CategoryModel GetCategoryById(int categoryId, int hospitalId, int? subHospitalId)
        {
            CategoryModel model = null;

            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("GetCategoryById", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_CategoryId", categoryId);
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model = new CategoryModel
                            {
                                CategoryId = Convert.ToInt32(reader["CategoryId"]),
                                CategoryName = reader["CategoryName"]?.ToString(),
                                HospitalId = reader["HospitalId"] != DBNull.Value ? Convert.ToInt32(reader["HospitalId"]) : 0,
                                SubHospitalId = reader["SubHospitalId"] != DBNull.Value ? Convert.ToInt32(reader["SubHospitalId"]) : (int?)null
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategoryById. CategoryId: {CategoryId}", categoryId);
                throw;
            }

            return model;
        }

        // ─────────────────────────────────────────
        // ADD
        // ─────────────────────────────────────────
        public void AddCategory(CategoryModel model)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("AddCategory", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_CategoryName", model.CategoryName);
                    cmd.Parameters.AddWithValue("p_HospitalId", model.HospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);

                    con.Open();
                    cmd.ExecuteNonQuery();

                    _logger.LogInformation("Category added: {CategoryName}", model.CategoryName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddCategory");
                throw;
            }
        }

        // ─────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────
        public void UpdateCategory(CategoryModel model)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("UpdateCategory", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_CategoryId", model.CategoryId);
                    cmd.Parameters.AddWithValue("p_CategoryName", model.CategoryName);
                    cmd.Parameters.AddWithValue("p_HospitalId", model.HospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);

                    con.Open();
                    cmd.ExecuteNonQuery();

                    _logger.LogInformation("Category updated. CategoryId: {CategoryId}", model.CategoryId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateCategory. CategoryId: {CategoryId}", model.CategoryId);
                throw;
            }
        }

        // ─────────────────────────────────────────
        // DELETE
        // ─────────────────────────────────────────
        public void DeleteCategory(int categoryId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("DeleteCategory", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_CategoryId", categoryId);
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);

                    con.Open();
                    cmd.ExecuteNonQuery();

                    _logger.LogInformation("Category deleted. CategoryId: {CategoryId}", categoryId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteCategory. CategoryId: {CategoryId}", categoryId);
                throw;
            }
        }
    }
}
