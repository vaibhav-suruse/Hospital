$(document).ready(function () {

    // ================= TEXT ONLY (First, Last, Specialization, Education) =================
    function allowTextOnly(inputId, errorId, fieldName) {

        $("#" + inputId).on("input", function () {

            let value = $(this).val();

            // Remove numbers & special characters
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
    allowTextOnly("Specialization", "errSpecialization", "Specialization");
    allowTextOnly("Education", "errEducation", "Education");


    // ================= MOBILE =================
    $("#MobileNo").on("input", function () {

        let value = $(this).val().replace(/[^0-9]/g, '');

        if (value.length > 10) {
            value = value.substring(0, 10);
        }

        $(this).val(value);

        if (value.length !== 10) {
            $("#errMobile").text("Mobile number must be 10 digits");
        } else {
            $("#errMobile").text("");
        }
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


    // ================= EXPERIENCE =================
    $("#ExperienceYears").on("input", function () {

        let value = $(this).val().replace(/[^0-9]/g, '');
        $(this).val(value);

        if (value === "") {
            $("#errExperience").text("Experience is required");
        }
        else if (parseInt(value) <= 0) {
            $("#errExperience").text("Experience must be greater than 0");
        }
        else {
            $("#errExperience").text("");
        }
    });


    // ================= FORM SUBMIT =================
    $("#doctorForm").on("submit", function (e) {

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

        // Gender
        if ($("#Gender").val() === "") {
            $("#errGender").text("Gender is required");
            isValid = false;
        }

        // Specialization
        let spec = $("#Specialization").val().trim();
        if (spec === "" || !namePattern.test(spec)) {
            $("#errSpecialization").text("Valid Specialization is required");
            isValid = false;
        }

        // Education
        let edu = $("#Education").val().trim();
        if (edu === "" || !namePattern.test(edu)) {
            $("#errEducation").text("Valid Education is required");
            isValid = false;
        }

        // Experience
        let exp = $("#ExperienceYears").val().trim();
        if (exp === "" || !/^[0-9]+$/.test(exp) || parseInt(exp) <= 0) {
            $("#errExperience").text("Experience must be greater than 0");
            isValid = false;
        }

        // Mobile
        let mobile = $("#MobileNo").val().trim();
        if (mobile.length !== 10) {
            $("#errMobile").text("Mobile number must be 10 digits");
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

        // Address
        if ($("#Address").val().trim() === "") {
            $("#errAddress").text("Address is required");
            isValid = false;
        }

        if (!isValid) {
            e.preventDefault();
        }

    });

});