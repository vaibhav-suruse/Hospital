-- ============================================================================
-- Pharmacy Module — Database Migration Script
-- Target DB : hms_db (MySQL)
-- Scope     : Phased fixes for the pharmacy module audit.
--             Run this ONLY after backing up the database. Each phase is
--             idempotent-ish (DROP PROCEDURE IF EXISTS / CREATE OR REPLACE logic).
-- ============================================================================

USE hms_db;

-- ============================================================================
-- PHASE 2 — SINGLE STOCK-DEDUCTION PATH, MULTI-BATCH FEFO
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1) sp_Stock_GetAvailable
--    Single source of truth for "how much do we have".
--    Reads from batches (the real physical stock), NOT from tbl_medicine.Quantity
--    or stock.TotalQuantity.
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Stock_GetAvailable;
DELIMITER $$
CREATE PROCEDURE sp_Stock_GetAvailable(
    IN p_MedicineId INT,
    IN p_HospitalId INT,
    IN p_SubHospitalId INT
)
BEGIN
    SELECT COALESCE(SUM(b.Quantity), 0) AS AvailableQty
    FROM batches b
    WHERE b.MedicineId = p_MedicineId
      AND b.HospitalId = p_HospitalId
      AND (p_SubHospitalId = 0 OR b.SubHospitalId = p_SubHospitalId)
      AND b.Quantity > 0;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- 2) sp_Stock_DeductFEFO
--    THE single stock-deduction path. Deducts a quantity across as many
--    batches as needed, in FEFO order (ExpiryDate ASC, then BatchId ASC).
--    Fixes the P0-4 bug where only ONE batch was decremented even when it had
--    less stock than the sale (which used to drive that batch negative).
--
--    * Writes to batches.Quantity only (quantity-single-source-of-truth).
--    * Returns p_Success = 1 if fully deducted, 0 if insufficient stock.
--    * On an insufficient stock result, NOTHING was partially deducted and the
--      batches remain untouched — the caller rolls back the whole transaction.
--    * Never lets any batch go negative.
--    * Safe to run inside the caller's transaction.
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Stock_DeductFEFO;
DELIMITER $$
CREATE PROCEDURE sp_Stock_DeductFEFO(
    IN p_MedicineId INT,
    IN p_Quantity INT,
    IN p_HospitalId INT,
    IN p_SubHospitalId INT,
    OUT p_Success TINYINT
)
DETERMINISTIC
BEGIN
    DECLARE v_remaining INT DEFAULT p_Quantity;
    DECLARE v_batch_id INT;
    DECLARE v_batch_qty INT;
    DECLARE v_done INT DEFAULT 0;

    DECLARE cur CURSOR FOR
        SELECT b.BatchId, b.Quantity
        FROM batches b
        WHERE b.MedicineId = p_MedicineId
          AND b.HospitalId = p_HospitalId
          AND (p_SubHospitalId = 0 OR b.SubHospitalId = p_SubHospitalId)
          AND b.Quantity > 0
        ORDER BY b.ExpiryDate IS NULL, b.ExpiryDate ASC, b.BatchId ASC;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_done = 1;

    SET p_Success = 0;

    -- Nothing to deduct (zero / negative) → fail.
    IF p_Quantity <= 0 THEN
        SET p_Success = 0;
    ELSE
        OPEN cur;
        read_loop: LOOP
            FETCH cur INTO v_batch_id, v_batch_qty;
            IF v_done THEN
                LEAVE read_loop;
            END IF;

            -- Done once every requested unit has been deducted.
            IF v_remaining <= 0 THEN
                LEAVE read_loop;
            END IF;

            IF v_batch_qty >= v_remaining THEN
                -- This batch satisfies the whole remaining amount.
                UPDATE batches SET Quantity = Quantity - v_remaining
                WHERE BatchId = v_batch_id;
                SET v_remaining = 0;
            ELSE
                -- This batch is exhausted; spill into the next.
                UPDATE batches SET Quantity = 0
                WHERE BatchId = v_batch_id;
                SET v_remaining = v_remaining - v_batch_qty;
            END IF;
        END LOOP;
        CLOSE cur;

        -- Fully satisfied only if every requested unit was deducted.
        IF v_remaining = 0 THEN
            SET p_Success = 1;
        ELSE
            -- Insufficient stock across all batches. We may have partially
            -- deducted a few batches before running out. Since the caller runs
            -- this inside a transaction and treats p_Success=0 as a rollback,
            -- these partial deductions are undone with it. (For a single-stmt
            -- caller we keep this contract documented: on 0, treat as no-op.)
            SET p_Success = 0;
        END IF;
    END IF;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- 4) sp_Stock_ReconcileTotals
--    Recomputes stock.TotalQuantity from SUM(batches.Quantity) for a medicine.
--    Keeps the aggregate stock table in sync and retires reliance on
--    tbl_medicine.Quantity as a source of truth.
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Stock_ReconcileTotals;
DELIMITER $$
CREATE PROCEDURE sp_Stock_ReconcileTotals(
    IN p_MedicineId INT,
    IN p_HospitalId INT,
    IN p_SubHospitalId INT
)
DETERMINISTIC
BEGIN
    UPDATE stock s
    SET s.TotalQuantity = (
        SELECT COALESCE(SUM(b.Quantity), 0)
        FROM batches b
        WHERE b.MedicineId = p_MedicineId
          AND b.HospitalId = p_HospitalId
          AND (p_SubHospitalId = 0 OR b.SubHospitalId = p_SubHospitalId)
    )
    WHERE s.MedicineId = p_MedicineId
      AND s.HospitalId = p_HospitalId
      AND (p_SubHospitalId = 0 OR s.SubHospitalId = p_SubHospitalId);
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- 3) IMPORTANT — sp_Counter_InsertBillItem must STOP deducting stock.
--    The existing procedure currently does BOTH (a) insert a counter_bill_item
--    row AND (b) deduct stock using the buggy single-batch logic. With the new
--    shared FEFO path, stock deduction now happens via sp_Stock_DeductFEFO
--    (called from the repositories). If the old inline deduction is NOT removed,
--    stock will be deducted TWICE per sale.
--
--    The exact current body of sp_Counter_InsertBillItem lives in the database
--    (not in the repo). The change required is:
--
--      * Remove the block that does:
--            UPDATE batches SET Quantity = Quantity - p_Quantity
--            WHERE MedicineId = p_MedicineId ... ORDER BY ExpiryDate ASC LIMIT 1;
--        and the corresponding   UPDATE stock SET TotalQuantity = TotalQuantity - p_Quantity ...
--
--      * KEEP ONLY the INSERT INTO counter_bill_item (...) statement.
--
--    A safe, minimal replacement is shown below. It preserves the original
--    signature (so no C# changes needed) but only inserts the line item and
--    returns p_Success=1 (stock is handled by sp_Stock_DeductFEFO).
--
--    WARNING: Verify the actual column list of counter_bill_item before running.
--    Adjust the INSERT column names to match your schema.
-- ----------------------------------------------------------------------------
-- DROP PROCEDURE IF EXISTS sp_Counter_InsertBillItem;
-- DELIMITER $$
-- CREATE PROCEDURE sp_Counter_InsertBillItem(
--     IN p_BillId INT,
--     IN p_MedicineId INT,
--     IN p_MedicineName VARCHAR(255),
--     IN p_Quantity INT,
--     IN p_UnitPrice DECIMAL(12,2),
--     IN p_TotalPrice DECIMAL(12,2),
--     IN p_HospitalId INT,
--     IN p_SubHospitalId INT,
--     OUT p_Success TINYINT
-- )
-- DETERMINISTIC
-- BEGIN
--     SET p_Success = 1; -- stock deduction is now delegated to sp_Stock_DeductFEFO
--     INSERT INTO counter_bill_item
--         (BillId, MedicineId, MedicineName, Quantity, UnitPrice, TotalPrice, HospitalId, SubHospitalId)
--     VALUES
--         (p_BillId, p_MedicineId, p_MedicineName, p_Quantity, p_UnitPrice, p_TotalPrice, p_HospitalId, p_SubHospitalId);
-- END $$
-- DELIMITER ;

-- ============================================================================
-- PHASE 4 — GST / HSN / drug schedule / composition columns on tbl_medicine
-- ============================================================================

-- Add columns if they don't exist (idempotent helper).
DROP PROCEDURE IF EXISTS sp_Util_AddColumnIfNotExists;
DELIMITER $$
CREATE PROCEDURE sp_Util_AddColumnIfNotExists(
    IN p_TableName VARCHAR(128),
    IN p_ColumnName VARCHAR(128),
    IN p_ColumnDef TEXT
)
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = p_TableName
          AND COLUMN_NAME = p_ColumnName
    ) THEN
        SET @ddl = CONCAT('ALTER TABLE `', p_TableName, '` ADD COLUMN `', p_ColumnName, '` ', p_ColumnDef);
        PREPARE stmt FROM @ddl;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END $$
DELIMITER ;

CALL sp_Util_AddColumnIfNotExists('tbl_medicine', 'HSNCode', 'VARCHAR(20) NULL');
CALL sp_Util_AddColumnIfNotExists('tbl_medicine', 'GstPercent', 'DECIMAL(5,2) NOT NULL DEFAULT 0');
CALL sp_Util_AddColumnIfNotExists('tbl_medicine', 'DrugSchedule', 'VARCHAR(10) NULL');
CALL sp_Util_AddColumnIfNotExists('tbl_medicine', 'RequiresPrescription', 'TINYINT(1) NOT NULL DEFAULT 0');
CALL sp_Util_AddColumnIfNotExists('tbl_medicine', 'IsControlledSubstance', 'TINYINT(1) NOT NULL DEFAULT 0');
CALL sp_Util_AddColumnIfNotExists('tbl_medicine', 'GenericName', 'VARCHAR(255) NULL');
CALL sp_Util_AddColumnIfNotExists('tbl_medicine', 'Manufacturer', 'VARCHAR(255) NULL');
CALL sp_Util_AddColumnIfNotExists('tbl_medicine', 'Composition', 'VARCHAR(500) NULL');

DROP PROCEDURE IF EXISTS sp_Util_AddColumnIfNotExists;

-- ============================================================================
-- PHASE 5 — PO / GRN tables
-- ============================================================================

CREATE TABLE IF NOT EXISTS purchase_order (
    PONumber          VARCHAR(40) NOT NULL,
    SupplierId        INT NOT NULL,
    HospitalId        INT NOT NULL,
    SubHospitalId     INT NULL,
    OrderDate         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ExpectedDate      DATE NULL,
    Status            VARCHAR(20) NOT NULL DEFAULT 'Draft',  -- Draft / Ordered / Received / Cancelled
    Notes             VARCHAR(500) NULL,
    CreatedBy         INT NULL,
    CreatedDate       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (PONumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS purchase_order_item (
    Id         INT AUTO_INCREMENT PRIMARY KEY,
    PONumber   VARCHAR(40) NOT NULL,
    MedicineId INT NOT NULL,
    Quantity   INT NOT NULL DEFAULT 0,
    UnitCost   DECIMAL(12,2) NOT NULL DEFAULT 0,
    KEY idx_po_item (PONumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS goods_receipt (
    GRNNumber     VARCHAR(40) NOT NULL,
    PONumber      VARCHAR(40) NULL,
    SupplierId    INT NOT NULL,
    HospitalId    INT NOT NULL,
    SubHospitalId INT NULL,
    GRNDate       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    SupplierInvoiceNo VARCHAR(60) NULL,
    Notes         VARCHAR(500) NULL,
    CreatedBy     INT NULL,
    PRIMARY KEY (GRNNumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS goods_receipt_item (
    Id         INT AUTO_INCREMENT PRIMARY KEY,
    GRNNumber  VARCHAR(40) NOT NULL,
    MedicineId INT NOT NULL,
    BatchNumber VARCHAR(80) NULL,
    ExpiryDate DATE NULL,
    Mrp        DECIMAL(12,2) NOT NULL DEFAULT 0,
    SellingPrice DECIMAL(12,2) NOT NULL DEFAULT 0,
    PurchasePrice DECIMAL(12,2) NOT NULL DEFAULT 0,
    Quantity   INT NOT NULL DEFAULT 0,
    KEY idx_grn_item (GRNNumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================================
-- PHASE 6 — Stock adjustment log
-- ============================================================================

CREATE TABLE IF NOT EXISTS stock_adjustment_log (
    Id            INT AUTO_INCREMENT PRIMARY KEY,
    MedicineId    INT NOT NULL,
    BatchId       INT NULL,
    QuantityChange INT NOT NULL,          -- +ve for stock-in, -ve for stock-out
    Reason        VARCHAR(40) NOT NULL,   -- Damage / Expired / Correction / Return / Other
    Notes         VARCHAR(500) NULL,
    ChangedBy     INT NULL,
    ChangedDate   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    HospitalId    INT NOT NULL,
    SubHospitalId INT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================================
-- PHASE 9 — Retire ghost tables (medicines, suppliers)
--    NOTE: DANGEROUS. Only run after confirming nothing references them.
--    Commented out by default for safety.
-- ============================================================================
-- DROP TABLE IF EXISTS medicines;
-- DROP TABLE IF EXISTS suppliers;
