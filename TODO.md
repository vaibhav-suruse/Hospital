# TODO - IPD Dashboard Performance + Walk-In Date Filter

## Task 1: Walk-In Consultation Date Filter
- [x] Verified existing date filter works (already implemented in controller + VM + view)
  - Controller `Index(int ipdId = 0, string date = "")` parses the `date` param
  - VM carries `SelectedDate` / `SelectedDateConsults` / `SelectedDateCount`
  - View has a date input (#walkInDateFilter) with auto-submit JS on change
  - Table renders `Model.SelectedDateConsults` for the chosen date
- [x] Confirmed filter matches OPD Appointment behavior

## Task 2: IPD Dashboard Performance
- [x] Created `IIPDDashboardRepository` + `IPDDashboardRepository`
  - Single multi-result-set SQL query for bed/ward/room/doctor/patient counts
  - Ward-wise bed status via GROUP BY SQL
- [x] Registered repository in `Startup.cs`
- [x] Refactored `IPDDashboardController.Index()` to use the new repository
  - Removed the 5 full-table in-memory loads (GetAllBeds/Wards/Rooms/Doctors/Patients)
  - Kept the sp_GetIPDDashboard SP call for admission/lab/critical/chart counts
- [x] Build & verify (build succeeded, no new warnings from changes)

## Dependent Files Edited
- `Repository/IIPDDashboardRepository.cs` (new)
- `Repository/IPDDashboardRepository.cs` (new)
- `Startup.cs` (register DI)
- `Controllers/IPDDashboardController.cs` (refactor)
