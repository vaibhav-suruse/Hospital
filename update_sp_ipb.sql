-- Update sp_IPB_GetBillSummary to also return PatientId in RS1
-- so the Billing Sheet back button can redirect to the IPD tabs page.

DROP PROCEDURE IF EXISTS `sp_IPB_GetBillSummary`;

DELIMITER $$

CREATE PROCEDURE `sp_IPB_GetBillSummary`(
    IN p_IPDId         INT,
    IN p_HospitalId    INT,
    IN p_SubHospitalId INT
)
BEGIN
    -- RS1: Patient + admission info (hospital-scoped)
    SELECT
        ia.IPDId,
        ia.PatientId,
        ia.AdmissionNumber,
        ia.AdmissionDateTime,
        ia.ActualDischargeDateTime,
        GREATEST(1, DATEDIFF(COALESCE(ia.ActualDischargeDateTime, NOW()), ia.AdmissionDateTime) + 1) AS TotalDays,
        CONCAT(p.FirstName, ' ', IFNULL(p.LastName, '')) AS PatientName,
        p.Age,
        p.Gender,
        p.PhoneNumber,
        CONCAT(d.FirstName, ' ', IFNULL(d.LastName, '')) AS DoctorName
    FROM `ipdadmission` ia
    INNER JOIN `tbl_patient` p ON ia.PatientId = p.Id
    LEFT JOIN `doctor` d ON ia.PrimaryDoctorId = d.Doctor_Id
    WHERE ia.IPDId = p_IPDId
      AND ia.ParentHospitalId = p_HospitalId
      AND (p_SubHospitalId = 0 OR ia.SubHospitalId = p_SubHospitalId OR ia.SubHospitalId IS NULL)
    LIMIT 1;

    -- RS2: Bed charges ? auto Days ₧ ChargesPerDay, per allocation
    SELECT
        ba.AllocationId,
        b.BedId,
        b.BedNumber,
        w.WardName,
        r.RoomNumber,
        b.ChargesPerDay,
        ba.StartDateTime,
        COALESCE(ba.EndDateTime, NOW()) AS EndDateTime,
        GREATEST(1, DATEDIFF(COALESCE(ba.EndDateTime, NOW()), ba.StartDateTime) + 1) AS Days,
        GREATEST(1, DATEDIFF(COALESCE(ba.EndDateTime, NOW()), ba.StartDateTime) + 1) * b.ChargesPerDay AS BedCharge
    FROM `ipdbedallocation` ba
    INNER JOIN `bed` b  ON ba.BedId  = b.BedId
    INNER JOIN `ward` w ON b.WardId  = w.WardId
    INNER JOIN `room` r ON b.RoomId  = r.RoomId
    WHERE ba.IPDId = p_IPDId
    ORDER BY ba.StartDateTime;

    -- RS3: Doctor Round charges ? auto-priced from the active "DoctorVisit" rate
    SELECT
        rnd.RoundId,
        rnd.RoundDateTime,
        rnd.RoundType,
        CONCAT(d.FirstName, ' ', d.LastName) AS DoctorName,
        IFNULL((
            SELECT bm.Amount
            FROM `billingmaster` bm
            WHERE bm.Category = 'DoctorVisit'
              AND bm.IsActive = 1
              AND bm.Hospital_Id = p_HospitalId
              AND (p_SubHospitalId = 0 OR bm.SubHospital_Id = p_SubHospitalId OR bm.SubHospital_Id IS NULL)
            ORDER BY (bm.SubHospital_Id = p_SubHospitalId) DESC
            LIMIT 1
        ), 0) AS VisitCharge
    FROM `ipd_doctor_round` rnd
    INNER JOIN `doctor` d ON rnd.DoctorId = d.Doctor_Id
    WHERE rnd.IPDId = p_IPDId AND rnd.IsActive = 1
    ORDER BY rnd.RoundDateTime;

    -- RS4: Prescribed medicines from ward rounds ? real price from tbl_medicine
    SELECT
        rp.Id,
        m.MedicineId,
        m.MedicineName,
        m.Type,
        rp.Days,
        rp.Dosage,
        rp.Status,
        IFNULL(m.SellingPrice, 0) AS UnitPrice,
        IFNULL(m.SellingPrice, 0) * GREATEST(IFNULL(rp.Days, 1), 1) AS TotalPrice
    FROM `ipd_round_prescription` rp
    INNER JOIN `tbl_medicine` m ON rp.MedicineId = m.MedicineId
    WHERE rp.IPDId = p_IPDId AND rp.IsActive = 1;

    -- RS5: Discharge medicines ? real price from tbl_medicine
    SELECT
        dm.Id,
        m.MedicineId,
        m.MedicineName,
        m.Type,
        dm.Days,
        dm.Dosage,
        IFNULL(m.SellingPrice, 0) AS UnitPrice,
        IFNULL(m.SellingPrice, 0) * GREATEST(IFNULL(dm.Days, 1), 1) AS TotalPrice
    FROM `ipd_discharge_medicine` dm
    INNER JOIN `tbl_medicine` m ON dm.MedicineId = m.MedicineId
    WHERE dm.IPDId = p_IPDId AND dm.IsActive = 1;

    -- RS6: Investigations ordered ? with a best-effort suggested price
    SELECT
        ri.Id,
        ri.InvestigationType,
        ri.TestName,
        ri.Priority,
        ri.Status,
        IFNULL((
            SELECT bm.Amount
            FROM `billingmaster` bm
            WHERE bm.Category = 'Lab'
              AND bm.IsActive = 1
              AND bm.Hospital_Id = p_HospitalId
              AND (p_SubHospitalId = 0 OR bm.SubHospital_Id = p_SubHospitalId OR bm.SubHospital_Id IS NULL)
              AND bm.Name = ri.TestName
            ORDER BY (bm.SubHospital_Id = p_SubHospitalId) DESC
            LIMIT 1
        ), 0) AS SuggestedCharge
    FROM `ipd_round_investigation` ri
    WHERE ri.IPDId = p_IPDId AND ri.IsActive = 1;

    -- RS7: Procedures performed during this admission ? with a best-effort
    -- suggested price (exact-name match against an active Billing Master
    -- "Procedure" item).
    SELECT
        pm.ProcedureId,
        pm.ProcedureName,
        pm.ProcedureCategory,
        pm.Status,
        COALESCE(pm.ActualDate, pm.PlannedDate) AS ProcedureDate,
        IFNULL((
            SELECT bm.Amount
            FROM `billingmaster` bm
            WHERE bm.Category = 'Procedure'
              AND bm.IsActive = 1
              AND bm.Hospital_Id = p_HospitalId
              AND (p_SubHospitalId = 0 OR bm.SubHospital_Id = p_SubHospitalId OR bm.SubHospital_Id IS NULL)
              AND bm.Name = pm.ProcedureName
            ORDER BY (bm.SubHospital_Id = p_SubHospitalId) DESC
            LIMIT 1
        ), 0) AS SuggestedCharge
    FROM `procedure_master` pm
    WHERE pm.VisitContextType = 'IPD'
      AND pm.VisitContextId = p_IPDId
      AND pm.IsActive = 1;
END$$

DELIMITER ;
