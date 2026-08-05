using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    /// <summary>
    /// Efficient data access for the IPD Dashboard.
    /// Replaces the previous pattern of loading ALL beds, wards, rooms,
    /// doctors and patients into memory and counting them in C# LINQ
    /// (a multi-round-trip, full-table scan per request). All bed/ward/
    /// room/doctor/patient counts are now computed in a single aggregated
    /// SQL query with one DB round-trip.
    /// </summary>
    public interface IIPDDashboardRepository
    {
        /// <summary>
        /// Returns the bed/ward/room/doctor/patient counts and ward-wise
        /// bed status for the given hospital, computed entirely in SQL.
        /// </summary>
        IPDDashboardVM GetDashboardCounts(int hospitalId, int? subHospitalId);
    }
}
