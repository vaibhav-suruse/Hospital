DROP PROCEDURE IF EXISTS LoginUser;

DELIMITER $$
CREATE PROCEDURE `LoginUser`(
    IN p_LoginName VARCHAR(100),
    IN p_Password VARCHAR(100),
    IN p_MainHospitalId INT,
    IN p_SubHospitalId INT
)
BEGIN
    DECLARE v_UserId INT;
    DECLARE v_Password VARCHAR(100);
    DECLARE v_IsActive INT;
    DECLARE v_Role VARCHAR(50);

    -- Step 1: Username check
    SELECT
        u.Id, u.Password, u.IsActive, u.Role
    INTO
        v_UserId, v_Password, v_IsActive, v_Role
    FROM `users` u
    WHERE u.LoginName = p_LoginName
    LIMIT 1;

    -- Step 2: Username + Password wrong
    IF v_UserId IS NULL AND TRIM(p_Password) != '' THEN
        SELECT 'Invalid Username or Password' AS Message, NULL AS UserId;

    -- Step 3: Username wrong (Password blank)
    ELSEIF v_UserId IS NULL THEN
        SELECT 'Invalid Username' AS Message, NULL AS UserId;

    -- Step 4: Password wrong
    ELSEIF TRIM(v_Password) != TRIM(p_Password) THEN
        SELECT 'Invalid Password' AS Message, NULL AS UserId;

    -- Step 5: Account Inactive
    ELSEIF v_IsActive != 1 THEN
        SELECT 'Account is Inactive' AS Message, NULL AS UserId;

    -- Step 6: Login Successful
    -- Note: HospitalId/SubHospitalId are taken from the user's OWN record.
    -- The p_MainHospitalId / p_SubHospitalId params are ignored → auto-select.
    ELSE
        SELECT
            'Login Successful' AS Message,
            u.Id AS UserId,
            u.LoginName,
            u.Role,
            u.HospitalId,
            u.SubHospitalId,
            u.IsActive
        FROM `users` u
        WHERE u.LoginName = p_LoginName
        LIMIT 1;
    END IF;
END$$
DELIMITER ;
