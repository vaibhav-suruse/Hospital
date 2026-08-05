using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{

    public class PatientHistoryRepository : IPatientHistory
    {
        private readonly string _conn;
        public PatientHistoryRepository(IConfiguration cfg)
        {
            _conn = cfg.GetConnectionString("MySqlConnection");
        }

        // ── Vitals ───────────────────────────────────────────────────────
        public int SaveVitals(PatientVitals v)
        {
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_Vitals_Save", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_PatientId", v.PatientId);
            cmd.Parameters.AddWithValue("p_Hospital_Id", v.HospitalId);
            cmd.Parameters.AddWithValue("p_AppointmentId", (object?)v.AppointmentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_Temperature", (object?)v.Temperature ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_PulseRate", (object?)v.PulseRate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_BPSystolic", (object?)v.BPSystolic ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_BPDiastolic", (object?)v.BPDiastolic ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_SPO2", (object?)v.SPO2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_RBS", (object?)v.RBS ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_Weight", (object?)v.Weight ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_Height", (object?)v.Height ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_BMI", (object?)v.BMI ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_Respiration", (object?)v.Respiration ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_WaistCircumference", (object?)v.WaistCircumference ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_FIB4", (object?)v.FIB4 ?? DBNull.Value);
            con.Open();
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public PatientVitals GetLatestVitals(int patientId, int hospitalId)
        {
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_Vitals_GetByPatient", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_PatientId", patientId);
            cmd.Parameters.AddWithValue("p_Hospital_Id", hospitalId);
            con.Open();
            using var r = cmd.ExecuteReader();
            if (r.Read()) return MapVitals(r);
            return null;
        }

        // ── Medical Conditions ────────────────────────────────────────────
        public int SaveMedicalCondition(PatientMedicalCondition c)
        {
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_MedCondition_Save", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_Id", c.Id);
            cmd.Parameters.AddWithValue("p_PatientId", c.PatientId);
            cmd.Parameters.AddWithValue("p_Hospital_Id", c.HospitalId);
            cmd.Parameters.AddWithValue("p_ConditionName", c.ConditionName);
            cmd.Parameters.AddWithValue("p_SinceDays", (object?)c.SinceDays ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_SinceValue", (object?)c.SinceDays ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_SinceUnit", c.SinceUnit ?? "Days");
            cmd.Parameters.AddWithValue("p_Status", c.Status ?? "Active");
            cmd.Parameters.AddWithValue("p_OnMedication", c.OnMedication ? 1 : 0);
            cmd.Parameters.AddWithValue("p_Notes", (object?)c.Notes ?? DBNull.Value);
            con.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void DeleteMedicalCondition(int id)
        {
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_MedCondition_Delete", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_Id", id);
            con.Open(); cmd.ExecuteNonQuery();
        }

        public List<PatientMedicalCondition> GetMedicalConditions(int patientId, int hospitalId)
        {
            var list = new List<PatientMedicalCondition>();
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_MedCondition_GetByPatient", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_PatientId", patientId);
            cmd.Parameters.AddWithValue("p_Hospital_Id", hospitalId);
            con.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new PatientMedicalCondition
            {
                Id = Convert.ToInt32(r["Id"]),
                PatientId = patientId,
                HospitalId = hospitalId,
                ConditionName = r["ConditionName"].ToString(),
                SinceDays = r["SinceDays"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["SinceDays"]),
                Status = r["Status"].ToString(),
                OnMedication = Convert.ToBoolean(r["OnMedication"]),
                Notes = r["Notes"]?.ToString(),
                CreatedDate = Convert.ToDateTime(r["CreatedDate"])
            });
            return list;
        }

        // ── Allergies ────────────────────────────────────────────────────
        public int SaveAllergy(PatientAllergy a)
        {
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_Allergy_Save", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_Id", a.Id);
            cmd.Parameters.AddWithValue("p_PatientId", a.PatientId);
            cmd.Parameters.AddWithValue("p_Hospital_Id", a.HospitalId);
            cmd.Parameters.AddWithValue("p_AllergyTo", a.AllergyTo);
            cmd.Parameters.AddWithValue("p_SinceHours", (object?)a.SinceHours ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_Status", a.Status ?? "Active");
            cmd.Parameters.AddWithValue("p_Notes", (object?)a.Notes ?? DBNull.Value);
            con.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void DeleteAllergy(int id)
        {
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_Allergy_Delete", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_Id", id); con.Open(); cmd.ExecuteNonQuery();
        }

        public List<PatientAllergy> GetAllergies(int patientId, int hospitalId)
        {
            var list = new List<PatientAllergy>();
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_Allergy_GetByPatient", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_PatientId", patientId);
            cmd.Parameters.AddWithValue("p_Hospital_Id", hospitalId);
            con.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new PatientAllergy
            {
                Id = Convert.ToInt32(r["Id"]),
                PatientId = patientId,
                HospitalId = hospitalId,
                AllergyTo = r["AllergyTo"].ToString(),
                SinceHours = r["SinceHours"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["SinceHours"]),
                Status = r["Status"].ToString(),
                Notes = r["Notes"]?.ToString(),
                CreatedDate = Convert.ToDateTime(r["CreatedDate"])
            });
            return list;
        }

        // ── Surgical History ─────────────────────────────────────────────
        public int SaveSurgery(PatientSurgicalHistory s)
        {
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_Surgery_Save", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_Id", s.Id);
            cmd.Parameters.AddWithValue("p_PatientId", s.PatientId);
            cmd.Parameters.AddWithValue("p_Hospital_Id", s.HospitalId);
            cmd.Parameters.AddWithValue("p_ProcedureName", s.ProcedureName);
            cmd.Parameters.AddWithValue("p_SurgeryDate", (object?)s.SurgeryDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_Location", (object?)s.Location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_Notes", (object?)s.Notes ?? DBNull.Value);
            con.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void DeleteSurgery(int id)
        {
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_Surgery_Delete", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_Id", id); con.Open(); cmd.ExecuteNonQuery();
        }

        public List<PatientSurgicalHistory> GetSurgeries(int patientId, int hospitalId)
        {
            var list = new List<PatientSurgicalHistory>();
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_Surgery_GetByPatient", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_PatientId", patientId);
            cmd.Parameters.AddWithValue("p_Hospital_Id", hospitalId);
            con.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new PatientSurgicalHistory
            {
                Id = Convert.ToInt32(r["Id"]),
                PatientId = patientId,
                HospitalId = hospitalId,
                ProcedureName = r["ProcedureName"].ToString(),
                SurgeryDate = r["SurgeryDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["SurgeryDate"]),
                Location = r["Location"]?.ToString(),
                Notes = r["Notes"]?.ToString()
            });
            return list;
        }

        // ── Family History ────────────────────────────────────────────────
        public int SaveFamilyHistory(PatientFamilyHistory f)
        {
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_FamilyHistory_Save", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_Id", f.Id);
            cmd.Parameters.AddWithValue("p_PatientId", f.PatientId);
            cmd.Parameters.AddWithValue("p_Hospital_Id", f.HospitalId);
            cmd.Parameters.AddWithValue("p_Relation", f.Relation);
            cmd.Parameters.AddWithValue("p_ConditionName", f.ConditionName);
            cmd.Parameters.AddWithValue("p_Notes", (object?)f.Notes ?? DBNull.Value);
            con.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void DeleteFamilyHistory(int id)
        {
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_FamilyHistory_Delete", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_Id", id); con.Open(); cmd.ExecuteNonQuery();
        }

        public List<PatientFamilyHistory> GetFamilyHistory(int patientId, int hospitalId)
        {
            var list = new List<PatientFamilyHistory>();
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_FamilyHistory_GetByPatient", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_PatientId", patientId);
            cmd.Parameters.AddWithValue("p_Hospital_Id", hospitalId);
            con.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new PatientFamilyHistory
            {
                Id = Convert.ToInt32(r["Id"]),
                PatientId = patientId,
                HospitalId = hospitalId,
                Relation = r["Relation"].ToString(),
                ConditionName = r["ConditionName"].ToString(),
                Notes = r["Notes"]?.ToString(),
                CreatedDate = Convert.ToDateTime(r["CreatedDate"])
            });
            return list;
        }

        // ── Lab Results ──────────────────────────────────────────────────
        public int SaveLabResult(PatientLabResult l)
        {
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_LabResult_Save", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_Id", l.Id);
            cmd.Parameters.AddWithValue("p_PatientId", l.PatientId);
            cmd.Parameters.AddWithValue("p_Hospital_Id", l.HospitalId);
            cmd.Parameters.AddWithValue("p_AppointmentId", (object?)l.AppointmentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_TestName", l.TestName);
            cmd.Parameters.AddWithValue("p_ResultValue", (object?)l.ResultValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_Unit", (object?)l.Unit ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_NormalRange", (object?)l.NormalRange ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_IsAbnormal", l.IsAbnormal ? 1 : 0);
            cmd.Parameters.AddWithValue("p_ResultDate", l.ResultDate);
            cmd.Parameters.AddWithValue("p_FilePath", (object?)l.FilePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_Notes", (object?)l.Notes ?? DBNull.Value);
            con.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void DeleteLabResult(int id)
        {
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_LabResult_Delete", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_Id", id); con.Open(); cmd.ExecuteNonQuery();
        }

        public List<PatientLabResult> GetLabResults(int patientId, int hospitalId)
        {
            var list = new List<PatientLabResult>();
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_LabResult_GetByPatient", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_PatientId", patientId);
            cmd.Parameters.AddWithValue("p_Hospital_Id", hospitalId);
            con.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new PatientLabResult
            {
                Id = Convert.ToInt32(r["Id"]),
                PatientId = patientId,
                HospitalId = hospitalId,
                AppointmentId = r["AppointmentId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["AppointmentId"]),
                TestName = r["TestName"].ToString(),
                ResultValue = r["ResultValue"]?.ToString(),
                Unit = r["Unit"]?.ToString(),
                NormalRange = r["NormalRange"]?.ToString(),
                IsAbnormal = Convert.ToBoolean(r["IsAbnormal"]),
                ResultDate = Convert.ToDateTime(r["ResultDate"]),
                FilePath = r["FilePath"]?.ToString(),
                Notes = r["Notes"]?.ToString()
            });
            return list;
        }

        // ── Combined (all sections) ───────────────────────────────────────
        public PatientHistoryVM GetFullHistory(int patientId, int hospitalId)
        {
            var vm = new PatientHistoryVM();
            using var con = new MySqlConnection(_conn);
            using var cmd = new MySqlCommand("sp_PatientHistory_GetAll", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_PatientId", patientId);
            cmd.Parameters.AddWithValue("p_Hospital_Id", hospitalId);
            con.Open();
            using var r = cmd.ExecuteReader();

            // Result 1: Conditions
            while (r.Read()) vm.Conditions.Add(new PatientMedicalCondition
            {
                Id = Convert.ToInt32(r["Id"]),
                ConditionName = r["Name"].ToString(),
                SinceDays = r["SinceDays"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["SinceDays"]),
                Status = r["Status"]?.ToString(),
                OnMedication = r["OnMedication"] != DBNull.Value && Convert.ToBoolean(r["OnMedication"]),
                Notes = r["Notes"]?.ToString(),
                CreatedDate = Convert.ToDateTime(r["CreatedDate"])
            });

            // Result 2: Allergies
            if (r.NextResult()) while (r.Read()) vm.Allergies.Add(new PatientAllergy
            {
                Id = Convert.ToInt32(r["Id"]),
                AllergyTo = r["Name"].ToString(),
                SinceHours = r["SinceHours"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["SinceHours"]),
                Status = r["Status"]?.ToString(),
                Notes = r["Notes"]?.ToString(),
                CreatedDate = Convert.ToDateTime(r["CreatedDate"])
            });

            // Result 3: Surgeries
            if (r.NextResult()) while (r.Read()) vm.Surgeries.Add(new PatientSurgicalHistory
            {
                Id = Convert.ToInt32(r["Id"]),
                ProcedureName = r["Name"].ToString(),
                Notes = r["Notes"]?.ToString(),
                SurgeryDate = r["CreatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CreatedDate"])
            });

            // Result 4: Family History
            if (r.NextResult()) while (r.Read()) vm.FamilyHistory.Add(new PatientFamilyHistory
            {
                Id = Convert.ToInt32(r["Id"]),
                ConditionName = r["Name"].ToString(),
                Notes = r["Notes"]?.ToString(),
                CreatedDate = Convert.ToDateTime(r["CreatedDate"])
            });

            // Result 5: Latest Vitals
            if (r.NextResult() && r.Read()) vm.LatestVitals = MapVitals(r);

            // Result 6: Lab Results
            if (r.NextResult()) while (r.Read()) vm.LabResults.Add(new PatientLabResult
            {
                Id = Convert.ToInt32(r["Id"]),
                TestName = r["TestName"].ToString(),
                ResultValue = r["ResultValue"]?.ToString(),
                Unit = r["Unit"]?.ToString(),
                NormalRange = r["NormalRange"]?.ToString(),
                IsAbnormal = Convert.ToBoolean(r["IsAbnormal"]),
                ResultDate = Convert.ToDateTime(r["ResultDate"]),
                FilePath = r["FilePath"]?.ToString()
            });

            return vm;
        }

        // ── Private helper ────────────────────────────────────────────────
        private PatientVitals MapVitals(MySqlDataReader r) => new PatientVitals
        {
            Id = Convert.ToInt32(r["Id"]),
            PatientId = Convert.ToInt32(r["PatientId"]),
            HospitalId = Convert.ToInt32(r["Hospital_Id"]),
            Temperature = r["Temperature"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["Temperature"]),
            PulseRate = r["PulseRate"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["PulseRate"]),
            BPSystolic = r["BPSystolic"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["BPSystolic"]),
            BPDiastolic = r["BPDiastolic"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["BPDiastolic"]),
            SPO2 = r["SPO2"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["SPO2"]),
            RBS = r["RBS"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["RBS"]),
            Weight = r["Weight"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["Weight"]),
            Height = r["Height"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["Height"]),
            BMI = r["BMI"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["BMI"]),
            Respiration = r["Respiration"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Respiration"]),
            WaistCircumference = r["WaistCircumference"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["WaistCircumference"]),
            FIB4 = r["FIB4"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["FIB4"]),
            RecordedDate = Convert.ToDateTime(r["RecordedDate"])
        };
    }
}
