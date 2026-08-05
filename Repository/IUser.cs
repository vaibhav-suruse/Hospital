using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IUser
    {
        List<User> GetAllUsers();
        User GetUserById(int id);
        void InsertUser(User user);
        void UpdateUser(User user);
        void DeleteUser(int id);
        DataTable GetSubHospitals(int mainHospitalId);
       // User LoginUser(string loginName, string password, int? mainHospitalId, int? subHospitalId);
        string GetUserRoleByUsername(string loginName);
        User LoginUser(string loginName, string password, int? mainHospitalId, int? subHospitalId);
    }
}