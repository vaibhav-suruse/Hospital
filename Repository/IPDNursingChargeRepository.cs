using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class IPDNursingChargeRepository : IIPDNursingCharge
    {
        private readonly string _connectionString;
        private readonly ILogger<IPDNursingChargeRepository> _logger;

        public IPDNursingChargeRepository(
            IConfiguration configuration,
            ILogger<IPDNursingChargeRepository> logger)
        {
            _connectionString =
                configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        // ── GET BY IPD ID ────────────────────────────────────────────────
        public List<IPDNursingCharge> GetByIPDId(int ipdId)
        {
            var list = new List<IPDNursingCharge>();
            try
            {
                _logger.LogInformation(
                    "GetByIPDId NursingCharges called. IPDId={IPDId}",
                    ipdId);

                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    SELECT
                        nc.Id,
                        nc.IPDId,
                        nc.ParentHospitalId,
                        nc.SubHospitalId,
                        nc.VitalsId,
                        nc.NursingMasterId,
                        nc.ChargeName,
                        nc.ChargeType,
                        nc.ChargeDate,
                        nc.Quantity,
                        nc.UnitCharge,
                        nc.TotalCharge,
                        nc.NurseId,
                        nc.Notes,
                        nc.IsActive,
                        CONCAT(
                            IFNULL(n.FirstName,''),
                            ' ',
                            IFNULL(n.LastName,'')
                        ) AS NurseName
                    FROM ipd_nursing_charges nc
                    LEFT JOIN nurse n
                           ON nc.NurseId = n.NurseId
                    WHERE nc.IPDId    = @ipdId
                      AND nc.IsActive = 1
                    ORDER BY nc.ChargeDate DESC,
                             nc.CreatedDate DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@ipdId", ipdId);
                    conn.Open();

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(MapCharge(r));
                        }
                    }
                }

                _logger.LogInformation(
                    "GetByIPDId NursingCharges found {Count} records.",
                    list.Count);
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in GetByIPDId NursingCharges. IPDId={IPDId}",
                    ipdId);
                throw new Exception(
                    "Error fetching nursing charges.", ex);
            }
            return list;
        }

        // ── SAVE CHARGES IN TRANSACTION ──────────────────────────────────
        public void SaveCharges(List<IPDNursingCharge> charges)
        {
            if (charges == null || charges.Count == 0)
            {
                _logger.LogInformation(
                    "SaveCharges called with empty list, skipping.");
                return;
            }

            // ── VALIDATION before hitting DB ────────────────────────────
            foreach (var c in charges)
            {
                if (c.IPDId <= 0)
                    throw new Exception(
                        "IPDId is required for nursing charge.");
                if (c.ParentHospitalId <= 0)
                    throw new Exception(
                        "ParentHospitalId is required for nursing charge.");
                if (string.IsNullOrWhiteSpace(c.ChargeName))
                    throw new Exception(
                        "ChargeName is required for nursing charge.");
                if (c.UnitCharge <= 0)
                    throw new Exception(
                        $"UnitCharge must be > 0 for '{c.ChargeName}'.");
                // Auto-calculate TotalCharge if not set
                if (c.TotalCharge <= 0)
                    c.TotalCharge = c.Quantity * c.UnitCharge;
                // Default ChargeType
                if (string.IsNullOrWhiteSpace(c.ChargeType))
                    c.ChargeType = "PerProcedure";
                // Default Quantity
                if (c.Quantity <= 0)
                    c.Quantity = 1;
            }

            try
            {
                _logger.LogInformation(
                    "SaveCharges NursingCharges called. Count={Count}",
                    charges.Count);

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (var c in charges)
                            {
                                using (var cmd = new MySqlCommand(@"
                                    INSERT INTO ipd_nursing_charges
                                    (
                                        IPDId,
                                        ParentHospitalId,
                                        SubHospitalId,
                                        VitalsId,
                                        NursingMasterId,
                                        ChargeName,
                                        ChargeType,
                                        ChargeDate,
                                        Quantity,
                                        UnitCharge,
                                        TotalCharge,
                                        NurseId,
                                        Notes,
                                        IsActive,
                                        CreatedDate
                                    )
                                    VALUES
                                    (
                                        @ipdId,
                                        @hid,
                                        @subid,
                                        @vitalsId,
                                        @masterId,
                                        @name,
                                        @type,
                                        @date,
                                        @qty,
                                        @unit,
                                        @total,
                                        @nurseId,
                                        @notes,
                                        1,
                                        NOW()
                                    )",
                                    conn, tran))
                                {
                                    cmd.Parameters.AddWithValue(
                                        "@ipdId", c.IPDId);
                                    cmd.Parameters.AddWithValue(
                                        "@hid", c.ParentHospitalId);
                                    cmd.Parameters.AddWithValue(
                                        "@subid",
                                        c.SubHospitalId.HasValue
                                            ? (object)c.SubHospitalId.Value
                                            : DBNull.Value);
                                    cmd.Parameters.AddWithValue(
                                        "@vitalsId",
                                        c.VitalsId.HasValue
                                            ? (object)c.VitalsId.Value
                                            : DBNull.Value);
                                    cmd.Parameters.AddWithValue(
                                        "@masterId",
                                        c.NursingMasterId.HasValue
                                            ? (object)c.NursingMasterId.Value
                                            : DBNull.Value);
                                    cmd.Parameters.AddWithValue(
                                        "@name", c.ChargeName);
                                    cmd.Parameters.AddWithValue(
                                        "@type", c.ChargeType);
                                    cmd.Parameters.AddWithValue(
                                        "@date", c.ChargeDate.Date);
                                    cmd.Parameters.AddWithValue(
                                        "@qty", c.Quantity);
                                    cmd.Parameters.AddWithValue(
                                        "@unit", c.UnitCharge);
                                    cmd.Parameters.AddWithValue(
                                        "@total", c.TotalCharge);
                                    cmd.Parameters.AddWithValue(
                                        "@nurseId",
                                        c.NurseId.HasValue
                                            ? (object)c.NurseId.Value
                                            : DBNull.Value);
                                    cmd.Parameters.AddWithValue(
                                        "@notes",
                                        !string.IsNullOrWhiteSpace(c.Notes)
                                            ? (object)c.Notes
                                            : DBNull.Value);

                                    int rows = cmd.ExecuteNonQuery();
                                    _logger.LogInformation(
                                        "Inserted nursing charge: {Name}, " +
                                        "IPDId={IPDId}, Rows={Rows}",
                                        c.ChargeName, c.IPDId, rows);
                                }
                            }

                            tran.Commit();
                            _logger.LogInformation(
                                "SaveCharges committed. Count={Count}",
                                charges.Count);
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in SaveCharges NursingCharges.");
                throw new Exception(
                    "Error saving nursing charges.", ex);
            }
        }

        // ── SOFT DELETE ──────────────────────────────────────────────────
        public void Delete(int id)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    UPDATE ipd_nursing_charges
                    SET IsActive = 0
                    WHERE Id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in Delete NursingCharge. Id={Id}", id);
                throw new Exception(
                    "Error deleting nursing charge.", ex);
            }
        }

        // ── GET TOTAL ────────────────────────────────────────────────────
        public decimal GetTotalByIPDId(int ipdId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    SELECT IFNULL(SUM(TotalCharge), 0)
                    FROM   ipd_nursing_charges
                    WHERE  IPDId    = @ipdId
                      AND  IsActive = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@ipdId", ipdId);
                    conn.Open();
                    return Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in GetTotalByIPDId. IPDId={IPDId}", ipdId);
                throw new Exception(
                    "Error calculating nursing total.", ex);
            }
        }

        // ── MAP ──────────────────────────────────────────────────────────
        private IPDNursingCharge MapCharge(MySqlDataReader r)
        {
            return new IPDNursingCharge
            {
                Id = Convert.ToInt32(r["Id"]),
                IPDId = Convert.ToInt32(r["IPDId"]),
                ParentHospitalId =
                    Convert.ToInt32(r["ParentHospitalId"]),
                SubHospitalId =
                    r["SubHospitalId"] == DBNull.Value
                    ? (int?)null
                    : Convert.ToInt32(r["SubHospitalId"]),
                VitalsId =
                    r["VitalsId"] == DBNull.Value
                    ? (int?)null
                    : Convert.ToInt32(r["VitalsId"]),
                NursingMasterId =
                    r["NursingMasterId"] == DBNull.Value
                    ? (int?)null
                    : Convert.ToInt32(r["NursingMasterId"]),
                ChargeName = r["ChargeName"].ToString(),
                ChargeType = r["ChargeType"].ToString(),
                ChargeDate = Convert.ToDateTime(r["ChargeDate"]),
                Quantity = Convert.ToDecimal(r["Quantity"]),
                UnitCharge = Convert.ToDecimal(r["UnitCharge"]),
                TotalCharge = Convert.ToDecimal(r["TotalCharge"]),
                NurseId =
                    r["NurseId"] == DBNull.Value
                    ? (int?)null
                    : Convert.ToInt32(r["NurseId"]),
                NurseName = r["NurseName"]?.ToString()?.Trim(),
                Notes = r["Notes"]?.ToString()
            };
        }
    }
}