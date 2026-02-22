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
    public interface ICouponRepository
    {
        // CRUD Operations
        Task<int> CreateCouponAsync(CouponModel coupon);
        Task<int> UpdateCouponAsync(CouponModel coupon);
        Task<int> DeleteCouponAsync(int couponId, string updatedBy);
        Task<CouponModel> GetCouponByIdAsync(int couponId);
        Task<CouponModel> GetCouponByCodeAsync(string couponCode);
        Task<IEnumerable<CouponModel>> GetAllCouponsAsync(bool includeInactive = false);
        Task<IEnumerable<CouponModel>> GetCouponsByEventIdAsync(int eventId, bool onlyValid = true);

        // Business Logic
        Task<CouponModel> FindBestMatchingCouponAsync(int eventId, decimal ticketBasePrice, int totalSeats, decimal subtotal);
        Task<bool> IsCouponValidForEventAsync(int couponId, int eventId);
        Task<int> ApplyCouponToBookingAsync(int bookingId, int couponId, decimal discountAmount, string couponCode);
        Task<int> RemoveCouponFromBookingAsync(int bookingId);
    }
    public class CouponRepository: ICouponRepository
    {
        private readonly ITHDBConnection _dbConnection;
        private readonly string coupons = DatabaseConfiguration.coupons;
        private readonly string booking = DatabaseConfiguration.booking;

        public CouponRepository(ITHDBConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<int> CreateCouponAsync(CouponModel coupon)
        {
            using var connection = _dbConnection.GetConnection();

            // Generate coupon code with prefix th_
            var query = $@"
                INSERT INTO {coupons} 
                (event_ids, coupon_name, coupon_code, discount_type, discount_amount, 
                 min_order_amount, max_order_amount, min_seats, max_seats, 
                 min_ticket_price, max_ticket_price, valid_from, valid_to, 
                 created_by, updated_by)
                VALUES 
                (@event_ids, @coupon_name, @coupon_code, @discount_type, @discount_amount,
                 @min_order_amount, @max_order_amount, @min_seats, @max_seats,
                 @min_ticket_price, @max_ticket_price, @valid_from, @valid_to,
                 @created_by, @updated_by)
                RETURNING coupon_id";

            return await connection.ExecuteScalarAsync<int>(query, coupon);
        }

        public async Task<int> UpdateCouponAsync(CouponModel coupon)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                UPDATE {coupons} 
                SET event_ids = @event_ids,
                    coupon_name = @coupon_name,
                    discount_type = @discount_type,
                    discount_amount = @discount_amount,
                    min_order_amount = @min_order_amount,
                    max_order_amount = @max_order_amount,
                    min_seats = @min_seats,
                    max_seats = @max_seats,
                    min_ticket_price = @min_ticket_price,
                    max_ticket_price = @max_ticket_price,
                    valid_from = @valid_from,
                    valid_to = @valid_to,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP,
                    active = @active
                WHERE coupon_id = @coupon_id AND active = 1";

            return await connection.ExecuteAsync(query, coupon);
        }

        public async Task<int> DeleteCouponAsync(int couponId, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                UPDATE {coupons} 
                SET active = 0,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE coupon_id = @coupon_id AND active = 1";

            return await connection.ExecuteAsync(query, new { coupon_id = couponId, updated_by = updatedBy });
        }

        public async Task<CouponModel> GetCouponByIdAsync(int couponId)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                SELECT * FROM {coupons} 
                WHERE coupon_id = @CouponId AND active = 1";

            return await connection.QueryFirstOrDefaultAsync<CouponModel>(query, new { CouponId = couponId });
        }

        public async Task<CouponModel> GetCouponByCodeAsync(string couponCode)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                SELECT * FROM {coupons} 
                WHERE coupon_code = @CouponCode AND active = 1";

            return await connection.QueryFirstOrDefaultAsync<CouponModel>(query, new { CouponCode = couponCode });
        }

        public async Task<IEnumerable<CouponModel>> GetAllCouponsAsync(bool includeInactive = false)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                SELECT * FROM {coupons} 
                WHERE 1=1 {(includeInactive ? "" : "AND active = 1")}
                ORDER BY created_on DESC";

            return await connection.QueryAsync<CouponModel>(query);
        }

        public async Task<IEnumerable<CouponModel>> GetCouponsByEventIdAsync(int eventId, bool onlyValid = true)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                SELECT * FROM {coupons} 
                WHERE active = 1
                AND event_ids LIKE '%' || @EventId || '%'
                OR event_ids LIKE @EventId || ',%'
                OR event_ids LIKE '%,' || @EventId
                OR event_ids LIKE '%,' || @EventId || ',%'";

            if (onlyValid)
            {
                query += @" AND valid_from <= CURRENT_TIMESTAMP 
                           AND valid_to >= CURRENT_TIMESTAMP";
            }

            return await connection.QueryAsync<CouponModel>(query, new { EventId = eventId.ToString() });
        }

        public async Task<CouponModel> FindBestMatchingCouponAsync(int eventId, decimal ticketBasePrice, int totalSeats, decimal subtotal)
        {
            using var connection = _dbConnection.GetConnection();

            // FIXED: Correct SQL with proper parentheses and event_id matching
            var query = $@"
            SELECT * FROM {coupons} 
            WHERE active = 1
            AND valid_from <= CURRENT_TIMESTAMP 
            AND valid_to >= CURRENT_TIMESTAMP
            AND (event_ids = 'all' 
                 OR event_ids = @EventId
                 OR event_ids LIKE @EventId || ',%'
                 OR event_ids LIKE '%,' || @EventId
                 OR event_ids LIKE '%,' || @EventId || ',%')
            AND min_order_amount <= @Subtotal
            AND max_order_amount >= @Subtotal
            AND min_seats <= @TotalSeats
            AND max_seats >= @TotalSeats
            AND min_ticket_price <= @TicketBasePrice
            AND max_ticket_price >= @TicketBasePrice
            ORDER BY 
                CASE 
                    WHEN discount_type = 'fixed' THEN discount_amount 
                    ELSE (discount_amount * @Subtotal / 100) 
                END DESC
            LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<CouponModel>(query, new
            {
                EventId = eventId.ToString(), // Convert to string for exact match
                TicketBasePrice = ticketBasePrice,
                TotalSeats = totalSeats,
                Subtotal = subtotal
            });
        }

        public async Task<bool> IsCouponValidForEventAsync(int couponId, int eventId)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                SELECT COUNT(1) FROM {coupons} 
                WHERE coupon_id = @CouponId
                AND active = 1
                AND valid_from <= CURRENT_TIMESTAMP 
                AND valid_to >= CURRENT_TIMESTAMP
                AND (event_ids = 'all' OR event_ids LIKE '%' || @EventId || '%'
                    OR event_ids LIKE @EventId || ',%'
                    OR event_ids LIKE '%,' || @EventId
                    OR event_ids LIKE '%,' || @EventId || ',%')";

            var count = await connection.ExecuteScalarAsync<int>(query, new
            {
                CouponId = couponId,
                EventId = eventId.ToString()
            });

            return count > 0;
        }

        public async Task<int> ApplyCouponToBookingAsync(int bookingId, int couponId, decimal discountAmount, string couponCode)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                UPDATE {booking} 
                SET coupon_id = @coupon_id,
                    discount_amount = @discount_amount,
                    coupon_code_applied = @coupon_code_applied,
                    updated_on = CURRENT_TIMESTAMP
                WHERE booking_id = @booking_id AND active = 1";

            return await connection.ExecuteAsync(query, new
            {
                booking_id = bookingId,
                coupon_id = couponId,
                discount_amount = discountAmount,
                coupon_code_applied = couponCode
            });
        }

        public async Task<int> RemoveCouponFromBookingAsync(int bookingId)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                UPDATE {booking} 
                SET coupon_id = NULL,
                    discount_amount = 0,
                    coupon_code_applied = NULL,
                    updated_on = CURRENT_TIMESTAMP
                WHERE booking_id = @booking_id AND active = 1";

            return await connection.ExecuteAsync(query, new { booking_id = bookingId });
        }
    }
}
