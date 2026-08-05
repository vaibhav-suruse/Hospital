@section Scripts{

    <script>

        let currentTab = "doctor";

        $(document).ready(function () {

            loadDoctorNotes();

        $("#doctorTab").click(function () {

            currentTab = "doctor";

        $("#doctorTab")
        .removeClass("btn-outline-primary")
        .addClass("btn-primary");

        $("#nurseTab")
        .removeClass("btn-primary")
        .addClass("btn-outline-primary");

        loadDoctorNotes();
    });

        $("#nurseTab").click(function () {

            currentTab = "nurse";

        $("#nurseTab")
        .removeClass("btn-outline-primary")
        .addClass("btn-primary");

        $("#doctorTab")
        .removeClass("btn-primary")
        .addClass("btn-outline-primary");

        loadNurseNotes();

    });

        $("#btnAddNote").click(function () {

        if (currentTab == "doctor") {

            $("#doctorNoteModal").modal("show");

        }
        else {

            $("#nurseNoteModal").modal("show");

        }

    });

});



        /*--------------------------------------
        LOAD DOCTOR NOTES
        ---------------------------------------*/

        function loadDoctorNotes() {

            $.ajax({

                url: "/DailyNotes/GetDoctorNotesList",

                type: "GET",

                data: {

                    ipId: $("#IpId").val()

                },

                success: function (res) {

                    $("#notesList").html(res);

                }

            });

}



        /*--------------------------------------
        LOAD NURSE NOTES
        ---------------------------------------*/

        function loadNurseNotes() {

            $.ajax({

                url: "/DailyNotes/GetNurseNotesList",

                type: "GET",

                data: {

                    ipId: $("#IpId").val()

                },

                success: function (res) {

                    $("#notesList").html(res);

                }

            });

}



        /*--------------------------------------
        SAVE DOCTOR NOTE
        ---------------------------------------*/

        function saveDoctorNote() {

    var model = {

            NoteId: $("#dnNoteId").val(),

        IpId: $("#IpId").val(),

        Temperature: $("#dnTemperature").val(),

        Pulse: $("#dnPulse").val(),

        RespRate: $("#dnRespRate").val(),

        BpSystolic: $("#dnBpSystolic").val(),

        BpDiastolic: $("#dnBpDiastolic").val(),

        SpO2: $("#dnSpo2").val(),

        Notes: $("#dnNotes").val(),

        NoteDate: $("#dnDate").val(),

        NoteTime: $("#dnTime").val(),

        TemplateId: $("#dnTemplateId").val()

    };

        $.ajax({

            url: "/DailyNotes/SaveDoctorNote",

        type: "POST",

        contentType: "application/json",

        data: JSON.stringify(model),

        success: function (res) {

            if (res.success) {

            $("#doctorNoteModal").modal("hide");

        loadDoctorNotes();

            }
        else {

            alert(res.message);

            }

        }

    });

}



        /*--------------------------------------
        SAVE NURSE NOTE
        ---------------------------------------*/

        function saveNurseNote() {

    var model = {

            NoteId: $("#nnNoteId").val(),

        IpId: $("#IpId").val(),

        Notes: $("#nnNotes").val(),

        NoteDate: $("#nnDate").val(),

        NoteTime: $("#nnTime").val(),

        TemplateId: $("#nnTemplateId").val()

    };

        $.ajax({

            url: "/DailyNotes/SaveNurseNote",

        type: "POST",

        contentType: "application/json",

        data: JSON.stringify(model),

        success: function (res) {

            if (res.success) {

            $("#nurseNoteModal").modal("hide");

        loadNurseNotes();

            }
        else {

            alert(res.message);

            }

        }

    });

}



        /*--------------------------------------
        RESET
        ---------------------------------------*/

        function resetDoctorForm() {

            $("#doctorNoteModal input").val("");

        $("#doctorNoteModal textarea").val("");

}



        function resetNurseForm() {

            $("#nurseNoteModal input").val("");

        $("#doctorNoteModal textarea").val("");

}

    </script>

}