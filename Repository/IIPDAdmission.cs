using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IIPDAdmission
    {
        int AddIPDAdmission(IPDAdmissionModel model, int hospitalId, int? subHospitalId);

        List<IPDAdmissionModel> GetAllIPDAdmissions(int hospitalId, int? subHospitalId);

        IPDAdmissionModel GetIPDAdmissionById(int ipdId, int hospitalId, int? subHospitalId);

       // List<IPDAdmissionWithBedVM> GetAll_IPDAdmissions(int hospitalId, int? subHospitalId);

        //IPDAdmissionModel GetIPDAdmissionById(int ipdId, int hospitalId, int? subHospitalId);

        int UpdateIPDAdmission(IPDAdmissionModel model, int hospitalId, int? subHospitalId);

        int DeleteIPDAdmission(int ipdId, int hospitalId, int? subHospitalId);





    }
}
