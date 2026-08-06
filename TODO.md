# TODO - Professional Full Patient History (OPD)

## Goal
Rebuild the OPD "History" button page into a comprehensive, professional
medical-history dashboard showing the ENTIRE history of a patient's OPD visits
plus clinical history (conditions/allergies/surgeries/family/lab results).

## Steps
1. [x] Models: Add `DoctorName` + `AppointmentTime` (non-mapped) to `OPD`;
        create `PatientFullHistoryVM` aggregating patient + visits + clinical history.
2. [x] Repository: Enhance `IOPDAppointment.GetPatientFullHistory` to also fetch
        doctor name + diagnoses per visit; update signature to pass hospitalId.
3. [x] Controller: Inject `IPatientHistory`; assemble combined VM in `PatientHistory` action.
4. [x] View: Rewrite `PatientHistory.cshtml` as a professional full-history dashboard.
5. [x] Build the solution to verify compilation (Build succeeded).
6. [x] Fix secondary caller `PatientController.GetPatientHistory` to pass hospitalId/subHospitalId.
