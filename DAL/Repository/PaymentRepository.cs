using DAL.Utilities;
using Dapper;
using MODEL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public interface IPaymentRepository
    {
        Task<int> UpdateBookingPaymentDetailsAsync(int bookingId, string orderId, string paymentId,
            string signature, string paymentMethod, string paymentStatus, string updatedBy);
        Task<int> CreatePaymentHistoryAsync(PaymentHistoryModel paymentHistory);
        Task<PaymentHistoryModel> GetPaymentHistoryByBookingIdAsync(int bookingId);
        Task<IEnumerable<PaymentHistoryModel>> GetPaymentHistoryByUserIdAsync(Guid userId);
        Task<int> UpdatePaymentStatusAsync(int bookingId, string paymentStatus, string updatedBy);
    }
    public class PaymentRepository: IPaymentRepository
    {
        private readonly ITHDBConnection _dbConnection;
        private readonly string booking = DatabaseConfiguration.booking;
        private readonly string payment_history = DatabaseConfiguration.payment_history; // Use from DatabaseConfiguration

        public PaymentRepository(ITHDBConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<int> UpdateBookingPaymentDetailsAsync(int bookingId, string orderId, string paymentId,
            string signature, string paymentMethod, string paymentStatus, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                UPDATE {booking} 
                SET razorpay_order_id = @razorpay_order_id,
                    razorpay_payment_id = @razorpay_payment_id,
                    razorpay_signature = @razorpay_signature,
                    payment_method = @payment_method,
                    payment_status = @payment_status,
                    payment_date = CASE WHEN @payment_status = 'captured' THEN CURRENT_TIMESTAMP ELSE payment_date END,
                    status = CASE WHEN @payment_status = 'captured' THEN 'confirmed' ELSE status END,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE booking_id = @booking_id AND active = 1";

            return await connection.ExecuteAsync(query, new
            {
                booking_id = bookingId,
                razorpay_order_id = orderId,
                razorpay_payment_id = paymentId,
                razorpay_signature = signature,
                payment_method = paymentMethod,
                payment_status = paymentStatus,
                updated_by = updatedBy
            });
        }

        public async Task<int> CreatePaymentHistoryAsync(PaymentHistoryModel paymentHistory)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                INSERT INTO {payment_history} 
                (booking_id, razorpay_order_id, razorpay_payment_id, razorpay_signature,
                 transaction_id, amount, currency, payment_method, payment_status,
                 payment_date, card_last4, card_network, bank_name, wallet, vpa,
                 razorpay_notes, created_by, updated_by)
                VALUES 
                (@booking_id, @razorpay_order_id, @razorpay_payment_id, @razorpay_signature,
                 @transaction_id, @amount, @currency, @payment_method, @payment_status,
                 @payment_date, @card_last4, @card_network, @bank_name, @wallet, @vpa,
                 @razorpay_notes, @created_by, @updated_by)
                RETURNING payment_id";

            return await connection.ExecuteScalarAsync<int>(query, paymentHistory);
        }

        public async Task<PaymentHistoryModel> GetPaymentHistoryByBookingIdAsync(int bookingId)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                SELECT * FROM {payment_history} 
                WHERE booking_id = @booking_id AND active = 1
                ORDER BY created_on DESC
                LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<PaymentHistoryModel>(query,
                new { booking_id = bookingId });
        }

        public async Task<IEnumerable<PaymentHistoryModel>> GetPaymentHistoryByUserIdAsync(Guid userId)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                SELECT ph.* FROM {payment_history} ph
                INNER JOIN {booking} b ON ph.booking_id = b.booking_id
                WHERE b.user_id = @user_id AND ph.active = 1 AND b.active = 1
                ORDER BY ph.created_on DESC";

            return await connection.QueryAsync<PaymentHistoryModel>(query,
                new { user_id = userId });
        }

        public async Task<int> UpdatePaymentStatusAsync(int bookingId, string paymentStatus, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                UPDATE {booking} 
                SET payment_status = @payment_status,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE booking_id = @booking_id AND active = 1";

            return await connection.ExecuteAsync(query, new
            {
                booking_id = bookingId,
                payment_status = paymentStatus,
                updated_by = updatedBy
            });
        }
    }
}
