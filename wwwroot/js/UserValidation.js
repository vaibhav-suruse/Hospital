$(document).ready(function () {

    // ---------------- PASSWORD REAL TIME ----------------
    $("#Password").on("keyup", function () {

        let password = $(this).val();

        let capitalFirst = /^[A-Z]/.test(password);
        let lengthValid = password.length >= 8 && password.length <= 16;
        let hasNumber = /\d/.test(password);
        let hasSymbol = /[@$!%*?&#]/.test(password);

        if (capitalFirst && lengthValid && hasNumber && hasSymbol) {
            $("#passwordRule")
                .text("Strong password ✔")
                .removeClass("text-danger")
                .addClass("text-success");
        } else {
            $("#passwordRule")
                .text("Start with capital, 8-16 chars, 1 number & 1 symbol (@$!%*?&#)")
                .removeClass("text-success")
                .addClass("text-danger");
        }
    });

    // ---------------- NAME & ROLE REAL TIME ----------------
    function allowTextOnly(inputId, errorId, fieldName) {

        $("#" + inputId).on("input", function () {

            let value = $(this).val();

            // remove numbers & special characters
            value = value.replace(/[^A-Za-z\s]/g, '');
            $(this).val(value);

            if (value.trim() === "") {
                $("#" + errorId).text(fieldName + " is required");
            } else {
                $("#" + errorId).text("");
            }
        });
    }

    allowTextOnly("FirstName", "errFirstName", "First Name");
    allowTextOnly("LastName", "errLastName", "Last Name");
    allowTextOnly("Role", "errRole", "Role");

    // ---------------- EMAIL REAL TIME ----------------
    var lowerCaseEmailPattern = /^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$/;

    $("#EmailId").on("input", function () {

        let val = $(this).val().trim();

        if (val === "") {
            $("#errEmail").text("");
            $(this).removeClass("is-invalid");
            return;
        }

        if (val !== val.toLowerCase()) {
            $("#errEmail").text("Email must be lowercase only");
            $(this).addClass("is-invalid");
            return;
        }

        if (!lowerCaseEmailPattern.test(val)) {
            $("#errEmail").text("Please enter valid email format");
            $(this).addClass("is-invalid");
        } else {
            $("#errEmail").text("");
            $(this).removeClass("is-invalid");
        }
    });

    // ---------------- PHONE ONLY NUMBERS ----------------
    $("#PhoneNo").on("input", function () {
        let value = $(this).val().replace(/[^0-9]/g, '');
        $(this).val(value);
    });

    // ---------------- FORM SUBMIT VALIDATION ----------------
    $("#userForm").on("submit", function (e) {

        let isValid = true;
        let isUpdate = $("#Id").val() !== "" && $("#Id").val() !== "0";

        let namePattern = /^[A-Za-z\s]+$/;

        // First Name
        let firstName = $("#FirstName").val().trim();
        if (firstName === "") {
            $("#errFirstName").text("First Name is required");
            isValid = false;
        } else if (!namePattern.test(firstName)) {
            $("#errFirstName").text("Only characters allowed");
            isValid = false;
        }

        // Last Name
        let lastName = $("#LastName").val().trim();
        if (lastName === "") {
            $("#errLastName").text("Last Name is required");
            isValid = false;
        } else if (!namePattern.test(lastName)) {
            $("#errLastName").text("Only characters allowed");
            isValid = false;
        }

        // Role
        let role = $("#Role").val().trim();
        if (role === "") {
            $("#errRole").text("Role is required");
            isValid = false;
        } else if (!namePattern.test(role)) {
            $("#errRole").text("Only characters allowed");
            isValid = false;
        }

        // Login Name
        if ($("#LoginName").val().trim() === "") {
            $("#errLoginName").text("Login Name is required");
            isValid = false;
        }

        // Gender
        if ($("#Gender").val() === "") {
            $("#errGender").text("Gender is required");
            isValid = false;
        }

        // Main Hospital
        if ($("#MainHospital").val() === "") {
            $("#errMainHospital").text("Please select Main Hospital");
            isValid = false;
        }

        // Phone
        let phone = $("#PhoneNo").val().trim();
        if (phone.length !== 10) {
            $("#errPhoneNo").text("Phone number must be 10 digits");
            isValid = false;
        }

        // Email
        let emailVal = $("#EmailId").val().trim();

        if (emailVal === "") {
            $("#errEmail").text("Email is required");
            isValid = false;
        }
        else if (emailVal !== emailVal.toLowerCase()) {
            $("#errEmail").text("Email must be lowercase only");
            isValid = false;
        }
        else if (!lowerCaseEmailPattern.test(emailVal)) {
            $("#errEmail").text("Please enter valid email format");
            isValid = false;
        }

        // Password (Only on Create)
        if (!isUpdate) {

            let pwd = $("#Password").val().trim();

            let capitalFirst = /^[A-Z]/.test(pwd);
            let hasNumber = /\d/.test(pwd);
            let hasSymbol = /[@$!%*?&#]/.test(pwd);

            if (pwd === "") {
                $("#passwordRule").text("Password is required")
                    .removeClass("text-success")
                    .addClass("text-danger");
                isValid = false;
            }
            else if (pwd.length < 8 || pwd.length > 16) {
                $("#passwordRule").text("Password must be 8 to 16 characters long")
                    .removeClass("text-success")
                    .addClass("text-danger");
                isValid = false;
            }
            else if (!capitalFirst) {
                $("#passwordRule").text("Password must start with capital letter")
                    .removeClass("text-success")
                    .addClass("text-danger");
                isValid = false;
            }
            else if (!hasNumber) {
                $("#passwordRule").text("Password must contain at least 1 number")
                    .removeClass("text-success")
                    .addClass("text-danger");
                isValid = false;
            }
            else if (!hasSymbol) {
                $("#passwordRule").text("Password must contain 1 symbol (@$!%*?&#)")
                    .removeClass("text-success")
                    .addClass("text-danger");
                isValid = false;
            }
        }

        if (!isValid) {
            e.preventDefault();
        }

    });

});