namespace WebApplicationSampleTest2.Models
{
    public class SymptomOrderModel
    {
        public int IPDId { get; set; }
        public int SymptomId { get; set; }
        public int DoctorId { get; set; }
        // Round this symptom belongs to - links it back to the note it was added with
        public int RoundId { get; set; }
    }
}
