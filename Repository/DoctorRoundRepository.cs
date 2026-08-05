// Repository/DoctorRoundRepository.cs
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class DoctorRoundRepository : IDoctorRound
    {
        private readonly string _connectionString;

        public DoctorRoundRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // ===============================
        // GET ALL ROUNDS BY IPD
        // ===============================
        public List<IPDDoctorRound> GetRoundsByIPD(int ipdId, int parentHospitalId)
        {
            var list = new List<IPDDoctorRound>();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetDoctorRoundsByIPD", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                    cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(MapRound(reader));
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while fetching doctor rounds.", ex);
            }

            return list;
        }

        // ===============================
        // GET SINGLE ROUND DETAIL
        // ===============================
        public IPDDoctorRound GetRoundDetail(int roundId)
        {
            IPDDoctorRound round = null;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetDoctorRoundDetail", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_RoundId", roundId);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        // Result Set 1 - Round Info
                        if (reader.Read())
                        {
                            round = MapRound(reader);
                        }

                        if (round == null) return null;

                        // Result Set 2 - Symptoms
                        reader.NextResult();
                        while (reader.Read())
                        {
                            round.Symptoms.Add(new IPDRoundSymptom
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                SymptomId = Convert.ToInt32(reader["SymptomId"]),
                                SymptomName = reader["SymptomName"]?.ToString(),
                                SubName = reader["SubName"]?.ToString()
                            });
                        }

                        // Result Set 3 - Prescriptions
                        reader.NextResult();
                        while (reader.Read())
                        {
                            round.Prescriptions.Add(new IPDRoundPrescription
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                MedicineId = Convert.ToInt32(reader["MedicineId"]),
                                MedicineName = reader["MedicineName"]?.ToString(),
                                MedicineType = reader["Type"]?.ToString(),
                                Morning = reader["Morning"] != DBNull.Value && Convert.ToInt32(reader["Morning"]) == 1,
                                Afternoon = reader["Afternoon"] != DBNull.Value && Convert.ToInt32(reader["Afternoon"]) == 1,
                                Evening = reader["Evening"] != DBNull.Value && Convert.ToInt32(reader["Evening"]) == 1,
                                Days = reader["Days"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Days"]),
                                Route = reader["Route"]?.ToString(),
                                Dosage = reader["Dosage"]?.ToString(),
                                Instructions = reader["Instructions"]?.ToString(),
                                Status = reader["Status"]?.ToString()
                            });
                        }

                        // Result Set 4 - Investigations
                        reader.NextResult();
                        while (reader.Read())
                        {
                            round.Investigations.Add(new IPDRoundInvestigation
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                InvestigationType = reader["InvestigationType"]?.ToString(),
                                TestName = reader["TestName"]?.ToString(),
                                Priority = reader["Priority"]?.ToString(),
                                Status = reader["Status"]?.ToString(),
                                Instructions = reader["Instructions"]?.ToString(),
                                OrderedDateTime = Convert.ToDateTime(reader["OrderedDateTime"]),
                                Result = reader["Result"]?.ToString(),
                                ResultFilePath = reader["ResultFilePath"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while fetching round detail.", ex);
            }

            return round;
        }

        // ===============================
        // INSERT ROUND → returns RoundId
        // ===============================
        public int CreateRound(IPDDoctorRound model)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_InsertDoctorRound", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_ParentHospitalId", model.ParentHospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
                    cmd.Parameters.AddWithValue("p_DoctorId", model.DoctorId);
                    cmd.Parameters.AddWithValue("p_RoundType", model.RoundType);
                    cmd.Parameters.AddWithValue("p_RoundDateTime", model.RoundDateTime);
                    cmd.Parameters.AddWithValue("p_PatientCondition", model.PatientCondition ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p_Diagnosis", model.Diagnosis ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p_Instructions", model.Instructions ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p_Notes", model.Notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p_IsAbnormal", model.IsAbnormal);

                    var roundIdParam = new MySqlParameter("p_RoundId", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };

                    cmd.Parameters.Add(roundIdParam);
                    cmd.Parameters.Add(successParam);
                    cmd.Parameters.Add(messageParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    bool success = Convert.ToBoolean(successParam.Value);
                    string message = messageParam.Value?.ToString();

                    if (!success)
                        throw new Exception(message);

                    return Convert.ToInt32(roundIdParam.Value);
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while inserting doctor round.", ex);
            }
        }

        // ===============================
        // INSERT SYMPTOM
        // ===============================
        public void InsertSymptom(IPDRoundSymptom model)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_InsertRoundSymptoms", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_RoundId", model.RoundId);
                    cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
                    cmd.Parameters.AddWithValue("p_ParentHospitalId", model.ParentHospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_SymptomId", model.SymptomId);

                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(successParam);
                    cmd.Parameters.Add(messageParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    bool success = Convert.ToBoolean(successParam.Value);
                    if (!success)
                        throw new Exception(messageParam.Value?.ToString());
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while inserting symptom.", ex);
            }
        }

        // ===============================
        // INSERT PRESCRIPTION
        // ===============================
        public void InsertPrescription(IPDRoundPrescription model)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_InsertRoundPrescription", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_RoundId", model.RoundId);
                    cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
                    cmd.Parameters.AddWithValue("p_ParentHospitalId", model.ParentHospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_MedicineId", model.MedicineId);
                    cmd.Parameters.AddWithValue("p_Morning", model.Morning);
                    cmd.Parameters.AddWithValue("p_Afternoon", model.Afternoon);
                    cmd.Parameters.AddWithValue("p_Evening", model.Evening);
                    cmd.Parameters.AddWithValue("p_Days", model.Days.HasValue ? (object)model.Days.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_Route", model.Route ?? "Oral");
                    cmd.Parameters.AddWithValue("p_Dosage", model.Dosage ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("p_Instructions", model.Instructions ?? (object)DBNull.Value);

                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(successParam);
                    cmd.Parameters.Add(messageParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    bool success = Convert.ToBoolean(successParam.Value);
                    if (!success)
                        throw new Exception(messageParam.Value?.ToString());
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while inserting prescription.", ex);
            }
        }

        // ===============================
        // INSERT INVESTIGATION
        // ===============================
        public void InsertInvestigation(IPDRoundInvestigation model)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_InsertRoundInvestigation", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_RoundId", model.RoundId);
                    cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
                    cmd.Parameters.AddWithValue("p_ParentHospitalId", model.ParentHospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_InvestigationType", model.InvestigationType);
                    cmd.Parameters.AddWithValue("p_TestName", model.TestName);
                    cmd.Parameters.AddWithValue("p_Priority", model.Priority ?? "Routine");
                    cmd.Parameters.AddWithValue("p_Instructions", model.Instructions ?? (object)DBNull.Value);

                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(successParam);
                    cmd.Parameters.Add(messageParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    bool success = Convert.ToBoolean(successParam.Value);
                    if (!success)
                        throw new Exception(messageParam.Value?.ToString());
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while inserting investigation.", ex);
            }
        }

        // ===============================
        // SOFT DELETE ROUND
        // ===============================
        public void DeleteRound(int roundId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_DeleteDoctorRound", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_RoundId", roundId);

                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(successParam);
                    cmd.Parameters.Add(messageParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    bool success = Convert.ToBoolean(successParam.Value);
                    if (!success)
                        throw new Exception(messageParam.Value?.ToString());
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while deleting round.", ex);
            }
        }

        // ===============================
        // PRINT — single round
        // ===============================
        public IPDPrescriptionVM GetRoundPrescriptionPrint(int roundId)
        {
            IPDPrescriptionVM vm = null;
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetRoundPrescriptionPrint", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_RoundId", roundId);
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            vm = MapPrescriptionVM(reader);
                            vm.Round = MapRoundPrintVM(reader);
                        }

                        if (vm == null) return null;

                        reader.NextResult();
                        while (reader.Read())
                        {
                            vm.Round.Symptoms.Add(new IPDSymptomPrintVM
                            {
                                SymptomName = reader["SymptomName"]?.ToString(),
                                SubName = reader["SubName"]?.ToString()
                            });
                        }

                        reader.NextResult();
                        while (reader.Read())
                        {
                            vm.Round.Medicines.Add(new IPDMedicinePrintVM
                            {
                                MedicineName = reader["MedicineName"]?.ToString(),
                                MedicineType = reader["MedicineType"]?.ToString(),
                                Morning = reader["Morning"] != DBNull.Value && Convert.ToInt32(reader["Morning"]) == 1,
                                Afternoon = reader["Afternoon"] != DBNull.Value && Convert.ToInt32(reader["Afternoon"]) == 1,
                                Evening = reader["Evening"] != DBNull.Value && Convert.ToInt32(reader["Evening"]) == 1,
                                Days = reader["Days"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Days"]),
                                Route = reader["Route"]?.ToString(),
                                Dosage = reader["Dosage"]?.ToString(),
                                MedicineInstructions = reader["MedicineInstructions"]?.ToString(),
                                Status = reader["Status"]?.ToString()
                            });
                        }

                        reader.NextResult();
                        while (reader.Read())
                        {
                            vm.Round.Investigations.Add(new IPDInvestigationPrintVM
                            {
                                InvestigationType = reader["InvestigationType"]?.ToString(),
                                TestName = reader["TestName"]?.ToString(),
                                Priority = reader["Priority"]?.ToString(),
                                Status = reader["Status"]?.ToString(),
                                InvInstructions = reader["InvInstructions"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Error fetching round prescription.", ex);
            }
            return vm;
        }

        // ===============================
        // PRINT — all rounds
        // ===============================
        public IPDPrescriptionVM GetAllRoundsPrescriptionPrint(int ipdId)
        {
            IPDPrescriptionVM vm = null;
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetAllRoundsPrescriptionPrint", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        vm = new IPDPrescriptionVM();
                        vm.Rounds = new List<IPDRoundPrintVM>();
                        bool headerSet = false;

                        while (reader.Read())
                        {
                            if (!headerSet)
                            {
                                MapPrescriptionVMFromReader(vm, reader);
                                headerSet = true;
                            }
                            vm.Rounds.Add(MapRoundPrintVM(reader));
                        }

                        if (!headerSet) return null;

                        reader.NextResult();
                        while (reader.Read())
                        {
                            int roundId = Convert.ToInt32(reader["RoundId"]);
                            var round = vm.Rounds.Find(r => r.RoundId == roundId);
                            round?.Symptoms.Add(new IPDSymptomPrintVM
                            {
                                SymptomName = reader["SymptomName"]?.ToString(),
                                SubName = reader["SubName"]?.ToString()
                            });
                        }

                        reader.NextResult();
                        while (reader.Read())
                        {
                            int roundId = Convert.ToInt32(reader["RoundId"]);
                            var round = vm.Rounds.Find(r => r.RoundId == roundId);
                            round?.Medicines.Add(new IPDMedicinePrintVM
                            {
                                MedicineName = reader["MedicineName"]?.ToString(),
                                MedicineType = reader["MedicineType"]?.ToString(),
                                Morning = reader["Morning"] != DBNull.Value && Convert.ToInt32(reader["Morning"]) == 1,
                                Afternoon = reader["Afternoon"] != DBNull.Value && Convert.ToInt32(reader["Afternoon"]) == 1,
                                Evening = reader["Evening"] != DBNull.Value && Convert.ToInt32(reader["Evening"]) == 1,
                                Days = reader["Days"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Days"]),
                                Route = reader["Route"]?.ToString(),
                                Dosage = reader["Dosage"]?.ToString(),
                                MedicineInstructions = reader["MedicineInstructions"]?.ToString(),
                                Status = reader["Status"]?.ToString()
                            });
                        }

                        reader.NextResult();
                        while (reader.Read())
                        {
                            int roundId = Convert.ToInt32(reader["RoundId"]);
                            var round = vm.Rounds.Find(r => r.RoundId == roundId);
                            round?.Investigations.Add(new IPDInvestigationPrintVM
                            {
                                InvestigationType = reader["InvestigationType"]?.ToString(),
                                TestName = reader["TestName"]?.ToString(),
                                Priority = reader["Priority"]?.ToString(),
                                Status = reader["Status"]?.ToString(),
                                InvInstructions = reader["InvInstructions"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Error fetching all rounds prescription.", ex);
            }
            return vm;
        }

        // ===============================
        // NEW — INSERT IPD PHARMACY NOTIFICATION
        // Called after prescriptions are saved in DoctorRoundController
        // Reuses the same InsertMedicineNotification stored proc as OPD
        // ===============================
        //public void InsertIPDPharmacyNotification(MedicineNotificationModel model)
        //{
        //    try
        //    {
        //        using (var conn = new MySqlConnection(_connectionString))
        //        using (var cmd = new MySqlCommand("InsertMedicineNotification", conn))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("p_PatientId", model.PatientId);
        //            cmd.Parameters.AddWithValue("p_PatientName", model.PatientName ?? "");
        //            cmd.Parameters.AddWithValue("p_OPDId", DBNull.Value);                              // NULL for IPD
        //            cmd.Parameters.AddWithValue("p_AppointmentId", DBNull.Value);                              // NULL for IPD
        //            cmd.Parameters.AddWithValue("p_IPDId", (object)model.IPDId ?? DBNull.Value);
        //            cmd.Parameters.AddWithValue("p_RoundId", (object)model.RoundId ?? DBNull.Value);
        //            cmd.Parameters.AddWithValue("p_DoctorName", model.DoctorName ?? "");
        //            cmd.Parameters.AddWithValue("p_MedicineCount", model.MedicineCount);
        //            cmd.Parameters.AddWithValue("p_MedicinesSummary", model.MedicinesSummary ?? "");
        //            cmd.Parameters.AddWithValue("p_Type", "IPD");
        //            cmd.Parameters.AddWithValue("p_HospitalId", model.HospitalId);
        //            cmd.Parameters.AddWithValue("p_SubHospitalId",
        //                model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);

        //            conn.Open();
        //            cmd.ExecuteNonQuery();
        //        }
        //    }
        //    catch { /* non-blocking — round save must not fail because of notification */ }
        //}


        public void InsertIPDPharmacyNotification(MedicineNotificationModel model)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("InsertIPDMedicineNotification", conn)) // ← changed
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_PatientId", model.PatientId);
                    cmd.Parameters.AddWithValue("p_PatientName", model.PatientName ?? "");
                    cmd.Parameters.AddWithValue("p_IPDId", (object)model.IPDId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("p_RoundId", (object)model.RoundId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("p_DoctorName", model.DoctorName ?? "");
                    cmd.Parameters.AddWithValue("p_MedicineCount", model.MedicineCount);
                    cmd.Parameters.AddWithValue("p_MedicinesSummary", model.MedicinesSummary ?? "");
                    cmd.Parameters.AddWithValue("p_HospitalId", model.HospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId",
                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* non-blocking */ }
        }

        // ===============================
        // MAPPER HELPERS
        // ===============================
        private IPDDoctorRound MapRound(MySqlDataReader reader)
        {
            return new IPDDoctorRound
            {
                RoundId = Convert.ToInt32(reader["RoundId"]),
                RoundType = reader["RoundType"]?.ToString(),
                RoundDateTime = Convert.ToDateTime(reader["RoundDateTime"]),
                PatientCondition = reader["PatientCondition"]?.ToString(),
                Diagnosis = reader["Diagnosis"]?.ToString(),
                Instructions = reader["Instructions"]?.ToString(),
                Notes = reader["Notes"]?.ToString(),
                IsAbnormal = Convert.ToBoolean(reader["IsAbnormal"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                DoctorName = reader["DoctorName"]?.ToString(),
                Specialization = reader["Specialization"]?.ToString()
            };
        }

        private IPDPrescriptionVM MapPrescriptionVM(MySqlDataReader reader)
        {
            var vm = new IPDPrescriptionVM();
            MapPrescriptionVMFromReader(vm, reader);
            return vm;
        }

        private void MapPrescriptionVMFromReader(IPDPrescriptionVM vm, MySqlDataReader reader)
        {
            vm.HospitalName = reader["HospitalName"]?.ToString();
            vm.HospitalAddress = reader["HospitalAddress"]?.ToString();
            vm.HospitalPhone = reader["HospitalPhone"]?.ToString();
            vm.HospitalEmail = reader["HospitalEmail"]?.ToString();
            vm.HospitalLogo = reader["HospitalLogo"]?.ToString();
            vm.HospitalRegNo = reader["HospitalRegNo"]?.ToString();
            vm.PatientName = reader["PatientName"]?.ToString();
            vm.Age = reader["Age"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Age"]);
            vm.Gender = reader["Gender"]?.ToString();
            vm.PatientPhone = reader["PatientPhone"]?.ToString();
            vm.AdmissionNumber = reader["AdmissionNumber"]?.ToString();
            vm.AdmissionDateTime = Convert.ToDateTime(reader["AdmissionDateTime"]);
            vm.BedNumber = reader["BedNumber"]?.ToString();
            vm.WardName = reader["WardName"]?.ToString();
            vm.RoomNumber = reader["RoomNumber"]?.ToString();
        }

        private IPDRoundPrintVM MapRoundPrintVM(MySqlDataReader reader)
        {
            return new IPDRoundPrintVM
            {
                RoundId = Convert.ToInt32(reader["RoundId"]),
                RoundType = reader["RoundType"]?.ToString(),
                RoundDateTime = Convert.ToDateTime(reader["RoundDateTime"]),
                DoctorName = reader["DoctorName"]?.ToString(),
                Specialization = reader["Specialization"]?.ToString(),
                Education = reader["Education"]?.ToString(),
                PatientCondition = reader["PatientCondition"]?.ToString(),
                Diagnosis = reader["Diagnosis"]?.ToString(),
                Instructions = reader["Instructions"]?.ToString(),
                Notes = reader["Notes"]?.ToString()
            };
        }


    }
}


//// Repository/DoctorRoundRepository.cs
//using Microsoft.Extensions.Configuration;
//using MySql.Data.MySqlClient;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using WebApplicationSampleTest2.Models;

//namespace WebApplicationSampleTest2.Repository
//{
//    public class DoctorRoundRepository : IDoctorRound
//    {
//        private readonly string _connectionString;

//        public DoctorRoundRepository(IConfiguration configuration)
//        {
//            _connectionString = configuration.GetConnectionString("MySqlConnection");
//        }

//        // ===============================
//        // GET ALL ROUNDS BY IPD
//        // ===============================
//        public List<IPDDoctorRound> GetRoundsByIPD(int ipdId, int parentHospitalId)
//        {
//            var list = new List<IPDDoctorRound>();

//            try
//            {
//                using (var conn = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("sp_GetDoctorRoundsByIPD", conn))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("p_IPDId", ipdId);
//                    cmd.Parameters.AddWithValue("p_ParentHospitalId", parentHospitalId);

//                    conn.Open();

//                    using (var reader = cmd.ExecuteReader())
//                    {
//                        while (reader.Read())
//                        {
//                            list.Add(MapRound(reader));
//                        }
//                    }
//                }
//            }
//            catch (MySqlException ex)
//            {
//                throw new Exception("Database error while fetching doctor rounds.", ex);
//            }

//            return list;
//        }

//        // ===============================
//        // GET SINGLE ROUND DETAIL
//        // ===============================
//        public IPDDoctorRound GetRoundDetail(int roundId)
//        {
//            IPDDoctorRound round = null;

//            try
//            {
//                using (var conn = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("sp_GetDoctorRoundDetail", conn))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("p_RoundId", roundId);

//                    conn.Open();

//                    using (var reader = cmd.ExecuteReader())
//                    {
//                        // Result Set 1 - Round Info
//                        if (reader.Read())
//                        {
//                            round = MapRound(reader);
//                        }

//                        if (round == null) return null;

//                        // Result Set 2 - Symptoms
//                        reader.NextResult();
//                        while (reader.Read())
//                        {
//                            round.Symptoms.Add(new IPDRoundSymptom
//                            {
//                                Id = Convert.ToInt32(reader["Id"]),
//                                SymptomId = Convert.ToInt32(reader["SymptomId"]),
//                                SymptomName = reader["SymptomName"]?.ToString(),
//                                SubName = reader["SubName"]?.ToString()
//                            });
//                        }

//                        // Result Set 3 - Prescriptions
//                        reader.NextResult();
//                        while (reader.Read())
//                        {
//                            round.Prescriptions.Add(new IPDRoundPrescription
//                            {
//                                Id = Convert.ToInt32(reader["Id"]),
//                                MedicineId = Convert.ToInt32(reader["MedicineId"]),
//                                MedicineName = reader["MedicineName"]?.ToString(),
//                                MedicineType = reader["Type"]?.ToString(),
//                                //Morning = Convert.ToBoolean(reader["Morning"]),
//                                //Afternoon = Convert.ToBoolean(reader["Afternoon"]),
//                                Morning = reader["Morning"] != DBNull.Value && Convert.ToInt32(reader["Morning"]) == 1,
//                                Afternoon = reader["Afternoon"] != DBNull.Value && Convert.ToInt32(reader["Afternoon"]) == 1,
//                                Evening=reader["Evening"] != DBNull.Value && Convert.ToInt32(reader["Evening"]) == 1,
//                                Days = reader["Days"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Days"]),
//                                Route = reader["Route"]?.ToString(),
//                                Dosage = reader["Dosage"]?.ToString(),
//                                Instructions = reader["Instructions"]?.ToString(),
//                                Status = reader["Status"]?.ToString()
//                            });
//                        }

//                        // Result Set 4 - Investigations
//                        reader.NextResult();
//                        while (reader.Read())
//                        {
//                            round.Investigations.Add(new IPDRoundInvestigation
//                            {
//                                Id = Convert.ToInt32(reader["Id"]),
//                                InvestigationType = reader["InvestigationType"]?.ToString(),
//                                TestName = reader["TestName"]?.ToString(),
//                                Priority = reader["Priority"]?.ToString(),
//                                Status = reader["Status"]?.ToString(),
//                                Instructions = reader["Instructions"]?.ToString(),
//                                OrderedDateTime = Convert.ToDateTime(reader["OrderedDateTime"]),
//                                Result = reader["Result"]?.ToString(),
//                                ResultFilePath = reader["ResultFilePath"]?.ToString()
//                            });
//                        }
//                    }
//                }
//            }
//            catch (MySqlException ex)
//            {
//                throw new Exception("Database error while fetching round detail.", ex);
//            }

//            return round;
//        }

//        // ===============================
//        // INSERT ROUND → returns RoundId
//        // ===============================
//        public int CreateRound(IPDDoctorRound model)
//        {
//            try
//            {
//                using (var conn = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("sp_InsertDoctorRound", conn))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;

//                    cmd.Parameters.AddWithValue("p_ParentHospitalId", model.ParentHospitalId);
//                    cmd.Parameters.AddWithValue("p_SubHospitalId",
//                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);
//                    cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
//                    cmd.Parameters.AddWithValue("p_DoctorId", model.DoctorId);
//                    cmd.Parameters.AddWithValue("p_RoundType", model.RoundType);
//                    cmd.Parameters.AddWithValue("p_RoundDateTime", model.RoundDateTime);
//                    cmd.Parameters.AddWithValue("p_PatientCondition", model.PatientCondition ?? (object)DBNull.Value);
//                    cmd.Parameters.AddWithValue("p_Diagnosis", model.Diagnosis ?? (object)DBNull.Value);
//                    cmd.Parameters.AddWithValue("p_Instructions", model.Instructions ?? (object)DBNull.Value);
//                    cmd.Parameters.AddWithValue("p_Notes", model.Notes ?? (object)DBNull.Value);
//                    cmd.Parameters.AddWithValue("p_IsAbnormal", model.IsAbnormal);

//                    // OUT params
//                    var roundIdParam = new MySqlParameter("p_RoundId", MySqlDbType.Int32)
//                    { Direction = ParameterDirection.Output };
//                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte)
//                    { Direction = ParameterDirection.Output };
//                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255)
//                    { Direction = ParameterDirection.Output };

//                    cmd.Parameters.Add(roundIdParam);
//                    cmd.Parameters.Add(successParam);
//                    cmd.Parameters.Add(messageParam);

//                    conn.Open();
//                    cmd.ExecuteNonQuery();

//                    bool success = Convert.ToBoolean(successParam.Value);
//                    string message = messageParam.Value?.ToString();

//                    if (!success)
//                        throw new Exception(message);

//                    return Convert.ToInt32(roundIdParam.Value);
//                }
//            }
//            catch (MySqlException ex)
//            {
//                throw new Exception("Database error while inserting doctor round.", ex);
//            }
//        }

//        // ===============================
//        // INSERT SYMPTOM
//        // ===============================
//        public void InsertSymptom(IPDRoundSymptom model)
//        {
//            try
//            {
//                using (var conn = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("sp_InsertRoundSymptoms", conn))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;

//                    cmd.Parameters.AddWithValue("p_RoundId", model.RoundId);
//                    cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
//                    cmd.Parameters.AddWithValue("p_ParentHospitalId", model.ParentHospitalId);
//                    cmd.Parameters.AddWithValue("p_SubHospitalId",
//                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);
//                    cmd.Parameters.AddWithValue("p_SymptomId", model.SymptomId);

//                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte)
//                    { Direction = ParameterDirection.Output };
//                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255)
//                    { Direction = ParameterDirection.Output };

//                    cmd.Parameters.Add(successParam);
//                    cmd.Parameters.Add(messageParam);

//                    conn.Open();
//                    cmd.ExecuteNonQuery();

//                    bool success = Convert.ToBoolean(successParam.Value);
//                    if (!success)
//                        throw new Exception(messageParam.Value?.ToString());
//                }
//            }
//            catch (MySqlException ex)
//            {
//                throw new Exception("Database error while inserting symptom.", ex);
//            }
//        }

//        // ===============================
//        // INSERT PRESCRIPTION
//        // ===============================
//        public void InsertPrescription(IPDRoundPrescription model)
//        {
//            try
//            {
//                using (var conn = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("sp_InsertRoundPrescription", conn))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;

//                    cmd.Parameters.AddWithValue("p_RoundId", model.RoundId);
//                    cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
//                    cmd.Parameters.AddWithValue("p_ParentHospitalId", model.ParentHospitalId);
//                    cmd.Parameters.AddWithValue("p_SubHospitalId",
//                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);
//                    cmd.Parameters.AddWithValue("p_MedicineId", model.MedicineId);
//                    cmd.Parameters.AddWithValue("p_Morning", model.Morning);
//                    cmd.Parameters.AddWithValue("p_Afternoon", model.Afternoon);
//                    cmd.Parameters.AddWithValue("p_Evening", model.Evening);
//                    cmd.Parameters.AddWithValue("p_Days", model.Days.HasValue ? (object)model.Days.Value : DBNull.Value);
//                    cmd.Parameters.AddWithValue("p_Route", model.Route ?? "Oral");
//                    cmd.Parameters.AddWithValue("p_Dosage", model.Dosage ?? (object)DBNull.Value);
//                    cmd.Parameters.AddWithValue("p_Instructions", model.Instructions ?? (object)DBNull.Value);

//                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte)
//                    { Direction = ParameterDirection.Output };
//                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255)
//                    { Direction = ParameterDirection.Output };

//                    cmd.Parameters.Add(successParam);
//                    cmd.Parameters.Add(messageParam);

//                    conn.Open();
//                    cmd.ExecuteNonQuery();

//                    bool success = Convert.ToBoolean(successParam.Value);
//                    if (!success)
//                        throw new Exception(messageParam.Value?.ToString());
//                }
//            }
//            catch (MySqlException ex)
//            {
//                throw new Exception("Database error while inserting prescription.", ex);
//            }
//        }

//        // ===============================
//        // INSERT INVESTIGATION
//        // ===============================
//        public void InsertInvestigation(IPDRoundInvestigation model)
//        {
//            try
//            {
//                using (var conn = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("sp_InsertRoundInvestigation", conn))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;

//                    cmd.Parameters.AddWithValue("p_RoundId", model.RoundId);
//                    cmd.Parameters.AddWithValue("p_IPDId", model.IPDId);
//                    cmd.Parameters.AddWithValue("p_ParentHospitalId", model.ParentHospitalId);
//                    cmd.Parameters.AddWithValue("p_SubHospitalId",
//                        model.SubHospitalId.HasValue ? (object)model.SubHospitalId.Value : DBNull.Value);
//                    cmd.Parameters.AddWithValue("p_InvestigationType", model.InvestigationType);
//                    cmd.Parameters.AddWithValue("p_TestName", model.TestName);
//                    cmd.Parameters.AddWithValue("p_Priority", model.Priority ?? "Routine");
//                    cmd.Parameters.AddWithValue("p_Instructions", model.Instructions ?? (object)DBNull.Value);

//                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte)
//                    { Direction = ParameterDirection.Output };
//                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255)
//                    { Direction = ParameterDirection.Output };

//                    cmd.Parameters.Add(successParam);
//                    cmd.Parameters.Add(messageParam);

//                    conn.Open();
//                    cmd.ExecuteNonQuery();

//                    bool success = Convert.ToBoolean(successParam.Value);
//                    if (!success)
//                        throw new Exception(messageParam.Value?.ToString());
//                }
//            }
//            catch (MySqlException ex)
//            {
//                throw new Exception("Database error while inserting investigation.", ex);
//            }
//        }

//        // ===============================
//        // SOFT DELETE ROUND
//        // ===============================
//        public void DeleteRound(int roundId)
//        {
//            try
//            {
//                using (var conn = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("sp_DeleteDoctorRound", conn))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("p_RoundId", roundId);

//                    var successParam = new MySqlParameter("p_Success", MySqlDbType.Byte)
//                    { Direction = ParameterDirection.Output };
//                    var messageParam = new MySqlParameter("p_Message", MySqlDbType.VarChar, 255)
//                    { Direction = ParameterDirection.Output };

//                    cmd.Parameters.Add(successParam);
//                    cmd.Parameters.Add(messageParam);

//                    conn.Open();
//                    cmd.ExecuteNonQuery();

//                    bool success = Convert.ToBoolean(successParam.Value);
//                    if (!success)
//                        throw new Exception(messageParam.Value?.ToString());
//                }
//            }
//            catch (MySqlException ex)
//            {
//                throw new Exception("Database error while deleting round.", ex);
//            }
//        }

//        // ===============================
//        // MAPPER
//        // ===============================
//        private IPDDoctorRound MapRound(MySqlDataReader reader)
//        {
//            return new IPDDoctorRound
//            {
//                RoundId = Convert.ToInt32(reader["RoundId"]),
//                RoundType = reader["RoundType"]?.ToString(),
//                RoundDateTime = Convert.ToDateTime(reader["RoundDateTime"]),
//                PatientCondition = reader["PatientCondition"]?.ToString(),
//                Diagnosis = reader["Diagnosis"]?.ToString(),
//                Instructions = reader["Instructions"]?.ToString(),
//                Notes = reader["Notes"]?.ToString(),
//                IsAbnormal = Convert.ToBoolean(reader["IsAbnormal"]),
//                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
//                DoctorName = reader["DoctorName"]?.ToString(),
//                Specialization = reader["Specialization"]?.ToString()
//            };
//        }

//        // Add to DoctorRoundRepository.cs

//        public IPDPrescriptionVM GetRoundPrescriptionPrint(int roundId)
//        {
//            IPDPrescriptionVM vm = null;
//            try
//            {
//                using (var conn = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("sp_GetRoundPrescriptionPrint", conn))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("p_RoundId", roundId);
//                    conn.Open();

//                    using (var reader = cmd.ExecuteReader())
//                    {
//                        // Result Set 1 - Round + Hospital + Patient info
//                        if (reader.Read())
//                        {
//                            vm = MapPrescriptionVM(reader);
//                            vm.Round = MapRoundPrintVM(reader);
//                        }

//                        if (vm == null) return null;

//                        // Result Set 2 - Symptoms
//                        reader.NextResult();
//                        while (reader.Read())
//                        {
//                            vm.Round.Symptoms.Add(new IPDSymptomPrintVM
//                            {
//                                SymptomName = reader["SymptomName"]?.ToString(),
//                                SubName = reader["SubName"]?.ToString()
//                            });
//                        }

//                        // Result Set 3 - Medicines
//                        reader.NextResult();
//                        while (reader.Read())
//                        {
//                            vm.Round.Medicines.Add(new IPDMedicinePrintVM
//                            {
//                                MedicineName = reader["MedicineName"]?.ToString(),
//                                MedicineType = reader["MedicineType"]?.ToString(),
//                                Morning = reader["Morning"] != DBNull.Value && Convert.ToInt32(reader["Morning"]) == 1,
//                                Afternoon = reader["Afternoon"] != DBNull.Value && Convert.ToInt32(reader["Afternoon"]) == 1,
//                                Evening = reader["Evening"] != DBNull.Value && Convert.ToInt32(reader["Evening"]) == 1,
//                                Days = reader["Days"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Days"]),
//                                Route = reader["Route"]?.ToString(),
//                                Dosage = reader["Dosage"]?.ToString(),
//                                MedicineInstructions = reader["MedicineInstructions"]?.ToString(),
//                                Status = reader["Status"]?.ToString()
//                            });
//                        }

//                        // Result Set 4 - Investigations
//                        reader.NextResult();
//                        while (reader.Read())
//                        {
//                            vm.Round.Investigations.Add(new IPDInvestigationPrintVM
//                            {
//                                InvestigationType = reader["InvestigationType"]?.ToString(),
//                                TestName = reader["TestName"]?.ToString(),
//                                Priority = reader["Priority"]?.ToString(),
//                                Status = reader["Status"]?.ToString(),
//                                InvInstructions = reader["InvInstructions"]?.ToString()
//                            });
//                        }
//                    }
//                }
//            }
//            catch (MySqlException ex)
//            {
//                throw new Exception("Error fetching round prescription.", ex);
//            }
//            return vm;
//        }

//        public IPDPrescriptionVM GetAllRoundsPrescriptionPrint(int ipdId)
//        {
//            IPDPrescriptionVM vm = null;
//            try
//            {
//                using (var conn = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("sp_GetAllRoundsPrescriptionPrint", conn))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("p_IPDId", ipdId);
//                    conn.Open();

//                    using (var reader = cmd.ExecuteReader())
//                    {
//                        vm = new IPDPrescriptionVM();
//                        vm.Rounds = new List<IPDRoundPrintVM>();
//                        bool headerSet = false;

//                        // Result Set 1 - All Rounds
//                        while (reader.Read())
//                        {
//                            if (!headerSet)
//                            {
//                                // Set hospital + patient info once
//                                MapPrescriptionVMFromReader(vm, reader);
//                                headerSet = true;
//                            }
//                            vm.Rounds.Add(MapRoundPrintVM(reader));
//                        }

//                        if (!headerSet) return null;

//                        // Result Set 2 - All Symptoms
//                        reader.NextResult();
//                        while (reader.Read())
//                        {
//                            int roundId = Convert.ToInt32(reader["RoundId"]);
//                            var round = vm.Rounds.Find(r => r.RoundId == roundId);
//                            round?.Symptoms.Add(new IPDSymptomPrintVM
//                            {
//                                SymptomName = reader["SymptomName"]?.ToString(),
//                                SubName = reader["SubName"]?.ToString()
//                            });
//                        }

//                        // Result Set 3 - All Medicines
//                        reader.NextResult();
//                        while (reader.Read())
//                        {
//                            int roundId = Convert.ToInt32(reader["RoundId"]);
//                            var round = vm.Rounds.Find(r => r.RoundId == roundId);
//                            round?.Medicines.Add(new IPDMedicinePrintVM
//                            {
//                                MedicineName = reader["MedicineName"]?.ToString(),
//                                MedicineType = reader["MedicineType"]?.ToString(),
//                                Morning = reader["Morning"] != DBNull.Value && Convert.ToInt32(reader["Morning"]) == 1,
//                                Afternoon = reader["Afternoon"] != DBNull.Value && Convert.ToInt32(reader["Afternoon"]) == 1,
//                                Evening = reader["Evening"] != DBNull.Value && Convert.ToInt32(reader["Evening"]) == 1,
//                                Days = reader["Days"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Days"]),
//                                Route = reader["Route"]?.ToString(),
//                                Dosage = reader["Dosage"]?.ToString(),
//                                MedicineInstructions = reader["MedicineInstructions"]?.ToString(),
//                                Status = reader["Status"]?.ToString()
//                            });
//                        }

//                        // Result Set 4 - All Investigations
//                        reader.NextResult();
//                        while (reader.Read())
//                        {
//                            int roundId = Convert.ToInt32(reader["RoundId"]);
//                            var round = vm.Rounds.Find(r => r.RoundId == roundId);
//                            round?.Investigations.Add(new IPDInvestigationPrintVM
//                            {
//                                InvestigationType = reader["InvestigationType"]?.ToString(),
//                                TestName = reader["TestName"]?.ToString(),
//                                Priority = reader["Priority"]?.ToString(),
//                                Status = reader["Status"]?.ToString(),
//                                InvInstructions = reader["InvInstructions"]?.ToString()
//                            });
//                        }
//                    }
//                }
//            }
//            catch (MySqlException ex)
//            {
//                throw new Exception("Error fetching all rounds prescription.", ex);
//            }
//            return vm;
//        }

//        // ===============================
//        // HELPER MAPPERS
//        // ===============================
//        private IPDPrescriptionVM MapPrescriptionVM(MySqlDataReader reader)
//        {
//            var vm = new IPDPrescriptionVM();
//            MapPrescriptionVMFromReader(vm, reader);
//            return vm;
//        }

//        private void MapPrescriptionVMFromReader(IPDPrescriptionVM vm, MySqlDataReader reader)
//        {
//            vm.HospitalName = reader["HospitalName"]?.ToString();
//            vm.HospitalAddress = reader["HospitalAddress"]?.ToString();
//            vm.HospitalPhone = reader["HospitalPhone"]?.ToString();
//            vm.HospitalEmail = reader["HospitalEmail"]?.ToString();
//            vm.HospitalLogo = reader["HospitalLogo"]?.ToString();
//            vm.HospitalRegNo = reader["HospitalRegNo"]?.ToString();
//            vm.PatientName = reader["PatientName"]?.ToString();
//            vm.Age = reader["Age"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Age"]);
//            vm.Gender = reader["Gender"]?.ToString();
//            vm.PatientPhone = reader["PatientPhone"]?.ToString();
//            vm.AdmissionNumber = reader["AdmissionNumber"]?.ToString();
//            vm.AdmissionDateTime = Convert.ToDateTime(reader["AdmissionDateTime"]);
//            vm.BedNumber = reader["BedNumber"]?.ToString();
//            vm.WardName = reader["WardName"]?.ToString();
//            vm.RoomNumber = reader["RoomNumber"]?.ToString();
//        }

//        private IPDRoundPrintVM MapRoundPrintVM(MySqlDataReader reader)
//        {
//            return new IPDRoundPrintVM
//            {
//                RoundId = Convert.ToInt32(reader["RoundId"]),
//                RoundType = reader["RoundType"]?.ToString(),
//                RoundDateTime = Convert.ToDateTime(reader["RoundDateTime"]),
//                DoctorName = reader["DoctorName"]?.ToString(),
//                Specialization = reader["Specialization"]?.ToString(),
//                Education = reader["Education"]?.ToString(),
//                PatientCondition = reader["PatientCondition"]?.ToString(),
//                Diagnosis = reader["Diagnosis"]?.ToString(),
//                Instructions = reader["Instructions"]?.ToString(),
//                Notes = reader["Notes"]?.ToString()
//            };
//        }


//    }
//}