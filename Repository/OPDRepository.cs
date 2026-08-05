using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class OPDRepository : IOPD
    {
        private readonly string _connectionString;

        public OPDRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // ✅ FIX #1: Changed return type from bool to int.
        // sp_AddOPDWithSymptomMedicine already does "SELECT v_OPDId AS OPDId" at the end.
        // We use ExecuteScalar() to read that value instead of discarding it with ExecuteNonQuery().
        // Previously: opdId = _IOPD.AddOPD(...) ? 1 : 0  → always stored OPDId=1 in notifications!
        public int AddOPD(OPD model, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_AddOPDWithSymptomMedicine", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new MySqlParameter("p_AppointmentId", model.AppointmentId));
                    cmd.Parameters.Add(new MySqlParameter("p_BP", model.BP ?? ""));
                    cmd.Parameters.Add(new MySqlParameter("p_Pulse", model.Pulse ?? ""));
                    cmd.Parameters.Add(new MySqlParameter("p_Investigation", model.Investigation ?? ""));
                    cmd.Parameters.Add(new MySqlParameter("p_ReportDetail", model.ReportDetail ?? ""));
                    cmd.Parameters.AddWithValue("p_ReportFilePath", model.ReportFilePath ?? "");
                    cmd.Parameters.Add(new MySqlParameter("p_HospitalId", hospitalId));
                    cmd.Parameters.AddWithValue("@p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);
                    cmd.Parameters.Add(new MySqlParameter("p_NextAppointmentDate", model.NextAppointmentDate));

                    var symptomsParam = new MySqlParameter("p_Symptoms", MySqlDbType.JSON);
                    symptomsParam.Value = JsonSerializer.Serialize(model.Symptoms ?? new List<int>());
                    cmd.Parameters.Add(symptomsParam);

                    var medicinesParam = new MySqlParameter("p_Medicines", MySqlDbType.JSON);
                    medicinesParam.Value = JsonSerializer.Serialize(model.Medicines ?? new List<OPDMedicine>());
                    cmd.Parameters.Add(medicinesParam);

                    var diagnosesParam = new MySqlParameter("p_Diagnoses", MySqlDbType.JSON);
                    diagnosesParam.Value = JsonSerializer.Serialize(model.Diagnoses ?? new List<OPDDiagnosis>());
                    cmd.Parameters.Add(diagnosesParam);

                    con.Open();

                    // ✅ FIX: Use ExecuteScalar to capture the OPDId returned by the SP
                    // SP ends with: SELECT v_OPDId AS OPDId;
                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting OPD", ex);
            }
        }

        public int GetOPDIdByAppointmentId(int appointmentId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("sp_GetOPDIdByAppointmentId", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@p_AppointmentId", appointmentId);
                    cmd.Parameters.AddWithValue("@p_HospitalId", hospitalId);
                    cmd.Parameters.AddWithValue("@p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return result == null ? 0 : Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting OPD ID", ex);
            }
        }

        public OPD GetOPDById(int opdId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                using var command = new MySqlCommand("sp_GetOPDById", connection);
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("p_OPDId", opdId);
                command.Parameters.AddWithValue("p_HospitalId", hospitalId);
                command.Parameters.AddWithValue("@p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                var opd = new OPD();

                using var reader = command.ExecuteReader();

                // 1️⃣ OPD Master
                if (reader.Read())
                {
                    opd.Id = Convert.ToInt32(reader["Id"]);
                    opd.AppointmentId = Convert.ToInt32(reader["AppointmentId"]);
                    opd.BP = reader["BP"]?.ToString();
                    opd.Pulse = reader["Pulse"]?.ToString();
                    opd.Investigation = reader["Investigation"]?.ToString();
                    opd.ReportDetail = reader["ReportDetail"]?.ToString();
                    opd.ReportFilePath = reader["ReportFilePath"]?.ToString();
                    opd.HospitalId = Convert.ToInt32(reader["Hospital_Id"]);
                    opd.SubHospitalId = reader["SubHospital_Id"] == DBNull.Value
                        ? (int?)null
                        : Convert.ToInt32(reader["SubHospital_Id"]);
                    opd.NextAppointmentDate = reader["NextAppointmentDate"] == DBNull.Value
                        ? (DateTime?)null
                        : Convert.ToDateTime(reader["NextAppointmentDate"]);
                }

                // 2️⃣ Symptoms
                if (reader.NextResult())
                {
                    while (reader.Read())
                    {
                        opd.Symptoms.Add(Convert.ToInt32(reader["SymptomId"]));
                    }
                }

                // 3️⃣ Medicines — SP now JOINs tbl_medicine (not the missing tbl_tablet)
                if (reader.NextResult())
                {
                    while (reader.Read())
                    {
                        opd.Medicines.Add(new OPDMedicine
                        {
                            MedicineId = Convert.ToInt32(reader["Medicine_Id"]),
                            MedicineName = reader["TablateName"]?.ToString(),
                            Morning = Convert.ToInt32(reader["Morning"]).ToString(),
                            Afternoon = Convert.ToInt32(reader["Afternoon"]).ToString(),
                            Evening = Convert.ToInt32(reader["Evening"]).ToString(),
                            Days = Convert.ToInt32(reader["Days"]),
                            Frequency = reader["Frequency"]?.ToString() ?? "TDS",
                            WhenToTake = reader["WhenToTake"]?.ToString() ?? "After Food",
                            Dose = reader["Dose"]?.ToString() ?? "1",
                            Unit = reader["Unit"]?.ToString() ?? "Tablet(s)",
                            Quantity = reader["Quantity"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Quantity"])
                        });
                    }
                }

                // 4️⃣ Diagnoses
                if (reader.NextResult())
                {
                    while (reader.Read())
                    {
                        opd.Diagnoses.Add(new OPDDiagnosis
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            OPDId = Convert.ToInt32(reader["OPD_Id"]),
                            DiagnosisName = reader["DiagnosisName"]?.ToString(),
                            Type = reader["Type"]?.ToString() ?? "Final",
                            Notes = reader["Notes"]?.ToString()
                        });
                    }
                }

                return opd;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting OPD by ID", ex);
            }
        }

        public int UpdateOPD(OPD opd)
        {
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                {
                    using (var cmd = new MySqlCommand("sp_UpdateOPDWithSymptomMedicine_Smart", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("p_OPDId", opd.Id);
                        cmd.Parameters.AddWithValue("p_BP", opd.BP ?? string.Empty);
                        cmd.Parameters.AddWithValue("p_Pulse", opd.Pulse ?? string.Empty);
                        cmd.Parameters.AddWithValue("p_Investigation", opd.Investigation ?? string.Empty);
                        cmd.Parameters.AddWithValue("p_ReportDetail", opd.ReportDetail ?? string.Empty);
                        cmd.Parameters.AddWithValue("p_ReportFilePath", opd.ReportFilePath ?? string.Empty);
                        cmd.Parameters.AddWithValue("p_HospitalId", opd.HospitalId);
                        cmd.Parameters.AddWithValue("p_SubHospitalId", opd.SubHospitalId.HasValue ? (object)opd.SubHospitalId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("p_NextAppointmentDate", opd.NextAppointmentDate ?? (object)DBNull.Value);

                        var symptomsParam = new MySqlParameter("p_Symptoms", MySqlDbType.JSON);
                        symptomsParam.Value = JsonSerializer.Serialize(opd.Symptoms ?? new List<int>());
                        cmd.Parameters.Add(symptomsParam);

                        var medicinesParam = new MySqlParameter("p_Medicines", MySqlDbType.JSON);
                        medicinesParam.Value = JsonSerializer.Serialize(opd.Medicines ?? new List<OPDMedicine>());
                        cmd.Parameters.Add(medicinesParam);

                        var diagnosesParam = new MySqlParameter("p_Diagnoses", MySqlDbType.JSON);
                        diagnosesParam.Value = JsonSerializer.Serialize(opd.Diagnoses ?? new List<OPDDiagnosis>());
                        cmd.Parameters.Add(diagnosesParam);

                        con.Open();
                        var result = cmd.ExecuteScalar(); // SP returns OPDId
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception("Database error while updating OPD: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while updating OPD: " + ex.Message, ex);
            }
        }

        public List<OPDSymptomVM> GetOPDSymptomsByOPDId(int opdId)
        {
            List<OPDSymptomVM> symptomList = new List<OPDSymptomVM>();

            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_GetSymptomsByOPDId", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_OPDId", opdId);

                        con.Open();
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                symptomList.Add(new OPDSymptomVM
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    OPD_Id = Convert.ToInt32(reader["OPD_Id"]),
                                    Symptom_Id = Convert.ToInt32(reader["Symptom_Id"]),
                                    SymptomName = reader["SymptomName"].ToString(),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                    IsActive = Convert.ToBoolean(reader["IsActive"])
                                });
                            }
                        }
                    }
                }

                return symptomList;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting OPD symptoms", ex);
            }
        }

        public List<OPDMedicine> GetMedicinesByOPDId(int opdId)
        {
            List<OPDMedicine> list = new List<OPDMedicine>();
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_GetMedicinesByOPDId", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_OPDId", opdId);

                        con.Open();

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new OPDMedicine
                                {
                                    MedicineId = Convert.ToInt32(reader["Medicine_Id"]),
                                    MedicineName = reader["MedicineName"].ToString(),
                                    Morning = Convert.ToBoolean(reader["Morning"]) ? "Y" : "N",
                                    Afternoon = Convert.ToBoolean(reader["Afternoon"]) ? "Y" : "N",
                                    Evening = Convert.ToBoolean(reader["Evening"]) ? "Y" : "N",
                                    Days = Convert.ToInt32(reader["Days"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting medicines by OPD ID", ex);
            }
            return list;
        }

        // ── GET ALL DIAGNOSES (for autocomplete) ─────────────────────────
        public List<DiagnosisMaster> GetAllDiagnoses(int hospitalId)
        {
            var list = new List<DiagnosisMaster>();
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_GetAllDiagnoses", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new DiagnosisMaster
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Name = reader["Name"].ToString(),
                        IcdCode = reader["IcdCode"]?.ToString(),
                        Category = reader["Category"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting diagnoses: " + ex.Message, ex);
            }
            return list;
        }
    }
}

//using Microsoft.Extensions.Configuration;
//using MySql.Data.MySqlClient;
//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Data;
//using System.Text.Json;
//using System.Text.Json.Serialization;
//using System.Threading.Tasks;
//using WebApplicationSampleTest2.Models;

//namespace WebApplicationSampleTest2.Repository
//{
//    public class OPDRepository : IOPD
//    {
//        private readonly string _connectionString;

//        public OPDRepository(IConfiguration configuration)
//        {
//            _connectionString = configuration.GetConnectionString("MySqlConnection");
//        }

//        public bool AddOPD(OPD model, int hospitalId, int? subHospitalId)
//        {
//            try
//            {
//                using (var con = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("sp_AddOPDWithSymptomMedicine", con))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;

//                    cmd.Parameters.Add(new MySqlParameter("p_AppointmentId", model.AppointmentId));
//                    cmd.Parameters.Add(new MySqlParameter("p_BP", model.BP ?? ""));
//                    cmd.Parameters.Add(new MySqlParameter("p_Pulse", model.Pulse ?? ""));
//                    cmd.Parameters.Add(new MySqlParameter("p_Investigation", model.Investigation ?? ""));
//                    cmd.Parameters.Add(new MySqlParameter("p_ReportDetail", model.ReportDetail ?? ""));
//                    cmd.Parameters.AddWithValue("p_ReportFilePath", model.ReportFilePath ?? "");
//                    cmd.Parameters.Add(new MySqlParameter("p_HospitalId", hospitalId));
//                    cmd.Parameters.AddWithValue("@p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);
//                    cmd.Parameters.Add(new MySqlParameter("p_NextAppointmentDate", model.NextAppointmentDate));

//                    // 🔥 MOST IMPORTANT PART
//                    var symptomsParam = new MySqlParameter("p_Symptoms", MySqlDbType.JSON);
//                    symptomsParam.Value = JsonSerializer.Serialize(model.Symptoms ?? new List<int>());
//                    cmd.Parameters.Add(symptomsParam);

//                    var medicinesParam = new MySqlParameter("p_Medicines", MySqlDbType.JSON);
//                    medicinesParam.Value = JsonSerializer.Serialize(model.Medicines ?? new List<OPDMedicine>());
//                    cmd.Parameters.Add(medicinesParam);

//                    con.Open();
//                    cmd.ExecuteNonQuery(); // rows check 
//                    return true;
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new Exception("Error while inserting hospital", ex);
//            }

//        }
//        public int GetOPDIdByAppointmentId(int appointmentId, int hospitalId, int? subHospitalId)
//        {
//            try
//            {
//                using (var con = new MySqlConnection(_connectionString))
//                using (var cmd = new MySqlCommand("sp_GetOPDIdByAppointmentId", con))
//                {
//                    cmd.CommandType = CommandType.StoredProcedure;
//                    cmd.Parameters.AddWithValue("@p_AppointmentId", appointmentId);
//                    cmd.Parameters.AddWithValue("@p_HospitalId", hospitalId);
//                    cmd.Parameters.AddWithValue("@p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);
//                    con.Open();
//                    var result = cmd.ExecuteScalar();

//                    return result == null ? 0 : Convert.ToInt32(result);
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new Exception("Error while inserting hospital", ex);
//            }

//        }


//        public OPD GetOPDById(int opdId, int hospitalId, int? subHospitalId)
//        {
//            try
//            {
//                using var connection = new MySqlConnection(_connectionString);
//                connection.Open();

//                using var command = new MySqlCommand("sp_GetOPDById", connection);
//                command.CommandType = CommandType.StoredProcedure;

//                command.Parameters.AddWithValue("p_OPDId", opdId);
//                command.Parameters.AddWithValue("p_HospitalId", hospitalId);
//                command.Parameters.AddWithValue("@p_SubHospitalId", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

//                var opd = new OPD();


//                using var reader = command.ExecuteReader();



//                // 1️⃣ OPD Master
//                if (reader.Read())
//                {
//                    opd.Id = Convert.ToInt32(reader["Id"]);
//                    opd.AppointmentId = Convert.ToInt32(reader["AppointmentId"]);
//                    opd.BP = reader["BP"]?.ToString();
//                    opd.Pulse = reader["Pulse"]?.ToString();
//                    opd.Investigation = reader["Investigation"]?.ToString();
//                    opd.ReportDetail = reader["ReportDetail"]?.ToString();
//                    opd.ReportFilePath = reader["ReportFilePath"]?.ToString();
//                    opd.HospitalId = Convert.ToInt32(reader["Hospital_Id"]);
//                    opd.SubHospitalId = reader["SubHospital_Id"] == DBNull.Value
//                        ? (int?)null
//                        : Convert.ToInt32(reader["SubHospital_Id"]);
//                    opd.NextAppointmentDate = reader["NextAppointmentDate"] == DBNull.Value
//                        ? (DateTime?)null
//                        : Convert.ToDateTime(reader["NextAppointmentDate"]);
//                }

//                // 2️⃣ Symptoms
//                if (reader.NextResult())
//                {
//                    while (reader.Read())
//                    {
//                        opd.Symptoms.Add(Convert.ToInt32(reader["SymptomId"]));
//                    }
//                }

//                // 3️⃣ Medicines
//                if (reader.NextResult())
//                {
//                    while (reader.Read())
//                    {
//                        opd.Medicines.Add(new OPDMedicine
//                        {
//                            MedicineId = Convert.ToInt32(reader["Medicine_Id"]),
//                            MedicineName = reader["TablateName"]?.ToString(),
//                            Morning = Convert.ToInt32(reader["Morning"]).ToString(),
//                            Afternoon = Convert.ToInt32(reader["Afternoon"]).ToString(),
//                            Evening = Convert.ToInt32(reader["Evening"]).ToString(),
//                            Days = Convert.ToInt32(reader["Days"])
//                        });
//                    }
//                }

//                return opd;
//            }
//            catch (Exception ex)
//            {
//                throw new Exception("Error while inserting hospital", ex);
//            }


//        }

//        public int UpdateOPD(OPD opd)
//        {
//            try
//            {
//                using (var con = new MySqlConnection(_connectionString))
//                {
//                    using (var cmd = new MySqlCommand("sp_UpdateOPDWithSymptomMedicine_Smart", con))
//                    {
//                        cmd.CommandType = CommandType.StoredProcedure;

//                        // ✅ Regular fields
//                        cmd.Parameters.AddWithValue("p_OPDId", opd.Id);
//                        cmd.Parameters.AddWithValue("p_BP", opd.BP ?? string.Empty);
//                        cmd.Parameters.AddWithValue("p_Pulse", opd.Pulse ?? string.Empty);
//                        cmd.Parameters.AddWithValue("p_Investigation", opd.Investigation ?? string.Empty);
//                        cmd.Parameters.AddWithValue("p_ReportDetail", opd.ReportDetail ?? string.Empty);
//                        cmd.Parameters.AddWithValue("p_ReportFilePath", opd.ReportFilePath ?? string.Empty);
//                        cmd.Parameters.AddWithValue("p_HospitalId", opd.HospitalId);
//                        cmd.Parameters.AddWithValue("p_SubHospitalId", opd.SubHospitalId.HasValue ? (object)opd.SubHospitalId.Value : DBNull.Value);
//                        cmd.Parameters.AddWithValue("p_NextAppointmentDate", opd.NextAppointmentDate ?? (object)DBNull.Value);

//                        // ✅ JSON Parameters — MUST use MySqlParameter with MySqlDbType.JSON
//                        var symptomsParam = new MySqlParameter("p_Symptoms", MySqlDbType.JSON);
//                        symptomsParam.Value = JsonSerializer.Serialize(opd.Symptoms ?? new List<int>());
//                        cmd.Parameters.Add(symptomsParam);

//                        var medicinesParam = new MySqlParameter("p_Medicines", MySqlDbType.JSON);
//                        medicinesParam.Value = JsonSerializer.Serialize(opd.Medicines ?? new List<OPDMedicine>());
//                        cmd.Parameters.Add(medicinesParam);

//                        con.Open();
//                        var result = cmd.ExecuteScalar(); // SP returns OPDId
//                        return Convert.ToInt32(result);
//                    }
//                }
//            }
//            catch (MySqlException ex)
//            {
//                throw new Exception("Database error while updating OPD: " + ex.Message, ex);
//            }
//            catch (Exception ex)
//            {
//                throw new Exception("Unexpected error while updating OPD: " + ex.Message, ex);
//            }
//        }
//        public List<OPDSymptomVM> GetOPDSymptomsByOPDId(int opdId)
//        {
//            List<OPDSymptomVM> symptomList = new List<OPDSymptomVM>();

//            try
//            {
//                using (MySqlConnection con = new MySqlConnection(_connectionString))
//                {
//                    using (MySqlCommand cmd = new MySqlCommand("sp_GetSymptomsByOPDId", con))
//                    {
//                        cmd.CommandType = CommandType.StoredProcedure;
//                        cmd.Parameters.AddWithValue("p_OPDId", opdId);

//                        con.Open();
//                        using (MySqlDataReader reader = cmd.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                symptomList.Add(new OPDSymptomVM
//                                {
//                                    Id = Convert.ToInt32(reader["Id"]),
//                                    OPD_Id = Convert.ToInt32(reader["OPD_Id"]),
//                                    Symptom_Id = Convert.ToInt32(reader["Symptom_Id"]),
//                                    SymptomName = reader["SymptomName"].ToString(),
//                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
//                                    IsActive = Convert.ToBoolean(reader["IsActive"])
//                                });
//                            }
//                        }
//                    }
//                }

//                return symptomList;
//            }
//            catch (Exception ex)
//            {
//                throw new Exception("Error while inserting hospital", ex);
//            }

//        }


//        public List<OPDMedicine> GetMedicinesByOPDId(int opdId)
//        {
//            List<OPDMedicine> list = new List<OPDMedicine>();
//            try
//            {
//                using (MySqlConnection con = new MySqlConnection(_connectionString))
//                {
//                    using (MySqlCommand cmd = new MySqlCommand("sp_GetMedicinesByOPDId", con))
//                    {
//                        cmd.CommandType = CommandType.StoredProcedure;
//                        cmd.Parameters.AddWithValue("p_OPDId", opdId);

//                        con.Open();

//                        using (MySqlDataReader reader = cmd.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                list.Add(new OPDMedicine
//                                {
//                                    MedicineId = Convert.ToInt32(reader["Medicine_Id"]),
//                                    MedicineName = reader["MedicineName"].ToString(),
//                                    Morning = Convert.ToBoolean(reader["Morning"]) ? "Y" : "N",
//                                    Afternoon = Convert.ToBoolean(reader["Afternoon"]) ? "Y" : "N",
//                                    Evening = Convert.ToBoolean(reader["Evening"]) ? "Y" : "N",
//                                    Days = Convert.ToInt32(reader["Days"])
//                                });
//                            }
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new Exception("Error while inserting hospital", ex);
//            }
//            return list;
//        }


//    }
//}
