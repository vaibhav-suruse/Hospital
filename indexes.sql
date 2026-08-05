-- ═══════════════════════════════════════════════════════════════════════
--  Performance indexes for the slow list/pagination pages
--
--  The app was taking ~30s because the controllers loaded ENTIRE tables
--  into memory and paginated in C#. We've now moved pagination into SQL
--  (WHERE + COUNT + LIMIT). These indexes make those WHERE/JOIN/ORDER BY
--  clauses fast so page loads stay under a second even as data grows.
--
--  Run this once against your MySQL database. It is idempotent (each
--  CREATE INDEX IF NOT EXISTS is safe to run multiple times).
-- ═══════════════════════════════════════════════════════════════════════

-- ── tbl_patient: used by PatientList (search + hospital filter + ORDER BY Id DESC)
CREATE INDEX IF NOT EXISTS idx_patient_hospital ON tbl_patient (Hospital_Id, SubHospital_Id);
CREATE INDEX IF NOT EXISTS idx_patient_name ON tbl_patient (FirstName, LastName);
CREATE INDEX IF NOT EXISTS idx_patient_phone ON tbl_patient (PhoneNumber);

-- ── opdappointment: used by OPDAppointment Index / Queue (date + hospital + JOIN)
--    The paged query filters: HospitalId, SubHospitalId, IsActive, IsWalkIn, DATE(AppointmentDate)
CREATE INDEX IF NOT EXISTS idx_opdappt_hospital_date
    ON opdappointment (HospitalId, SubHospitalId, AppointmentDate, IsActive, IsWalkIn);
CREATE INDEX IF NOT EXISTS idx_opdappt_patient ON opdappointment (PatientId);
CREATE INDEX IF NOT EXISTS idx_opdappt_doctor ON opdappointment (DoctorId);

-- ── opdmaster: JOIN on AppointmentId (used by GetOPDWithIPDStatusBatch + WalkIn)
CREATE INDEX IF NOT EXISTS idx_opdmaster_appt ON opdmaster (AppointmentId, Hospital_Id);

-- ── doctor: JOIN on Doctor_Id (used by appointment list / queue)
CREATE INDEX IF NOT EXISTS idx_doctor_id ON doctor (Doctor_Id);

-- ── opdsymptom / symptoms: JOIN for symptom batching
CREATE INDEX IF NOT EXISTS idx_opdsymptom_opd ON opdsymptom (OPD_Id, IsActive);

-- ── ipdadmission: JOIN on OPDVisitId (used by GetOPDWithIPDStatusBatch)
CREATE INDEX IF NOT EXISTS idx_ipdadm_opd ON ipdadmission (OPDVisitId, ParentHospitalId, IsActiveAdmission);
