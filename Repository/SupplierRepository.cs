using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class SupplierRepository : ISupplier
    {
        private readonly string _connectionString;
        private readonly ILogger<SupplierRepository> _logger;

        public SupplierRepository(IConfiguration configuration, ILogger<SupplierRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        // ─────────────────────────────────────────
        // GET ALL
        // ─────────────────────────────────────────
        public List<SupplierModel> GetAllSuppliers(int hospitalId, int? subHospitalId)
        {
            var list = new List<SupplierModel>();

            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("GetAllStoreSuppliers", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new SupplierModel
                            {
                                SupplierId = Convert.ToInt32(reader["SupplierId"]),
                                SupplierName = reader["SupplierName"]?.ToString(),
                                ContactNo = reader["ContactNo"]?.ToString(),
                                Address = reader["Address"]?.ToString(),
                                HospitalId = reader["HospitalId"] != DBNull.Value ? Convert.ToInt32(reader["HospitalId"]) : 0,
                                SubHospitalId = reader["SubHospitalId"] != DBNull.Value ? Convert.ToInt32(reader["SubHospitalId"]) : (int?)null
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllSuppliers");
                throw;
            }

            return list;
        }

        // ─────────────────────────────────────────
        // GET BY ID
        // ─────────────────────────────────────────
        public SupplierModel GetSupplierById(int supplierId, int hospitalId, int? subHospitalId)
        {
            SupplierModel model = null;

            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("GetStoreSupplierById", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_SupplierId", supplierId);
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model = new SupplierModel
                            {
                                SupplierId = Convert.ToInt32(reader["SupplierId"]),
                                SupplierName = reader["SupplierName"]?.ToString(),
                                ContactNo = reader["ContactNo"]?.ToString(),
                                Address = reader["Address"]?.ToString(),
                                HospitalId = reader["HospitalId"] != DBNull.Value ? Convert.ToInt32(reader["HospitalId"]) : 0,
                                SubHospitalId = reader["SubHospitalId"] != DBNull.Value ? Convert.ToInt32(reader["SubHospitalId"]) : (int?)null
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSupplierById. SupplierId: {SupplierId}", supplierId);
                throw;
            }

            return model;
        }

        // ─────────────────────────────────────────
        // ADD
        // ─────────────────────────────────────────
        public void AddSupplier(SupplierModel model)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("AddStoreSupplier", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_SupplierName", model.SupplierName);
                    cmd.Parameters.AddWithValue("p_ContactNo", model.ContactNo);
                    cmd.Parameters.AddWithValue("p_Address", model.Address);
                    cmd.Parameters.AddWithValue("p_HospitalId", model.HospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);

                    con.Open();
                    cmd.ExecuteNonQuery();

                    _logger.LogInformation("Supplier added: {SupplierName}", model.SupplierName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddSupplier");
                throw;
            }
        }

        // ─────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────
        public void UpdateSupplier(SupplierModel model)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("UpdateStoreSupplier", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_SupplierId", model.SupplierId);
                    cmd.Parameters.AddWithValue("p_SupplierName", model.SupplierName);
                    cmd.Parameters.AddWithValue("p_ContactNo", model.ContactNo);
                    cmd.Parameters.AddWithValue("p_Address", model.Address);
                    cmd.Parameters.AddWithValue("p_HospitalId", model.HospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);

                    con.Open();
                    cmd.ExecuteNonQuery();

                    _logger.LogInformation("Supplier updated. SupplierId: {SupplierId}", model.SupplierId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateSupplier. SupplierId: {SupplierId}", model.SupplierId);
                throw;
            }
        }

        // ─────────────────────────────────────────
        // DELETE
        // ─────────────────────────────────────────
        public void DeleteSupplier(int supplierId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("DeleteStoreSupplier", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_SupplierId", supplierId);
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? 0);

                    con.Open();
                    cmd.ExecuteNonQuery();

                    _logger.LogInformation("Supplier deleted. SupplierId: {SupplierId}", supplierId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteSupplier. SupplierId: {SupplierId}", supplierId);
                throw;
            }
        }
    }
}
