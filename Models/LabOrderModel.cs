using System;

namespace WebApplicationSampleTest2.Models
{
    public class LabOrderModel
    {
        public int OrderId { get; set; }


        public string Category { get; set; } = string.Empty;


        public int OrderedBy { get; set; }
        public string OrderedByName { get; set; } = string.Empty;


        public string Result { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;



        public int IPDId { get; set; }
        public string InvestigationType { get; set; }  // Lab/Xray/CT...
        public string TestName { get; set; }
        public string Priority { get; set; }          // Routine/Urgent/Emergency
        public string Instructions { get; set; }
        public string Status { get; set; }
        public DateTime OrderedDateTime { get; set; }
        // Round this order belongs to - links it back to the note it was added with
        public int RoundId { get; set; }
    }
}
