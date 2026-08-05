using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class OperationMasterRepository : IOperationMaster
    {
        private readonly string _connectionString;
        private readonly ILogger<OperationMasterRepository> _logger;

        public OperationMasterRepository(
            IConfiguration configuration,
            ILogger<OperationMasterRepository> logger)
        {
            _connectionString =
                configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        public List<OperationMaster> GetAll(int hospitalId, int? subHospitalId)
        {
            var list = new List<OperationMaster>();
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    SELECT * FROM operationchargesmaster
                    WHERE ParentHospitalId = @hid
                      AND (@subid IS NULL OR SubHospitalId = @subid)
                      AND IsActive = 1
                    ORDER BY OperationName", conn))
                {
                    cmd.Parameters.AddWithValue("@hid", hospitalId);
                    cmd.Parameters.AddWithValue("@subid",
                        subHospitalId ?? (object)DBNull.Value);
                    conn.Open();
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapOp(r));
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in GetAll OperationMaster. HospitalId={HospitalId}",
                    hospitalId);
                throw new Exception(
                    "Error fetching operation masters.", ex);
            }
            return list;
        }

        public OperationMaster GetById(int id)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    SELECT * FROM operationchargesmaster
                    WHERE OperationId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) return MapOp(r);
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in GetById OperationMaster. Id={Id}", id);
                throw new Exception(
                    "Error fetching operation master by id.", ex);
            }
            return null;
        }

        public void Create(OperationMaster model,
            int hospitalId, int? subHospitalId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    INSERT INTO operationchargesmaster
                        (ParentHospitalId, SubHospitalId,
                         OperationName, OperationCode, Category,
                         DefaultCharge, AnesthesiaCharge,
                         SurgeonCharge, OTCharge)
                    VALUES
                        (@hid, @subid,
                         @name, @code, @cat,
                         @def, @anes,
                         @surg, @ot)", conn))
                {
                    cmd.Parameters.AddWithValue("@hid", hospitalId);
                    cmd.Parameters.AddWithValue("@subid",
                        subHospitalId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@name", model.OperationName);
                    cmd.Parameters.AddWithValue("@code",
                        model.OperationCode ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@cat",
                        model.Category ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@def", model.DefaultCharge);
                    cmd.Parameters.AddWithValue("@anes", model.AnesthesiaCharge);
                    cmd.Parameters.AddWithValue("@surg", model.SurgeonCharge);
                    cmd.Parameters.AddWithValue("@ot", model.OTCharge);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in Create OperationMaster. Name={Name}",
                    model.OperationName);
                throw new Exception(
                    "Error creating operation master.", ex);
            }
        }

        public int Update(OperationMaster model)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    UPDATE operationchargesmaster
                    SET OperationName    = @name,
                        OperationCode    = @code,
                        Category         = @cat,
                        DefaultCharge    = @def,
                        AnesthesiaCharge = @anes,
                        SurgeonCharge    = @surg,
                        OTCharge         = @ot,
                        IsActive         = @active
                    WHERE OperationId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", model.OperationId);
                    cmd.Parameters.AddWithValue("@name", model.OperationName);
                    cmd.Parameters.AddWithValue("@code",
                        model.OperationCode ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@cat",
                        model.Category ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@def", model.DefaultCharge);
                    cmd.Parameters.AddWithValue("@anes", model.AnesthesiaCharge);
                    cmd.Parameters.AddWithValue("@surg", model.SurgeonCharge);
                    cmd.Parameters.AddWithValue("@ot", model.OTCharge);
                    cmd.Parameters.AddWithValue("@active",
                        model.IsActive ? 1 : 0);
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in Update OperationMaster. Id={Id}",
                    model.OperationId);
                throw new Exception(
                    "Error updating operation master.", ex);
            }
        }

        public void Delete(int id)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    UPDATE operationchargesmaster
                    SET IsActive = 0
                    WHERE OperationId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in Delete OperationMaster. Id={Id}", id);
                throw new Exception(
                    "Error deleting operation master.", ex);
            }
        }

        private OperationMaster MapOp(MySqlDataReader r) =>
            new OperationMaster
            {
                OperationId = Convert.ToInt32(r["OperationId"]),
                OperationName = r["OperationName"].ToString(),
                OperationCode = r["OperationCode"]?.ToString(),
                Category = r["Category"]?.ToString(),
                DefaultCharge = Convert.ToDecimal(r["DefaultCharge"]),
                AnesthesiaCharge = Convert.ToDecimal(r["AnesthesiaCharge"]),
                SurgeonCharge = Convert.ToDecimal(r["SurgeonCharge"]),
                OTCharge = Convert.ToDecimal(r["OTCharge"]),
                IsActive = Convert.ToBoolean(r["IsActive"])
            };
    }
}