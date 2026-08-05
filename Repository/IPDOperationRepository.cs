using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class IPDOperationRepository : IIPDOperation
    {
        private readonly string _connectionString;
        private readonly ILogger<IPDOperationRepository> _logger;

        public IPDOperationRepository(
            IConfiguration configuration,
            ILogger<IPDOperationRepository> logger)
        {
            _connectionString =
                configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        // ── GET ALL OPERATIONS FOR ONE IPD ──────────────────────────────
        public List<IPDOperationModel> GetByIPDId(int ipdId)
        {
            var list = new List<IPDOperationModel>();
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    SELECT
                        io.*,
                        om.OperationName,
                        CONCAT(ds.FirstName,' ',ds.LastName)   AS SurgeonName,
                        CONCAT(da.FirstName,' ',da.LastName)   AS AnesthesistName
                    FROM ipdoperations io
                    INNER JOIN operationchargesmaster om
                            ON io.OperationId = om.OperationId
                    LEFT  JOIN doctor ds
                            ON io.SurgeonId     = ds.Doctor_Id
                    LEFT  JOIN doctor da
                            ON io.AnesthesistId = da.Doctor_Id
                    WHERE io.IPDId    = @ipdId
                      AND io.IsActive = 1
                    ORDER BY io.OperationDate DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@ipdId", ipdId);
                    conn.Open();
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(MapOperation(r));
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in GetByIPDId Operations. IPDId={IPDId}",
                    ipdId);
                throw new Exception(
                    "Error fetching operations for IPD.", ex);
            }
            return list;
        }

        // ── SAVE OPERATION + STAFF IN ONE TRANSACTION ───────────────────
        public int SaveAndReturnId(IPDOperationModel model,
            int hospitalId, int? subHospitalId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            // Step 1 — Save operation header
                            int operationId;
                            using (var cmd = new MySqlCommand(@"
                                INSERT INTO ipdoperations
                                    (IPDId, ParentHospitalId, SubHospitalId,
                                     OperationId, OperationDate,
                                     SurgeonId, AnesthesistId,
                                     ActualCharge, AnesthesiaCharge,
                                     SurgeonCharge, OTCharge, Notes)
                                VALUES
                                    (@ipdId, @hid, @subid,
                                     @opId, @opDate,
                                     @surgId, @anesId,
                                     @actual, @anes,
                                     @surg, @ot, @notes);
                                SELECT LAST_INSERT_ID();", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@ipdId",
                                    model.IPDId);
                                cmd.Parameters.AddWithValue("@hid",
                                    hospitalId);
                                cmd.Parameters.AddWithValue("@subid",
                                    subHospitalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@opId",
                                    model.OperationId);
                                cmd.Parameters.AddWithValue("@opDate",
                                    model.OperationDate);
                                cmd.Parameters.AddWithValue("@surgId",
                                    model.SurgeonId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@anesId",
                                    model.AnesthesistId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@actual",
                                    model.ActualCharge);
                                cmd.Parameters.AddWithValue("@anes",
                                    model.AnesthesiaCharge);
                                cmd.Parameters.AddWithValue("@surg",
                                    model.SurgeonCharge);
                                cmd.Parameters.AddWithValue("@ot",
                                    model.OTCharge);
                                cmd.Parameters.AddWithValue("@notes",
                                    model.Notes ?? (object)DBNull.Value);
                                operationId =
                                    Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            // Step 2 — Save all staff
                            if (model.Staff != null && model.Staff.Count > 0)
                            {
                                foreach (var s in model.Staff)
                                {
                                    using (var cmd = new MySqlCommand(@"
                                        INSERT INTO ipdoperation_staff
                                            (IPDOperationId, StaffType,
                                             DoctorId, NurseId,
                                             StaffName, Charge)
                                        VALUES
                                            (@opId, @type,
                                             @docId, @nurseId,
                                             @name, @charge)",
                                        conn, tran))
                                    {
                                        cmd.Parameters.AddWithValue("@opId",
                                            operationId);
                                        cmd.Parameters.AddWithValue("@type",
                                            s.StaffType);
                                        cmd.Parameters.AddWithValue("@docId",
                                            s.DoctorId ?? (object)DBNull.Value);
                                        cmd.Parameters.AddWithValue("@nurseId",
                                            s.NurseId ?? (object)DBNull.Value);
                                        cmd.Parameters.AddWithValue("@name",
                                            s.StaffName ?? (object)DBNull.Value);
                                        cmd.Parameters.AddWithValue("@charge",
                                            s.Charge);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            tran.Commit();
                            return operationId;
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
                    "DB error in SaveAndReturnId Operation. IPDId={IPDId}",
                    model.IPDId);
                throw new Exception(
                    "Error saving operation.", ex);
            }
        }

        // ── SAVE STAFF SEPARATELY (if needed later) ─────────────────────
        public void SaveStaff(List<IPDOperationStaff> staffList)
        {
            if (staffList == null || staffList.Count == 0) return;
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (var s in staffList)
                            {
                                using (var cmd = new MySqlCommand(@"
                                    INSERT INTO ipdoperation_staff
                                        (IPDOperationId, StaffType,
                                         DoctorId, NurseId,
                                         StaffName, Charge)
                                    VALUES
                                        (@opId, @type,
                                         @docId, @nurseId,
                                         @name, @charge)",
                                    conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@opId",
                                        s.IPDOperationId);
                                    cmd.Parameters.AddWithValue("@type",
                                        s.StaffType);
                                    cmd.Parameters.AddWithValue("@docId",
                                        s.DoctorId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@nurseId",
                                        s.NurseId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@name",
                                        s.StaffName ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@charge",
                                        s.Charge);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            tran.Commit();
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
                    "DB error in SaveStaff.");
                throw new Exception("Error saving operation staff.", ex);
            }
        }

        // ── GET STAFF FOR ONE OPERATION ─────────────────────────────────
        public List<IPDOperationStaff> GetStaffByOperationId(int ipdOperationId)
        {
            var list = new List<IPDOperationStaff>();
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    SELECT
                        s.*,
                        CONCAT(d.FirstName,' ',d.LastName)
                            AS DoctorName,
                        CONCAT(n.FirstName,' ',IFNULL(n.LastName,''))
                            AS NurseName
                    FROM ipdoperation_staff s
                    LEFT JOIN doctor d ON s.DoctorId = d.Doctor_Id
                    LEFT JOIN nurse  n ON s.NurseId  = n.NurseId
                    WHERE s.IPDOperationId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", ipdOperationId);
                    conn.Open();
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new IPDOperationStaff
                            {
                                Id = Convert.ToInt32(r["Id"]),
                                IPDOperationId =
                                    Convert.ToInt32(r["IPDOperationId"]),
                                StaffType = r["StaffType"].ToString(),
                                DoctorId = r["DoctorId"] == DBNull.Value
                                    ? (int?)null
                                    : Convert.ToInt32(r["DoctorId"]),
                                NurseId = r["NurseId"] == DBNull.Value
                                    ? (int?)null
                                    : Convert.ToInt32(r["NurseId"]),
                                StaffName =
                                    r["DoctorId"] != DBNull.Value
                                        ? r["DoctorName"]?.ToString()
                                    : r["NurseId"] != DBNull.Value
                                        ? r["NurseName"]?.ToString()
                                    : r["StaffName"]?.ToString(),
                                Charge =
                                    Convert.ToDecimal(r["Charge"])
                            });
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in GetStaffByOperationId. Id={Id}",
                    ipdOperationId);
                throw new Exception(
                    "Error fetching operation staff.", ex);
            }
            return list;
        }

        // ── SOFT DELETE OPERATION + ITS STAFF ───────────────────────────
        public void Delete(int ipdOperationId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            // Delete staff first
                            using (var cmd = new MySqlCommand(@"
                                DELETE FROM ipdoperation_staff
                                WHERE IPDOperationId = @id",
                                conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@id",
                                    ipdOperationId);
                                cmd.ExecuteNonQuery();
                            }
                            // Soft delete operation
                            using (var cmd = new MySqlCommand(@"
                                UPDATE ipdoperations
                                SET IsActive = 0
                                WHERE IPDOperationId = @id",
                                conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@id",
                                    ipdOperationId);
                                cmd.ExecuteNonQuery();
                            }
                            tran.Commit();
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
                    "DB error in Delete Operation. Id={Id}",
                    ipdOperationId);
                throw new Exception("Error deleting operation.", ex);
            }
        }

        // ── GET TOTAL COST FOR IPD (used in billing) ────────────────────
        public decimal GetTotalByIPDId(int ipdId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    SELECT IFNULL(SUM(
                        io.ActualCharge + io.AnesthesiaCharge +
                        io.SurgeonCharge + io.OTCharge +
                        IFNULL((
                            SELECT SUM(s.Charge)
                            FROM ipdoperation_staff s
                            WHERE s.IPDOperationId = io.IPDOperationId
                        ), 0)
                    ), 0)
                    FROM ipdoperations io
                    WHERE io.IPDId    = @ipdId
                      AND io.IsActive = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@ipdId", ipdId);
                    conn.Open();
                    return Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "DB error in GetTotalByIPDId Operations. IPDId={IPDId}",
                    ipdId);
                throw new Exception(
                    "Error calculating operation total.", ex);
            }
        }

        private IPDOperationModel MapOperation(MySqlDataReader r) =>
            new IPDOperationModel
            {
                IPDOperationId = Convert.ToInt32(r["IPDOperationId"]),
                IPDId = Convert.ToInt32(r["IPDId"]),
                OperationId = Convert.ToInt32(r["OperationId"]),
                OperationName = r["OperationName"].ToString(),
                OperationDate = Convert.ToDateTime(r["OperationDate"]),
                SurgeonId = r["SurgeonId"] == DBNull.Value
                    ? (int?)null : Convert.ToInt32(r["SurgeonId"]),
                SurgeonName = r["SurgeonName"]?.ToString(),
                AnesthesistId = r["AnesthesistId"] == DBNull.Value
                    ? (int?)null : Convert.ToInt32(r["AnesthesistId"]),
                AnesthesistName = r["AnesthesistName"]?.ToString(),
                ActualCharge = Convert.ToDecimal(r["ActualCharge"]),
                AnesthesiaCharge = Convert.ToDecimal(r["AnesthesiaCharge"]),
                SurgeonCharge = Convert.ToDecimal(r["SurgeonCharge"]),
                OTCharge = Convert.ToDecimal(r["OTCharge"]),
                Notes = r["Notes"]?.ToString(),
                Staff = new List<IPDOperationStaff>()
            };
    }
}