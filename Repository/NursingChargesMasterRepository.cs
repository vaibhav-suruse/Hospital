using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class NursingChargesMasterRepository : INursingChargesMaster
    {
        private readonly string _connectionString;
        private readonly ILogger<NursingChargesMasterRepository> _logger;

        public NursingChargesMasterRepository(
            IConfiguration configuration,
            ILogger<NursingChargesMasterRepository> logger)
        {
            _connectionString =
                configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        public List<NursingChargesMaster> GetAll(
            int hospitalId, int? subHospitalId)
        {
            var list = new List<NursingChargesMaster>();
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    SELECT * FROM nursingchargesmaster
                    WHERE ParentHospitalId = @hid
                      AND (@subid IS NULL OR SubHospitalId = @subid)
                      AND IsActive = 1
                    ORDER BY ChargeName", conn))
                {
                    cmd.Parameters.AddWithValue("@hid", hospitalId);
                    cmd.Parameters.AddWithValue("@subid",
                        subHospitalId ?? (object)DBNull.Value);
                    conn.Open();
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) list.Add(MapNursing(r));
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in GetAll NursingChargesMaster.");
                throw new Exception(
                    "Error fetching nursing charges master.", ex);
            }
            return list;
        }

        public NursingChargesMaster GetById(int id)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    SELECT * FROM nursingchargesmaster
                    WHERE NursingId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) return MapNursing(r);
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in GetById NursingChargesMaster. Id={Id}", id);
                throw new Exception(
                    "Error fetching nursing charge by id.", ex);
            }
            return null;
        }

        public void Create(NursingChargesMaster model,
            int hospitalId, int? subHospitalId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    INSERT INTO nursingchargesmaster
                        (ParentHospitalId, SubHospitalId,
                         ChargeName, ChargeType, DefaultCharge)
                    VALUES
                        (@hid, @subid,
                         @name, @type, @charge)", conn))
                {
                    cmd.Parameters.AddWithValue("@hid", hospitalId);
                    cmd.Parameters.AddWithValue("@subid",
                        subHospitalId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@name", model.ChargeName);
                    cmd.Parameters.AddWithValue("@type", model.ChargeType);
                    cmd.Parameters.AddWithValue("@charge", model.DefaultCharge);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in Create NursingChargesMaster. Name={Name}",
                    model.ChargeName);
                throw new Exception(
                    "Error creating nursing charge master.", ex);
            }
        }

        public int Update(NursingChargesMaster model)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    UPDATE nursingchargesmaster
                    SET ChargeName    = @name,
                        ChargeType    = @type,
                        DefaultCharge = @charge,
                        IsActive      = @active
                    WHERE NursingId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", model.NursingId);
                    cmd.Parameters.AddWithValue("@name", model.ChargeName);
                    cmd.Parameters.AddWithValue("@type", model.ChargeType);
                    cmd.Parameters.AddWithValue("@charge", model.DefaultCharge);
                    cmd.Parameters.AddWithValue("@active", model.IsActive ? 1 : 0);
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in Update NursingChargesMaster. Id={Id}",
                    model.NursingId);
                throw new Exception(
                    "Error updating nursing charge master.", ex);
            }
        }

        public void Delete(int id)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    UPDATE nursingchargesmaster
                    SET IsActive = 0
                    WHERE NursingId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in Delete NursingChargesMaster. Id={Id}", id);
                throw new Exception(
                    "Error deleting nursing charge master.", ex);
            }
        }

        private NursingChargesMaster MapNursing(MySqlDataReader r) =>
            new NursingChargesMaster
            {
                NursingId = Convert.ToInt32(r["NursingId"]),
                ChargeName = r["ChargeName"].ToString(),
                ChargeType = r["ChargeType"].ToString(),
                DefaultCharge = Convert.ToDecimal(r["DefaultCharge"]),
                IsActive = Convert.ToBoolean(r["IsActive"])
            };
    }
}