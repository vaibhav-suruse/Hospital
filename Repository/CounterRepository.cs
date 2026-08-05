using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class CounterRepository : ICounter
    {
        private readonly string _connectionString;

        public CounterRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }

        public CounterCustomerModel GetOrCreateCustomer(string mobileNumber, string customerName, string address, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_Counter_GetOrCreateCustomer", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_MobileNumber", mobileNumber);
                cmd.Parameters.AddWithValue("p_CustomerName", customerName);
                cmd.Parameters.AddWithValue("p_Address", address ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? (object)DBNull.Value);

                var pCustomerId = new MySqlParameter("p_CustomerId", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
                var pIsNew = new MySqlParameter("p_IsNew", MySqlDbType.Byte) { Direction = ParameterDirection.Output };

                cmd.Parameters.Add(pCustomerId);
                cmd.Parameters.Add(pIsNew);

                conn.Open();
                cmd.ExecuteNonQuery();

                return new CounterCustomerModel
                {
                    CustomerId = Convert.ToInt32(pCustomerId.Value),
                    MobileNumber = mobileNumber,
                    CustomerName = customerName,
                    Address = address,
                    IsNew = Convert.ToBoolean(pIsNew.Value)
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error in GetOrCreateCustomer", ex);
            }
        }

        public List<CounterCustomerModel> SearchCustomer(string search, int hospitalId, int? subHospitalId)
        {
            var list = new List<CounterCustomerModel>();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_Counter_SearchCustomer", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_Search", search);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? (object)DBNull.Value);

                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new CounterCustomerModel
                    {
                        CustomerId = Convert.ToInt32(reader["CustomerId"]),
                        CustomerName = reader["CustomerName"].ToString(),
                        MobileNumber = reader["MobileNumber"].ToString(),
                        Address = reader["Address"]?.ToString(),
                        PendingBillCount = Convert.ToInt32(reader["PendingBillCount"]),
                        TotalPendingAmount = Convert.ToDecimal(reader["TotalPendingAmount"])
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in SearchCustomer", ex);
            }

            return list;
        }

        public List<CounterPendingBillModel> GetPendingBills(int customerId, int hospitalId)
        {
            var list = new List<CounterPendingBillModel>();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_Counter_GetPendingBills", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_CustomerId", customerId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);

                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new CounterPendingBillModel
                    {
                        BillId = Convert.ToInt32(reader["BillId"]),
                        BillNumber = reader["BillNumber"].ToString(),
                        BillDate = Convert.ToDateTime(reader["BillDate"]),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                        PaidAmount = Convert.ToDecimal(reader["PaidAmount"]),
                        DueAmount = Convert.ToDecimal(reader["DueAmount"]),
                        PaymentStatus = reader["PaymentStatus"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in GetPendingBills", ex);
            }

            return list;
        }

        public List<CounterMedicineSearchModel> SearchMedicine(string search, int hospitalId, int? subHospitalId)
        {
            var list = new List<CounterMedicineSearchModel>();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_Counter_SearchMedicine", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_Search", search);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? (object)DBNull.Value);

                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new CounterMedicineSearchModel
                    {
                        MedicineId = Convert.ToInt32(reader["MedicineId"]),
                        MedicineName = reader["MedicineName"].ToString(),
                        MRP = Convert.ToDecimal(reader["MRP"]),
                        SellingPrice = Convert.ToDecimal(reader["SellingPrice"]),
                        StockQuantity = Convert.ToInt32(reader["StockQuantity"]),
                        SupplierName = reader["SupplierName"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in SearchMedicine", ex);
            }

            return list;
        }

        public int SaveBill(CounterBillModel bill, List<CounterCartItem> cartItems, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var tran = conn.BeginTransaction();

                try
                {
                    // Insert Bill
                    int billId;
                    using (var cmd = new MySqlCommand("sp_Counter_InsertBill", conn, tran))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("p_BillNumber", bill.BillNumber);
                        cmd.Parameters.AddWithValue("p_CustomerId", bill.CustomerId);
                        cmd.Parameters.AddWithValue("p_CustomerName", bill.CustomerName);
                        cmd.Parameters.AddWithValue("p_MobileNumber", bill.MobileNumber);
                        cmd.Parameters.AddWithValue("p_SubTotal", bill.SubTotal);
                        cmd.Parameters.AddWithValue("p_DiscountType", bill.DiscountType);
                        cmd.Parameters.AddWithValue("p_DiscountValue", bill.DiscountValue);
                        cmd.Parameters.AddWithValue("p_DiscountAmount", bill.DiscountAmount);
                        cmd.Parameters.AddWithValue("p_TotalAmount", bill.TotalAmount);
                        cmd.Parameters.AddWithValue("p_PaymentMode", bill.PaymentMode);
                        cmd.Parameters.AddWithValue("p_PaidAmount", bill.PaidAmount);
                        cmd.Parameters.AddWithValue("p_DueAmount", bill.DueAmount);
                        cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                        cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_CreatedBy", bill.CreatedBy);

                        var pBillId = new MySqlParameter("p_BillId", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
                        cmd.Parameters.Add(pBillId);

                        cmd.ExecuteNonQuery();
                        billId = Convert.ToInt32(pBillId.Value);
                    }

                    // Insert Bill Items and Update Stock
                    foreach (var item in cartItems)
                    {
                        using (var cmd = new MySqlCommand("sp_Counter_InsertBillItem", conn, tran))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("p_BillId", billId);
                            cmd.Parameters.AddWithValue("p_MedicineId", item.MedicineId);
                            cmd.Parameters.AddWithValue("p_MedicineName", item.MedicineName);
                            cmd.Parameters.AddWithValue("p_Quantity", item.Quantity);
                            cmd.Parameters.AddWithValue("p_UnitPrice", item.UnitPrice);
                            cmd.Parameters.AddWithValue("p_TotalPrice", item.TotalPrice);
                            cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                            cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? (object)DBNull.Value);

                            var pSuccess = new MySqlParameter("p_Success", MySqlDbType.Byte) { Direction = ParameterDirection.Output };
                            cmd.Parameters.Add(pSuccess);

                            cmd.ExecuteNonQuery();

                            if (Convert.ToBoolean(pSuccess.Value) == false)
                            {
                                throw new Exception($"Insufficient stock for {item.MedicineName}");
                            }
                        }
                    }

                    tran.Commit();
                    return billId;
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in SaveBill", ex);
            }
        }

        public CounterBillModel GetBillDetails(int billId, int hospitalId)
        {
            CounterBillModel bill = null;

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_Counter_GetBillDetails", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_BillId", billId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);

                conn.Open();
                using var reader = cmd.ExecuteReader();

                // First result set - Bill Header
                if (reader.Read())
                {
                    bill = new CounterBillModel
                    {
                        BillId = Convert.ToInt32(reader["BillId"]),
                        BillNumber = reader["BillNumber"].ToString(),
                        BillDate = Convert.ToDateTime(reader["BillDate"]),
                        CustomerName = reader["CustomerName"].ToString(),
                        MobileNumber = reader["MobileNumber"].ToString(),
                        SubTotal = Convert.ToDecimal(reader["SubTotal"]),
                        DiscountType = reader["DiscountType"].ToString(),
                        DiscountValue = Convert.ToDecimal(reader["DiscountValue"]),
                        DiscountAmount = Convert.ToDecimal(reader["DiscountAmount"]),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                        PaymentMode = reader["PaymentMode"].ToString(),
                        PaymentStatus = reader["PaymentStatus"].ToString(),
                        PaidAmount = Convert.ToDecimal(reader["PaidAmount"]),
                        DueAmount = Convert.ToDecimal(reader["DueAmount"]),
                        HospitalName = reader["HospitalName"].ToString(),
                        HospitalAddress = reader["HospitalAddress"]?.ToString(),
                        HospitalPhone = reader["HospitalPhone"]?.ToString(),
                        HospitalEmail = reader["HospitalEmail"]?.ToString(),
                        HospitalLogo = reader["HospitalLogo"]?.ToString(),
                        CreatedByName = reader["CreatedByName"]?.ToString(),
                        Items = new List<CounterBillItemModel>()
                    };
                }

                // Second result set - Bill Items
                if (reader.NextResult())
                {
                    while (reader.Read())
                    {
                        bill.Items.Add(new CounterBillItemModel
                        {
                            ItemId = Convert.ToInt32(reader["ItemId"]),
                            MedicineName = reader["MedicineName"].ToString(),
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                            TotalPrice = Convert.ToDecimal(reader["TotalPrice"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in GetBillDetails", ex);
            }

            return bill;
        }

        public CounterTodaySummaryModel GetTodaySummary(int hospitalId, int? subHospitalId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_Counter_GetTodaySummary", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? (object)DBNull.Value);

                conn.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new CounterTodaySummaryModel
                    {
                        TotalBills = Convert.ToInt32(reader["TotalBills"]),
                        TotalSales = Convert.ToDecimal(reader["TotalSales"]),
                        TotalCollected = Convert.ToDecimal(reader["TotalCollected"]),
                        TotalPending = Convert.ToDecimal(reader["TotalPending"])
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in GetTodaySummary", ex);
            }

            return new CounterTodaySummaryModel();
        }


        // Add this method to CounterRepository.cs
        public bool CollectPayment(int billId, decimal amount, string paymentMode, int userId, int hospitalId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_Counter_CollectPayment", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_BillId", billId);
                cmd.Parameters.AddWithValue("p_Amount", amount);
                cmd.Parameters.AddWithValue("p_PaymentMode", paymentMode);
                cmd.Parameters.AddWithValue("p_UserId", userId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);

                conn.Open();
                int result = cmd.ExecuteNonQuery();

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error collecting payment: " + ex.Message, ex);
            }
        }


        public List<CounterBillModel> GetCustomerHistory(int customerId, int hospitalId, int? subHospitalId)
        {
            var list = new List<CounterBillModel>();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_Counter_GetCustomerHistory", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_CustomerId", customerId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? (object)DBNull.Value);

                conn.Open();
                using var reader = cmd.ExecuteReader();

                // First result set - Bills
                while (reader.Read())
                {
                    list.Add(new CounterBillModel
                    {
                        BillId = Convert.ToInt32(reader["BillId"]),
                        BillNumber = reader["BillNumber"].ToString(),
                        BillDate = Convert.ToDateTime(reader["BillDate"]),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                        PaidAmount = Convert.ToDecimal(reader["PaidAmount"]),
                        DueAmount = Convert.ToDecimal(reader["DueAmount"]),
                        PaymentStatus = reader["PaymentStatus"].ToString(),
                        PaymentMode = reader["PaymentMode"].ToString(),
                        Items = new List<CounterBillItemModel>()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting customer history", ex);
            }

            return list;
        }

        public List<CounterBillItemModel> GetCustomerPurchaseItems(int customerId, int hospitalId, int? subHospitalId)
        {
            var list = new List<CounterBillItemModel>();

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_Counter_GetCustomerItems", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_CustomerId", customerId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId ?? (object)DBNull.Value);

                conn.Open();

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var item = new CounterBillItemModel
                    {
                        BillId = Convert.ToInt32(reader["BillId"]),
                        MedicineName = reader["MedicineName"].ToString(),
                        Quantity = Convert.ToInt32(reader["Quantity"]),
                        UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                        TotalPrice = Convert.ToDecimal(reader["TotalPrice"]),
                        BillNumber = reader["BillNumber"].ToString(),
                        BillDate = Convert.ToDateTime(reader["BillDate"])
                    };
                    list.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting customer purchase items: " + ex.Message);
            }

            return list;
        }

        public CounterCustomerModel GetCustomerById(int customerId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("SELECT * FROM counter_customers WHERE CustomerId = @CustomerId AND HospitalId = @HospitalId", conn);

                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@HospitalId", hospitalId);

                conn.Open();
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new CounterCustomerModel
                    {
                        CustomerId = Convert.ToInt32(reader["CustomerId"]),
                        CustomerName = reader["CustomerName"].ToString(),
                        MobileNumber = reader["MobileNumber"].ToString(),
                        Address = reader["Address"]?.ToString(),
                        HospitalId = Convert.ToInt32(reader["HospitalId"]),
                        SubHospitalId = reader["SubHospitalId"] != DBNull.Value ? Convert.ToInt32(reader["SubHospitalId"]) : (int?)null,
                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting customer by ID: " + ex.Message);
            }

            return null;
        }
    }
}