using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface ICounter
    {
        // Customer
        CounterCustomerModel GetOrCreateCustomer(string mobileNumber, string customerName, string address, int hospitalId, int? subHospitalId);
        List<CounterCustomerModel> SearchCustomer(string search, int hospitalId, int? subHospitalId);
        List<CounterPendingBillModel> GetPendingBills(int customerId, int hospitalId);

        // Medicine
        List<CounterMedicineSearchModel> SearchMedicine(string search, int hospitalId, int? subHospitalId);

        // Bill
        int SaveBill(CounterBillModel bill, List<CounterCartItem> cartItems, int hospitalId, int? subHospitalId);
        CounterBillModel GetBillDetails(int billId, int hospitalId);
        CounterTodaySummaryModel GetTodaySummary(int hospitalId, int? subHospitalId);

        // Add this method to ICounter.cs
        bool CollectPayment(int billId, decimal amount, string paymentMode, int userId, int hospitalId);
        // Get customer purchase history
        List<CounterBillModel> GetCustomerHistory(int customerId, int hospitalId, int? subHospitalId);
        List<CounterBillItemModel> GetCustomerPurchaseItems(int customerId, int hospitalId, int? subHospitalId);
        // Get customer by ID
        CounterCustomerModel GetCustomerById(int customerId, int hospitalId, int? subHospitalId);
    }
}