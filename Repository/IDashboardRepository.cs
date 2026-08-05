using System.Collections.Generic;

namespace WebApplicationSampleTest2.Repository
{
    /// <summary>
    /// Aggregated dashboard stats — all counts computed server-side
    /// inside a SINGLE efficient SQL query instead of loading every
    /// row into memory and counting in C# LINQ.
    /// </summary>
    public class DashboardCountsVM
    {
        /* ── Top cards ── */
        public int TotalPatients { get; set; }
        public int TotalAppointments { get; set; }
        public int TotalDoctors { get; set; }
        public int CompletedAppointments { get; set; }
        public int PendingAppointments { get; set; }

        /* ── Available doctors (doctors not booked on a given day) ── */
        public int TodayAvailableDoctors { get; set; }
        public int YesterdayAvailableDoctors { get; set; }
        public int TomorrowAvailableDoctors { get; set; }

        /* ── Today ── */
        public int TodayAppointments { get; set; }
        public int TodayCompleted { get; set; }
        public int TodayPending { get; set; }

        /* ── Yesterday ── */
        public int YesterdayAppointments { get; set; }
        public int YesterdayCompleted { get; set; }
        public int YesterdayPending { get; set; }

        /* ── Tomorrow ── */
        public int TomorrowAppointments { get; set; }
        public int TomorrowCompleted { get; set; }
        public int TomorrowPending { get; set; }

        /* ── Aggregates ── */
        public int WeeklyAppointments { get; set; }
        public int MonthlyAppointments { get; set; }
        public int YearlyAppointments { get; set; }

        /* ── Monthly chart data (12 months) ── */
        public List<int> MonthlyTotal { get; set; } = new List<int>(new int[12]);
        public List<int> MonthlyCompleted { get; set; } = new List<int>(new int[12]);
        public List<int> MonthlyPending { get; set; } = new List<int>(new int[12]);

        /* ── Appointment lists ── */
        public List<object> TodayAppointmentList { get; set; } = new List<object>();
        public List<object> YesterdayAppointmentList { get; set; } = new List<object>();
        public List<object> TomorrowAppointmentList { get; set; } = new List<object>();
    }

    public interface IDashboardRepository
    {
        /// <summary>
        /// Returns ALL dashboard counts in a single DB round-trip using
        /// aggregated SQL queries — no in-memory LINQ counting.
        /// </summary>
        DashboardCountsVM GetDashboardCounts(int hospitalId, int? subHospitalId);
    }
}

