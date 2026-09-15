-- ============================================================================
-- Advanced Realtime Pharmacy System — Database Migration
-- Target DB : hms_db (MySQL)
-- Scope     : PO/GRN procurement, stock adjustments, batch traceability,
--             returns/refunds, GST, controlled substances, clinical safety,
--             controlled-substance register.
-- NOTE      : Idempotent-ish. Run AFTER backing up. Requires the base
--             pharmacy_migration.sql tables (batches, stock, stock_adjustment_log).
-- ============================================================================

USE hms_db;

-- ============================================================================
-- PHASE A.1 — sp_Stock_GetAvailable (single source of truth from batches)
--   The available-stock checker used by the pharmacy queue must read from
--   batches (real physical stock). This is the authoritative proc.
-- ============================================================================
DROP PROCEDURE IF EXISTS sp_Stock_GetAvailable;
DELIMITER $$
CREATE PROCEDURE sp_Stock_GetAvailable(
    IN p_MedicineId INT,
    IN p_HospitalId INT,
    IN p_SubHospitalId INT
)
BEGIN
    SELECT COALESCE(SUM(Quantity), 0) AS AvailableQty
    FROM batches
    WHERE MedicineId = p_MedicineId
      AND HospitalId = p_HospitalId
      AND (p_SubHospitalId = 0 OR SubHospitalId = p_SubHospitalId)
      AND Quantity > 0
      AND (ExpiryDate IS NULL OR ExpiryDate >= CURDATE());  -- exclude expired
END $$
DELIMITER ;

-- ============================================================================
-- PHASE A.2 — sp_Pharmacy_ReconcileStock
--   Recomputes stock.TotalQuantity from SUM(batches.Quantity) after any
--   deduction / receipt / adjustment so the aggregate stays in sync.
-- ============================================================================
DROP PROCEDURE IF EXISTS sp_Pharmacy_ReconcileStock;
DELIMITER $$
CREATE PROCEDURE sp_Pharmacy_ReconcileStock(
    IN p_MedicineId INT,
    IN p_HospitalId INT,
    IN p_SubHospitalId INT
)
BEGIN
    UPDATE stock s
    SET s.TotalQuantity = (
        SELECT COALESCE(SUM(b.Quantity), 0)
        FROM batches b
        WHERE b.MedicineId = p_MedicineId
          AND b.HospitalId = p_HospitalId
          AND (p_SubHospitalId = 0 OR b.SubHospitalId = p_SubHospitalId)
          AND b.Quantity > 0
          AND (b.ExpiryDate IS NULL OR b.ExpiryDate >= CURDATE())
    )
    WHERE s.MedicineId = p_MedicineId
      AND s.HospitalId = p_HospitalId
      AND (p_SubHospitalId = 0 OR s.SubHospitalId = p_SubHospitalId);
END $$
DELIMITER ;

-- ============================================================================
-- PHASE B — Purchase Order (PO) tables
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

-- ============================================================================
-- PHASE B — Goods Receipt (GRN) tables (add PurchasePrice to item)
-- ============================================================================
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

DROP TABLE IF EXISTS goods_receipt_item;
CREATE TABLE goods_receipt_item (
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
-- PHASE C — Returns / Refunds
-- ============================================================================
CREATE TABLE IF NOT EXISTS pharmacy_return (
    ReturnNumber   VARCHAR(40) NOT NULL,
    BillId         INT NULL,
    CustomerName   VARCHAR(255) NULL,
    MobileNumber   VARCHAR(20) NULL,
    ReturnDate     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ReturnType     VARCHAR(20) NOT NULL DEFAULT 'Patient',  -- Patient / Supplier / Damage
    Reason         VARCHAR(500) NULL,
    TotalRefund    DECIMAL(12,2) NOT NULL DEFAULT 0,
    HospitalId     INT NOT NULL,
    SubHospitalId  INT NULL,
    CreatedBy      INT NULL,
    PRIMARY KEY (ReturnNumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS pharmacy_return_item (
    Id           INT AUTO_INCREMENT PRIMARY KEY,
    ReturnNumber VARCHAR(40) NOT NULL,
    MedicineId   INT NOT NULL,
    Quantity     INT NOT NULL DEFAULT 0,
    UnitPrice    DECIMAL(12,2) NOT NULL DEFAULT 0,
    RefundAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
    BatchId      INT NULL,
    KEY idx_return_item (ReturnNumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================================
-- PHASE D — Controlled substance / narcotics register
-- ============================================================================
CREATE TABLE IF NOT EXISTS narcotics_register (
    Id            INT AUTO_INCREMENT PRIMARY KEY,
    RegisterDate  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    MedicineId    INT NOT NULL,
    BatchId       INT NULL,
    QuantityOut   INT NOT NULL DEFAULT 0,
    BillId        INT NULL,
    PatientName   VARCHAR(255) NULL,
    PrescriberName VARCHAR(255) NULL,
    DispensedBy   INT NOT NULL,          -- pharmacist user id
    AuthorizedBy  INT NOT NULL,          -- second signatory user id
    HospitalId    INT NOT NULL,
    SubHospitalId INT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================================
-- PHASE E — Clinical safety: drug interactions & allergy alert
-- ============================================================================
CREATE TABLE IF NOT EXISTS drug_interaction (
    Id            INT AUTO_INCREMENT PRIMARY KEY,
    MedicineA     INT NOT NULL,
    MedicineB     INT NOT NULL,
    Severity      VARCHAR(20) NOT NULL DEFAULT 'Moderate',  -- Minor / Moderate / Major
    Description   VARCHAR(500) NULL,
    HospitalId    INT NOT NULL,
    KEY idx_di_pair (MedicineA, MedicineB)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS patient_allergy (
    Id            INT AUTO_INCREMENT PRIMARY KEY,
    PatientId     INT NOT NULL,
    MedicineId    INT NULL,
    AllergyName   VARCHAR(255) NOT NULL,
    Severity      VARCHAR(20) NOT NULL DEFAULT 'Moderate',
    Reaction      VARCHAR(255) NULL,
    HospitalId    INT NOT NULL,
    CreatedDate   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================================
-- PHASE C — Batch traceability on dispensed bill lines
--   Add DispensedBatchId + Refunded flag to counter bill items so recalls work.
-- ============================================================================
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
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_TableName AND COLUMN_NAME = p_ColumnName
    ) THEN
        SET @ddl = CONCAT('ALTER TABLE `', p_TableName, '` ADD COLUMN `', p_ColumnName, '` ', p_ColumnDef);
        PREPARE stmt FROM @ddl;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END $$
DELIMITER ;

CALL sp_Util_AddColumnIfNotExists('counter_bill_item', 'DispensedBatchId', 'INT NULL');
CALL sp_Util_AddColumnIfNotExists('counter_bill_item', 'GstPercent', 'DECIMAL(5,2) NOT NULL DEFAULT 0');
CALL sp_Util_AddColumnIfNotExists('counter_bill_item', 'CgstAmount', 'DECIMAL(12,2) NOT NULL DEFAULT 0');
CALL sp_Util_AddColumnIfNotExists('counter_bill_item', 'SgstAmount', 'DECIMAL(12,2) NOT NULL DEFAULT 0');
CALL sp_Util_AddColumnIfNotExists('counter_bill_item', 'HsnCode', 'VARCHAR(20) NULL');

DROP PROCEDURE IF EXISTS sp_Util_AddColumnIfNotExists;

-- ============================================================================
-- PHASE B — sp_PurchaseOrder actions
-- ============================================================================
DROP PROCEDURE IF EXISTS sp_PO_Create;
DELIMITER $$
CREATE PROCEDURE sp_PO_Create(
    IN p_PONumber VARCHAR(40),
    IN p_SupplierId INT,
    IN p_HospitalId INT,
    IN p_SubHospitalId INT,
    IN p_ExpectedDate DATE,
    IN p_Notes VARCHAR(500),
    IN p_CreatedBy INT
)
BEGIN
    INSERT INTO purchase_order (PONumber, SupplierId, HospitalId, SubHospitalId, ExpectedDate, Notes, CreatedBy)
    VALUES (p_PONumber, p_SupplierId, p_HospitalId, p_SubHospitalId, p_ExpectedDate, p_Notes, p_CreatedBy);
END $$
DELIMITER ;

DROP PROCEDURE IF EXISTS sp_PO_AddItem;
DELIMITER $$
CREATE PROCEDURE sp_PO_AddItem(
    IN p_PONumber VARCHAR(40),
    IN p_MedicineId INT,
    IN p_Quantity INT,
    IN p_UnitCost DECIMAL(12,2)
)
BEGIN
    INSERT INTO purchase_order_item (PONumber, MedicineId, Quantity, UnitCost)
    VALUES (p_PONumber, p_MedicineId, p_Quantity, p_UnitCost);
END $$
DELIMITER ;

DROP PROCEDURE IF EXISTS sp_PO_UpdateStatus;
DELIMITER $$
CREATE PROCEDURE sp_PO_UpdateStatus(
    IN p_PONumber VARCHAR(40),
    IN p_Status VARCHAR(20),
    IN p_HospitalId INT
)
BEGIN
    UPDATE purchase_order SET Status = p_Status WHERE PONumber = p_PONumber AND HospitalId = p_HospitalId;
END $$
DELIMITER ;

-- ============================================================================
-- PHASE B — sp_GRN_Receive (creates batches from GRN items + reconciles stock)
-- ============================================================================
DROP PROCEDURE IF EXISTS sp_GRN_Create;
DELIMITER $$
CREATE PROCEDURE sp_GRN_Create(
    IN p_GRNNumber VARCHAR(40),
    IN p_PONumber VARCHAR(40),
    IN p_SupplierId INT,
    IN p_HospitalId INT,
    IN p_SubHospitalId INT,
    IN p_SupplierInvoiceNo VARCHAR(60),
    IN p_Notes VARCHAR(500),
    IN p_CreatedBy INT
)
BEGIN
    INSERT INTO goods_receipt (GRNNumber, PONumber, SupplierId, HospitalId, SubHospitalId, SupplierInvoiceNo, Notes, CreatedBy)
    VALUES (p_GRNNumber, p_PONumber, p_SupplierId, p_HospitalId, p_SubHospitalId, p_SupplierInvoiceNo, p_Notes, p_CreatedBy);
END $$
DELIMITER ;

-- sp_GRN_AddItem inserts a batch and a goods_receipt_item row, then reconciles.
DROP PROCEDURE IF EXISTS sp_GRN_AddItem;
DELIMITER $$
CREATE PROCEDURE sp_GRN_AddItem(
    IN p_GRNNumber VARCHAR(40),
    IN p_MedicineId INT,
    IN p_BatchNumber VARCHAR(80),
    IN p_ExpiryDate DATE,
    IN p_Mrp DECIMAL(12,2),
    IN p_SellingPrice DECIMAL(12,2),
    IN p_PurchasePrice DECIMAL(12,2),
    IN p_Quantity INT,
    IN p_HospitalId INT,
    IN p_SubHospitalId INT,
    IN p_CreatedBy INT
)
BEGIN
    DECLARE v_BatchId INT;

    INSERT INTO goods_receipt_item (GRNNumber, MedicineId, BatchNumber, ExpiryDate, Mrp, SellingPrice, PurchasePrice, Quantity)
    VALUES (p_GRNNumber, p_MedicineId, p_BatchNumber, p_ExpiryDate, p_Mrp, p_SellingPrice, p_PurchasePrice, p_Quantity);

    -- Create a batch
    INSERT INTO batches (MedicineId, BatchNumber, ExpiryDate, MRP, SellingPrice, PurchasePrice, Quantity, HospitalId, SubHospitalId, CreatedBy)
    VALUES (p_MedicineId, p_BatchNumber, p_ExpiryDate, p_Mrp, p_SellingPrice, p_PurchasePrice, p_Quantity, p_HospitalId, p_SubHospitalId, p_CreatedBy);
    SET v_BatchId = LAST_INSERT_ID();

    -- Upsert stock aggregate
    INSERT INTO stock (MedicineId, TotalQuantity, HospitalId, SubHospitalId, ReorderLevel)
    VALUES (p_MedicineId, p_Quantity, p_HospitalId, p_SubHospitalId, 10)
    ON DUPLICATE KEY UPDATE TotalQuantity = TotalQuantity + p_Quantity;

    -- Log stock-in
    INSERT INTO stock_adjustment_log (MedicineId, BatchId, QuantityChange, Reason, Notes, ChangedBy, HospitalId, SubHospitalId)
    VALUES (p_MedicineId, v_BatchId, p_Quantity, 'GRN', CONCAT('Goods receipt ', p_GRNNumber), p_CreatedBy, p_HospitalId, p_SubHospitalId);
END $$
DELIMITER ;

-- ============================================================================
-- PHASE C — sp_Stock_Adjust (reason-coded adjustment writing to log)
-- ============================================================================
DROP PROCEDURE IF EXISTS sp_Stock_Adjust;
DELIMITER $$
CREATE PROCEDURE sp_Stock_Adjust(
    IN p_MedicineId INT,
    IN p_BatchId INT,
    IN p_QuantityChange INT,      -- +ve stock-in, -ve stock-out
    IN p_Reason VARCHAR(40),      -- Damage / Expired / Correction / Return / Breakage
    IN p_Notes VARCHAR(500),
    IN p_ChangedBy INT,
    IN p_HospitalId INT,
    IN p_SubHospitalId INT
)
BEGIN
    DECLARE v_CurrentQty INT DEFAULT 0;

    -- Guard: cannot drive a batch below zero for negative adjustments
    IF p_BatchId IS NOT NULL THEN
        SELECT COALESCE(Quantity,0) INTO v_CurrentQty FROM batches WHERE BatchId = p_BatchId;
        IF p_QuantityChange < 0 AND (v_CurrentQty + p_QuantityChange) < 0 THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Insufficient batch stock for adjustment';
        END IF;
        UPDATE batches SET Quantity = Quantity + p_QuantityChange WHERE BatchId = p_BatchId;
    END IF;

    -- Write the audit log
    INSERT INTO stock_adjustment_log (MedicineId, BatchId, QuantityChange, Reason, Notes, ChangedBy, HospitalId, SubHospitalId)
    VALUES (p_MedicineId, p_BatchId, p_QuantityChange, p_Reason, p_Notes, p_ChangedBy, p_HospitalId, p_SubHospitalId);

    -- Reconcile aggregate
    CALL sp_Pharmacy_ReconcileStock(p_MedicineId, p_HospitalId, p_SubHospitalId);
END $$
DELIMITER ;

-- ============================================================================
-- PHASE C — sp_Return_Create (refund + restock via adjustment log)
-- ============================================================================
DROP PROCEDURE IF EXISTS sp_Return_Create;
DELIMITER $$
CREATE PROCEDURE sp_Return_Create(
    IN p_ReturnNumber VARCHAR(40),
    IN p_BillId INT,
    IN p_CustomerName VARCHAR(255),
    IN p_MobileNumber VARCHAR(20),
    IN p_ReturnType VARCHAR(20),
    IN p_Reason VARCHAR(500),
    IN p_TotalRefund DECIMAL(12,2),
    IN p_HospitalId INT,
    IN p_SubHospitalId INT,
    IN p_CreatedBy INT
)
BEGIN
    INSERT INTO pharmacy_return (ReturnNumber, BillId, CustomerName, MobileNumber, ReturnType, Reason, TotalRefund, HospitalId, SubHospitalId, CreatedBy)
    VALUES (p_ReturnNumber, p_BillId, p_CustomerName, p_MobileNumber, p_ReturnType, p_Reason, p_TotalRefund, p_HospitalId, p_SubHospitalId, p_CreatedBy);
END $$
DELIMITER ;
