$(document).ready(function () {
    // Password validation
    $("#Password").on("keyup", function () {
        let password = $(this).val();
        let capitalFirst = /^[A-Z]/.test(password);
        let minLength = password.length >= 8;
        let hasNumber = /\d/.test(password);
        let hasSymbol = /[@$!%*?&#]/.test(password);

        if (capitalFirst && minLength && hasNumber && hasSymbol) {
            $("#passwordRule").text("Strong password ✔").removeClass("text-danger").addClass("text-success");
        } else {
            $("#passwordRule").text("Start with capital, min 8 chars, 1 number & 1 symbol (@$!%*?&#)").removeClass("text-success").addClass("text-danger");
        }
    });

    // Mobile number validation
    $("#PhoneNumber").on("input", function () {
        let value = $(this).val().replace(/[^0-9]/g, '');
        $(this).val(value);
        if (value.length !== 10) {
            $("#mobileError").text("Mobile number must be 10 digits");
        } else {
            $("#mobileError").text("");
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
    // Form submit validation
    $("#signupForm").submit(function (e) {

        let isValid = true;
        let namePattern = /^[A-Za-z\s]+$/;

        // First Name
        let firstName = $("#FirstName").val().trim();
        if (firstName === "" || !namePattern.test(firstName)) {
            $("#errFirstName").text("Valid First Name is required");
            isValid = false;
        }

        // Last Name
        let lastName = $("#LastName").val().trim();
        if (lastName === "" || !namePattern.test(lastName)) {
            $("#errLastName").text("Valid Last Name is required");
            isValid = false;
        }

        let gender = $("select[name='Gender']").val();
        let age = $("input[name='Age']").val();
        let email = $("input[name='Email']").val().trim();
        let address = $("textarea[name='Address']").val().trim();
        let valid = true;
        let errorMsg = "";
        let emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (firstName === "") { valid = false; errorMsg += "First name required<br>"; }
        if (lastName === "") { valid = false; errorMsg += "Last name required<br>"; }
        if (gender === "") { valid = false; errorMsg += "Gender required<br>"; }
        if (age === "" || age <= 0) { valid = false; errorMsg += "Valid age required<br>"; }
        if (address === "") { valid = false; errorMsg += "Address required<br>"; }
        if (email === "" || !emailPattern.test(email)) { valid = false; errorMsg += "Valid email required<br>"; }
        if ($("#mobileError").text() !== "") { valid = false; errorMsg += "Mobile number invalid<br>"; }
        if ($("#passwordRule").hasClass("text-danger")) { valid = false; errorMsg += "Password invalid<br>"; }

        if (!valid) {
            e.preventDefault();
            toastr.error(errorMsg, "Validation Error", { timeOut: 5000, extendedTimeOut: 2000 });
        }
    });
});
