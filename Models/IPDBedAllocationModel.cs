using System;

namespace WebApplicationSampleTest2.Models
{
    public class IPDBedAllocationModel
    {
        public int AllocationId { get; set; }
        public int IPDId { get; set; }
        public int BedId { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public bool IsCurrent { get; set; }
        public int AllocatedBy { get; set; }
        public int Days { get; set; }
    }
}
