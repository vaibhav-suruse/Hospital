-- ============================================================================
-- Pharmacy Module — Missing Stored Procedures & Tables Fix
-- Target DB : hms_db (MySQL)
-- Scope     : Creates the stored procedures & tables the code references but
--             which are missing from the database, causing:
--               1. Pharmacy dashboard counts showing 0 (low stock, OPD/IPD)
--               2. Inventory "Add Medicine" not working (AddInventory SP missing)
--               3. Inventory search bar not working
--               4. Counter medicine search not showing results
--             (sp_Counter_SearchMedicine SP missing)
--
-- Safe to run: DROP PROCEDURE IF EXISTS + CREATE PROCEDURE (idempotent).
-- Run against the SAME database as pharmacy_migration.sql + advanced_pharmacy_migration.sql.
-- ============================================================================

USE hms_db;

-- ============================================================================
-- 1. medicine_notifications table (used by dashboard OPD/IPD patient counts +
--    pharmacy queue). Referenced by PharmacyDashboardController & PharmacyQueue.
-- ============================================================================
CREATE TABLE IF NOT EXISTS medicine_notifications (
    NotificationId INT AUTO_INCREMENT PRIMARY KEY,
    PatientId      INT NOT NULL,
    PatientName    VARCHAR(255) NULL,
    OPDId          INT NULL,
    AppointmentId  INT NULL,
    IPDId          INT NULL,
    RoundId        INT NULL,
    DoctorName     VARCHAR(255) NULL,
    MedicineCount  INT NOT NULL DEFAULT 0,
    MedicinesSummary TEXT NULL,
    Type           VARCHAR(20) NULL,        -- 'OPD' / 'IPD'
    Status         VARCHAR(20) NULL,        -- 'Pending' / 'Dispensed' / 'Billed'
    CreatedAt      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DispensedAt    DATETIME NULL,
    WardName       VARCHAR(100) NULL,
    RoomNo         VARCHAR(50) NULL,
    BedNo          VARCHAR(50) NULL,
    HospitalId     INT NOT NULL,
    SubHospitalId  INT NULL,
    KEY idx_notif_hosp (HospitalId, SubHospitalId),
    KEY idx_notif_type (Type, Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================================
-- 2. Inventory stored procedures
-- ============================================================================

-- ----------------------------------------------------------------------------
-- GetAllInventory — returns inventory list for the Inventory grid
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS GetAllInventory;
DELIMITER $$
CREATE PROCEDURE GetAllInventory(
    IN p_HospitalId INT,
    IN p_SubHospitalId INT
)
BEGIN
    SELECT
        m.MedicineId,
        b.BatchId,
        m.MedicineName,
        c.CategoryName,
        s.SupplierName,
        m.Unit,
        m.MRP,
        b.Quantity AS Stock,
        b.ReorderLevel,
        CASE WHEN COALESCE(b.Quantity,0) <= COALESCE(b.ReorderLevel,0) THEN 'Low Stock' ELSE 'In Stock' END AS Status,
        b.BatchNumber,
        b.ExpiryDate
    FROM tbl_medicine m
    LEFT JOIN batches b        ON b.MedicineId = m.MedicineId AND b.HospitalId = p_HospitalId
    LEFT JOIN categories c     ON c.CategoryId = m.CategoryId
    LEFT JOIN store_suppliers s ON s.SupplierId = m.SupplierId
    WHERE m.HospitalId = p_HospitalId
      AND (p_SubHospitalId = 0 OR b.SubHospitalId = p_SubHospitalId OR b.SubHospitalId IS NULL)
    ORDER BY m.MedicineName;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- AddInventory — creates medicine + batch + stock row
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS AddInventory;
DELIMITER $$
CREATE PROCEDURE AddInventory(
    IN p_MedicineName  VARCHAR(255),
    IN p_BatchNumber   VARCHAR(80),
    IN p_ExpiryDate    DATE,
    IN p_Quantity      INT,
    IN p_ReorderLevel  INT,
    IN p_CategoryId    INT,
    IN p_SupplierId    INT,
    IN p_Unit          VARCHAR(50),
    IN p_MRP           DECIMAL(12,2),
    IN p_SellingPrice  DECIMAL(12,2),
    IN p_HospitalId    INT,
    IN p_SubHospitalId INT
)
BEGIN
    DECLARE v_MedicineId INT;

    -- Insert or reuse the medicine record
    INSERT INTO tbl_medicine (MedicineName, CategoryId, SupplierId, Unit, MRP, SellingPrice, HospitalId, IsActive)
    VALUES (p_MedicineName, p_CategoryId, p_SupplierId, p_Unit, p_MRP, p_SellingPrice, p_HospitalId, 1);
    SET v_MedicineId = LAST_INSERT_ID();

    -- Insert the batch
    INSERT INTO batches (MedicineId, BatchNumber, Quantity, ReorderLevel, ExpiryDate, HospitalId, SubHospitalId)
    VALUES (v_MedicineId, p_BatchNumber, p_Quantity, p_ReorderLevel, p_ExpiryDate, p_HospitalId, p_SubHospitalId);

    -- Sync aggregate stock
    INSERT INTO stock (MedicineId, TotalQuantity, ReorderLevel, HospitalId, SubHospitalId)
    VALUES (v_MedicineId, p_Quantity, p_ReorderLevel, p_HospitalId, p_SubHospitalId);
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- UpdateInventoryItem — updates medicine + batch + stock
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS UpdateInventoryItem;
DELIMITER $$
CREATE PROCEDURE UpdateInventoryItem(
    IN p_MedicineId     INT,
    IN p_MedicineName   VARCHAR(255),
    IN p_CategoryId     INT,
    IN p_SupplierId     INT,
    IN p_Unit           VARCHAR(50),
    IN p_MRP            DECIMAL(12,2),
    IN p_SellingPrice   DECIMAL(12,2),
    IN p_BatchId        INT,
    IN p_BatchNumber    VARCHAR(80),
    IN p_ExpiryDate     DATE,
    IN p_Quantity       INT,
    IN p_ReorderLevel   INT,
    IN p_HospitalId     INT,
    IN p_SubHospitalId  INT
)
BEGIN
    UPDATE tbl_medicine
    SET MedicineName = p_MedicineName,
        CategoryId   = p_CategoryId,
        SupplierId   = p_SupplierId,
        Unit         = p_Unit,
        MRP          = p_MRP,
        SellingPrice = p_SellingPrice
    WHERE MedicineId = p_MedicineId;

    UPDATE batches
    SET BatchNumber  = p_BatchNumber,
        Quantity     = p_Quantity,
        ReorderLevel = p_ReorderLevel,
        ExpiryDate   = p_ExpiryDate
    WHERE BatchId = p_BatchId;

    UPDATE stock
    SET TotalQuantity = p_Quantity,
        ReorderLevel  = p_ReorderLevel
    WHERE MedicineId = p_MedicineId AND HospitalId = p_HospitalId;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- GetInventoryById - loads one inventory row for the Edit screen
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS GetInventoryById;
DELIMITER $$
CREATE PROCEDURE GetInventoryById(
    IN p_BatchId       INT,
    IN p_MedicineId    INT,
    IN p_HospitalId    INT,
    IN p_SubHospitalId INT
)
BEGIN
    SELECT
        m.MedicineId,
        m.MedicineName,
        m.CategoryId,
        c.CategoryName,
        m.SupplierId,
        s.SupplierName,
        m.Unit,
        m.MRP,
        m.SellingPrice,
        b.BatchId,
        b.BatchNumber,
        b.Quantity,
        b.ReorderLevel,
        b.ExpiryDate,
        m.HospitalId,
        b.SubHospitalId
    FROM tbl_medicine m
    LEFT JOIN batches b        ON b.BatchId = p_BatchId
    LEFT JOIN categories c     ON c.CategoryId = m.CategoryId
    LEFT JOIN store_suppliers s ON s.SupplierId = m.SupplierId
    WHERE m.MedicineId = p_MedicineId
      AND m.HospitalId = p_HospitalId
    LIMIT 1;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- DeleteInventory - deletes batch + stock row (keeps medicine master)
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS DeleteInventory;
DELIMITER $$
CREATE PROCEDURE DeleteInventory(
    IN p_BatchId       INT,
    IN p_MedicineId    INT,
    IN p_HospitalId    INT,
    IN p_SubHospitalId INT
)
BEGIN
    DELETE FROM batches WHERE BatchId = p_BatchId AND MedicineId = p_MedicineId;
    DELETE FROM stock   WHERE MedicineId = p_MedicineId AND HospitalId = p_HospitalId;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- GetAllSuppliers - dropdown for inventory form
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS GetAllSuppliers;
DELIMITER $$
CREATE PROCEDURE GetAllSuppliers(
    IN p_HospitalId    INT,
    IN p_SubHospitalId INT
)
BEGIN
    SELECT SupplierId, SupplierName
    FROM store_suppliers
    WHERE HospitalId = p_HospitalId
      AND (p_SubHospitalId = 0 OR SubHospitalId = p_SubHospitalId OR SubHospitalId IS NULL)
    ORDER BY SupplierName;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- GetAllCategories - dropdown for inventory form
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS GetAllCategories;
DELIMITER $$
CREATE PROCEDURE GetAllCategories(
    IN p_HospitalId    INT,
    IN p_SubHospitalId INT
)
BEGIN
    SELECT CategoryId, CategoryName
    FROM categories
    WHERE HospitalId = p_HospitalId
      AND (p_SubHospitalId = 0 OR SubHospitalId = p_SubHospitalId OR SubHospitalId IS NULL)
    ORDER BY CategoryName;
END $$
DELIMITER ;

-- ============================================================================
-- 3. Counter stored procedures (medicine search, customers, bills, summary)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- sp_Counter_SearchMedicine — used by Counter page medicine search
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Counter_SearchMedicine;
DELIMITER $$
CREATE PROCEDURE sp_Counter_SearchMedicine(
    IN p_Search        VARCHAR(255),
    IN p_HospitalId    INT,
    IN p_SubHospitalId INT
)
BEGIN
    SELECT
        m.MedicineId,
        m.MedicineName,
        m.MRP,
        m.SellingPrice,
        COALESCE(SUM(b.Quantity),0) AS StockQuantity,
        s.SupplierName
    FROM tbl_medicine m
    LEFT JOIN batches b ON b.MedicineId = m.MedicineId
                       AND b.HospitalId = p_HospitalId
                       AND (p_SubHospitalId = 0 OR b.SubHospitalId = p_SubHospitalId OR b.SubHospitalId IS NULL)
                       AND b.Quantity > 0
    LEFT JOIN store_suppliers s ON s.SupplierId = m.SupplierId
    WHERE m.HospitalId = p_HospitalId
      AND m.IsActive = 1
      AND (m.MedicineName LIKE CONCAT('%', p_Search, '%')
           OR COALESCE(m.MedicineName,'') LIKE CONCAT('%', p_Search, '%'))
    GROUP BY m.MedicineId, m.MedicineName, m.MRP, m.SellingPrice, s.SupplierName
    ORDER BY m.MedicineName
    LIMIT 20;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- sp_Counter_SearchCustomer - customer search (mobile/name)
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Counter_SearchCustomer;
DELIMITER $$
CREATE PROCEDURE sp_Counter_SearchCustomer(
    IN p_Search        VARCHAR(100),
    IN p_HospitalId    INT,
    IN p_SubHospitalId INT
)
BEGIN
    SELECT c.CustomerId, c.CustomerName, c.MobileNumber, c.Address,
           COALESCE(SUM(CASE WHEN cb.PaymentStatus = 'Pending' THEN 1 ELSE 0 END),0) AS PendingBillCount,
           COALESCE(SUM(CASE WHEN cb.PaymentStatus = 'Pending' THEN cb.DueAmount ELSE 0 END),0) AS TotalPendingAmount
    FROM counter_customers c
    LEFT JOIN counter_bill cb ON cb.CustomerId = c.CustomerId AND cb.HospitalId = p_HospitalId
    WHERE c.HospitalId = p_HospitalId
      AND (c.MobileNumber LIKE CONCAT('%', p_Search, '%')
           OR c.CustomerName LIKE CONCAT('%', p_Search, '%'))
    GROUP BY c.CustomerId, c.CustomerName, c.MobileNumber, c.Address
    ORDER BY c.CustomerName
    LIMIT 10;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- sp_Counter_GetOrCreateCustomer
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Counter_GetOrCreateCustomer;
DELIMITER $$
CREATE PROCEDURE sp_Counter_GetOrCreateCustomer(
    IN p_MobileNumber  VARCHAR(20),
    IN p_CustomerName  VARCHAR(255),
    IN p_Address       VARCHAR(500),
    IN p_HospitalId    INT,
    IN p_SubHospitalId INT,
    OUT p_CustomerId   INT,
    OUT p_IsNew        TINYINT
)
BEGIN
    DECLARE v_Id INT;
    SELECT CustomerId INTO v_Id FROM counter_customers
    WHERE MobileNumber = p_MobileNumber AND HospitalId = p_HospitalId LIMIT 1;

    IF v_Id IS NULL THEN
        INSERT INTO counter_customers (MobileNumber, CustomerName, Address, HospitalId, SubHospitalId)
        VALUES (p_MobileNumber, p_CustomerName, p_Address, p_HospitalId, p_SubHospitalId);
        SET p_CustomerId = LAST_INSERT_ID();
        SET p_IsNew = 1;
    ELSE
        UPDATE counter_customers SET CustomerName = p_CustomerName, Address = COALESCE(p_Address, Address)
        WHERE CustomerId = v_Id;
        SET p_CustomerId = v_Id;
        SET p_IsNew = 0;
    END IF;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- sp_Counter_GetPendingBills
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Counter_GetPendingBills;
DELIMITER $$
CREATE PROCEDURE sp_Counter_GetPendingBills(
    IN p_CustomerId INT,
    IN p_HospitalId INT
)
BEGIN
    SELECT BillId, BillNumber, BillDate, TotalAmount, PaidAmount, DueAmount, PaymentStatus
    FROM counter_bill
    WHERE CustomerId = p_CustomerId AND HospitalId = p_HospitalId AND PaymentStatus = 'Pending'
    ORDER BY BillDate DESC;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- sp_Counter_InsertBill
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Counter_InsertBill;
DELIMITER $$
CREATE PROCEDURE sp_Counter_InsertBill(
    IN p_BillNumber    VARCHAR(40),
    IN p_CustomerId    INT,
    IN p_CustomerName  VARCHAR(255),
    IN p_MobileNumber  VARCHAR(20),
    IN p_SubTotal      DECIMAL(12,2),
    IN p_DiscountType  VARCHAR(20),
    IN p_DiscountValue DECIMAL(12,2),
    IN p_DiscountAmount DECIMAL(12,2),
    IN p_TotalAmount   DECIMAL(12,2),
    IN p_PaymentMode   VARCHAR(20),
    IN p_PaidAmount    DECIMAL(12,2),
    IN p_DueAmount     DECIMAL(12,2),
    IN p_HospitalId    INT,
    IN p_SubHospitalId INT,
    IN p_CreatedBy     INT,
    OUT p_BillId       INT
)
BEGIN
    INSERT INTO counter_bill (BillNumber, CustomerId, CustomerName, MobileNumber, SubTotal,
                              DiscountType, DiscountValue, DiscountAmount, TotalAmount,
                              PaymentMode, PaidAmount, DueAmount, PaymentStatus,
                              HospitalId, SubHospitalId, CreatedBy)
    VALUES (p_BillNumber, p_CustomerId, p_CustomerName, p_MobileNumber, p_SubTotal,
            p_DiscountType, p_DiscountValue, p_DiscountAmount, p_TotalAmount,
            p_PaymentMode, p_PaidAmount, p_DueAmount,
            CASE WHEN p_DueAmount > 0 THEN 'Pending' ELSE 'Paid' END,
            p_HospitalId, p_SubHospitalId, p_CreatedBy);
    SET p_BillId = LAST_INSERT_ID();
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- sp_Counter_InsertBillItem (does not deduct stock — FEFO proc handles that)
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Counter_InsertBillItem;
DELIMITER $$
CREATE PROCEDURE sp_Counter_InsertBillItem(
    IN p_BillId        INT,
    IN p_MedicineId    INT,
    IN p_MedicineName  VARCHAR(255),
    IN p_Quantity      INT,
    IN p_UnitPrice     DECIMAL(12,2),
    IN p_TotalPrice    DECIMAL(12,2),
    IN p_HospitalId    INT,
    IN p_SubHospitalId INT,
    OUT p_Success      TINYINT
)
BEGIN
    INSERT INTO counter_bill_item (BillId, MedicineId, MedicineName, Quantity, UnitPrice, TotalPrice)
    VALUES (p_BillId, p_MedicineId, p_MedicineName, p_Quantity, p_UnitPrice, p_TotalPrice);
    SET p_Success = 1;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- sp_Counter_GetTodaySummary
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Counter_GetTodaySummary;
DELIMITER $$
CREATE PROCEDURE sp_Counter_GetTodaySummary(
    IN p_HospitalId    INT,
    IN p_SubHospitalId INT
)
BEGIN
    SELECT
        COUNT(*) AS TotalBills,
        COALESCE(SUM(TotalAmount),0) AS TotalSales,
        COALESCE(SUM(PaidAmount),0)  AS TotalCollected,
        COALESCE(SUM(CASE WHEN PaymentStatus = 'Pending' THEN DueAmount ELSE 0 END),0) AS TotalPending
    FROM counter_bill
    WHERE HospitalId = p_HospitalId
      AND DATE(BillDate) = CURDATE()
      AND (p_SubHospitalId = 0 OR SubHospitalId = p_SubHospitalId OR SubHospitalId IS NULL);
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- sp_Counter_GetBillDetails (header + items for print)
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Counter_GetBillDetails;
DELIMITER $$
CREATE PROCEDURE sp_Counter_GetBillDetails(
    IN p_BillId     INT,
    IN p_HospitalId INT
)
BEGIN
    SELECT cb.BillId, cb.BillNumber, cb.BillDate, cb.CustomerName, cb.MobileNumber,
           cb.SubTotal, cb.DiscountType, cb.DiscountValue, cb.DiscountAmount,
           cb.TotalAmount, cb.PaymentMode, cb.PaymentStatus, cb.PaidAmount, cb.DueAmount,
           COALESCE(h.HospitalName,'') AS HospitalName,
           COALESCE(h.Address,'') AS HospitalAddress,
           COALESCE(h.PhoneNo,'') AS HospitalPhone,
           COALESCE(h.Email,'') AS HospitalEmail,
           COALESCE(h.LogoPath,'') AS HospitalLogo,
           COALESCE(u.FirstName,'') AS CreatedByName
    FROM counter_bill cb
    LEFT JOIN hospitals h ON h.Id = cb.HospitalId
    LEFT JOIN users u ON u.Id = cb.CreatedBy
    WHERE cb.BillId = p_BillId AND cb.HospitalId = p_HospitalId;

    SELECT ItemId, MedicineName, Quantity, UnitPrice, TotalPrice
    FROM counter_bill_item
    WHERE BillId = p_BillId;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- sp_Counter_CollectPayment
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Counter_CollectPayment;
DELIMITER $$
CREATE PROCEDURE sp_Counter_CollectPayment(
    IN p_BillId     INT,
    IN p_Amount     DECIMAL(12,2),
    IN p_PaymentMode VARCHAR(20),
    IN p_UserId     INT,
    IN p_HospitalId INT
)
BEGIN
    UPDATE counter_bill
    SET PaidAmount = PaidAmount + p_Amount,
        PaymentMode = p_PaymentMode,
        DueAmount = GREATEST(0, DueAmount - p_Amount),
        PaymentStatus = CASE WHEN (DueAmount - p_Amount) <= 0 THEN 'Paid' ELSE 'Pending' END
    WHERE BillId = p_BillId AND HospitalId = p_HospitalId;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- sp_Counter_GetCustomerHistory
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Counter_GetCustomerHistory;
DELIMITER $$
CREATE PROCEDURE sp_Counter_GetCustomerHistory(
    IN p_CustomerId    INT,
    IN p_HospitalId    INT,
    IN p_SubHospitalId INT
)
BEGIN
    SELECT BillId, BillNumber, BillDate, TotalAmount, PaidAmount, DueAmount, PaymentStatus, PaymentMode
    FROM counter_bill
    WHERE CustomerId = p_CustomerId AND HospitalId = p_HospitalId
      AND (p_SubHospitalId = 0 OR SubHospitalId = p_SubHospitalId OR SubHospitalId IS NULL)
    ORDER BY BillDate DESC;
END $$
DELIMITER ;

-- ----------------------------------------------------------------------------
-- sp_Counter_GetCustomerItems
-- ----------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Counter_GetCustomerItems;
DELIMITER $$
CREATE PROCEDURE sp_Counter_GetCustomerItems(
    IN p_CustomerId    INT,
    IN p_HospitalId    INT,
    IN p_SubHospitalId INT
)
BEGIN
    SELECT cbi.BillId, cbi.MedicineName, cbi.Quantity, cbi.UnitPrice, cbi.TotalPrice,
           cb.BillNumber, cb.BillDate
    FROM counter_bill_item cbi
    INNER JOIN counter_bill cb ON cb.BillId = cbi.BillId
    WHERE cb.CustomerId = p_CustomerId AND cb.HospitalId = p_HospitalId
      AND (p_SubHospitalId = 0 OR cb.SubHospitalId = p_SubHospitalId OR cb.SubHospitalId IS NULL)
    ORDER BY cb.BillDate DESC;
END $$
DELIMITER ;

-- ============================================================================
-- 4. Dashboard OPD/IPD patient counts
--    Uses medicine_notifications (created above) instead of a non-existent table.
-- ============================================================================
-- (The code now reads from medicine_notifications which we created above.
--   No additional proc needed — the dashboard uses inline SQL.)

-- ============================================================================
-- 5. Reconcile aggregate stock so dashboard low-stock counts reflect reality
-- ============================================================================
DROP PROCEDURE IF EXISTS sp_Pharmacy_ReconcileAllStock;
DELIMITER $$
CREATE PROCEDURE sp_Pharmacy_ReconcileAllStock(
    IN p_HospitalId INT
)
BEGIN
    UPDATE stock s
    SET s.TotalQuantity = (
        SELECT COALESCE(SUM(b.Quantity),0)
        FROM batches b
        WHERE b.MedicineId = s.MedicineId
          AND b.HospitalId = s.HospitalId
          AND b.Quantity > 0
          AND (b.ExpiryDate IS NULL OR b.ExpiryDate >= CURDATE())
    )
    WHERE s.HospitalId = p_HospitalId;
END $$
DELIMITER ;
