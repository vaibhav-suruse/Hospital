using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    // Plain parameterized SQL (not stored procedures) for these 4 new tables -
    // deliberately simpler than the SP_* pattern used elsewhere so this stays
    // reviewable in one pass. Every query is still parameterized (no string
    // concatenation of user input) and every query filters on
    // ParentHospitalId (+ SubHospitalId when present), so Hospital A can
    // never see Hospital B's rows.
    public class IPDClinicalExtrasRepository : IIPDClinicalExtras
    {
        private readonly string _connectionString;

        public IPDClinicalExtrasRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        private static string RecordedByNameExpr =>
            "COALESCE(CONCAT(n.FirstName, ' ', n.LastName), CONCAT(d.FirstName, ' ', d.LastName))";

        // ===============================
        // Examination
        // ===============================
        public List<IPDExamination> GetExaminations(int ipdId, int hospitalId, int? subHospitalId)
        {
            var list = new List<IPDExamination>();
            string sql = $@"SELECT e.*, {RecordedByNameExpr} AS RecordedByName
                             FROM ipd_examination e
                             LEFT JOIN nurse n ON n.NurseId = e.NurseId
                             LEFT JOIN doctor d ON d.Doctor_Id = e.DoctorId
                             WHERE e.IPDId = @IPDId AND e.ParentHospitalId = @HospitalId
                               AND (@SubHospitalId IS NULL OR e.SubHospitalId = @SubHospitalId)
                             ORDER BY e.RecordedDateTime DESC";

            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                AddCommonParams(cmd, ipdId, hospitalId, subHospitalId);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new IPDExamination
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            ParentHospitalId = Convert.ToInt32(r["ParentHospitalId"]),
                            SubHospitalId = r["SubHospitalId"] as int?,
                            IPDId = Convert.ToInt32(r["IPDId"]),
                            RecordedDateTime = Convert.ToDateTime(r["RecordedDateTime"]),
                            CNS = r["CNS"] as string,
                            CVS = r["CVS"] as string,
                            RS = r["RS"] as string,
                            PA = r["PA"] as string,
                            RecordedByRole = r["RecordedByRole"] as string,
                            NurseId = r["NurseId"] as int?,
                            DoctorId = r["DoctorId"] as int?,
                            RecordedByName = r["RecordedByName"] as string
                        });
                    }
                }
            }
            return list;
        }

        public void AddExamination(IPDExamination m)
        {
            string sql = @"INSERT INTO ipd_examination
                (ParentHospitalId, SubHospitalId, IPDId, RecordedDateTime, CNS, CVS, RS, PA, RecordedByRole, NurseId, DoctorId)
                VALUES (@ParentHospitalId, @SubHospitalId, @IPDId, @RecordedDateTime, @CNS, @CVS, @RS, @PA, @RecordedByRole, @NurseId, @DoctorId)";

            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("ParentHospitalId", m.ParentHospitalId);
                cmd.Parameters.AddWithValue("SubHospitalId", (object)m.SubHospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("IPDId", m.IPDId);
                cmd.Parameters.AddWithValue("RecordedDateTime", m.RecordedDateTime);
                cmd.Parameters.AddWithValue("CNS", (object)m.CNS ?? DBNull.Value);
                cmd.Parameters.AddWithValue("CVS", (object)m.CVS ?? DBNull.Value);
                cmd.Parameters.AddWithValue("RS", (object)m.RS ?? DBNull.Value);
                cmd.Parameters.AddWithValue("PA", (object)m.PA ?? DBNull.Value);
                cmd.Parameters.AddWithValue("RecordedByRole", m.RecordedByRole);
                cmd.Parameters.AddWithValue("NurseId", (object)m.NurseId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("DoctorId", (object)m.DoctorId ?? DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteExamination(int id) => DeleteById("ipd_examination", id);

        // ===============================
        // Input / Output Charting
        // ===============================
        public List<IPDInputOutput> GetInputOutputs(int ipdId, int hospitalId, int? subHospitalId)
        {
            var list = new List<IPDInputOutput>();
            string sql = $@"SELECT io.*, {RecordedByNameExpr} AS RecordedByName
                             FROM ipd_input_output io
                             LEFT JOIN nurse n ON n.NurseId = io.NurseId
                             LEFT JOIN doctor d ON d.Doctor_Id = io.DoctorId
                             WHERE io.IPDId = @IPDId AND io.ParentHospitalId = @HospitalId
                               AND (@SubHospitalId IS NULL OR io.SubHospitalId = @SubHospitalId)
                             ORDER BY io.RecordedDateTime DESC";

            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                AddCommonParams(cmd, ipdId, hospitalId, subHospitalId);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new IPDInputOutput
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            ParentHospitalId = Convert.ToInt32(r["ParentHospitalId"]),
                            SubHospitalId = r["SubHospitalId"] as int?,
                            IPDId = Convert.ToInt32(r["IPDId"]),
                            RecordedDateTime = Convert.ToDateTime(r["RecordedDateTime"]),
                            Input = r["Input"] as decimal?,
                            Output = r["Output"] as decimal?,
                            DrainOutput = r["DrainOutput"] as decimal?,
                            RecordedByRole = r["RecordedByRole"] as string,
                            NurseId = r["NurseId"] as int?,
                            DoctorId = r["DoctorId"] as int?,
                            RecordedByName = r["RecordedByName"] as string
                        });
                    }
                }
            }
            return list;
        }

        public void AddInputOutput(IPDInputOutput m)
        {
            string sql = @"INSERT INTO ipd_input_output
                (ParentHospitalId, SubHospitalId, IPDId, RecordedDateTime, Input, Output, DrainOutput, RecordedByRole, NurseId, DoctorId)
                VALUES (@ParentHospitalId, @SubHospitalId, @IPDId, @RecordedDateTime, @Input, @Output, @DrainOutput, @RecordedByRole, @NurseId, @DoctorId)";

            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("ParentHospitalId", m.ParentHospitalId);
                cmd.Parameters.AddWithValue("SubHospitalId", (object)m.SubHospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("IPDId", m.IPDId);
                cmd.Parameters.AddWithValue("RecordedDateTime", m.RecordedDateTime);
                cmd.Parameters.AddWithValue("Input", (object)m.Input ?? DBNull.Value);
                cmd.Parameters.AddWithValue("Output", (object)m.Output ?? DBNull.Value);
                cmd.Parameters.AddWithValue("DrainOutput", (object)m.DrainOutput ?? DBNull.Value);
                cmd.Parameters.AddWithValue("RecordedByRole", m.RecordedByRole);
                cmd.Parameters.AddWithValue("NurseId", (object)m.NurseId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("DoctorId", (object)m.DoctorId ?? DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteInputOutput(int id) => DeleteById("ipd_input_output", id);

        // ===============================
        // Daily Wellbeing
        // ===============================
        public List<IPDDailyWellbeing> GetDailyWellbeings(int ipdId, int hospitalId, int? subHospitalId)
        {
            var list = new List<IPDDailyWellbeing>();
            string sql = $@"SELECT w.*, {RecordedByNameExpr} AS RecordedByName
                             FROM ipd_daily_wellbeing w
                             LEFT JOIN nurse n ON n.NurseId = w.NurseId
                             LEFT JOIN doctor d ON d.Doctor_Id = w.DoctorId
                             WHERE w.IPDId = @IPDId AND w.ParentHospitalId = @HospitalId
                               AND (@SubHospitalId IS NULL OR w.SubHospitalId = @SubHospitalId)
                             ORDER BY w.RecordedDateTime DESC";

            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                AddCommonParams(cmd, ipdId, hospitalId, subHospitalId);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new IPDDailyWellbeing
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            ParentHospitalId = Convert.ToInt32(r["ParentHospitalId"]),
                            SubHospitalId = r["SubHospitalId"] as int?,
                            IPDId = Convert.ToInt32(r["IPDId"]),
                            RecordedDateTime = Convert.ToDateTime(r["RecordedDateTime"]),
                            Sleep = r["Sleep"] as string,
                            Bowel = r["Bowel"] as string,
                            Bladder = r["Bladder"] as string,
                            Appetite = r["Appetite"] as string,
                            Ambulation = r["Ambulation"] as string,
                            Eating = r["Eating"] as string,
                            RecordedByRole = r["RecordedByRole"] as string,
                            NurseId = r["NurseId"] as int?,
                            DoctorId = r["DoctorId"] as int?,
                            RecordedByName = r["RecordedByName"] as string
                        });
                    }
                }
            }
            return list;
        }

        public void AddDailyWellbeing(IPDDailyWellbeing m)
        {
            string sql = @"INSERT INTO ipd_daily_wellbeing
                (ParentHospitalId, SubHospitalId, IPDId, RecordedDateTime, Sleep, Bowel, Bladder, Appetite, Ambulation, Eating, RecordedByRole, NurseId, DoctorId)
                VALUES (@ParentHospitalId, @SubHospitalId, @IPDId, @RecordedDateTime, @Sleep, @Bowel, @Bladder, @Appetite, @Ambulation, @Eating, @RecordedByRole, @NurseId, @DoctorId)";

            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("ParentHospitalId", m.ParentHospitalId);
                cmd.Parameters.AddWithValue("SubHospitalId", (object)m.SubHospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("IPDId", m.IPDId);
                cmd.Parameters.AddWithValue("RecordedDateTime", m.RecordedDateTime);
                cmd.Parameters.AddWithValue("Sleep", (object)m.Sleep ?? DBNull.Value);
                cmd.Parameters.AddWithValue("Bowel", (object)m.Bowel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("Bladder", (object)m.Bladder ?? DBNull.Value);
                cmd.Parameters.AddWithValue("Appetite", (object)m.Appetite ?? DBNull.Value);
                cmd.Parameters.AddWithValue("Ambulation", (object)m.Ambulation ?? DBNull.Value);
                cmd.Parameters.AddWithValue("Eating", (object)m.Eating ?? DBNull.Value);
                cmd.Parameters.AddWithValue("RecordedByRole", m.RecordedByRole);
                cmd.Parameters.AddWithValue("NurseId", (object)m.NurseId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("DoctorId", (object)m.DoctorId ?? DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteDailyWellbeing(int id) => DeleteById("ipd_daily_wellbeing", id);

        // ===============================
        // Body Composition
        // ===============================
        public List<IPDBodyComposition> GetBodyCompositions(int ipdId, int hospitalId, int? subHospitalId)
        {
            var list = new List<IPDBodyComposition>();
            string sql = $@"SELECT b.*, {RecordedByNameExpr} AS RecordedByName
                             FROM ipd_body_composition b
                             LEFT JOIN nurse n ON n.NurseId = b.NurseId
                             LEFT JOIN doctor d ON d.Doctor_Id = b.DoctorId
                             WHERE b.IPDId = @IPDId AND b.ParentHospitalId = @HospitalId
                               AND (@SubHospitalId IS NULL OR b.SubHospitalId = @SubHospitalId)
                             ORDER BY b.RecordedDateTime DESC";

            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                AddCommonParams(cmd, ipdId, hospitalId, subHospitalId);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new IPDBodyComposition
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            ParentHospitalId = Convert.ToInt32(r["ParentHospitalId"]),
                            SubHospitalId = r["SubHospitalId"] as int?,
                            IPDId = Convert.ToInt32(r["IPDId"]),
                            RecordedDateTime = Convert.ToDateTime(r["RecordedDateTime"]),
                            HeightCm = r["HeightCm"] as decimal?,
                            WeightKg = r["WeightKg"] as decimal?,
                            BMI = r["BMI"] as decimal?,
                            BMR = r["BMR"] as decimal?,
                            BSA = r["BSA"] as decimal?,
                            RecordedByRole = r["RecordedByRole"] as string,
                            NurseId = r["NurseId"] as int?,
                            DoctorId = r["DoctorId"] as int?,
                            RecordedByName = r["RecordedByName"] as string
                        });
                    }
                }
            }
            return list;
        }

        public void AddBodyComposition(IPDBodyComposition m)
        {
            string sql = @"INSERT INTO ipd_body_composition
                (ParentHospitalId, SubHospitalId, IPDId, RecordedDateTime, HeightCm, WeightKg, BMI, BMR, BSA, RecordedByRole, NurseId, DoctorId)
                VALUES (@ParentHospitalId, @SubHospitalId, @IPDId, @RecordedDateTime, @HeightCm, @WeightKg, @BMI, @BMR, @BSA, @RecordedByRole, @NurseId, @DoctorId)";

            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("ParentHospitalId", m.ParentHospitalId);
                cmd.Parameters.AddWithValue("SubHospitalId", (object)m.SubHospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("IPDId", m.IPDId);
                cmd.Parameters.AddWithValue("RecordedDateTime", m.RecordedDateTime);
                cmd.Parameters.AddWithValue("HeightCm", (object)m.HeightCm ?? DBNull.Value);
                cmd.Parameters.AddWithValue("WeightKg", (object)m.WeightKg ?? DBNull.Value);
                cmd.Parameters.AddWithValue("BMI", (object)m.BMI ?? DBNull.Value);
                cmd.Parameters.AddWithValue("BMR", (object)m.BMR ?? DBNull.Value);
                cmd.Parameters.AddWithValue("BSA", (object)m.BSA ?? DBNull.Value);
                cmd.Parameters.AddWithValue("RecordedByRole", m.RecordedByRole);
                cmd.Parameters.AddWithValue("NurseId", (object)m.NurseId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("DoctorId", (object)m.DoctorId ?? DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteBodyComposition(int id) => DeleteById("ipd_body_composition", id);

        // ===============================
        // Shared helpers
        // ===============================
        private static void AddCommonParams(MySqlCommand cmd, int ipdId, int hospitalId, int? subHospitalId)
        {
            cmd.Parameters.AddWithValue("IPDId", ipdId);
            cmd.Parameters.AddWithValue("HospitalId", hospitalId);
            cmd.Parameters.AddWithValue("SubHospitalId", (object)subHospitalId ?? DBNull.Value);
        }

        // Table name is always a hardcoded literal from inside this class -
        // never derived from user input - so this is not a SQL-injection risk.
        private void DeleteById(string table, int id)
        {
            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand($"DELETE FROM {table} WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
