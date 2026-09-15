using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class IPDBedAllocationRepository : IIPDBedAllocation
    {
        private readonly string _connectionString;
        private readonly ILogger<IPDBedAllocationRepository> _logger;

        public IPDBedAllocationRepository(IConfiguration configuration,
            ILogger<IPDBedAllocationRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        public void AllocateOrTransferBed(int ipdId, int newBedId, int allocatedBy)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                using (MySqlCommand cmd = new MySqlCommand("sp_AllocateOrTransferBed", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                    cmd.Parameters.AddWithValue("p_NewBedId", newBedId);
                    cmd.Parameters.AddWithValue("p_AllocatedBy", allocatedBy);



                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AllocateOrTransferBed. IPDId: {IPDId}, BedId: {BedId}", ipdId, newBedId);
                throw;
            }
        }

        public void UpdateBedAllocation(IPDBedAllocationModel model)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                using (MySqlCommand cmd = new MySqlCommand("sp_UpdateBedAllocation", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@p_AllocationId", model.AllocationId);
                    cmd.Parameters.AddWithValue("@p_IPDId", model.IPDId);
                    cmd.Parameters.AddWithValue("@p_BedId", model.BedId);
                    cmd.Parameters.AddWithValue("@p_StartDateTime", model.StartDateTime);
                    cmd.Parameters.AddWithValue("@p_EndDateTime", model.EndDateTime);
                    cmd.Parameters.AddWithValue("@p_IsCurrent", model.IsCurrent);
                    cmd.Parameters.AddWithValue("@p_AllocatedBy", model.AllocatedBy);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateBedAllocation. AllocationId: {AllocationId}", model.AllocationId);
                throw;
            }
        }

        public void DeleteBedAllocation(int allocationId)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                using (MySqlCommand cmd = new MySqlCommand("sp_DeleteBedAllocation", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@p_AllocationId", allocationId);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteBedAllocation. AllocationId: {AllocationId}", allocationId);
                throw;
            }
        }

        public IPDBedAllocationModel GetById(int allocationId)
        {
            try
            {
                IPDBedAllocationModel model = null;

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                using (MySqlCommand cmd = new MySqlCommand("sp_GetBedAllocationById", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@p_AllocationId", allocationId);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model = new IPDBedAllocationModel
                            {
                                AllocationId = Convert.ToInt32(reader["AllocationId"]),
                                IPDId = Convert.ToInt32(reader["IPDId"]),
                                BedId = Convert.ToInt32(reader["BedId"]),
                                StartDateTime = Convert.ToDateTime(reader["StartDateTime"]),
                                EndDateTime = reader["EndDateTime"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["EndDateTime"]),
                                IsCurrent = Convert.ToBoolean(reader["IsCurrent"]),
                                AllocatedBy = Convert.ToInt32(reader["AllocatedBy"]),
                                Days = Convert.ToInt32(reader["Days"])
                            };
                        }
                    }
                }

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetById. AllocationId: {AllocationId}", allocationId);
                throw;
            }
        }

        public List<IPDBedAllocationModel> GetAll()
        {
            try
            {
                List<IPDBedAllocationModel> list = new List<IPDBedAllocationModel>();

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                using (MySqlCommand cmd = new MySqlCommand("sp_GetAllBedAllocations", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new IPDBedAllocationModel
                            {
                                AllocationId = Convert.ToInt32(reader["AllocationId"]),
                                IPDId = Convert.ToInt32(reader["IPDId"]),
                                BedId = Convert.ToInt32(reader["BedId"]),
                                StartDateTime = Convert.ToDateTime(reader["StartDateTime"]),
                                EndDateTime = reader["EndDateTime"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["EndDateTime"]),
                                IsCurrent = Convert.ToBoolean(reader["IsCurrent"]),
                                AllocatedBy = Convert.ToInt32(reader["AllocatedBy"]),
                                Days = Convert.ToInt32(reader["Days"])
                            });
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll Bed Allocations");
                throw;
            }
        }

        public void AllocateBed(int ipdId, int newBedId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    using (var cmd = new MySqlCommand("sp_AllocateBedByIPD", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@p_IPDId", ipdId);
                        cmd.Parameters.AddWithValue("@p_NewBedId", newBedId);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error allocating bed for IPDId {IPDId}", ipdId);
                throw;
            }
        }

        public void UpdateBedStatus(int bedId, string status)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_UpdateBedStatus", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_BedId", bedId);
                cmd.Parameters.AddWithValue("@p_NewStatus", status);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bed status for BedId {BedId}", bedId);
                throw;
            }
        }


        public void ShiftBed(int ipdId, int newBedId, int allocatedBy, string reason, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_ShiftBed", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                    cmd.Parameters.AddWithValue("p_NewBedId", newBedId);
                    cmd.Parameters.AddWithValue("p_AllocatedBy", allocatedBy);
                    cmd.Parameters.AddWithValue("p_Reason", reason ?? "");
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : 0);

                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(successParam);
                    cmd.Parameters.Add(messageParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    if (!Convert.ToBoolean(successParam.Value))
                        throw new Exception(messageParam.Value?.ToString() ?? "Bed shift failed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShiftBed. IPDId: {IPDId}", ipdId);
                throw;
            }
        }

        public BedShiftVM GetCurrentBedInfo(int ipdId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    string query = @"
SELECT 
                    ia.IPDId,
                    ia.AdmissionNumber,
                    ia.Status,
                    p.Id AS PatientId,
                    CONCAT(p.FirstName,' ',p.LastName) AS PatientName,
                    b.BedId AS CurrentBedId,
                    b.BedNumber AS CurrentBedNumber,
                    w.WardName AS CurrentWardName,
                    r.RoomNumber AS CurrentRoomNumber
                FROM ipdadmission ia
                INNER JOIN tbl_patient p ON ia.PatientId = p.Id
                LEFT JOIN ipdbedallocation ba ON ia.IPDId = ba.IPDId AND ba.IsCurrent = 1
                LEFT JOIN bed b ON ba.BedId = b.BedId
                LEFT JOIN ward w ON b.WardId = w.WardId
                LEFT JOIN room r ON b.RoomId = r.RoomId
                WHERE ia.IPDId = @ipdId
                  AND ia.ParentHospitalId = @hospitalId
                  AND (@subHospitalId = 0 OR ia.SubHospitalId = @subHospitalId OR ia.SubHospitalId IS NULL)";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ipdId", ipdId);
                        cmd.Parameters.AddWithValue("@hospitalId", hospitalId);
                        cmd.Parameters.AddWithValue("@subHospitalId", subHospitalId.HasValue ? subHospitalId.Value : 0);
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
return new BedShiftVM
                                {
                                    IPDId = Convert.ToInt32(reader["IPDId"]),
                                    AdmissionNumber = reader["AdmissionNumber"].ToString(),
                                    Status = reader["Status"]?.ToString(),
                                    PatientId = Convert.ToInt32(reader["PatientId"]),
                                    PatientName = reader["PatientName"].ToString(),
                                    CurrentBedId = reader["CurrentBedId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CurrentBedId"]),
                                    CurrentBedNumber = reader["CurrentBedNumber"] == DBNull.Value ? "N/A" : reader["CurrentBedNumber"].ToString(),
                                    CurrentWardName = reader["CurrentWardName"] == DBNull.Value ? "N/A" : reader["CurrentWardName"].ToString(),
                                    CurrentRoomNumber = reader["CurrentRoomNumber"] == DBNull.Value ? "N/A" : reader["CurrentRoomNumber"].ToString()
                                };
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCurrentBedInfo. IPDId: {IPDId}", ipdId);
                throw;
            }
        }


    }
}
