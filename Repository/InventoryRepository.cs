using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class InventoryRepository:IInventory
    {
        private readonly string _connectionString;
        private readonly ILogger<InventoryRepository> _logger;

        public InventoryRepository(IConfiguration configuration, ILogger<InventoryRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;

        }

        public List<InventoryModel> GetAllInventory(int hospitalId, int? subHospitalId)
        {
            List<InventoryModel> list = new List<InventoryModel>();

            using (MySqlConnection con = new MySqlConnection(_connectionString))
            using (MySqlCommand cmd = new MySqlCommand("GetAllInventory", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);

                con.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new InventoryModel
                        {
                            MedicineId = Convert.ToInt32(reader["MedicineId"]),
                            BatchId = Convert.ToInt32(reader["BatchId"]),
                            MedicineName = reader["MedicineName"]?.ToString(),
                            CategoryName = reader["CategoryName"]?.ToString(),
                            SupplierName = reader["SupplierName"]?.ToString(),
                            Unit = reader["Unit"]?.ToString(),
                            MRP = reader["MRP"] != DBNull.Value ? Convert.ToDecimal(reader["MRP"]) : 0,
                            Stock = reader["Stock"] != DBNull.Value ? Convert.ToInt32(reader["Stock"]) : 0,
                            ReorderLevel = reader["ReorderLevel"] != DBNull.Value ? Convert.ToInt32(reader["ReorderLevel"]) : 0,
                            Status = reader["Status"]?.ToString(),
                            BatchNumber = reader["BatchNumber"]?.ToString(),
                            ExpiryDate = reader["ExpiryDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ExpiryDate"])
                        });
                    }
                }
            }

            return list;
        }

        public void AddInventory(InventoryModel model)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("AddInventory", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("p_MedicineName", model.MedicineName);
                        cmd.Parameters.AddWithValue("p_BatchNumber", model.BatchNumber);
                        cmd.Parameters.AddWithValue("p_ExpiryDate", model.ExpiryDate);
                        cmd.Parameters.AddWithValue("p_Quantity", model.Quantity);
                        cmd.Parameters.AddWithValue("p_ReorderLevel", model.ReorderLevel);
                        cmd.Parameters.AddWithValue("p_CategoryId", model.CategoryId);
                        cmd.Parameters.AddWithValue("p_SupplierId", model.SupplierId);
                        cmd.Parameters.AddWithValue("p_Unit", model.Unit);
                        cmd.Parameters.AddWithValue("p_MRP", model.MRP);
                        cmd.Parameters.AddWithValue("p_SellingPrice", model.SellingPrice);
                        cmd.Parameters.AddWithValue("p_HospitalId", model.HospitalId);

                        cmd.Parameters.AddWithValue("p_SubHospitalId",  model.SubHospitalId.HasValue
                          ? (object)model.SubHospitalId.Value
                          : DBNull.Value);

                        con.Open();
                        cmd.ExecuteNonQuery();

                        _logger.LogInformation("Inventory added successfully for MedicineId: {MedicineId}", model.MedicineId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding inventory for MedicineId: {MedicineId}", model.MedicineId);
                throw;
            }
        }
        public void UpdateInventory(InventoryModel model)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("UpdateInventoryItem", con)) // new SP
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // ✅ Medicine details
                        cmd.Parameters.AddWithValue("p_MedicineId", model.MedicineId);  // ADD THIS
                        cmd.Parameters.AddWithValue("p_MedicineName", model.MedicineName);
                        cmd.Parameters.AddWithValue("p_CategoryId", model.CategoryId);
                        cmd.Parameters.AddWithValue("p_SupplierId", model.SupplierId);
                        cmd.Parameters.AddWithValue("p_Unit", model.Unit);
                        cmd.Parameters.AddWithValue("p_MRP", model.MRP);
                        cmd.Parameters.AddWithValue("p_SellingPrice", model.SellingPrice);

                        // ✅ Batch details
                        cmd.Parameters.AddWithValue("p_BatchId", model.BatchId);
                        cmd.Parameters.AddWithValue("p_BatchNumber", model.BatchNumber);
                        cmd.Parameters.AddWithValue("p_ExpiryDate", model.ExpiryDate);
                        cmd.Parameters.AddWithValue("p_Quantity", model.Quantity);
                        cmd.Parameters.AddWithValue("p_ReorderLevel", model.ReorderLevel);

                        // ✅ Hospital info
                        cmd.Parameters.AddWithValue("p_HospitalId", model.HospitalId);

                        if (model.SubHospitalId.HasValue)
                            cmd.Parameters.AddWithValue("p_SubHospitalId", model.SubHospitalId.Value);
                        else
                            cmd.Parameters.AddWithValue("p_SubHospitalId", DBNull.Value);

                        con.Open();
                        cmd.ExecuteNonQuery();

                        _logger.LogInformation("Inventory updated for BatchId: {BatchId}, MedicineId: {MedicineId}", model.BatchId, model.MedicineId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory for BatchId: {BatchId}, MedicineId: {MedicineId}", model.BatchId, model.MedicineId);
                throw;
            }
        }


        public void DeleteInventory(int batchId, int medicineId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("DeleteInventory", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("p_BatchId", batchId);
                        cmd.Parameters.AddWithValue("p_MedicineId", medicineId);
                        cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);

                        // ✅ C# 8 compatible
                        if (subHospitalId.HasValue)
                            cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.Value);
                        else
                            cmd.Parameters.AddWithValue("p_SubHospitalId", DBNull.Value);

                        con.Open();
                        cmd.ExecuteNonQuery();

                        _logger.LogInformation("Inventory deleted for BatchId: {BatchId}", batchId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting inventory for BatchId: {BatchId}", batchId);
                throw;
            }
        }
        public InventoryModel GetInventoryById(int batchId, int medicineId, int hospitalId, int? subHospitalId)
        {
            InventoryModel model = null;

            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                using (MySqlCommand cmd = new MySqlCommand("GetInventoryById", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_BatchId", batchId);
                    cmd.Parameters.AddWithValue("p_MedicineId", medicineId);
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
  cmd.Parameters.AddWithValue("p_SubHospitalId",
                            subHospitalId.HasValue
                                ? (object)subHospitalId.Value
                                : DBNull.Value);
                    con.Open();
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model = new InventoryModel
                            {
                                MedicineId = reader["MedicineId"] != DBNull.Value ? Convert.ToInt32(reader["MedicineId"]) : 0,
                                MedicineName = reader["MedicineName"]?.ToString(),
                                CategoryId = reader["CategoryId"] != DBNull.Value ? Convert.ToInt32(reader["CategoryId"]) : 0,
                                SupplierId = reader["SupplierId"] != DBNull.Value ? Convert.ToInt32(reader["SupplierId"]) : 0,
                                Unit = reader["Unit"]?.ToString(),
                                MRP = reader["MRP"] != DBNull.Value ? Convert.ToDecimal(reader["MRP"]) : 0,
                                SellingPrice = reader["SellingPrice"] != DBNull.Value ? Convert.ToDecimal(reader["SellingPrice"]) : 0,

                                BatchId = reader["BatchId"] != DBNull.Value ? Convert.ToInt32(reader["BatchId"]) : 0,
                                BatchNumber = reader["BatchNumber"]?.ToString(),
                                ExpiryDate = reader["ExpiryDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ExpiryDate"]),
                                Quantity = reader["Quantity"] != DBNull.Value ? Convert.ToInt32(reader["Quantity"]) : 0,
                                ReorderLevel = reader["ReorderLevel"] != DBNull.Value ? Convert.ToInt32(reader["ReorderLevel"]) : 0,

                                HospitalId = reader["HospitalId"] != DBNull.Value ? Convert.ToInt32(reader["HospitalId"]) : 0,
                                SubHospitalId = reader["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SubHospitalId"])
                            };
                        }
                    }
                }
                _logger.LogInformation("GetInventoryById success for BatchId: {BatchId}, MedicineId: {MedicineId}", batchId, medicineId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInventoryById for BatchId: {BatchId}, MedicineId: {MedicineId}", batchId, medicineId);
                throw;
            }

            return model;
        }
        public List<SupplierModel> GetSuppliers(int hospitalId, int? subHospitalId)
        {
            List<SupplierModel> list = new List<SupplierModel>();

            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("GetAllSuppliers", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                        cmd.Parameters.AddWithValue("p_SubHospitalId",
                            subHospitalId.HasValue
                                ? (object)subHospitalId.Value
                                : DBNull.Value);

                        con.Open();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new SupplierModel
                                {
                                    SupplierId = Convert.ToInt32(reader["SupplierId"]),
                                    SupplierName = reader["SupplierName"].ToString()
                                });
                            }
                        }

                        _logger.LogInformation("Fetched Suppliers. Count: {Count}", list.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching suppliers");
                throw;
            }

            return list;
        }

        // ✅ GET CATEGORIES
        public List<CategoryModel> GetCategories(int hospitalId, int? subHospitalId)
        {
            List<CategoryModel> list = new List<CategoryModel>();

            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("GetAllCategories", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                        cmd.Parameters.AddWithValue("p_SubHospitalId",
                            subHospitalId.HasValue
                                ? (object)subHospitalId.Value
                                : DBNull.Value);

                        con.Open();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new CategoryModel
                                {
                                    CategoryId = Convert.ToInt32(reader["CategoryId"]),
                                    CategoryName = reader["CategoryName"].ToString()
                                });
                            }
                        }

                        _logger.LogInformation("Fetched Categories. Count: {Count}", list.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching categories");
                throw;
            }

            return list;
        }
    }
}
