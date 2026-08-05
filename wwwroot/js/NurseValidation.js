
    $(document).ready(function () {

        // ---------------- TEXT ONLY ----------------
        function allowTextOnly(inputId, errorId, fieldName) {
            $("#" + inputId).on("input", function () {
                let value = $(this).val().replace(/[^A-Za-z\s]/g, '');
                $(this).val(value);
                $("#" + errorId).text(value.trim() === "" ? fieldName + " is required" : "");
            });
        }

            allowTextOnly("FirstName", "errFirstName", "First Name");
    allowTextOnly("LastName", "errLastName", "Last Name");
    allowTextOnly("Qualification", "errQualification", "Qualification");
    allowTextOnly("Department", "errDepartment", "Department");

    // ---------------- PHONE ----------------
    $("#PhoneNumber").on("input", function () {
        let value = $(this).val().replace(/[^0-9]/g, '');
    $(this).val(value);
    $("#errPhoneNumber").text(value.length !== 10 ? "Phone number must be 10 digits" : "");
            });

    // ---------------- EMAIL ----------------
    $("#Email").on("input", function () {
        let value = $(this).val();
    let pattern = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
    $("#errEmail").text(!pattern.test(value) ? "Valid email required" : "");
            });

    // ---------------- FORM SUBMIT ----------------
    $("#nurseForm").on("submit", function (e) {
        let isValid = true;

    if ($("#FirstName").val().trim() === "") {
        $("#errFirstName").text("First Name is required");
    isValid = false;
                }
    if ($("#LastName").val().trim() === "") {
        $("#errLastName").text("Last Name is required");
    isValid = false;
                }
    if ($("#Gender").val() === "") {
        $("#errGender").text("Gender is required");
    isValid = false;
                }
    let phone = $("#PhoneNumber").val();
    if (phone.length !== 10) {
        $("#errPhoneNumber").text("Phone number must be 10 digits");
    isValid = false;
                }
    let email = $("#Email").val();
    let pattern = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
    if (!pattern.test(email)) {
        $("#errEmail").text("Valid email required");
    isValid = false;
                }

    if (!isValid) e.preventDefault();
            });
        });
