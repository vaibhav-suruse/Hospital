using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class BedService
    {
        public class BedRepository : IBed
        {
            private readonly string _connectionString;

            public BedRepository(IConfiguration configuration)
            {
                _connectionString = configuration.GetConnectionString("MySqlConnection");
            }
            public List<Bed> GetAllBeds(int hospitalId, int? subHospitalId, int wardId = 0, int roomId = 0)
            {
                var beds = new List<Bed>();

                try
                {
                    using (var conn = new MySqlConnection(_connectionString))
                    {
                        using (var cmd = new MySqlCommand("sp_GetAllBeds", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                            cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId);
                            cmd.Parameters.AddWithValue("p_WardId", wardId);
                            cmd.Parameters.AddWithValue("p_RoomId", roomId);

                            conn.Open();

                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    beds.Add(new Bed
                                    {
                                        BedId = Convert.ToInt32(reader["BedId"]),
                                        WardId = Convert.ToInt32(reader["WardId"]),
                                        WardName = reader["WardName"].ToString(),
                                        Floor = Convert.ToInt32(reader["Floor"]),
                                        RoomId = Convert.ToInt32(reader["RoomId"]),
                                        RoomNumber = reader["RoomNumber"].ToString(),
                                        BedNumber = reader["BedNumber"].ToString(),
                                        OperationalStatus = reader["OperationalStatus"].ToString(),
                                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                                        ChargesPerDay = reader["ChargesPerDay"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["ChargesPerDay"]),
                                        HospitalId = Convert.ToInt32(reader["HospitalId"]),
                                        SubHospitalId = reader["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SubHospitalId"])
                                    });
                                }
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Database error occurred while fetching beds.", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred while fetching beds.", ex);
                }

                return beds;
            }


            public Bed GetBedById(int bedId, int hospitalId, int? subHospitalId)
            {
                Bed bed = null;

                try
                {
                    using (var conn = new MySqlConnection(_connectionString))
                    {
                        using (var cmd = new MySqlCommand("sp_GetBedById", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("p_BedId", bedId);
                            cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                            cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId);

                            conn.Open();

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    bed = new Bed
                                    {
                                        BedId = Convert.ToInt32(reader["BedId"]),
                                        WardId = Convert.ToInt32(reader["WardId"]),
                                        RoomId = Convert.ToInt32(reader["RoomId"]),
                                        BedNumber = reader["BedNumber"].ToString(),
                                        OperationalStatus = reader["OperationalStatus"].ToString(),
                                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                                        HospitalId = Convert.ToInt32(reader["HospitalId"]),
                                        SubHospitalId = reader["SubHospitalId"] == DBNull.Value
                                            ? (int?)null
                                            : Convert.ToInt32(reader["SubHospitalId"])
                                    };
                                }
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Database error occurred while fetching bed details.", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred while fetching bed details.", ex);
                }

                return bed;
            }

            public void CreateBed(Bed bed)
            {
                try
                {
                    using (var conn = new MySqlConnection(_connectionString))
                    {
                        using (var cmd = new MySqlCommand("sp_CreateBed", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("p_WardId", bed.WardId);
                            cmd.Parameters.AddWithValue("p_RoomId", bed.RoomId);
                            cmd.Parameters.AddWithValue("p_HospitalId", bed.HospitalId);
                            cmd.Parameters.AddWithValue("p_SubHospitalId", bed.SubHospitalId);
                            cmd.Parameters.AddWithValue("p_BedNumber", bed.BedNumber);
                            cmd.Parameters.AddWithValue("p_ChargesPerDay", bed.ChargesPerDay);

                            conn.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1062)
                        throw new Exception("This bed number already exists in this room.", ex);

                    throw new Exception("Database error occurred while creating bed.", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred while creating bed.", ex);
                }
            }

            public void UpdateBed(Bed bed)
            {
                try
                {
                    using (var conn = new MySqlConnection(_connectionString))
                    {
                        using (var cmd = new MySqlCommand("sp_UpdateBed", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("p_BedId", bed.BedId);
                            cmd.Parameters.AddWithValue("p_HospitalId", bed.HospitalId);
                            cmd.Parameters.AddWithValue("p_SubHospitalId", bed.SubHospitalId);
                            cmd.Parameters.AddWithValue("p_BedNumber", bed.BedNumber);
                            cmd.Parameters.AddWithValue("p_ChargesPerDay", bed.ChargesPerDay); // NEW
                            cmd.Parameters.AddWithValue("p_OperationalStatus", bed.OperationalStatus);

                            conn.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1062)
                        throw new Exception("This bed number already exists in this room.", ex);

                    throw new Exception("Database error occurred while updating bed.", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred while updating bed.", ex);
                }
            }

            public void DeleteBed(int bedId, int hospitalId, int? subHospitalId)
            {
                try
                {
                    using (var conn = new MySqlConnection(_connectionString))
                    {
                        using (var cmd = new MySqlCommand("sp_SoftDeleteBed", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("p_BedId", bedId);
                            cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                            cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId);

                            conn.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Database error occurred while deleting bed.", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred while deleting bed.", ex);
                }
            }

            public Bed GetActiveBedByNumber(string bedNumber, int wardId, int roomId, int hospitalId, int? subHospitalId, int excludeBedId = 0)
            {
                var beds = GetAllBeds(hospitalId, subHospitalId, wardId, roomId);
                return beds
                    .Where(b => b.IsActive)
                    .Where(b => b.BedNumber.Equals(bedNumber, StringComparison.OrdinalIgnoreCase))
                    .Where(b => b.BedId != excludeBedId)
                    .FirstOrDefault();
            }

            public List<Bed> GetAllBed(int hospitalId, int? subHospitalId)
            {
                List<Bed> beds = new List<Bed>();

                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("SP_GetAllBeds_ByHospital", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_HospitalId", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospitalId",
                            subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);

                        con.Open();

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                beds.Add(new Bed
                                {
                                    BedId = Convert.ToInt32(reader["BedId"]),
                                    WardId = Convert.ToInt32(reader["WardId"]),
                                    RoomId = Convert.ToInt32(reader["RoomId"]),
                                    BedNumber = reader["BedNumber"].ToString(),
                                    OperationalStatus = reader["OperationalStatus"].ToString(),
                                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                                    HospitalId = Convert.ToInt32(reader["HospitalId"]),
                                    SubHospitalId = reader["SubHospitalId"] == DBNull.Value
                                                        ? (int?)null
                                                        : Convert.ToInt32(reader["SubHospitalId"]),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                    UpdatedDate = reader["UpdatedDate"] == DBNull.Value
                                                        ? (DateTime?)null
                                                        : Convert.ToDateTime(reader["UpdatedDate"]),
                                    Floor = Convert.ToInt32(reader["Floor"]),
                                    ChargesPerDay = Convert.ToDecimal(reader["ChargesPerDay"])
                                });
                            }
                        }
                    }
                }

                return beds;
            }
        }
    }
}
