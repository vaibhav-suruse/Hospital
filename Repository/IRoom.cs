using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IRoom
    {
        int CreateRoom(Room room);
        int UpdateRoom(Room room);
        Room GetRoomById(int roomId, int hospitalId, int? subHospitalId);
        Room GetRoomByNumber(string roomNumber, int wardId, int hospitalId, int? subHospitalId);
        List<Room> GetAllRooms(int hospitalId, int? subHospitalId, int wardId);
        int DeleteRoom(int roomId, int hospitalId, int? subHospitalId);
        public List<Room> GetRoomsByWardId(int wardId, int hospitalId, int? subHospitalId);
        List<RoomListVM> GetRoomsWithBedCount(int hospitalId, int? subHospitalId, int wardId);
        //only with hospital and sub hospital id, without ward id, for dashboard
        public List<Room> GetAllRoom(int hospitalId, int? subHospitalId);

    }
}
