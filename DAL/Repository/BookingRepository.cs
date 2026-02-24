using DAL.Utilities;
using Dapper;
using MODEL.Entities;
using MODEL.Request;
using MODEL.Response;
using System;
using System.Collections.Generic;
using System.Data;
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
        Task<IEnumerable<MyBookingsResponse>> GetMyBookingsByUserIdAsync(Guid userId);

        // Ticket Scanning
        Task<BookingDetailsResponse> GetBookingForScanningAsync(string bookingCode);
        Task<TicketScanResponse> ScanTicketAsync(ScanTicketRequest request);
        Task<TicketScanResponse> PartialScanTicketAsync(PartialScanRequest request);

        // **ADD THESE MISSING METHODS:**
        Task<IEnumerable<TicketScanHistoryModel>> GetScanHistoryAsync(int bookingId);
        Task<bool> ValidateTicketForScanAsync(string bookingCode, int seatTypeId, int quantityToScan);
        Task<int> ResetScanCountAsync(int bookingId, string updatedBy);

        // Admin methods
        Task<BookingScanSummaryResponse> GetBookingScanSummaryAsync(int bookingId);
        Task<BookingDetailedResponse> GetBookingDetailsByIdAsync(int bookingId);
        Task<EventSummaryResponse> GetEventSummaryByEventIdAsync(int eventId);
        Task<PagedBookingHistoryResponse> GetPagedBookingHistoryByUserIdAsync(BookingHistoryRequest request);
        Task<int> UpdateBookingQRCodeAsync(int bookingId, string qrCodeBase64, string updatedBy);
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

        //public async Task<int> CreateBookingAsync(BookingModel booking)
        //{
        //    using var connection = _dbConnection.GetConnection();
        //    var query = $@"
        //        INSERT INTO {this.booking} 
        //        (booking_code, user_id, event_id, total_amount, status, created_by, updated_by)
        //        VALUES 
        //        (@booking_code, @user_id, @event_id, @total_amount, @status, @created_by, @updated_by)
        //        RETURNING booking_id";

        //    var bookingId = await connection.ExecuteScalarAsync<int>(query, new
        //    {
        //        booking_code = booking.booking_code,
        //        user_id = booking.user_id,
        //        event_id = booking.event_id,
        //        total_amount = booking.total_amount,
        //        status = booking.status,
        //        created_by = booking.created_by,
        //        updated_by = booking.updated_by
        //    });

        //    return bookingId;
        //}

        public async Task<int> CreateBookingAsync(BookingModel booking)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
            INSERT INTO {this.booking} 
            (booking_code, user_id, event_id, total_amount, booking_amount, 
             convenience_fee, gst_amount, final_amount, status, 
             created_by, updated_by, currency, coupon_id, discount_amount, coupon_code_applied)
            VALUES 
            (@booking_code, @user_id, @event_id, @total_amount, @booking_amount,
             @convenience_fee, @gst_amount, @final_amount, @status, 
             @created_by, @updated_by, @currency, @coupon_id, @discount_amount, @coupon_code_applied)
            RETURNING booking_id";

            var bookingId = await connection.ExecuteScalarAsync<int>(query, new
            {
                booking_code = booking.booking_code,
                user_id = booking.user_id,
                event_id = booking.event_id,
                total_amount = booking.total_amount,
                booking_amount = booking.booking_amount,
                convenience_fee = booking.convenience_fee,
                gst_amount = booking.gst_amount,
                final_amount = booking.final_amount,
                status = booking.status,
                created_by = booking.created_by,
                updated_by = booking.updated_by,
                currency = booking.currency,
                // Coupon fields - pass null if not applied
                coupon_id = booking.coupon_id,
                discount_amount = booking.discount_amount,
                coupon_code_applied = booking.coupon_code_applied
            });

            return bookingId;
        }

        public async Task<int> CreateBookingSeatsAsync(List<BookingSeatModel> bookingSeats)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                INSERT INTO {booking_seat} 
                (booking_id, event_seat_type_inventory_id, quantity, remaining_quantity, price_per_seat, subtotal, created_by, updated_by)
                VALUES 
                (@booking_id, @event_seat_type_inventory_id, @quantity, @remaining_quantity, @price_per_seat, @subtotal, @created_by, @updated_by)
                RETURNING booking_seat_id";

            var affectedRows = 0;
            foreach (var seat in bookingSeats)
            {
                var seatId = await connection.ExecuteScalarAsync<int>(query, new
                {
                    booking_id = seat.booking_id,
                    event_seat_type_inventory_id = seat.event_seat_type_inventory_id,
                    quantity = seat.quantity,
                    remaining_quantity = seat.quantity,  // Add this line to pass quantity value to remaining_quantity
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

        public async Task<int> UpdateBookingStatusAndSeatsAsync(int bookingId, string status, List<SeatUpdateRequest> seatUpdates, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                Console.WriteLine($"UpdateBookingStatusAndSeatsAsync started for BookingId: {bookingId}, Status: {status}");

                // FIRST: Check if booking exists and get current status
                var checkBookingQuery = $@"
            SELECT status, payment_status FROM {booking} 
            WHERE booking_id = @booking_id AND active = 1";

                var bookingInfo = await connection.QueryFirstOrDefaultAsync<(string status, string payment_status)>(checkBookingQuery,
                    new { booking_id = bookingId }, transaction);

                if (bookingInfo == default)
                {
                    Console.WriteLine($"Booking {bookingId} not found");
                    transaction.Rollback();
                    return 0;
                }

                Console.WriteLine($"Current booking status: {bookingInfo.status}, Payment status: {bookingInfo.payment_status}");

                // If already confirmed, don't deduct seats again
                if (bookingInfo.status?.ToLower() == "confirmed")
                {
                    Console.WriteLine($"Booking {bookingId} is already confirmed, skipping seat deduction");
                    transaction.Commit();
                    return 1;
                }

                // Update booking status FIRST
                var updateBookingQuery = $@"
            UPDATE {booking} 
            SET status = @status,
                updated_by = @updated_by,
                updated_on = CURRENT_TIMESTAMP
            WHERE booking_id = @booking_id AND active = 1";

                var bookingUpdateResult = await connection.ExecuteAsync(updateBookingQuery, new
                {
                    booking_id = bookingId,
                    status = status,
                    updated_by = updatedBy
                }, transaction);

                Console.WriteLine($"Booking status update result: {bookingUpdateResult}");

                // Update seat availability ONLY if status is confirmed
                if (status.ToLower() == "confirmed" && seatUpdates != null && seatUpdates.Any())
                {
                    Console.WriteLine($"Processing {seatUpdates.Count} seat updates");

                    foreach (var seatUpdate in seatUpdates)
                    {
                        Console.WriteLine($"Processing seat type {seatUpdate.SeatTypeId} with quantity {seatUpdate.Quantity}");

                        // Check current available seats before deducting
                        var checkSeatQuery = $@"
                    SELECT available_seats 
                    FROM {event_seat_type_inventory} 
                    WHERE event_seat_type_inventory_id = @seat_type_id 
                    AND active = 1";

                        var currentAvailable = await connection.QueryFirstOrDefaultAsync<int?>(checkSeatQuery,
                            new { seat_type_id = seatUpdate.SeatTypeId }, transaction);

                        Console.WriteLine($"Current available seats for type {seatUpdate.SeatTypeId}: {currentAvailable}");

                        if (currentAvailable.HasValue && currentAvailable.Value >= seatUpdate.Quantity)
                        {
                            var updateSeatQuery = $@"
                        UPDATE {event_seat_type_inventory} 
                        SET available_seats = available_seats - @quantity,
                            updated_by = @updated_by,
                            updated_on = CURRENT_TIMESTAMP
                        WHERE event_seat_type_inventory_id = @seat_type_id 
                        AND active = 1
                        AND available_seats >= @quantity";

                            var rowsAffected = await connection.ExecuteAsync(updateSeatQuery, new
                            {
                                seat_type_id = seatUpdate.SeatTypeId,
                                quantity = seatUpdate.Quantity,
                                updated_by = updatedBy
                            }, transaction);

                            Console.WriteLine($"Seat update for type {seatUpdate.SeatTypeId} affected {rowsAffected} rows");

                            if (rowsAffected == 0)
                            {
                                throw new Exception($"Failed to deduct seats for seat type {seatUpdate.SeatTypeId}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Insufficient seats for type {seatUpdate.SeatTypeId}. Available: {currentAvailable}, Required: {seatUpdate.Quantity}");
                            throw new Exception($"Insufficient seats available for seat type {seatUpdate.SeatTypeId}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No seat updates to process or status not confirmed");
                }

                transaction.Commit();
                Console.WriteLine($"Transaction committed successfully for booking {bookingId}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateBookingStatusAndSeatsAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                transaction.Rollback();
                throw;
            }
        }

        //----correct before wrong seats count deduction
        //public async Task<int> UpdateBookingStatusAndSeatsAsync(int bookingId, string status, List<SeatUpdateRequest> seatUpdates, string updatedBy)
        //{
        //    using var connection = _dbConnection.GetConnection();
        //    connection.Open();

        //    using var transaction = connection.BeginTransaction();

        //    try
        //    {
        //        // Update booking status
        //        var updateBookingQuery = $@"
        //            UPDATE {booking} 
        //            SET status = @status,
        //                updated_by = @updated_by,
        //                updated_on = CURRENT_TIMESTAMP
        //            WHERE booking_id = @booking_id AND active = 1";

        //        await connection.ExecuteAsync(updateBookingQuery, new
        //        {
        //            booking_id = bookingId,
        //            status = status,
        //            updated_by = updatedBy
        //        }, transaction);

        //        // Update seat availability
        //        if (status.ToLower() == "confirmed" && seatUpdates != null)
        //        {
        //            foreach (var seatUpdate in seatUpdates)
        //            {
        //                var updateSeatQuery = $@"
        //                    UPDATE {event_seat_type_inventory} 
        //                    SET available_seats = available_seats - @quantity,
        //                        updated_by = @updated_by,
        //                        updated_on = CURRENT_TIMESTAMP
        //                    WHERE event_seat_type_inventory_id = @seat_type_id 
        //                    AND active = 1
        //                    AND available_seats >= @quantity";

        //                await connection.ExecuteAsync(updateSeatQuery, new
        //                {
        //                    seat_type_id = seatUpdate.SeatTypeId,
        //                    quantity = seatUpdate.Quantity,
        //                    updated_by = updatedBy
        //                }, transaction);
        //            }
        //        }

        //        transaction.Commit();
        //        return 1;
        //    }
        //    catch
        //    {
        //        transaction.Rollback();
        //        throw;
        //    }
        //}

        public async Task<BookingDetailsResponse> GetBookingDetailsAsync(int bookingId)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
            SELECT 
                b.booking_id,
                b.booking_code,
                b.user_id,
                b.event_id,
                b.total_amount,
                b.booking_amount,
                b.convenience_fee,
                b.gst_amount,
                b.final_amount,
                b.status,
                b.created_on,
                b.qr_code,
                b.coupon_id,              
                b.discount_amount,         
                b.coupon_code_applied,    
                e.event_name,
                e.event_date,
                e.start_time,
                e.end_time,
                e.location,
                e.geo_map_url,
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

        //public async Task<BookingDetailsResponse> GetBookingDetailsAsync(int bookingId)
        //{
        //    using var connection = _dbConnection.GetConnection();

        //    var query = $@"
        //        SELECT 
        //            b.*,
        //            e.event_name,
        //            e.event_date,
        //            e.start_time,
        //            e.end_time,
        //            e.location,
        //            e.banner_image,
        //            u.first_name,
        //            u.last_name,
        //            u.email,
        //            u.mobile
        //        FROM {booking} b
        //        INNER JOIN {events} e ON b.event_id = e.event_id
        //        INNER JOIN {Users} u ON b.user_id = u.user_id
        //        WHERE b.booking_id = @BookingId 
        //        AND b.active = 1
        //        AND e.active = 1
        //        AND u.active = 1";

        //    var bookingDetails = await connection.QueryFirstOrDefaultAsync<BookingDetailsResponse>(query,
        //        new { BookingId = bookingId });

        //    if (bookingDetails != null)
        //    {
        //        // Get booking seats
        //        var seatsQuery = $@"
        //            SELECT 
        //                bs.*,
        //                esti.seat_name
        //            FROM {booking_seat} bs
        //            INNER JOIN {event_seat_type_inventory} esti 
        //                ON bs.event_seat_type_inventory_id = esti.event_seat_type_inventory_id
        //            WHERE bs.booking_id = @BookingId 
        //            AND bs.active = 1
        //            AND esti.active = 1";

        //        var seats = await connection.QueryAsync<BookingSeatResponse>(seatsQuery,
        //            new { BookingId = bookingId });

        //        bookingDetails.BookingSeats = seats.ToList();
        //    }

        //    return bookingDetails;
        //}

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

        public async Task<IEnumerable<MyBookingsResponse>> GetMyBookingsByUserIdAsync(Guid userId)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
            SELECT 
                b.booking_id,
                b.booking_code,
                b.user_id,
                b.event_id,
                b.total_amount,
                b.status,
                b.created_on,
                e.event_name,
                e.event_date,
                e.start_time,
                e.end_time,
                e.location,
                e.banner_image
            FROM {booking} b
            INNER JOIN {events} e ON b.event_id = e.event_id
            WHERE b.user_id = @UserId 
            AND b.active = 1
            AND e.active = 1
            ORDER BY b.created_on DESC";

            var bookings = await connection.QueryAsync<MyBookingsResponse>(query, new { UserId = userId });

            // Get booking seats for each booking
            foreach (var booking in bookings)
            {
                // FIXED: Include scanned_quantity and remaining_quantity
                var seatsQuery = $@"
                SELECT 
                    bs.*,
                    esti.seat_name,
                    bs.scanned_quantity,  -- Add this
                    bs.remaining_quantity -- Add this
                FROM {booking_seat} bs
                INNER JOIN {event_seat_type_inventory} esti 
                    ON bs.event_seat_type_inventory_id = esti.event_seat_type_inventory_id
                WHERE bs.booking_id = @BookingId 
                AND bs.active = 1
                AND esti.active = 1";

                var seats = await connection.QueryAsync<BookingSeatResponse>(seatsQuery,
                    new { BookingId = booking.booking_id });

                booking.BookingSeats = seats.ToList();
            }

            return bookings;
        }

        public async Task<BookingDetailsResponse> GetBookingForScanningAsync(string bookingCode)
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
                // FIXED: Include all scanning-related fields
                var seatsQuery = $@"
                SELECT 
                    bs.booking_seat_id,
                    bs.booking_id,
                    bs.event_seat_type_inventory_id,
                    bs.quantity,
                    bs.price_per_seat,
                    bs.subtotal,
                    bs.scanned_quantity,  -- Ensure this is included
                    bs.remaining_quantity, -- Ensure this is included
                    bs.last_scan_time,
                    bs.scanned_by,
                    bs.created_on,
                    bs.created_by,
                    bs.updated_on,
                    bs.updated_by,
                    bs.active,
                    esti.seat_name
                FROM {booking_seat} bs
                INNER JOIN {event_seat_type_inventory} esti 
                    ON bs.event_seat_type_inventory_id = esti.event_seat_type_inventory_id
                WHERE bs.booking_id = @BookingId 
                AND bs.active = 1
                AND esti.active = 1";

                var seats = await connection.QueryAsync<BookingSeatResponse>(seatsQuery,
                    new { BookingId = bookingDetails.booking_id });

                // Ensure remaining_quantity is correctly calculated if it's null in DB
                foreach (var seat in seats)
                {
                    if (seat.scanned_quantity > 0 && seat.remaining_quantity <= 0)
                    {
                        seat.remaining_quantity = seat.quantity - seat.scanned_quantity;
                    }
                }

                bookingDetails.BookingSeats = seats.ToList();
            }

            return bookingDetails;
        }

        public async Task<TicketScanResponse> ScanTicketAsync(ScanTicketRequest request)
        {
            using var connection = _dbConnection.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Get booking details
                var bookingQuery = $@"
                SELECT b.* FROM {booking} b
                WHERE b.booking_code = @BookingCode 
                AND b.active = 1
                AND b.status = 'confirmed'";

                //var booking = await connection.QueryFirstOrDefaultAsync<BookingModel>(bookingQuery,
                //    new { BookingCode = request.BookingCode }, transaction);

                // To use a different variable name:
                var bookingRecord = await connection.QueryFirstOrDefaultAsync<BookingModel>(bookingQuery,
                                    new { BookingCode = request.BookingCode }, transaction);

                if (bookingRecord == null)
                {
                    return new TicketScanResponse
                    {
                        IsSuccess = false,
                        Message = "Booking not found or not confirmed",
                        Status = "error"
                    };
                }

                var response = new TicketScanResponse
                {
                    BookingId = bookingRecord.booking_id,
                    BookingCode = bookingRecord.booking_code,
                    ScanTime = DateTime.UtcNow
                };

                if (request.SeatTypeId.HasValue)
                {
                    // Partial scan for specific seat type
                    return await ProcessPartialScanAsync(connection, transaction, bookingRecord.booking_id,
                        request.SeatTypeId.Value, request.QuantityToScan, request, response);
                }
                else
                {
                    // Scan all available seats
                    return await ProcessFullScanAsync(connection, transaction, bookingRecord.booking_id,
                        request, response);
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw;
            }
        }

        private async Task<TicketScanResponse> ProcessFullScanAsync(IDbConnection connection,
            IDbTransaction transaction, int bookingId, ScanTicketRequest request,
            TicketScanResponse response)
        {
            // Get all booking seats
            var seatsQuery = $@"
            SELECT bs.*, esti.seat_name
            FROM {booking_seat} bs
            INNER JOIN {event_seat_type_inventory} esti 
                ON bs.event_seat_type_inventory_id = esti.event_seat_type_inventory_id
            WHERE bs.booking_id = @BookingId 
            AND bs.active = 1";

            var bookingSeats = await connection.QueryAsync<BookingSeatModel>(seatsQuery,
                new { BookingId = bookingId }, transaction);

            var scanResults = new List<SeatScanResult>();
            var allSuccess = true;

            foreach (var seat in bookingSeats)
            {
                if (seat.remaining_quantity <= 0)
                {
                    scanResults.Add(new SeatScanResult
                    {
                        SeatTypeId = seat.event_seat_type_inventory_id,
                        SeatName = await GetSeatNameAsync(connection, seat.event_seat_type_inventory_id),
                        RequestedQuantity = 1,
                        ScannedQuantity = 0,
                        RemainingQuantity = 0,
                        IsFullyScanned = true,
                        Status = "already_fully_scanned"
                    });
                    continue;
                }

                // Calculate quantity to scan
                int quantityToScan = Math.Min(1, seat.remaining_quantity);

                // Update scan count
                var updateQuery = $@"
                UPDATE {booking_seat} 
                SET scanned_quantity = scanned_quantity + @QuantityToScan,
                    remaining_quantity = remaining_quantity - @QuantityToScan,
                    last_scan_time = CURRENT_TIMESTAMP,
                    scanned_by = @ScannedBy,
                    updated_on = CURRENT_TIMESTAMP,
                    updated_by = @ScannedBy
                WHERE booking_seat_id = @BookingSeatId
                AND remaining_quantity >= @QuantityToScan
                RETURNING scanned_quantity, remaining_quantity";

                var result = await connection.QueryFirstOrDefaultAsync<(int scannedQuantity, int remainingQuantity)>(updateQuery, new
                {
                    BookingSeatId = seat.booking_seat_id,
                    QuantityToScan = quantityToScan,
                    ScannedBy = request.ScannedBy
                }, transaction);

                if (result != default)
                {
                    // Use result.scannedQuantity and result.remainingQuantity
                    scanResults.Add(new SeatScanResult
                    {
                        SeatTypeId = seat.event_seat_type_inventory_id,
                        SeatName = await GetSeatNameAsync(connection, seat.event_seat_type_inventory_id),
                        RequestedQuantity = 1,
                        ScannedQuantity = quantityToScan,
                        RemainingQuantity = result.remainingQuantity, // Changed here
                        IsFullyScanned = result.remainingQuantity == 0, // Changed here
                        Status = "success"
                    });
                }

                //if (result != default)
                //{
                //    // Log scan history
                //    await LogScanHistoryAsync(connection, transaction, new TicketScanHistoryModel
                //    {
                //        booking_id = bookingId,
                //        booking_seat_id = seat.booking_seat_id,
                //        scanned_quantity = quantityToScan,
                //        scanned_by = request.ScannedBy,
                //        scan_type = request.ScanType,
                //        scan_status = "success",
                //        device_info = request.DeviceInfo,
                //        remarks = "Full scan - automatic"
                //    });

                //    scanResults.Add(new SeatScanResult
                //    {
                //        SeatTypeId = seat.event_seat_type_inventory_id,
                //        SeatName = await GetSeatNameAsync(connection, seat.event_seat_type_inventory_id),
                //        RequestedQuantity = 1,
                //        ScannedQuantity = quantityToScan,
                //        RemainingQuantity = result.Item2,
                //        IsFullyScanned = result.Item2 == 0,
                //        Status = "success"
                //    });
                //}
                //else
                //{
                //    allSuccess = false;
                //    scanResults.Add(new SeatScanResult
                //    {
                //        SeatTypeId = seat.event_seat_type_inventory_id,
                //        SeatName = await GetSeatNameAsync(connection, seat.event_seat_type_inventory_id),
                //        RequestedQuantity = 1,
                //        ScannedQuantity = 0,
                //        RemainingQuantity = seat.remaining_quantity,
                //        IsFullyScanned = false,
                //        Status = "scan_failed"
                //    });
                //}
            }

            transaction.Commit();

            response.IsSuccess = allSuccess;
            response.Status = allSuccess ? "success" : "partial";
            response.Message = allSuccess ? "All tickets scanned successfully" : "Partial scan completed";
            response.ScanResults = scanResults;
            response.Summary = CalculateScanSummary(scanResults);

            return response;
        }

        //private async Task<TicketScanResponse> ProcessPartialScanAsync(IDbConnection connection,
        //    IDbTransaction transaction, int bookingId, int seatTypeId, int quantityToScan,
        //    ScanTicketRequest request, TicketScanResponse response)
        //{
        //    // Get specific booking seat
        //    var seatQuery = $@"
        //    SELECT bs.*, esti.seat_name
        //    FROM {booking_seat} bs
        //    INNER JOIN {event_seat_type_inventory} esti 
        //        ON bs.event_seat_type_inventory_id = esti.event_seat_type_inventory_id
        //    WHERE bs.booking_id = @BookingId 
        //    AND bs.event_seat_type_inventory_id = @SeatTypeId
        //    AND bs.active = 1";

        //    var bookingSeat = await connection.QueryFirstOrDefaultAsync<BookingSeatModel>(seatQuery,
        //        new { BookingId = bookingId, SeatTypeId = seatTypeId }, transaction);

        //    if (bookingSeat == null)
        //    {
        //        return new TicketScanResponse
        //        {
        //            IsSuccess = false,
        //            Message = "Seat type not found in booking",
        //            Status = "error"
        //        };
        //    }

        //    if (bookingSeat.remaining_quantity < quantityToScan && !request.ForceScan)
        //    {
        //        return new TicketScanResponse
        //        {
        //            IsSuccess = false,
        //            Message = $"Only {bookingSeat.remaining_quantity} tickets remaining for this seat type",
        //            Status = "error"
        //        };
        //    }

        //    // Calculate actual quantity to scan
        //    int actualQuantityToScan = request.ForceScan ?
        //        Math.Min(quantityToScan, bookingSeat.quantity - bookingSeat.scanned_quantity) :
        //        Math.Min(quantityToScan, bookingSeat.remaining_quantity);

        //    // Update scan count
        //    var updateQuery = $@"
        //    UPDATE {booking_seat} 
        //    SET scanned_quantity = scanned_quantity + @QuantityToScan,
        //        remaining_quantity = remaining_quantity - @QuantityToScan,
        //        last_scan_time = CURRENT_TIMESTAMP,
        //        scanned_by = @ScannedBy,
        //        updated_on = CURRENT_TIMESTAMP,
        //        updated_by = @ScannedBy
        //    WHERE booking_seat_id = @BookingSeatId
        //    RETURNING scanned_quantity, remaining_quantity";

        //    var result = await connection.QueryFirstOrDefaultAsync<(int scannedQuantity, int remainingQuantity)>(updateQuery, new
        //    {
        //        BookingSeatId = bookingSeat.booking_seat_id,
        //        QuantityToScan = actualQuantityToScan,
        //        ScannedBy = request.ScannedBy
        //    }, transaction);

        //    // Log scan history
        //    await LogScanHistoryAsync(connection, transaction, new TicketScanHistoryModel
        //    {
        //        booking_id = bookingId,
        //        booking_seat_id = bookingSeat.booking_seat_id,
        //        scanned_quantity = actualQuantityToScan,
        //        scanned_by = request.ScannedBy,
        //        scan_type = request.ScanType,
        //        scan_status = "success",
        //        device_info = request.DeviceInfo,
        //        remarks = $"Partial scan: {actualQuantityToScan} tickets"
        //    });

        //    var scanResults = new List<SeatScanResult>
        //{
        //    new SeatScanResult
        //    {
        //        SeatTypeId = seatTypeId,
        //        //SeatName = bookingSeat.seat_name,
        //        SeatName = await GetSeatNameAsync(connection, seatTypeId),
        //        RequestedQuantity = quantityToScan,
        //        ScannedQuantity = actualQuantityToScan,
        //        RemainingQuantity = result.Item2,
        //        IsFullyScanned = result.Item2 == 0,
        //        Status = "success"
        //    }
        //};

        //    transaction.Commit();

        //    response.IsSuccess = true;
        //    response.Status = "success";
        //    response.Message = $"Successfully scanned {actualQuantityToScan} tickets";
        //    response.ScanResults = scanResults;
        //    response.Summary = CalculateScanSummary(scanResults);

        //    return response;
        //}

        private async Task<TicketScanResponse> ProcessPartialScanAsync(IDbConnection connection, IDbTransaction transaction, int bookingId, int seatTypeId, int quantityToScan, ScanTicketRequest request, TicketScanResponse response)
        {
            // Get specific booking seat WITH scanning fields
            var seatQuery = $@"
            SELECT 
                bs.*, 
                esti.seat_name,
                bs.scanned_quantity,  -- Explicitly include
                bs.remaining_quantity -- Explicitly include
            FROM {booking_seat} bs
            INNER JOIN {event_seat_type_inventory} esti 
                ON bs.event_seat_type_inventory_id = esti.event_seat_type_inventory_id
            WHERE bs.booking_id = @BookingId 
            AND bs.event_seat_type_inventory_id = @SeatTypeId
            AND bs.active = 1";

            var bookingSeat = await connection.QueryFirstOrDefaultAsync<BookingSeatModel>(seatQuery,
                new { BookingId = bookingId, SeatTypeId = seatTypeId }, transaction);

            if (bookingSeat == null)
            {
                return new TicketScanResponse
                {
                    IsSuccess = false,
                    Message = "Seat type not found in booking",
                    Status = "error"
                };
            }

            // Use remaining_quantity from DB
            int remainingInDb = bookingSeat.remaining_quantity;

            if (remainingInDb < quantityToScan && !request.ForceScan)
            {
                return new TicketScanResponse
                {
                    IsSuccess = false,
                    Message = $"Only {remainingInDb} tickets remaining for this seat type",
                    Status = "error"
                };
            }

            // Calculate actual quantity to scan
            int actualQuantityToScan = request.ForceScan ?
                Math.Min(quantityToScan, bookingSeat.quantity - bookingSeat.scanned_quantity) :
                Math.Min(quantityToScan, remainingInDb);

            // Update scan count - make sure to set both scanned and remaining
            var updateQuery = $@"
            UPDATE {booking_seat} 
            SET scanned_quantity = scanned_quantity + @QuantityToScan,
                remaining_quantity = remaining_quantity - @QuantityToScan,
                last_scan_time = CURRENT_TIMESTAMP,
                scanned_by = @ScannedBy,
                updated_on = CURRENT_TIMESTAMP,
                updated_by = @ScannedBy
            WHERE booking_seat_id = @BookingSeatId
            RETURNING scanned_quantity, remaining_quantity";

            var result = await connection.QueryFirstOrDefaultAsync<(int scannedQuantity, int remainingQuantity)>(updateQuery, new
            {
                BookingSeatId = bookingSeat.booking_seat_id,
                QuantityToScan = actualQuantityToScan,
                ScannedBy = request.ScannedBy
            }, transaction);

            var scanResults = new List<SeatScanResult>
            {
                new SeatScanResult
                {
                    SeatTypeId = seatTypeId,
                    SeatName = await GetSeatNameAsync(connection, seatTypeId),
                    RequestedQuantity = quantityToScan,
                    ScannedQuantity = actualQuantityToScan,
                    RemainingQuantity = result.remainingQuantity,  // This should show correctly
                    IsFullyScanned = result.remainingQuantity == 0,
                    Status = "success"
                }
            };

            transaction.Commit();

            response.IsSuccess = true;
            response.Status = "success";
            response.Message = $"Successfully scanned {actualQuantityToScan} tickets";
            response.ScanResults = scanResults;
            response.Summary = CalculateScanSummary(scanResults);

            return response;
        }

        public async Task<TicketScanResponse> PartialScanTicketAsync(PartialScanRequest request)
        {
            using var connection = _dbConnection.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                var scanResults = new List<SeatScanResult>();
                var allSuccess = true;

                foreach (var scanDetail in request.SeatScanDetails)
                {
                    // Get booking seat - FIXED: Remove seat_name from select since BookingSeatModel doesn't have it
                    var seatQuery = $@"
                SELECT bs.*
                FROM {booking_seat} bs
                INNER JOIN {event_seat_type_inventory} esti 
                    ON bs.event_seat_type_inventory_id = esti.event_seat_type_inventory_id
                WHERE bs.booking_id = @BookingId 
                AND bs.event_seat_type_inventory_id = @SeatTypeId
                AND bs.active = 1";

                    var bookingSeat = await connection.QueryFirstOrDefaultAsync<BookingSeatModel>(seatQuery,
                        new { request.BookingId, scanDetail.SeatTypeId }, transaction);

                    if (bookingSeat == null)
                    {
                        scanResults.Add(new SeatScanResult
                        {
                            SeatTypeId = scanDetail.SeatTypeId,
                            SeatName = "Unknown",
                            RequestedQuantity = scanDetail.QuantityToScan,
                            ScannedQuantity = 0,
                            RemainingQuantity = 0,
                            IsFullyScanned = false,
                            Status = "seat_type_not_found"
                        });
                        allSuccess = false;
                        continue;
                    }

                    if (bookingSeat.remaining_quantity < scanDetail.QuantityToScan)
                    {
                        // FIX: Get seat name separately since BookingSeatModel doesn't have it
                        var seatName = await GetSeatNameAsync(connection, scanDetail.SeatTypeId);

                        scanResults.Add(new SeatScanResult
                        {
                            SeatTypeId = scanDetail.SeatTypeId,
                            SeatName = seatName,
                            RequestedQuantity = scanDetail.QuantityToScan,
                            ScannedQuantity = 0,
                            RemainingQuantity = bookingSeat.remaining_quantity,
                            IsFullyScanned = false,
                            Status = "insufficient_tickets"
                        });
                        allSuccess = false;
                        continue;
                    }

                    // Update scan count
                    var updateQuery = $@"
                    UPDATE {booking_seat} 
                    SET scanned_quantity = scanned_quantity + @QuantityToScan,
                        remaining_quantity = remaining_quantity - @QuantityToScan,
                        last_scan_time = CURRENT_TIMESTAMP,
                        scanned_by = @ScannedBy,
                        updated_on = CURRENT_TIMESTAMP,
                        updated_by = @ScannedBy
                    WHERE booking_seat_id = @BookingSeatId
                    RETURNING scanned_quantity, remaining_quantity";

                    // Execute the query and get results separately
                    var result = await connection.QueryFirstOrDefaultAsync<(int scannedQuantity, int remainingQuantity)>(updateQuery, new
                    {
                        BookingSeatId = bookingSeat.booking_seat_id,
                        QuantityToScan = scanDetail.QuantityToScan,
                        ScannedBy = request.ScannedBy
                    }, transaction);

                    // Get seat name for logging
                    var seatNameForLog = await GetSeatNameAsync(connection, scanDetail.SeatTypeId);

                    // Log scan history
                    await LogScanHistoryAsync(connection, transaction, new TicketScanHistoryModel
                    {
                        booking_id = request.BookingId,
                        booking_seat_id = bookingSeat.booking_seat_id,
                        scanned_quantity = scanDetail.QuantityToScan,
                        scanned_by = request.ScannedBy,
                        scan_type = "partial",
                        scan_status = "success",
                        device_info = request.DeviceInfo,
                        remarks = $"Partial scan for {seatNameForLog}"
                    });

                    scanResults.Add(new SeatScanResult
                    {
                        SeatTypeId = scanDetail.SeatTypeId,
                        SeatName = seatNameForLog,
                        RequestedQuantity = scanDetail.QuantityToScan,
                        ScannedQuantity = scanDetail.QuantityToScan,
                        RemainingQuantity = result.remainingQuantity, // Access by name
                        IsFullyScanned = result.remainingQuantity == 0,
                        Status = "success"
                    });
                }

                transaction.Commit();

                // Get booking details - FIX: Removed nullable operator from tuple
                var bookingQuery = $@"
            SELECT b.booking_code, e.event_name, u.first_name, u.last_name
            FROM {booking} b
            INNER JOIN {events} e ON b.event_id = e.event_id
            INNER JOIN {Users} u ON b.user_id = u.user_id
            WHERE b.booking_id = @BookingId";

                var bookingInfo = await connection.QueryFirstOrDefaultAsync<(string, string, string, string)>(
                    bookingQuery, new { request.BookingId });

                return new TicketScanResponse
                {
                    IsSuccess = allSuccess,
                    Status = allSuccess ? "success" : "partial",
                    Message = allSuccess ? "Partial scan completed successfully" : "Partial scan completed with errors",
                    BookingId = request.BookingId,
                    BookingCode = bookingInfo != default ? bookingInfo.Item1 : "",
                    EventName = bookingInfo != default ? bookingInfo.Item2 : "",
                    CustomerName = bookingInfo != default ? $"{bookingInfo.Item3} {bookingInfo.Item4}" : "",
                    ScanTime = DateTime.UtcNow,
                    ScanResults = scanResults,
                    Summary = CalculateScanSummary(scanResults)
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<BookingScanSummaryResponse> GetBookingScanSummaryAsync(int bookingId)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
            SELECT 
                b.booking_id,
                b.booking_code,
                e.event_name,
                e.event_date,
                u.first_name,
                u.last_name
            FROM {booking} b
            INNER JOIN {events} e ON b.event_id = e.event_id
            INNER JOIN {Users} u ON b.user_id = u.user_id
            WHERE b.booking_id = @BookingId";

            var bookingInfo = await connection.QueryFirstOrDefaultAsync<BookingScanSummaryResponse>(query,
                new { BookingId = bookingId });

            if (bookingInfo != null)
            {
                // Get seat scan details
                var seatsQuery = $@"
                SELECT 
                    bs.event_seat_type_inventory_id,
                    esti.seat_name,
                    bs.quantity as TotalQuantity,
                    bs.scanned_quantity,
                    bs.remaining_quantity,
                    bs.last_scan_time,
                    bs.scanned_by
                FROM {booking_seat} bs
                INNER JOIN {event_seat_type_inventory} esti 
                    ON bs.event_seat_type_inventory_id = esti.event_seat_type_inventory_id
                WHERE bs.booking_id = @BookingId
                ORDER BY esti.seat_name";

                var seatScanInfo = await connection.QueryAsync<SeatScanInfo>(seatsQuery,
                    new { BookingId = bookingId });

                bookingInfo.SeatScanInfo = seatScanInfo.ToList();

                // Calculate summary
                var totalTickets = seatScanInfo.Sum(s => s.TotalQuantity);
                var scannedTickets = seatScanInfo.Sum(s => s.ScannedQuantity);
                var remainingTickets = seatScanInfo.Sum(s => s.RemainingQuantity);

                bookingInfo.Summary = new ScanSummary
                {
                    TotalTickets = totalTickets,
                    ScannedTickets = scannedTickets,
                    RemainingTickets = remainingTickets,
                    IsFullyScanned = remainingTickets == 0,
                    PercentageScanned = totalTickets > 0 ? (scannedTickets * 100m / totalTickets) : 0
                };

                // Get scan times
                var timesQuery = $@"
                SELECT MIN(last_scan_time) as FirstScanTime, 
                       MAX(last_scan_time) as LastScanTime
                FROM {booking_seat}
                WHERE booking_id = @BookingId
                AND last_scan_time IS NOT NULL";

                var times = await connection.QueryFirstOrDefaultAsync<(DateTime?, DateTime?)>(timesQuery,
                    new { BookingId = bookingId });

                bookingInfo.FirstScanTime = times.Item1;
                bookingInfo.LastScanTime = times.Item2;
            }

            return bookingInfo;
        }

        private async Task<string> GetSeatNameAsync(IDbConnection connection, int seatTypeId)
        {
            var query = $"SELECT seat_name FROM {event_seat_type_inventory} WHERE event_seat_type_inventory_id = @SeatTypeId";
            return await connection.QueryFirstOrDefaultAsync<string>(query, new { SeatTypeId = seatTypeId });
        }

        private async Task LogScanHistoryAsync(IDbConnection connection, IDbTransaction transaction, TicketScanHistoryModel scanHistory)
        {
            var query = $@"
            INSERT INTO ticket_scan_history 
            (booking_id, booking_seat_id, scanned_quantity, scanned_by, 
             scan_type, scan_status, remarks, device_info)
            VALUES 
            (@booking_id, @booking_seat_id, @scanned_quantity, @scanned_by,
             @scan_type, @scan_status, @remarks, @device_info)";

            await connection.ExecuteAsync(query, scanHistory, transaction);
        }

        private ScanSummary CalculateScanSummary(List<SeatScanResult> scanResults)
        {
            var totalRequested = scanResults.Sum(r => r.RequestedQuantity);
            var totalScanned = scanResults.Sum(r => r.ScannedQuantity);
            var totalRemaining = scanResults.Sum(r => r.RemainingQuantity);

            return new ScanSummary
            {
                TotalTickets = totalRequested,
                ScannedTickets = totalScanned,
                RemainingTickets = totalRemaining,
                IsFullyScanned = totalRemaining == 0,
                PercentageScanned = totalRequested > 0 ? (totalScanned * 100m / totalRequested) : 0
            };
        }

        public async Task<IEnumerable<TicketScanHistoryModel>> GetScanHistoryAsync(int bookingId)
        {
            using var connection = _dbConnection.GetConnection();

            var query = @"
            SELECT 
                tsh.*,
                bs.event_seat_type_inventory_id,
                esti.seat_name,
                u.first_name as scanned_by_name,
                u.last_name as scanned_by_last_name
            FROM ticket_scan_history tsh
            LEFT JOIN booking_seat bs ON tsh.booking_seat_id = bs.booking_seat_id
            LEFT JOIN event_seat_type_inventory esti ON bs.event_seat_type_inventory_id = esti.event_seat_type_inventory_id
            LEFT JOIN users u ON tsh.scanned_by = u.email
            WHERE tsh.booking_id = @BookingId
            ORDER BY tsh.scan_time DESC";

            return await connection.QueryAsync<TicketScanHistoryModel>(query, new { BookingId = bookingId });
        }

        public async Task<bool> ValidateTicketForScanAsync(string bookingCode, int seatTypeId, int quantityToScan)
        {
            using var connection = _dbConnection.GetConnection();

            var query = @"
            SELECT 
                b.booking_id,
                b.status,
                bs.remaining_quantity
            FROM booking b
            INNER JOIN booking_seat bs ON b.booking_id = bs.booking_id
            WHERE b.booking_code = @BookingCode 
            AND bs.event_seat_type_inventory_id = @SeatTypeId
            AND b.active = 1
            AND bs.active = 1
            AND b.status = 'confirmed'";

            var result = await connection.QueryFirstOrDefaultAsync<(int, string, int)>(query, new
            {
                BookingCode = bookingCode,
                SeatTypeId = seatTypeId
            });

            if (result == default)
                return false;

            return result.Item3 >= quantityToScan; // Check if remaining quantity is sufficient
        }

        public async Task<int> ResetScanCountAsync(int bookingId, string adminEmail)
        {
            using var connection = _dbConnection.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Get user by email to get user_id
                var userQuery = "SELECT user_id FROM users WHERE email = @Email AND active = 1";
                var userId = await connection.QueryFirstOrDefaultAsync<Guid?>(userQuery,
                    new { Email = adminEmail }, transaction);

                if (!userId.HasValue)
                {
                    throw new Exception("User not found");
                }

                string userIdString = userId.Value.ToString();

                // Reset scan counts for all seats in booking
                var resetQuery = $@"
                UPDATE {booking_seat} 
                SET scanned_quantity = 0,
                    remaining_quantity = quantity,
                    last_scan_time = NULL,
                    scanned_by = NULL,
                    updated_on = CURRENT_TIMESTAMP,
                    updated_by = @updated_by
                WHERE booking_id = @booking_id
                AND active = 1";

                var affectedRows = await connection.ExecuteAsync(resetQuery, new
                {
                    booking_id = bookingId,
                    updated_by = userIdString // Use user_id string
                }, transaction);

                // Clear scan history
                var clearHistoryQuery = "DELETE FROM ticket_scan_history WHERE booking_id = @booking_id";
                await connection.ExecuteAsync(clearHistoryQuery, new { booking_id = bookingId }, transaction);

                transaction.Commit();
                return affectedRows;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<BookingDetailedResponse> GetBookingDetailsByIdAsync(int bookingId)
        {
            using var connection = _dbConnection.GetConnection();

            // 1. Fetch main booking details with event and user information
            var mainQuery = $@"
            SELECT 
                -- Booking fields
                b.booking_id,
                b.booking_code,
                b.status,
                b.created_on,
                b.updated_on,
                b.total_amount,
                b.booking_amount,
                b.convenience_fee,
                b.gst_amount,
                b.final_amount,
                b.currency,
                b.payment_status,
                b.payment_method,
                b.payment_date,
                b.razorpay_order_id,
                b.razorpay_payment_id,
            
                -- Event fields (as event_info)
                e.event_id,
                e.event_name,
                e.event_date,
                e.start_time,
                e.end_time,
                e.location,
                e.geo_map_url,
                e.full_address,
                e.banner_image,
                e.status as event_status,
            
                -- User fields (as user_info)
                u.user_id,
                u.first_name,
                u.last_name,
                u.email,
                u.country_code,
                u.mobile
            
            FROM {booking} b
            INNER JOIN {events} e ON b.event_id = e.event_id AND e.active = 1
            INNER JOIN {Users} u ON b.user_id = u.user_id AND u.active = 1
            WHERE b.booking_id = @BookingId AND b.active = 1";

            var bookingDictionary = new Dictionary<int, BookingDetailedResponse>();

            var bookingResult = await connection.QueryAsync<BookingDetailedResponse, EventInfo, UserInfo, BookingDetailedResponse>(
                mainQuery,
                (booking, eventInfo, userInfo) =>
                {
                    if (!bookingDictionary.TryGetValue(booking.booking_id, out var bookingEntry))
                    {
                        bookingEntry = booking;
                        bookingEntry.event_info = eventInfo;
                        bookingEntry.user_info = userInfo;
                        bookingEntry.booking_seats = new List<BookingSeatDetailedInfo>();
                        bookingEntry.scan_history = new List<TicketScanHistoryInfo>();
                        bookingDictionary.Add(booking.booking_id, bookingEntry);
                    }
                    return bookingEntry;
                },
                new { BookingId = bookingId },
                splitOn: "event_id,user_id"
            );

            var bookingDetails = bookingDictionary.Values.FirstOrDefault();

            if (bookingDetails == null)
                return null;

            // 2. Fetch booking seats with seat names and scan information
            var seatsQuery = $@"
            SELECT 
                bs.booking_seat_id,
                bs.event_seat_type_inventory_id,
                bs.quantity,
                bs.scanned_quantity,
                bs.remaining_quantity,
                bs.price_per_seat,
                bs.subtotal,
                bs.last_scan_time,
                bs.scanned_by,
                esti.seat_name,
            
                -- Calculate derived fields
                CASE 
                    WHEN bs.quantity > 0 
                    THEN ROUND((CAST(bs.scanned_quantity AS DECIMAL) / bs.quantity) * 100, 2)
                    ELSE 0 
                END as scan_percentage,
            
                CASE 
                    WHEN bs.remaining_quantity = 0 OR bs.scanned_quantity >= bs.quantity 
                    THEN true 
                    ELSE false 
                END as is_fully_scanned
            
            FROM {booking_seat} bs
            INNER JOIN {event_seat_type_inventory} esti 
                ON bs.event_seat_type_inventory_id = esti.event_seat_type_inventory_id 
                AND esti.active = 1
            WHERE bs.booking_id = @BookingId 
                AND bs.active = 1
            ORDER BY esti.seat_name";

            var bookingSeats = await connection.QueryAsync<BookingSeatDetailedInfo>(seatsQuery, new { BookingId = bookingId });
            bookingDetails.booking_seats = bookingSeats.ToList();

            // 3. Calculate overall scan summary
            var totalTickets = bookingSeats.Sum(s => s.quantity);
            var totalScanned = bookingSeats.Sum(s => s.scanned_quantity);
            var totalRemaining = bookingSeats.Sum(s => s.remaining_quantity);

            // Get first and last scan times
            var scanTimesQuery = $@"
            SELECT 
                MIN(last_scan_time) as FirstScanTime,
                MAX(last_scan_time) as LastScanTime
            FROM {booking_seat}
            WHERE booking_id = @BookingId 
                AND last_scan_time IS NOT NULL";

            var scanTimes = await connection.QueryFirstOrDefaultAsync<(DateTime? FirstScan, DateTime? LastScan)>(
                scanTimesQuery, new { BookingId = bookingId });

            // Get total scan events count
            var scanEventsQuery = @"
            SELECT COUNT(*) 
            FROM ticket_scan_history 
            WHERE booking_id = @BookingId";

            var totalScanEvents = await connection.ExecuteScalarAsync<int>(scanEventsQuery, new { BookingId = bookingId });

            bookingDetails.scan_summary = new ScanSummaryInfo
            {
                total_tickets = totalTickets,
                total_scanned = totalScanned,
                total_remaining = totalRemaining,
                overall_scan_percentage = totalTickets > 0
                    ? Math.Round((decimal)totalScanned / totalTickets * 100, 2)
                    : 0,
                is_fully_scanned = totalRemaining == 0,
                first_scan_time = scanTimes.FirstScan,
                last_scan_time = scanTimes.LastScan,
                total_scan_events = totalScanEvents
            };

            // 4. Fetch ticket scan history with user details
            var historyQuery = @"
            SELECT 
                tsh.scan_id,
                tsh.booking_seat_id,
                tsh.scanned_quantity,
                tsh.scan_time,
                tsh.scan_type,
                tsh.scan_status,
                tsh.remarks,
                tsh.device_info,
                esti.seat_name,
                CONCAT(u.first_name, ' ', u.last_name) as scanned_by_name
            FROM ticket_scan_history tsh
            INNER JOIN booking_seat bs ON tsh.booking_seat_id = bs.booking_seat_id
            INNER JOIN event_seat_type_inventory esti ON bs.event_seat_type_inventory_id = esti.event_seat_type_inventory_id
            LEFT JOIN users u ON tsh.scanned_by = u.user_id::text OR tsh.scanned_by = u.email
            WHERE tsh.booking_id = @BookingId
            ORDER BY tsh.scan_time DESC";

            var scanHistory = await connection.QueryAsync<TicketScanHistoryInfo>(historyQuery, new { BookingId = bookingId });
            bookingDetails.scan_history = scanHistory.ToList();

            return bookingDetails;
        }

        public async Task<EventSummaryResponse> GetEventSummaryByEventIdAsync(int eventId)
        {
            using var connection = _dbConnection.GetConnection();

            var multiQuery = $@"
            -- 1. Event Info
            SELECT 
                e.event_name,
                e.event_date,
                e.start_time,
                e.end_time,
                e.location
            FROM {events} e
            WHERE e.event_id = @EventId AND e.active = 1;

            -- 2. Seat Type Inventory
            SELECT 
                esti.event_seat_type_inventory_id AS SeatTypeId,
                esti.seat_name AS SeatName,
                esti.price AS Price,
                esti.total_seats AS TotalSeats,
                esti.available_seats AS AvailableSeats,
                (esti.total_seats - esti.available_seats) AS BookedSeats,
                ((esti.total_seats - esti.available_seats) * esti.price) AS Revenue
            FROM {event_seat_type_inventory} esti
            WHERE esti.event_id = @EventId AND esti.active = 1
            ORDER BY esti.seat_name;

            -- 3. Payment Summary
            SELECT 
                COALESCE(SUM(b.final_amount), 0) AS TotalProfit,
                COALESCE(SUM(b.convenience_fee), 0) AS TotalConvenienceFee,
                COALESCE(SUM(b.gst_amount), 0) AS TotalGST,
                COUNT(b.booking_id) AS TotalSuccessfulBookings,
                MIN(b.created_on) AS FirstBookingDate,
                MAX(b.created_on) AS LastBookingDate
            FROM {booking} b
            WHERE b.event_id = @EventId 
                AND b.status = 'confirmed'
                AND b.payment_status = 'captured'
                AND b.active = 1;

            -- 4. Actual Booked Seats by Type
            SELECT 
                bs.event_seat_type_inventory_id AS SeatTypeId,
                SUM(bs.quantity) AS TotalBooked
            FROM {booking_seat} bs
            INNER JOIN {booking} b ON bs.booking_id = b.booking_id
            WHERE b.event_id = @EventId 
                AND b.status = 'confirmed'
                AND b.payment_status = 'captured'
                AND b.active = 1
                AND bs.active = 1
            GROUP BY bs.event_seat_type_inventory_id;";

            using var multi = await connection.QueryMultipleAsync(multiQuery, new { EventId = eventId });

            // Read results
            var eventInfo = await multi.ReadFirstOrDefaultAsync<(string EventName, DateTime EventDate, TimeSpan StartTime, TimeSpan EndTime, string Location)>();
            var seatTypes = await multi.ReadAsync<SeatTypeSummary>();
            var paymentSummary = await multi.ReadFirstOrDefaultAsync<(decimal TotalProfit, decimal TotalConvenienceFee, decimal TotalGST, int TotalBookings, DateTime? FirstDate, DateTime? LastDate)>();
            var bookedByType = await multi.ReadAsync<(int SeatTypeId, int TotalBooked)>();

            if (eventInfo == default)
            {
                return null;
            }

            var seatTypeList = seatTypes.ToList();
            var bookedDict = bookedByType.ToDictionary(x => x.SeatTypeId, x => x.TotalBooked);

            // Update seat types with actual booked counts
            foreach (var seatType in seatTypeList)
            {
                if (bookedDict.TryGetValue(seatType.SeatTypeId, out int actualBooked))
                {
                    seatType.BookedSeats = actualBooked;
                    seatType.Revenue = actualBooked * seatType.Price;
                }

                seatType.OccupancyPercentage = seatType.TotalSeats > 0
                    ? Math.Round((decimal)seatType.BookedSeats / seatType.TotalSeats * 100, 2)
                    : 0;
            }

            var response = new EventSummaryResponse
            {
                EventId = eventId,
                EventName = eventInfo.EventName,
                EventDate = eventInfo.EventDate,
                StartTime = eventInfo.StartTime,
                EndTime = eventInfo.EndTime,
                Location = eventInfo.Location,
                SeatTypeDetails = seatTypeList,
                TotalSeats = seatTypeList.Sum(s => s.TotalSeats),
                AvailableSeats = seatTypeList.Sum(s => s.AvailableSeats),
                BookedSeats = seatTypeList.Sum(s => s.BookedSeats),
                TotalProfit = paymentSummary.TotalProfit,
                PaymentDetails = new PaymentSummary
                {
                    TotalSuccessfulBookings = paymentSummary.TotalBookings,
                    TotalAmount = paymentSummary.TotalProfit,
                    TotalConvenienceFee = paymentSummary.TotalConvenienceFee,
                    TotalGST = paymentSummary.TotalGST,
                    FirstBookingDate = paymentSummary.FirstDate,
                    LastBookingDate = paymentSummary.LastDate
                }
            };

            response.OccupancyPercentage = response.TotalSeats > 0
                ? Math.Round((decimal)response.BookedSeats / response.TotalSeats * 100, 2)
                : 0;

            return response;
        }


        public async Task<PagedBookingHistoryResponse> GetPagedBookingHistoryByUserIdAsync(BookingHistoryRequest request)
        {
            using var connection = _dbConnection.GetConnection();

            // Calculate offset
            int offset = (request.PageNumber - 1) * request.PageSize;

            // Count query for total records
            var countQuery = $@"
            SELECT COUNT(DISTINCT b.booking_id)
            FROM {booking} b
            INNER JOIN {events} e ON b.event_id = e.event_id
            WHERE b.user_id = @user_id 
              AND b.active = 1 
              AND e.active = 1";

            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, new { user_id = request.UserId });

            // Main query with pagination
            var query = $@"
            SELECT 
                b.booking_id,
                b.booking_code,
                b.qr_code,
                e.event_name,
                e.event_date,
                e.start_time,
                e.end_time,
                e.location,
                e.full_address,
                b.final_amount,
                b.total_amount,
                b.convenience_fee,
                b.gst_amount,
                b.currency,
                b.payment_method,
                b.payment_status,
                b.payment_date,
                b.razorpay_payment_id,
                b.razorpay_order_id,
                b.status as booking_status,
                b.created_on,
                bs.booking_seat_id,
                bs.event_seat_type_inventory_id,
                bs.quantity,
                bs.scanned_quantity,
                bs.remaining_quantity,
                bs.price_per_seat,
                bs.subtotal,
                bs.last_scan_time,
                bs.scanned_by,
                esti.seat_name
            FROM {booking} b
            INNER JOIN {events} e ON b.event_id = e.event_id
            INNER JOIN {booking_seat} bs ON b.booking_id = bs.booking_id
            INNER JOIN {event_seat_type_inventory} esti ON bs.event_seat_type_inventory_id = esti.event_seat_type_inventory_id
            WHERE b.user_id = @user_id 
              AND b.active = 1 
              AND e.active = 1
              AND bs.active = 1
            ORDER BY b.created_on DESC, b.booking_id, bs.booking_seat_id
            LIMIT @pageSize OFFSET @offset";

            var bookingDict = new Dictionary<int, BookingHistoryResponse>();

            var result = await connection.QueryAsync<BookingHistoryResponse, BookingSeatHistoryResponse, BookingHistoryResponse>(
                query,
                (booking, seat) =>
                {
                    if (!bookingDict.TryGetValue(booking.booking_id, out var bookingEntry))
                    {
                        bookingEntry = booking;
                        bookingEntry.seats = new List<BookingSeatHistoryResponse>();
                        bookingDict.Add(booking.booking_id, bookingEntry);
                    }

                    if (seat != null)
                    {
                        bookingEntry.seats.Add(seat);
                    }

                    return bookingEntry;
                },
                new
                {
                    user_id = request.UserId,
                    pageSize = request.PageSize,
                    offset = offset
                },
                splitOn: "booking_seat_id"
            );

            var bookings = bookingDict.Values.ToList();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            return new PagedBookingHistoryResponse
            {
                Status = "Success",
                Message = bookings.Any() ? "Booking history retrieved successfully" : "No booking history found",
                ErrorCode = "0",
                Data = bookings,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<int> UpdateBookingQRCodeAsync(int bookingId, string qrCodeBase64, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
            UPDATE {booking} 
            SET qr_code = @qr_code,
                updated_by = @updated_by,
                updated_on = CURRENT_TIMESTAMP
            WHERE booking_id = @booking_id AND active = 1";

            return await connection.ExecuteAsync(query, new
            {
                booking_id = bookingId,
                qr_code = qrCodeBase64,
                updated_by = updatedBy
            });
        }
    }
}
