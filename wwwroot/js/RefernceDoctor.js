$(document).ready(function () {

    // TEXT ONLY
    function allowTextOnly(inputId, errorId, fieldName) {
        $("#" + inputId).on("input", function () {
            let value = $(this).val().replace(/[^A-Za-z\s]/g, '');
            $(this).val(value);

            if (value.trim() === "")
                $("#" + errorId).text(fieldName + " is required");
            else
                $("#" + errorId).text("");
        });
    }

    allowTextOnly("DoctorName", "errDoctorName", "Doctor Name");
    allowTextOnly("ClinicName", "errClinicName", "Clinic Name");
    allowTextOnly("City", "errCity", "City");

    // MOBILE
    $("#MobileNumber").on("input", function () {
        let value = $(this).val().replace(/[^0-9]/g, '');
        $(this).val(value);

        if (value.length !== 10)
            $("#errMobile").text("Mobile number must be 10 digits");
        else
            $("#errMobile").text("");
    });

    // ================= EMAIL =================
    var lowerCaseEmailPattern = /^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$/;

    $("#Email").on("input", function () {

        let val = $(this).val().trim();

        if (val === "") {
            $("#errEmail").text("");
            return;
        }

        if (val !== val.toLowerCase()) {
            $("#errEmail").text("Email must be lowercase only");
            return;
        }

        if (!lowerCaseEmailPattern.test(val)) {
            $("#errEmail").text("Please enter valid email format");
        } else {
            $("#errEmail").text("");
        }
    });

    // ================= PERCENTAGE (>0 only) =================
    $("#Percentage").on("input", function () {

        let value = $(this).val().replace(/[^0-9.]/g, '');
        $(this).val(value);

        let number = parseFloat(value);

        if (value === "") {
            $("#errPercentage").text("Percentage is required");
        }
        else if (isNaN(number) || number <= 0) {
            $("#errPercentage").text("Percentage must be greater than 0");
        }
        else {
            $("#errPercentage").text("");
        }
    });

    // ADDRESS
    $("#Address").on("input", function () {
        let value = $(this).val();
        if (value.trim().length < 5)
            $("#errAddress").text("Address must be at least 5 characters");
        else
            $("#errAddress").text("Address is required");
    });

    // SUBMIT VALIDATION
    $("#doctorForm").on("submit", function (e) {

        let isValid = true;

        if ($("#DoctorName").val().trim() === "") {
            $("#errDoctorName").text("Doctor Name is required");
            isValid = false;
        }

        if ($("#ClinicName").val().trim() === "") {
            $("#errClinicName").text("Clinic Name is required");
            isValid = false;
        }

        if ($("#MobileNumber").val().length !== 10) {
            $("#errMobile").text("Valid mobile number required");
            isValid = false;
        }

        // Email
        let emailVal = $("#Email").val().trim();
        if (emailVal === "" ||
            emailVal !== emailVal.toLowerCase() ||
            !lowerCaseEmailPattern.test(emailVal)) {

            $("#errEmail").text("Valid lowercase email is required");
            isValid = false;
        }

        let percentageInput = $("#Percentage").val().trim();
        let percentageVal = parseFloat(percentageInput);

        if (percentageInput === "") {
            $("#errPercentage").text("Percentage is required");
            isValid = false;
        }
        else if (isNaN(percentageVal) || percentageVal <= 0 || percentageVal > 100) {
            $("#errPercentage").text("Percentage must be greater than 0");
            isValid = false;
        }
        else {
            $("#errPercentage").text("");
        }

        if ($("#Address").val().trim().length < 5) {
            $("#errAddress").text("Valid address required");
            isValid = false;
        }

        if (!isValid)
            e.preventDefault();
    });

});