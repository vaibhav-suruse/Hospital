using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{


    public class IPDDashboardVM
    {
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }
        public int MaintenanceBeds { get; set; }
        public int TotalRooms { get; set; }
        public int TotalWards { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalPatients { get; set; }
        public int TodayAdmissions { get; set; }
        public int TodayDischarges { get; set; }
        public int CurrentlyAdmitted { get; set; }
        public int WeeklyAdmissions { get; set; }
        public int MonthlyAdmissions { get; set; }
        public int YearlyAdmissions { get; set; }
        public int PendingLab { get; set; }
        public int CriticalPatients { get; set; }
        public List<WardBedInfo> WardWiseBeds { get; set; } = new List<WardBedInfo>();

        // For chart
        public List<string> ChartDates { get; set; } = new List<string>();
        public List<int> ChartCounts { get; set; } = new List<int>();
    }

    public class WardBedInfo
    {
        public string WardName { get; set; }
        public int Total { get; set; }
        public int Occupied { get; set; }
    }
}