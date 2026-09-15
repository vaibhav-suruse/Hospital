# Advanced Realtime Pharmacy System — Implementation Tracking

**Goal:** Turn the existing dispensing/counter engine into a full advanced pharmacy system:
procurement, batch traceability, returns, GST, controlled substances, clinical safety, and realtime push.

## Phase A — P0 Stock-integrity foundation
- [x] SQL: `advanced_pharmacy_migration.sql` (all tables + SPs for B/C/D/E)
- [x] Models: `AdvancedPharmacyModels.cs`
- [x] Fix `GetAvailableStock()` in `PharmacyQueueRepository` to read from batches via `sp_Stock_GetAvailable`
- [x] Reconcile `stock.TotalQuantity` after FEFO deduction (`sp_Pharmacy_ReconcileStock`) in PharmacyQueueRepository + CounterRepository
- [ ] Retire `tbl_medicine.Quantity` as a write target (Phase 3)

## Phase B — Procurement (PO → GRN)
- [x] Models: `PurchaseOrder`, `PurchaseOrderItem`, `GoodsReceipt`, `GoodsReceiptItem`
- [x] `IAdvancedPharmacy` + `AdvancedPharmacyRepository` (PO + GRN)
- [x] `PurchaseOrderController` + `GoodsReceiptController`
- [ ] Views: PO list/create, GRN create/receive
- [x] Register in `Startup.cs`

## Phase C — Stock integrity & traceability
- [x] `sp_Stock_Adjust` + wire `stock_adjustment_log`
- [x] `StockAdjustment` model + repo methods + controller
- [ ] Batch traceability on dispensing (capture BatchId on bill lines)
- [x] Returns / Refunds / Credit-note workflow

## Phase D — Regulatory & fiscal compliance
- [ ] GST/HSN on Medicine master (model + repo + CRUD + view)
- [ ] GST breakdown on billing (CGST/SGST)
- [x] Controlled-substance / narcotics register with dual signatory

## Phase E — Clinical safety
- [x] Drug-interaction check table + lookup at dispensing
- [x] Allergy alert at dispensing
- [x] Expired-batch quarantine + auto-block from dispensing

## Phase F — Realtime
- [ ] SignalR hub for pharmacy queue push (replaces 8s polling)
- [ ] Live dashboard updates
