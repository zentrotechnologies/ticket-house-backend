using DAL.Utilities;
using Dapper;
using MODEL.Entities;
using MODEL.Request;
using MODEL.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public interface IBookingRepository
    {
        // Seat Selection
        Task<IEnumerable<EventSeatTypeInventoryModel>> GetAvailableSeatsByEventIdAsync(int eventId);
        Task<EventSeatTypeInventoryModel> GetSeatTypeByIdAsync(int seatTypeId);
        Task<bool> CheckSeatAvailabilityAsync(int seatTypeId, int quantity);
        Task<int> ReserveSeatsAsync(int seatTypeId, int quantity, string updatedBy);
        Task<int> ReleaseSeatsAsync(int seatTypeId, int quantity, string updatedBy);

        // Booking
        Task<int> CreateBookingAsync(BookingModel booking);
        Task<int> CreateBookingSeatsAsync(List<BookingSeatModel> bookingSeats);
        Task<BookingModel> GetBookingByIdAsync(int bookingId);
        Task<BookingModel> GetBookingByCodeAsync(string bookingCode);
        Task<IEnumerable<BookingModel>> GetBookingsByUserIdAsync(Guid userId);
        Task<int> UpdateBookingStatusAsync(int bookingId, string status, string updatedBy);
        Task<int> UpdateBookingStatusAndSeatsAsync(int bookingId, string status,
            List<SeatUpdateRequest> seatUpdates, string updatedBy);

        // Booking with details
        Task<BookingDetailsResponse> GetBookingDetailsAsync(int bookingId);
        Task<BookingDetailsResponse> GetBookingDetailsByCodeAsync(string bookingCode);
    }
    public class BookingRepository: IBookingRepository
    {
        private readonly ITHDBConnection _dbConnection;
        private readonly string booking = DatabaseConfiguration.booking;
        private readonly string booking_seat = DatabaseConfiguration.booking_seat;
        private readonly string event_seat_type_inventory = DatabaseConfiguration.event_seat_type_inventory;
        private readonly string events = DatabaseConfiguration.events;
        private readonly string Users = DatabaseConfiguration.Users;

        public BookingRepository(ITHDBConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<EventSeatTypeInventoryModel>> GetAvailableSeatsByEventIdAsync(int eventId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {event_seat_type_inventory} 
                WHERE event_id = @EventId 
                AND active = 1 
                AND available_seats > 0
                ORDER BY price DESC";

            return await connection.QueryAsync<EventSeatTypeInventoryModel>(query, new { EventId = eventId });
        }

        public async Task<EventSeatTypeInventoryModel> GetSeatTypeByIdAsync(int seatTypeId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {event_seat_type_inventory} 
                WHERE event_seat_type_inventory_id = @SeatTypeId 
                AND active = 1";

            return await connection.QueryFirstOrDefaultAsync<EventSeatTypeInventoryModel>(query,
                new { SeatTypeId = seatTypeId });
        }

        public async Task<bool> CheckSeatAvailabilityAsync(int seatTypeId, int quantity)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT available_seats >= @Quantity 
                FROM {event_seat_type_inventory} 
                WHERE event_seat_type_inventory_id = @SeatTypeId 
                AND active = 1";

            return await connection.ExecuteScalarAsync<bool>(query, new
            {
                SeatTypeId = seatTypeId,
                Quantity = quantity
            });
        }

        public async Task<int> ReserveSeatsAsync(int seatTypeId, int quantity, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {event_seat_type_inventory} 
                SET available_seats = available_seats - @Quantity,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE event_seat_type_inventory_id = @seat_type_id 
                AND active = 1
                AND available_seats >= @Quantity
                RETURNING available_seats";

            return await connection.ExecuteAsync(query, new
            {
                seat_type_id = seatTypeId,
                Quantity = quantity,
                updated_by = updatedBy
            });
        }

        public async Task<int> ReleaseSeatsAsync(int seatTypeId, int quantity, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {event_seat_type_inventory} 
                SET available_seats = available_seats + @Quantity,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE event_seat_type_inventory_id = @seat_type_id 
                AND active = 1
                RETURNING available_seats";

            return await connection.ExecuteAsync(query, new
            {
                seat_type_id = seatTypeId,
                Quantity = quantity,
                updated_by = updatedBy
            });
        }

        public async Task<int> CreateBookingAsync(BookingModel booking)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                INSERT INTO {this.booking} 
                (booking_code, user_id, event_id, total_amount, status, created_by, updated_by)
                VALUES 
                (@booking_code, @user_id, @event_id, @total_amount, @status, @created_by, @updated_by)
                RETURNING booking_id";

            var bookingId = await connection.ExecuteScalarAsync<int>(query, new
            {
                booking_code = booking.booking_code,
                user_id = booking.user_id,
                event_id = booking.event_id,
                total_amount = booking.total_amount,
                status = booking.status,
                created_by = booking.created_by,
                updated_by = booking.updated_by
            });

            return bookingId;
        }

        public async Task<int> CreateBookingSeatsAsync(List<BookingSeatModel> bookingSeats)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                INSERT INTO {booking_seat} 
                (booking_id, event_seat_type_inventory_id, quantity, price_per_seat, subtotal, created_by, updated_by)
                VALUES 
                (@booking_id, @event_seat_type_inventory_id, @quantity, @price_per_seat, @subtotal, @created_by, @updated_by)
                RETURNING booking_seat_id";

            var affectedRows = 0;
            foreach (var seat in bookingSeats)
            {
                var seatId = await connection.ExecuteScalarAsync<int>(query, new
                {
                    booking_id = seat.booking_id,
                    event_seat_type_inventory_id = seat.event_seat_type_inventory_id,
                    quantity = seat.quantity,
                    price_per_seat = seat.price_per_seat,
                    subtotal = seat.subtotal,
                    created_by = seat.created_by,
                    updated_by = seat.updated_by
                });

                if (seatId > 0)
                {
                    affectedRows++;
                }
            }

            return affectedRows;
        }

        public async Task<BookingModel> GetBookingByIdAsync(int bookingId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {booking} 
                WHERE booking_id = @BookingId AND active = 1";

            return await connection.QueryFirstOrDefaultAsync<BookingModel>(query, new { BookingId = bookingId });
        }

        public async Task<BookingModel> GetBookingByCodeAsync(string bookingCode)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {booking} 
                WHERE booking_code = @BookingCode AND active = 1";

            return await connection.QueryFirstOrDefaultAsync<BookingModel>(query, new { BookingCode = bookingCode });
        }

        public async Task<IEnumerable<BookingModel>> GetBookingsByUserIdAsync(Guid userId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {booking} 
                WHERE user_id = @UserId AND active = 1
                ORDER BY created_on DESC";

            return await connection.QueryAsync<BookingModel>(query, new { UserId = userId });
        }

        public async Task<int> UpdateBookingStatusAsync(int bookingId, string status, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {booking} 
                SET status = @status,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE booking_id = @booking_id AND active = 1";

            return await connection.ExecuteAsync(query, new
            {
                booking_id = bookingId,
                status = status,
                updated_by = updatedBy
            });
        }

        public async Task<int> UpdateBookingStatusAndSeatsAsync(int bookingId, string status,
            List<SeatUpdateRequest> seatUpdates, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Update booking status
                var updateBookingQuery = $@"
                    UPDATE {booking} 
                    SET status = @status,
                        updated_by = @updated_by,
                        updated_on = CURRENT_TIMESTAMP
                    WHERE booking_id = @booking_id AND active = 1";

                await connection.ExecuteAsync(updateBookingQuery, new
                {
                    booking_id = bookingId,
                    status = status,
                    updated_by = updatedBy
                }, transaction);

                // Update seat availability
                if (status.ToLower() == "confirmed" && seatUpdates != null)
                {
                    foreach (var seatUpdate in seatUpdates)
                    {
                        var updateSeatQuery = $@"
                            UPDATE {event_seat_type_inventory} 
                            SET available_seats = available_seats - @quantity,
                                updated_by = @updated_by,
                                updated_on = CURRENT_TIMESTAMP
                            WHERE event_seat_type_inventory_id = @seat_type_id 
                            AND active = 1
                            AND available_seats >= @quantity";

                        await connection.ExecuteAsync(updateSeatQuery, new
                        {
                            seat_type_id = seatUpdate.SeatTypeId,
                            quantity = seatUpdate.Quantity,
                            updated_by = updatedBy
                        }, transaction);
                    }
                }

                transaction.Commit();
                return 1;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<BookingDetailsResponse> GetBookingDetailsAsync(int bookingId)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                SELECT 
                    b.*,
                    e.event_name,
                    e.event_date,
                    e.start_time,
                    e.end_time,
                    e.location,
                    e.banner_image,
                    u.first_name,
                    u.last_name,
                    u.email,
                    u.mobile
                FROM {booking} b
                INNER JOIN {events} e ON b.event_id = e.event_id
                INNER JOIN {Users} u ON b.user_id = u.user_id
                WHERE b.booking_id = @BookingId 
                AND b.active = 1
                AND e.active = 1
                AND u.active = 1";

            var bookingDetails = await connection.QueryFirstOrDefaultAsync<BookingDetailsResponse>(query,
                new { BookingId = bookingId });

            if (bookingDetails != null)
            {
                // Get booking seats
                var seatsQuery = $@"
                    SELECT 
                        bs.*,
                        esti.seat_name
                    FROM {booking_seat} bs
                    INNER JOIN {event_seat_type_inventory} esti 
                        ON bs.event_seat_type_inventory_id = esti.event_seat_type_inventory_id
                    WHERE bs.booking_id = @BookingId 
                    AND bs.active = 1
                    AND esti.active = 1";

                var seats = await connection.QueryAsync<BookingSeatResponse>(seatsQuery,
                    new { BookingId = bookingId });

                bookingDetails.BookingSeats = seats.ToList();
            }

            return bookingDetails;
        }

        public async Task<BookingDetailsResponse> GetBookingDetailsByCodeAsync(string bookingCode)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                SELECT 
                    b.*,
                    e.event_name,
                    e.event_date,
                    e.start_time,
                    e.end_time,
                    e.location,
                    e.banner_image,
                    u.first_name,
                    u.last_name,
                    u.email,
                    u.mobile
                FROM {booking} b
                INNER JOIN {events} e ON b.event_id = e.event_id
                INNER JOIN {Users} u ON b.user_id = u.user_id
                WHERE b.booking_code = @BookingCode 
                AND b.active = 1
                AND e.active = 1
                AND u.active = 1";

            var bookingDetails = await connection.QueryFirstOrDefaultAsync<BookingDetailsResponse>(query,
                new { BookingCode = bookingCode });

            if (bookingDetails != null)
            {
                // Get booking seats
                var seatsQuery = $@"
                    SELECT 
                        bs.*,
                        esti.seat_name
                    FROM {booking_seat} bs
                    INNER JOIN {event_seat_type_inventory} esti 
                        ON bs.event_seat_type_inventory_id = esti.event_seat_type_inventory_id
                    WHERE bs.booking_id = @BookingId 
                    AND bs.active = 1
                    AND esti.active = 1";

                var seats = await connection.QueryAsync<BookingSeatResponse>(seatsQuery,
                    new { BookingId = bookingDetails.booking_id });

                bookingDetails.BookingSeats = seats.ToList();
            }

            return bookingDetails;
        }
    }
}
