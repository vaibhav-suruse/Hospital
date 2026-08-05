$(document).ready(function () {

    console.log("Hospital validation loaded ✅");

    //  Phone number – only digits + 10 length
    $("#PhoneNumber").on("input", function () {
        let value = $(this).val().replace(/[^0-9]/g, '');
        $(this).val(value);

        if (value.length !== 10) {
            $("#errPhoneNumber").text("Phone number must be 10 digits");
        } else {
            $("#errPhoneNumber").text("");
        }
    });

    //  Strict lowercase email regex
    var lowerCaseEmailPattern = /^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$/;

    // 🔹 Real-time lowercase email validation
    $("#EmailId").on("input", function () {
        var val = $(this).val().replace(/^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$/g, '');
        if (val === "") {
            $("#errEmail").text("");
            $(this).removeClass("is-invalid");
        } else if (!lowerCaseEmailPattern.test(val)) {
            $("#errEmail").text("Please enter email in lowercase only");
            $(this).addClass("is-invalid");
        } else {
            $("#errEmail").text("");
            $(this).removeClass("is-invalid");
        }
    });
    //  Name – Only letters and spaces allowed
    $("#Name").on("input", function () {
        let value = $(this).val();

        // remove digits and special characters
        value = value.replace(/[^a-zA-Z\s]/g, '');
        $(this).val(value);

        if (value.trim() === "") {
            $("#errName").text("Hospital Name is required");
        } else {
            $("#errName").text("");
        }
    });


    //  FORM SUBMIT – HARD STOP + VALIDATE
    $("#hospitalForm").on("submit", function (e) {

        e.preventDefault();

        $(".text-danger").text(""); // clear all errors
        let isValid = true;

       

        //  Phone
        let phone = $("#PhoneNumber").val().trim();
        if (phone.length !== 10) {
            $("#errPhoneNumber").text("Valid 10 digit phone number required");
            isValid = false;
        }

        let nameVal = $("#Name").val().trim();
        let namePattern = /^[A-Za-z\s]+$/;

        if (nameVal === "") {
            $("#errName").text("Hospital Name is required");
            isValid = false;
        }
        else if (!namePattern.test(nameVal)) {
            $("#errName").text("Hospital Name must contain only letters");
            isValid = false;
        }

        //  Email
        let emailVal = $("#EmailId").val().trim();
        if (!lowerCaseEmailPattern.test(emailVal)) {
            $("#errEmail").text("Please enter email in lowercase only");
            $("#EmailId").addClass("is-invalid");
            isValid = false;
        } else {
            $("#EmailId").removeClass("is-invalid");
        }

        //  Registration
        let reg = $("#RegistrationNumber").val().trim();
        if (reg === "") {
            $("#errRegNumber").text("Registration Number is required");
            isValid = false;
        }

        //  Description
        let desc = $("#Description").val().trim();
        if (desc === "") {
            $("#errDescription").text("Description is required");
            isValid = false;
        } 
        //  Address
        let addressVal = $("#Address").val().trim();
        if (addressVal === "") {
            $("#errAddress").text("Address is required");
            isValid = false;
        }

        //  Meta
        let meta = $("#MetaLink").val().trim();
        if (meta === "") {
            $("#errMetaLink").text("Meta Link is required");
            isValid = false;
        }

        //  Instagram
        let insta = $("#InstaLink").val().trim();
        if (insta === "") {
            $("#errInstaLink").text("Instagram link is required");
            isValid = false;
        }
        let fileInput = $("#Logo")[0];

        if (fileInput && fileInput.files && fileInput.files.length > 0) {
            let file = fileInput.files[0];
            let allowedTypes = ["image/jpeg", "image/png", "image/gif", "application/pdf"];

            if (!allowedTypes.includes(file.type)) {
                $("#errLogo").text("Only images (jpg, png, gif) or PDF are allowed");
                isValid = false;
            } else {
                $("#errLogo").text("");
            }
        } 


        //  Parent Hospital
        let hospitalType = $("#HospitalType").val();
        let parentId = $("#ParentHospitalId").val();
        if (hospitalType === "true" && parentId === "") {
            alert("Please select Parent Hospital");
            isValid = false;
        }

        if (isValid) {
            this.submit(); 
        }

    });

});
