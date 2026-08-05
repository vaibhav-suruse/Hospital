using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class RoomService:IRoom
    {
        private readonly string _connectionString;

        public RoomService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // CREATE ROOM
        public int CreateRoom(Room room)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_CreateRoom", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_WardId", room.WardId);
                cmd.Parameters.AddWithValue("p_HospitalId", room.HospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", room.SubHospitalId.HasValue ? room.SubHospitalId.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_RoomNumber", room.RoomNumber);
                cmd.Parameters.AddWithValue("p_RoomType", room.RoomType);
                cmd.Parameters.AddWithValue("p_Description", room.Description);

                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating room: " + ex.Message);
            }
        }

        // UPDATE ROOM
        public int UpdateRoom(Room room)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_UpdateRoom", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_RoomId", room.RoomId);
                cmd.Parameters.AddWithValue("p_HospitalId", room.HospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", room.SubHospitalId.HasValue ? room.SubHospitalId.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_RoomNumber", room.RoomNumber);
                cmd.Parameters.AddWithValue("p_RoomType", room.RoomType);
                cmd.Parameters.AddWithValue("p_Description", room.Description);
                cmd.Parameters.AddWithValue("p_IsActive", room.IsActive ? 1 : 0);


                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating room: " + ex.Message);
            }
        }

        // GET ALL ROOMS BY WARD
        public List<Room> GetAllRooms(int hospitalId, int? subHospitalId, int wardId)
        {
            List<Room> list = new List<Room>();

            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_GetAllRooms", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_WardId", wardId);

                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new Room
                    {
                        RoomId = Convert.ToInt32(dr["RoomId"]),
                        WardId = Convert.ToInt32(dr["WardId"]),
                        RoomNumber = dr["RoomNumber"].ToString(),
                        RoomType = dr["RoomType"].ToString(),
                        IsActive = Convert.ToBoolean(dr["IsActive"])
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching rooms: " + ex.Message);
            }

            return list;
        }

        // GET ROOM BY ID
        public Room GetRoomById(int roomId, int hospitalId, int? subHospitalId)
        {
            Room room = null;

            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_GetRoomById", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_RoomId", roomId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                con.Open();
                using var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    room = new Room
                    {
                        RoomId = Convert.ToInt32(dr["RoomId"]),
                        WardId = Convert.ToInt32(dr["WardId"]),
                        RoomNumber = dr["RoomNumber"].ToString(),
                        RoomType = dr["RoomType"].ToString(),
                        Description = dr["Description"]?.ToString(), // <-- add this
                        IsActive = Convert.ToBoolean(dr["IsActive"])
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching room: " + ex.Message);
            }

            return room;
        }

        // SOFT DELETE ROOM
        public int DeleteRoom(int roomId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_SoftDeleteRoom", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_RoomId", roomId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting room: " + ex.Message);
            }
        }

        public Room GetRoomByNumber(string roomNumber, int wardId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("SELECT * FROM room WHERE RoomNumber = @RoomNumber AND WardId = @WardId AND HospitalId = @HospitalId AND SubHospitalId <=> @SubHospitalId AND IsActive = 1", con);
                cmd.Parameters.AddWithValue("@RoomNumber", roomNumber);
                cmd.Parameters.AddWithValue("@WardId", wardId);
                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("@SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                con.Open();
                using var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    return new Room
                    {
                        RoomId = Convert.ToInt32(dr["RoomId"]),
                        RoomNumber = dr["RoomNumber"].ToString(),
                        WardId = Convert.ToInt32(dr["WardId"]),
                        HospitalId = Convert.ToInt32(dr["HospitalId"]),
                        SubHospitalId = dr["SubHospitalId"] as int?,
                        RoomType = dr["RoomType"].ToString(),
                        Description = dr["Description"]?.ToString(),
                        IsActive = Convert.ToBoolean(dr["IsActive"])
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error checking room number: " + ex.Message);
            }
        }



        public List<Room> GetRoomsByWardId(int wardId, int hospitalId, int? subHospitalId)
        {
            var rooms = new List<Room>();

            using (var conn = new MySqlConnection(_connectionString))
            {
                using (var cmd = new MySqlCommand("sp_GetRoomsByWardId", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_WardId", wardId);
                    cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rooms.Add(new Room
                            {
                                RoomId = Convert.ToInt32(reader["RoomId"]),
                                RoomNumber = reader["RoomNumber"].ToString()
                            });
                        }
                    }
                }
            }

            return rooms;
        }



        public List<RoomListVM> GetRoomsWithBedCount(int hospitalId, int? subHospitalId, int wardId)
        {
            var rooms = new List<RoomListVM>();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    using (var cmd = new MySqlCommand("sp_GetRoomsWithBedCount", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                        cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId);
                        cmd.Parameters.AddWithValue("p_WardId", wardId);

                        conn.Open();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rooms.Add(new RoomListVM
                                {
                                    RoomId = Convert.ToInt32(reader["RoomId"]),
                                    RoomNumber = Convert.ToInt32(reader["RoomNumber"]),
                                    RoomType = reader["RoomType"].ToString(),
                                    WardId = Convert.ToInt32(reader["WardId"]),
                                    WardName = reader["WardName"].ToString(),
                                    FloorNumber = Convert.ToInt32(reader["FloorNumber"]),  // <-- Added
                                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                                    TotalBeds = Convert.ToInt32(reader["TotalBeds"])
                                });

                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw; // Let controller handle error
            }

            return rooms;
        }

        public List<Room> GetAllRoom(int hospitalId, int? subHospitalId)
        {
            List<Room> list = new List<Room>();
            using (var con = new MySqlConnection(_connectionString))
            {
                string query = @"
            SELECT RoomId, WardId, HospitalId, SubHospitalId,RoomNumber,RoomType
            FROM room
            WHERE (@HospitalId IS NULL OR HospitalId = @HospitalId)
              AND (@SubHospitalId IS NULL OR SubHospitalId = @SubHospitalId)";

                using (var cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("@SubHospitalId", subHospitalId ?? (object)DBNull.Value);

                    con.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new Room
                            {
                                RoomId = Convert.ToInt32(dr["RoomId"]),
                                WardId = Convert.ToInt32(dr["WardId"]),
                                HospitalId = Convert.ToInt32(dr["HospitalId"]),
                                SubHospitalId = dr["SubHospitalId"] == DBNull.Value ? null : (int?)Convert.ToInt32(dr["SubHospitalId"]),
                                RoomNumber = dr["RoomNumber"].ToString(),      // <-- Add this
                                RoomType = dr["RoomType"].ToString()
                            });

                        }
                    }
                }
            }
            return list;
        }

    }
}
