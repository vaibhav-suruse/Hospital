# TODO — Fix Pharmacy Module Issues

## Task
Resolve reported issues:
1. Pharmacy dashboard counts showing 0 (low stock, OPD/IPD patients, other counts)
2. Inventory "Add Medicine" not working
3. Inventory search bar not working
4. Counter medicine search not showing results

## Root Cause
- Missing stored procedures/tables in DB (AddInventory, UpdateInventoryItem, GetAllInventory, GetInventoryById, DeleteInventory, GetAllSuppliers, GetAllCategories, sp_Counter_* procedures, medicine_notifications)
- Inventory search form has no submit button
- Dashboard OPD/IPD counts query non-existent `medicine_notifications` table; silent `catch { return 0; }` hides errors

## Steps
- [x] 1. Create `pharmacy_fix_missing_sp.sql` — add missing inventory SPs
- [x] 2. Create missing `sp_Counter_*` stored procedures in SQL migration
- [x] 3. Fix `Views/Inventory/Index.cshtml` — add search submit button
- [ ] 4. Fix `Controllers/PharmacyDashboardController.cs` — robust OPD/IPD counts (use counter_bill), add error logging
- [ ] 5. Rebuild and verify build passes (DLL lock permitting)
- [ ] 6. Provide SQL migration for user to run against DB
