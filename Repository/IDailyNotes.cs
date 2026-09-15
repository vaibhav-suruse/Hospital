using System.Collections.Generic;
using WebApplicationSampleTest2.Models;
namespace WebApplicationSampleTest2.Repository
{
    public interface IDailyNotes
    {
        // Doctor Notes
        List<DoctorNoteModel> GetDoctorNotes(int ipId);
        DoctorNoteModel GetDoctorNoteById(int noteId);
        int InsertDoctorNote(DoctorNoteModel model);
        void UpdateDoctorNote(DoctorNoteModel model);
        void DeleteDoctorNote(int noteId);
        // Nurse Notes
        List<NurseNoteModel> GetNurseNotes(int ipId);
        NurseNoteModel GetNurseNoteById(int noteId);
        int InsertNurseNote(NurseNoteModel model);
        void UpdateNurseNote(NurseNoteModel model);
        void DeleteNurseNote(int noteId);
        // Templates
        List<NoteTemplateModel> GetTemplates(string type, string search = "");
        int InsertTemplate(NoteTemplateModel model, int createdBy);
        // NEW: Audit Trail
        void InsertNoteHistory(NoteHistoryModel history);
        List<NoteHistoryModel> GetNoteHistory(int noteId, string noteType);
        // Medicine Orders
        void InsertMedicineOrder(MedicineOrderModel model);
        List<MedicineOrderModel> GetPendingMedicineOrders(int ipId);
        // Lab Orders
        void InsertLabOrder(LabOrderModel model);
        List<LabOrderModel> GetPendingLabOrders(int ipId);
        List<LabTestMasterModel> SearchLabTests(int parentHospitalId, int? subHospitalId, string search);
        // Lab Orders (Daily Notes)
        // CHANGED: now returns the RoundId (reuses the note's round instead of
        // always creating a new disconnected one), and requires doctorId as
        // a fallback in case it's ever called without a round.
        int InsertDailyNotesLabOrder(int parentHospitalId, int? subHospitalId, LabOrderModel model, int doctorId);
        List<LabOrderModel> GetPendingLabOrdersByIPD(int ipdId);
        // Medicine Orders (Daily Notes)
        List<MedicineOrderModel> GetPendingMedicinesByIPD(int ipdId);
        int InsertDailyNotesMedicine(int parentHospitalId, int? subHospitalId, MedicineOrderModel model, int doctorId);
        List<MedicineOrderModel> SearchMedicinesForDailyNotes(string searchTerm);


// In your IDailyNotes interface
        // BUGFIX: hospital-scoped so the Daily Notes dropdowns only show
        // doctors/nurses belonging to the current facility.
        List<DoctorModel> GetDoctors(int hospitalId, int? subHospitalId);
        List<NurseDropdownModel> GetNurses(int hospitalId, int? subHospitalId);

        // Symptoms (Daily Notes)
        int InsertDailyNotesSymptom(int parentHospitalId, int? subHospitalId, int ipdId, int symptomId, int doctorId, int roundIdIn = 0);
        List<Symptom> GetActiveSymptomsByIPD(int ipdId);

        // Completed Lab Reports (uploaded by the lab, needs to be visible to the doctor)
        List<LabInvestigationModel> GetCompletedLabReportsByIPD(int ipdId);

        // Note <-> Round linkage (for per-note History)
        int CreateRoundForNote(int parentHospitalId, int? subHospitalId, int ipdId, int doctorId, int noteId);
        List<MedicineOrderModel> GetMedicinesByRoundId(int roundId);
        List<LabInvestigationModel> GetLabOrdersByRoundId(int roundId);
        List<Symptom> GetSymptomsByRoundId(int roundId);

        // Discontinue a medicine (Status enum only allows Active/Stopped/Completed)
        MedicineOrderModel DiscontinueMedicine(int prescriptionId, int hospitalId, int? subHospitalId);

        // MAR (Medication Administration Record)
        List<MARRowModel> GetOrCreateMARForDate(int ipdId, System.DateTime date);
        void RecordAdministration(RecordAdministrationModel model);

    }
}
