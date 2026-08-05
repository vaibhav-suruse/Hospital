using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class PatientPortalRepository : IPatientPortal
    {
        private readonly string _connectionString;

        public PatientPortalRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        // ── DASHBOARD SUMMARY ────────────────────────────────────────────────
        public PatientDashboardVM GetDashboardSummary(int patientId)
        {
            var vm = new PatientDashboardVM { PatientId = patientId };
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // 1️⃣ Patient basic info
                using (var cmd = new MySqlCommand(@"
                    SELECT Id, FirstName, LastName, Gender, Age,
                           PhoneNumber, Email, Address
                    FROM   tbl_patient
                    WHERE  Id = @patientId", conn))
                {
                    cmd.Parameters.AddWithValue("@patientId", patientId);
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        vm.PatientName = r["FirstName"] + " " + r["LastName"];
                        vm.Gender = r["Gender"].ToString();
                        vm.Age = r["Age"].ToString();
                        vm.PhoneNumber = r["PhoneNumber"].ToString();
                        vm.Email = r["Email"].ToString();
                        vm.Address = r["Address"].ToString();
                    }
                }

                // 2️⃣ Appointment counts + next appointment
                using (var cmd = new MySqlCommand(@"
                    SELECT
                        COUNT(*)                          AS TotalOPD,
                        SUM(CASE WHEN Status = 'Pending'
                                  AND AppointmentDate >= CURDATE()
                                 THEN 1 ELSE 0 END)       AS Upcoming
                    FROM opdappointment
                    WHERE PatientId = @patientId
                      AND IsActive  = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@patientId", patientId);
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        vm.TotalOPDVisits = Convert.ToInt32(r["TotalOPD"]);
                        vm.UpcomingAppointmentsCount = r["Upcoming"] == DBNull.Value
                                                          ? 0
                                                          : Convert.ToInt32(r["Upcoming"]);
                    }
                }

                // 3️⃣ Next appointment detail
                using (var cmd = new MySqlCommand(@"
                    SELECT a.AppointmentDate, a.AppointmentTime,
                           CONCAT(d.FirstName,' ',d.LastName) AS DoctorName
                    FROM   opdappointment a
                    LEFT   JOIN doctor d ON a.DoctorId = d.Doctor_Id
                    WHERE  a.PatientId       = @patientId
                      AND  a.Status          = 'Pending'
                      AND  a.AppointmentDate >= CURDATE()
                      AND  a.IsActive        = 1
                    ORDER  BY a.AppointmentDate, a.AppointmentTime
                    LIMIT  1", conn))
                {
                    cmd.Parameters.AddWithValue("@patientId", patientId);
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        vm.NextAppointmentDate = Convert.ToDateTime(r["AppointmentDate"]);
                        vm.NextAppointmentTime = TimeSpan.Parse(r["AppointmentTime"].ToString());
                        vm.NextDoctorName = r["DoctorName"].ToString();
                    }
                }

                // 4️⃣ IPD admissions count
                using (var cmd = new MySqlCommand(@"
                    SELECT COUNT(*) AS TotalIPD
                    FROM   ipdadmission
                    WHERE  PatientId = @patientId", conn))
                {
                    cmd.Parameters.AddWithValue("@patientId", patientId);
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                        vm.TotalIPDAdmissions = Convert.ToInt32(r["TotalIPD"]);
                }

                // 5️⃣ Total due amount (OPD bills)
                using (var cmd = new MySqlCommand(@"
                    SELECT IFNULL(SUM(ob.DueAmount), 0) AS OPDDue
                    FROM   opd_bill ob
                    INNER  JOIN opdappointment oa
                            ON ob.AppointmentId = oa.Id
                    WHERE  oa.PatientId = @patientId
                      AND  ob.PaymentStatus != 'Paid'", conn))
                {
                    cmd.Parameters.AddWithValue("@patientId", patientId);
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                        vm.TotalDueAmount = Convert.ToDecimal(r["OPDDue"]);
                }

                // 6️⃣ Also add IPD due
                using (var cmd = new MySqlCommand(@"
                    SELECT IFNULL(SUM(ib.DueAmount), 0) AS IPDDue
                    FROM   ipd_bill ib
                    INNER  JOIN ipdadmission ia ON ib.IPDId = ia.IPDId
                    WHERE  ia.PatientId      = @patientId
                      AND  ib.PaymentStatus != 'Paid'
                      AND  ib.IsActive       = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@patientId", patientId);
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                        vm.TotalDueAmount += Convert.ToDecimal(r["IPDDue"]);
                }

                // 7️⃣ Recent 3 appointments for dashboard
                vm.RecentAppointments = GetRecentAppointments(patientId, conn);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching patient dashboard", ex);
            }
            return vm;
        }

        private List<PatientAppointmentVM> GetRecentAppointments(int patientId, MySqlConnection conn)
        {
            var list = new List<PatientAppointmentVM>();
            using var cmd = new MySqlCommand(@"
                SELECT a.Id, a.PatientId, a.DoctorId,
                       a.AppointmentDate, a.AppointmentTime, a.Status,
                       CONCAT(d.FirstName,' ',d.LastName) AS DoctorName,
                       d.Specialization
                FROM   opdappointment a
                LEFT   JOIN doctor d ON a.DoctorId = d.Doctor_Id
                WHERE  a.PatientId = @patientId
                  AND  a.IsActive  = 1
                ORDER  BY a.AppointmentDate DESC
                LIMIT  3", conn);
            cmd.Parameters.AddWithValue("@patientId", patientId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new PatientAppointmentVM
                {
                    AppointmentId = Convert.ToInt32(r["Id"]),
                    PatientId = Convert.ToInt32(r["PatientId"]),
                    DoctorId = Convert.ToInt32(r["DoctorId"]),
                    DoctorName = r["DoctorName"].ToString(),
                    DoctorSpecialization = r["Specialization"].ToString(),
                    AppointmentDate = Convert.ToDateTime(r["AppointmentDate"]),
                    AppointmentTime = TimeSpan.Parse(r["AppointmentTime"].ToString()),
                    Status = r["Status"].ToString()
                });
            }
            return list;
        }

        // ── ALL APPOINTMENTS ─────────────────────────────────────────────────
        public List<PatientAppointmentVM> GetAppointmentsByPatientId(int patientId)
        {
            var list = new List<PatientAppointmentVM>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
                    SELECT  a.Id, a.PatientId, a.DoctorId,
                            a.AppointmentDate, a.AppointmentTime, a.Status,
                            CONCAT(d.FirstName,' ',d.LastName) AS DoctorName,
                            d.Specialization,
                            om.Id AS OPDId
                    FROM    opdappointment a
                    LEFT    JOIN doctor d
                            ON a.DoctorId = d.Doctor_Id
                    LEFT    JOIN opdmaster om
                            ON om.AppointmentId = a.Id
                    WHERE   a.PatientId = @patientId
                      AND   a.IsActive  = 1
                    ORDER   BY a.AppointmentDate DESC", conn);
                cmd.Parameters.AddWithValue("@patientId", patientId);
                conn.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new PatientAppointmentVM
                    {
                        AppointmentId = Convert.ToInt32(r["Id"]),
                        PatientId = Convert.ToInt32(r["PatientId"]),
                        DoctorId = Convert.ToInt32(r["DoctorId"]),
                        DoctorName = r["DoctorName"].ToString(),
                        DoctorSpecialization = r["Specialization"].ToString(),
                        AppointmentDate = Convert.ToDateTime(r["AppointmentDate"]),
                        AppointmentTime = TimeSpan.Parse(r["AppointmentTime"].ToString()),
                        Status = r["Status"].ToString(),
                        OPDId = r["OPDId"] == DBNull.Value
                                               ? (int?)null
                                               : Convert.ToInt32(r["OPDId"])
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching appointments", ex);
            }
            return list;
        }

        // ── CANCEL APPOINTMENT ───────────────────────────────────────────────
        // Patient can only cancel their own Pending appointments
        public bool CancelAppointment(int appointmentId, int patientId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
                    UPDATE opdappointment
                    SET    Status = 'Cancelled'
                    WHERE  Id        = @appointmentId
                      AND  PatientId = @patientId
                      AND  Status    = 'Pending'
                      AND  AppointmentDate >= CURDATE()", conn);
                cmd.Parameters.AddWithValue("@appointmentId", appointmentId);
                cmd.Parameters.AddWithValue("@patientId", patientId);
                conn.Open();
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error cancelling appointment", ex);
            }
        }

        // ── OPD HISTORY ──────────────────────────────────────────────────────
        public List<PatientOPDHistoryVM> GetOPDHistoryByPatientId(int patientId)
        {
            var list = new List<PatientOPDHistoryVM>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                // 1️⃣ OPD visits with doctor info
                using (var cmd = new MySqlCommand(@"
                    SELECT  om.Id AS OPDId,
                            om.AppointmentId,
                            a.AppointmentDate,
                            om.BP, om.Pulse, om.Investigation,
                            om.ReportDetail, om.ReportFilePath,
                            om.NextAppointmentDate,
                            CONCAT(d.FirstName,' ',d.LastName) AS DoctorName,
                            d.Specialization
                    FROM    opdmaster om
                    INNER   JOIN opdappointment a
                            ON om.AppointmentId = a.Id
                    LEFT    JOIN doctor d
                            ON a.DoctorId = d.Doctor_Id
                    WHERE   a.PatientId = @patientId
                      AND   a.IsActive  = 1
                    ORDER   BY a.AppointmentDate DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@patientId", patientId);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        list.Add(new PatientOPDHistoryVM
                        {
                            OPDId = Convert.ToInt32(r["OPDId"]),
                            AppointmentId = Convert.ToInt32(r["AppointmentId"]),
                            AppointmentDate = Convert.ToDateTime(r["AppointmentDate"]),
                            DoctorName = r["DoctorName"].ToString(),
                            DoctorSpecialization = r["Specialization"].ToString(),
                            BP = r["BP"].ToString(),
                            Pulse = r["Pulse"].ToString(),
                            Investigation = r["Investigation"].ToString(),
                            ReportDetail = r["ReportDetail"].ToString(),
                            ReportFilePath = r["ReportFilePath"].ToString(),
                            NextAppointmentDate = r["NextAppointmentDate"] == DBNull.Value
                                                  ? (DateTime?)null
                                                  : Convert.ToDateTime(r["NextAppointmentDate"])
                        });
                    }
                }

                // 2️⃣ Load symptoms for each OPD visit
                foreach (var opd in list)
                {
                    using var cmd = new MySqlCommand(@"
                        SELECT s.SymptomName
                        FROM   opdsymptom os
                        INNER  JOIN symptoms s ON os.Symptom_Id = s.SymptomId
                        WHERE  os.OPD_Id = @opdId", conn);
                    cmd.Parameters.AddWithValue("@opdId", opd.OPDId);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                        opd.Symptoms.Add(r["SymptomName"].ToString());
                }

                // 3️⃣ Load medicines for each OPD visit
                foreach (var opd in list)
                {
                    using var cmd = new MySqlCommand(@"
                        SELECT  m.MedicineName,
                                om.Morning, om.Afternoon,
                                om.Evening, om.Days
                        FROM    opdmedicine om
                        INNER   JOIN tbl_medicine m
                                ON om.Medicine_Id = m.MedicineId
                        WHERE   om.OPD_Id = @opdId", conn);
                    cmd.Parameters.AddWithValue("@opdId", opd.OPDId);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        opd.Medicines.Add(new OPDMedicineVM
                        {
                            MedicineName = r["MedicineName"].ToString(),
                            Morning = Convert.ToInt32(r["Morning"]),
                            Afternoon = Convert.ToInt32(r["Afternoon"]),
                            Evening = Convert.ToInt32(r["Evening"]),
                            Days = Convert.ToInt32(r["Days"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching OPD history", ex);
            }
            return list;
        }

        // ── IPD HISTORY ──────────────────────────────────────────────────────
        public List<PatientIPDHistoryVM> GetIPDHistoryByPatientId(int patientId)
        {
            var list = new List<PatientIPDHistoryVM>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
                    SELECT  ia.IPDId,
                            ia.AdmissionNumber,
                            ia.AdmissionDateTime,
                            ia.ActualDischargeDateTime,
                            ia.Status,
                            ia.ReasonForAdmission,
                            GREATEST(1, DATEDIFF(
                                IFNULL(ia.ActualDischargeDateTime, NOW()),
                                ia.AdmissionDateTime
                            ) + 1)                               AS TotalDays,
                            CONCAT(d.FirstName,' ',d.LastName)   AS DoctorName,
                            w.WardName,
                            b.BedNumber,
                            ib.BillId,
                            ib.PaymentStatus,
                            ib.TotalAmount,
                            ib.PaidAmount,
                            ib.DueAmount
                    FROM    ipdadmission ia
                    LEFT    JOIN doctor d
                            ON ia.PrimaryDoctorId = d.Doctor_Id
                    LEFT    JOIN ipdbedallocation ba
                            ON ia.IPDId = ba.IPDId AND ba.IsCurrent = 1
                    LEFT    JOIN bed b
                            ON ba.BedId = b.BedId
                    LEFT    JOIN ward w
                            ON b.WardId = w.WardId
                    LEFT    JOIN ipd_bill ib
                            ON ia.IPDId = ib.IPDId AND ib.IsActive = 1
                    WHERE   ia.PatientId = @patientId
                    ORDER   BY ia.AdmissionDateTime DESC", conn);
                cmd.Parameters.AddWithValue("@patientId", patientId);
                conn.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new PatientIPDHistoryVM
                    {
                        IPDId = Convert.ToInt32(r["IPDId"]),
                        AdmissionNumber = r["AdmissionNumber"].ToString(),
                        AdmissionDateTime = Convert.ToDateTime(r["AdmissionDateTime"]),
                        DischargeDateTime = r["ActualDischargeDateTime"] == DBNull.Value
                                              ? (DateTime?)null
                                              : Convert.ToDateTime(r["ActualDischargeDateTime"]),
                        Status = r["Status"].ToString(),
                        ReasonForAdmission = r["ReasonForAdmission"].ToString(),
                        TotalDays = Convert.ToInt32(r["TotalDays"]),
                        DoctorName = r["DoctorName"].ToString(),
                        WardName = r["WardName"].ToString(),
                        BedNumber = r["BedNumber"].ToString(),
                        HasBill = r["BillId"] != DBNull.Value,
                        BillId = r["BillId"] == DBNull.Value
                                              ? (int?)null
                                              : Convert.ToInt32(r["BillId"]),
                        PaymentStatus = r["PaymentStatus"].ToString(),
                        TotalAmount = r["TotalAmount"] == DBNull.Value
                                              ? 0 : Convert.ToDecimal(r["TotalAmount"]),
                        PaidAmount = r["PaidAmount"] == DBNull.Value
                                              ? 0 : Convert.ToDecimal(r["PaidAmount"]),
                        DueAmount = r["DueAmount"] == DBNull.Value
                                              ? 0 : Convert.ToDecimal(r["DueAmount"])
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching IPD history", ex);
            }
            return list;
        }

        // ── OPD BILLS ────────────────────────────────────────────────────────
        public List<PatientOPDBillVM> GetOPDBillsByPatientId(int patientId)
        {
            var list = new List<PatientOPDBillVM>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
                    SELECT  ob.BillId, ob.AppointmentId,
                            ob.BillNumber, ob.BillDate,
                            ob.ConsultationFee, ob.MedicineCharges,
                            ob.TotalAmount, ob.PaidAmount,
                            ob.DueAmount, ob.PaymentStatus,
                            CONCAT(d.FirstName,' ',d.LastName) AS DoctorName
                    FROM    opd_bill ob
                    INNER   JOIN opdappointment oa
                            ON ob.AppointmentId = oa.Id
                    LEFT    JOIN doctor d
                            ON oa.DoctorId = d.Doctor_Id
                    WHERE   oa.PatientId = @patientId
                    ORDER   BY ob.BillDate DESC", conn);
                cmd.Parameters.AddWithValue("@patientId", patientId);
                conn.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new PatientOPDBillVM
                    {
                        BillId = Convert.ToInt32(r["BillId"]),
                        AppointmentId = Convert.ToInt32(r["AppointmentId"]),
                        BillNumber = r["BillNumber"].ToString(),
                        BillDate = Convert.ToDateTime(r["BillDate"]),
                        DoctorName = r["DoctorName"].ToString(),
                        ConsultationFee = Convert.ToDecimal(r["ConsultationFee"]),
                        MedicineCharges = Convert.ToDecimal(r["MedicineCharges"]),
                        TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
                        PaidAmount = Convert.ToDecimal(r["PaidAmount"]),
                        DueAmount = Convert.ToDecimal(r["DueAmount"]),
                        PaymentStatus = r["PaymentStatus"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching OPD bills", ex);
            }
            return list;
        }


        public List<PatientIPDRoundVM> GetIPDRoundsByIPDId(
    int ipdId,
    int patientId,
    int hospitalId)
        {
            var list = new List<PatientIPDRoundVM>();

            try
            {
                using var conn =
                    new MySqlConnection(_connectionString);
                conn.Open();

              
                using (var cmd = new MySqlCommand(@"
            SELECT
                r.RoundId,
                r.RoundDateTime,
                r.RoundType,
                r.PatientCondition,
                r.Diagnosis,
                r.Notes,
                r.Instructions,
                r.IsAbnormal,
                CONCAT(d.FirstName,' ',d.LastName)
                    AS DoctorName,
                d.Specialization
            FROM ipd_doctor_round r
            LEFT JOIN doctor d
                   ON r.DoctorId = d.Doctor_Id
            INNER JOIN ipdadmission ia
                    ON r.IPDId = ia.IPDId
            WHERE r.IPDId    = @ipdId
              AND ia.PatientId = @patientId
              AND r.IsActive = 1
            ORDER BY r.RoundDateTime DESC",
                    conn))
                {
                    cmd.Parameters.AddWithValue(
                        "@ipdId", ipdId);
                   

                    cmd.Parameters.AddWithValue(
                        "@patientId", patientId);
                    

                    using var dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        
                        list.Add(new PatientIPDRoundVM
                        {
                            RoundId = Convert.ToInt32(
                                dr["RoundId"]),
                           

                            RoundDateTime = Convert.ToDateTime(
                                dr["RoundDateTime"]),
                           

                            RoundType = dr["RoundType"]
                                .ToString(),
                          

                            PatientCondition =
                                dr["PatientCondition"]
                                .ToString(),
                           

                            Diagnosis = dr["Diagnosis"]
                                .ToString(),
                            

                            Notes = dr["Notes"].ToString(),
                            

                            Instructions = dr["Instructions"]
                                .ToString(),
                           

                            IsAbnormal = Convert.ToBoolean(
                                dr["IsAbnormal"]),
                           

                            DoctorName = dr["DoctorName"]
                                .ToString(),
                         

                            Specialization =
                                dr["Specialization"]
                                .ToString()
                           
                        });
                    }
                }
               
                foreach (var round in list)
                {
                 
                    using (var cmd = new MySqlCommand(@"
                SELECT
                    s.SymptomName,
                    s.SubName
                FROM ipd_round_symptom rs
                INNER JOIN symptoms s
                        ON rs.SymptomId = s.SymptomId
                WHERE rs.RoundId  = @roundId
                  AND rs.IsActive = 1",
                        conn))
                    {
                        cmd.Parameters.AddWithValue(
                            "@roundId", round.RoundId);
                      

                        using var dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            round.Symptoms.Add(
                                new PatientRoundSymptomVM
                                {
                                    SymptomName =
                                        dr["SymptomName"]
                                        .ToString(),
                                   

                                    SubName =
                                        dr["SubName"]
                                            == DBNull.Value
                                        ? ""
                                        : dr["SubName"]
                                            .ToString()
                                    
                                });
                        }
                    }

                    // ── Fetch Medicines for this round ────
                    using (var cmd = new MySqlCommand(@"
                SELECT
                    m.MedicineName,
                    m.Type        AS MedicineType,
                    rp.Morning,
                    rp.Afternoon,
                    rp.Evening,
                    rp.Days,
                    rp.Route,
                    rp.Dosage,
                    rp.Instructions,
                    rp.Status
                FROM ipd_round_prescription rp
                INNER JOIN tbl_medicine m
                        ON rp.MedicineId = m.MedicineId
                WHERE rp.RoundId  = @roundId
                  AND rp.IsActive = 1",
                        conn))
                    {
                        cmd.Parameters.AddWithValue(
                            "@roundId", round.RoundId);

                        using var dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            round.Medicines.Add(
                                new PatientRoundMedicineVM
                                {
                                    MedicineName =
                                        dr["MedicineName"]
                                        .ToString(),

                                    MedicineType =
                                        dr["MedicineType"]
                                            == DBNull.Value
                                        ? ""
                                        : dr["MedicineType"]
                                            .ToString(),

                                    Morning =
                                        dr["Morning"] !=
                                        DBNull.Value &&
                                        Convert.ToInt32(
                                            dr["Morning"]) == 1,
                                  

                                    Afternoon =
                                        dr["Afternoon"] !=
                                        DBNull.Value &&
                                        Convert.ToInt32(
                                            dr["Afternoon"]) == 1,

                                    Evening =
                                        dr["Evening"] !=
                                        DBNull.Value &&
                                        Convert.ToInt32(
                                            dr["Evening"]) == 1,

                                    Days =
                                        dr["Days"] ==
                                        DBNull.Value
                                        ? (int?)null
                                        : Convert.ToInt32(
                                            dr["Days"]),
                                    

                                    Route =
                                        dr["Route"] ==
                                        DBNull.Value
                                        ? "Oral"
                                        : dr["Route"]
                                            .ToString(),

                                    Dosage =
                                        dr["Dosage"] ==
                                        DBNull.Value
                                        ? ""
                                        : dr["Dosage"]
                                            .ToString(),

                                    Instructions =
                                        dr["Instructions"] ==
                                        DBNull.Value
                                        ? ""
                                        : dr["Instructions"]
                                            .ToString(),

                                    Status =
                                        dr["Status"] ==
                                        DBNull.Value
                                        ? "Active"
                                        : dr["Status"]
                                            .ToString()
                                });
                        }
                    }

                   
                    using (var cmd = new MySqlCommand(@"
                SELECT
                    ri.InvestigationType,
                    ri.TestName,
                    ri.Priority,
                    ri.Status,
                    ri.Result,
                    ri.Instructions
                FROM ipd_round_investigation ri
                WHERE ri.RoundId  = @roundId
                  AND ri.IsActive = 1",
                        conn))
                    {
                        cmd.Parameters.AddWithValue(
                            "@roundId", round.RoundId);

                        using var dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            round.Investigations.Add(
                                new PatientRoundInvestigationVM
                                {
                                    InvestigationType =
                                        dr["InvestigationType"]
                                        .ToString(),
                                    

                                    TestName =
                                        dr["TestName"]
                                        .ToString(),
                                   

                                    Priority =
                                        dr["Priority"]
                                        .ToString(),
                                  

                                    Status =
                                        dr["Status"]
                                        .ToString(),
                                   

                                    Result =
                                        dr["Result"] ==
                                        DBNull.Value
                                        ? ""
                                        : dr["Result"]
                                            .ToString(),
                                   

                                    Instructions =
                                        dr["Instructions"] ==
                                        DBNull.Value
                                        ? ""
                                        : dr["Instructions"]
                                            .ToString()
                                });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Error fetching IPD rounds", ex);
            }

            return list;
          
        }

       
        public List<PatientVitalsVM> GetVitalsByIPDId(
            int ipdId,
            int patientId,
            int hospitalId,
            int? subHospitalId)
        {
            var list = new List<PatientVitalsVM>();

            try
            {
                using var conn =
                    new MySqlConnection(_connectionString);

                using var cmd = new MySqlCommand(@"
            SELECT
                v.RecordedDateTime,
                v.Temperature,
                v.Pulse,
                v.Systolic,
                v.Diastolic,
                v.OxygenSaturation,
                v.RespirationRate,
                v.IsAbnormal,
                v.Notes
            FROM ipd_nurse_vitals v
            INNER JOIN ipdadmission ia
                    ON v.IPDId = ia.IPDId
            WHERE v.IPDId      = @ipdId
              AND ia.PatientId = @patientId
              AND v.IsActive   = 1
            ORDER BY v.RecordedDateTime DESC",
                    conn);
               

                cmd.Parameters.AddWithValue(
                    "@ipdId", ipdId);
                cmd.Parameters.AddWithValue(
                    "@patientId", patientId);
               

                conn.Open();
                using var dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new PatientVitalsVM
                    {
                        RecordedDateTime =
                            Convert.ToDateTime(
                                dr["RecordedDateTime"]),
                      

                        Temperature =
                            dr["Temperature"] ==
                            DBNull.Value
                            ? (decimal?)null
                            : Convert.ToDecimal(
                                dr["Temperature"]),
                      

                        Pulse =
                            dr["Pulse"] == DBNull.Value
                            ? (int?)null
                            : Convert.ToInt32(dr["Pulse"]),

                        Systolic =
                            dr["Systolic"] == DBNull.Value
                            ? (int?)null
                            : Convert.ToInt32(
                                dr["Systolic"]),

                        Diastolic =
                            dr["Diastolic"] == DBNull.Value
                            ? (int?)null
                            : Convert.ToInt32(
                                dr["Diastolic"]),

                        OxygenSaturation =
                            dr["OxygenSaturation"] ==
                            DBNull.Value
                            ? (int?)null
                            : Convert.ToInt32(
                                dr["OxygenSaturation"]),

                        RespirationRate =
                            dr["RespirationRate"] ==
                            DBNull.Value
                            ? (int?)null
                            : Convert.ToInt32(
                                dr["RespirationRate"]),

                        IsAbnormal =
                            Convert.ToBoolean(
                                dr["IsAbnormal"]),
                       

                        Notes =
                            dr["Notes"] == DBNull.Value
                            ? ""
                            : dr["Notes"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Error fetching vitals", ex);
            }

            return list;
        }
    }
}
