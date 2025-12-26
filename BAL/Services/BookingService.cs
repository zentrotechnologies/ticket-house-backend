using DAL.Repository;
using MODEL.Entities;
using MODEL.Request;
using MODEL.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BAL.Services
{
    public interface IBookingService
    {
        // Seat Selection
        Task<CommonResponseModel<List<EventSeatTypeInventoryModel>>> GetAvailableSeatsByEventIdAsync(int eventId);
        Task<CommonResponseModel<SeatSelectionResponse>> SelectSeatsAsync(SeatSelectionRequest request, string userEmail);

        // Booking
        Task<CommonResponseModel<BookingResponse>> CreateBookingAsync(CreateBookingRequest request, string userEmail);
        Task<CommonResponseModel<BookingResponse>> ConfirmBookingAsync(int bookingId, string userEmail);
        Task<CommonResponseModel<BookingResponse>> CancelBookingAsync(int bookingId, string userEmail);

        // Get Bookings
        Task<CommonResponseModel<BookingDetailsResponse>> GetBookingDetailsAsync(int bookingId);
        Task<CommonResponseModel<BookingDetailsResponse>> GetBookingDetailsByCodeAsync(string bookingCode);
        Task<CommonResponseModel<List<BookingResponse>>> GetUserBookingsAsync(Guid userId);

        // Check seat availability
        Task<CommonResponseModel<bool>> CheckSeatAvailabilityAsync(SeatAvailabilityRequest request);
        Task<CommonResponseModel<List<MyBookingsResponse>>> GetMyBookingsByUserIdAsync(Guid userId);
    }
    public class BookingService: IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventDetailsRepository _eventDetailsRepository;
        private readonly IUserRepository _authRepository;

        public BookingService(
            IBookingRepository bookingRepository,
            IEventDetailsRepository eventDetailsRepository,
            IUserRepository authRepository)
        {
            _bookingRepository = bookingRepository;
            _eventDetailsRepository = eventDetailsRepository;
            _authRepository = authRepository;
        }

        public async Task<CommonResponseModel<List<EventSeatTypeInventoryModel>>> GetAvailableSeatsByEventIdAsync(int eventId)
        {
            var response = new CommonResponseModel<List<EventSeatTypeInventoryModel>>();

            try
            {
                if (eventId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid event ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                var seats = await _bookingRepository.GetAvailableSeatsByEventIdAsync(eventId);

                response.Status = "Success";
                response.Message = "Available seats fetched successfully";
                response.ErrorCode = "0";
                response.Data = seats.ToList();
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error fetching available seats: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<SeatSelectionResponse>> SelectSeatsAsync(SeatSelectionRequest request, string userEmail)
        {
            var response = new CommonResponseModel<SeatSelectionResponse>();

            try
            {
                if (request == null || request.SeatSelections == null || !request.SeatSelections.Any())
                {
                    response.Status = "Failure";
                    response.Message = "Seat selection data is required";
                    response.ErrorCode = "400";
                    return response;
                }

                // Check all seat availability
                foreach (var seatSelection in request.SeatSelections)
                {
                    var isAvailable = await _bookingRepository.CheckSeatAvailabilityAsync(
                        seatSelection.SeatTypeId, seatSelection.Quantity);

                    if (!isAvailable)
                    {
                        var seatType = await _bookingRepository.GetSeatTypeByIdAsync(seatSelection.SeatTypeId);
                        response.Status = "Failure";
                        response.Message = $"Not enough seats available for {seatType?.seat_name}";
                        response.ErrorCode = "400";
                        return response;
                    }
                }

                // Get user by email for user_id if user is logged in
                string updatedBy = "guest";
                if (userEmail != "guest")
                {
                    var user = await _authRepository.GetUserByEmail(userEmail);
                    if (user != null)
                    {
                        updatedBy = user.user_id.ToString();
                    }
                }

                // Calculate total amount
                decimal totalAmount = 0;
                var seatDetails = new List<SeatDetail>();

                foreach (var seatSelection in request.SeatSelections)
                {
                    var seatType = await _bookingRepository.GetSeatTypeByIdAsync(seatSelection.SeatTypeId);
                    if (seatType == null)
                    {
                        response.Status = "Failure";
                        response.Message = $"Invalid seat type ID: {seatSelection.SeatTypeId}";
                        response.ErrorCode = "400";
                        return response;
                    }

                    var subtotal = seatType.price * seatSelection.Quantity;
                    totalAmount += subtotal;

                    seatDetails.Add(new SeatDetail
                    {
                        SeatTypeId = seatType.event_seat_type_inventory_id,
                        SeatName = seatType.seat_name,
                        Price = seatType.price,
                        Quantity = seatSelection.Quantity,
                        Subtotal = subtotal,
                        AvailableSeats = seatType.available_seats
                    });
                }

                var seatSelectionResponse = new SeatSelectionResponse
                {
                    EventId = request.EventId,
                    SeatDetails = seatDetails,
                    TotalAmount = totalAmount
                };

                response.Status = "Success";
                response.Message = "Seats selected successfully";
                response.ErrorCode = "0";
                response.Data = seatSelectionResponse;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error selecting seats: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<BookingResponse>> CreateBookingAsync(CreateBookingRequest request, string userEmail)
        {
            var response = new CommonResponseModel<BookingResponse>();

            try
            {
                if (request == null)
                {
                    response.Status = "Failure";
                    response.Message = "Booking data is required";
                    response.ErrorCode = "400";
                    return response;
                }

                // Get user by email
                var user = await _authRepository.GetUserByEmail(userEmail);
                if (user == null)
                {
                    response.Status = "Failure";
                    response.Message = "User not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Check event exists
                var eventDetails = await _eventDetailsRepository.GetEventByIdAsync(request.EventId);
                if (eventDetails == null)
                {
                    response.Status = "Failure";
                    response.Message = "Event not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Check seat availability again
                foreach (var seatSelection in request.SeatSelections)
                {
                    var isAvailable = await _bookingRepository.CheckSeatAvailabilityAsync(
                        seatSelection.SeatTypeId, seatSelection.Quantity);

                    if (!isAvailable)
                    {
                        var seatType = await _bookingRepository.GetSeatTypeByIdAsync(seatSelection.SeatTypeId);
                        response.Status = "Failure";
                        response.Message = $"Not enough seats available for {seatType?.seat_name}";
                        response.ErrorCode = "400";
                        return response;
                    }
                }

                // Temporarily reserve seats
                //foreach (var seatSelection in request.SeatSelections)
                //{
                //    await _bookingRepository.ReserveSeatsAsync(
                //        seatSelection.SeatTypeId, seatSelection.Quantity, userEmail);
                //}

                // Temporarily reserve seats - pass user_id as string
                string userIdString = user.user_id.ToString();
                foreach (var seatSelection in request.SeatSelections)
                {
                    await _bookingRepository.ReserveSeatsAsync(
                        seatSelection.SeatTypeId, seatSelection.Quantity, userIdString);
                }

                // Calculate total amount
                decimal totalAmount = 0;
                var bookingSeats = new List<BookingSeatModel>();

                foreach (var seatSelection in request.SeatSelections)
                {
                    var seatType = await _bookingRepository.GetSeatTypeByIdAsync(seatSelection.SeatTypeId);
                    var subtotal = seatType.price * seatSelection.Quantity;
                    totalAmount += subtotal;
                }

                // Generate booking code
                var bookingCode = $"ZTH{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";

                // Create booking
                var booking = new BookingModel
                {
                    booking_code = bookingCode,
                    user_id = user.user_id,
                    event_id = request.EventId,
                    total_amount = totalAmount,
                    status = "pending", // Will be confirmed after payment
                    //created_by = userEmail,
                    //updated_by = userEmail
                    created_by = userIdString,
                    updated_by = userIdString
                };

                var bookingId = await _bookingRepository.CreateBookingAsync(booking);

                if (bookingId > 0)
                {
                    // Create booking seats
                    foreach (var seatSelection in request.SeatSelections)
                    {
                        var seatType = await _bookingRepository.GetSeatTypeByIdAsync(seatSelection.SeatTypeId);
                        var subtotal = seatType.price * seatSelection.Quantity;

                        var bookingSeat = new BookingSeatModel
                        {
                            booking_id = bookingId,
                            event_seat_type_inventory_id = seatSelection.SeatTypeId,
                            quantity = seatSelection.Quantity,
                            price_per_seat = seatType.price,
                            subtotal = subtotal,
                            //created_by = userEmail,
                            //updated_by = userEmail
                            created_by = userIdString,
                            updated_by = userIdString
                        };

                        bookingSeats.Add(bookingSeat);
                    }

                    await _bookingRepository.CreateBookingSeatsAsync(bookingSeats);

                    // Get booking details
                    var bookingDetails = await _bookingRepository.GetBookingDetailsAsync(bookingId);

                    response.Status = "Success";
                    response.Message = "Booking created successfully";
                    response.ErrorCode = "0";
                    response.Data = new BookingResponse
                    {
                        BookingId = bookingId,
                        BookingCode = bookingCode,
                        EventId = request.EventId,
                        EventName = eventDetails.event_name,
                        TotalAmount = totalAmount,
                        Status = "pending",
                        CreatedOn = DateTime.UtcNow
                    };
                }
                else
                {
                    //// Release reserved seats if booking creation failed
                    //foreach (var seatSelection in request.SeatSelections)
                    //{
                    //    await _bookingRepository.ReleaseSeatsAsync(
                    //        seatSelection.SeatTypeId, seatSelection.Quantity, userEmail);
                    //}

                    // Release reserved seats if booking creation failed
                    foreach (var seatSelection in request.SeatSelections)
                    {
                        await _bookingRepository.ReleaseSeatsAsync(
                            seatSelection.SeatTypeId, seatSelection.Quantity, userIdString);
                    }

                    response.Status = "Failure";
                    response.Message = "Failed to create booking";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error creating booking: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<BookingResponse>> ConfirmBookingAsync(int bookingId, string userEmail)
        {
            var response = new CommonResponseModel<BookingResponse>();

            try
            {
                if (bookingId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid booking ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    response.Status = "Failure";
                    response.Message = "Booking not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Get booking seats to update seat availability
                var bookingDetails = await _bookingRepository.GetBookingDetailsAsync(bookingId);
                if (bookingDetails == null)
                {
                    response.Status = "Failure";
                    response.Message = "Booking details not found";
                    response.ErrorCode = "404";
                    return response;
                }

                var seatUpdates = new List<SeatUpdateRequest>();
                foreach (var seat in bookingDetails.BookingSeats)
                {
                    seatUpdates.Add(new SeatUpdateRequest
                    {
                        SeatTypeId = seat.event_seat_type_inventory_id,
                        Quantity = seat.quantity
                    });
                }

                // Update booking status and reduce seat availability
                var affectedRows = await _bookingRepository.UpdateBookingStatusAndSeatsAsync(
                    bookingId, "confirmed", seatUpdates, userEmail);

                if (affectedRows > 0)
                {
                    // Get updated booking
                    var updatedBooking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                    var eventDetails = await _eventDetailsRepository.GetEventByIdAsync(updatedBooking.event_id);

                    response.Status = "Success";
                    response.Message = "Booking confirmed successfully";
                    response.ErrorCode = "0";
                    response.Data = new BookingResponse
                    {
                        BookingId = bookingId,
                        BookingCode = updatedBooking.booking_code,
                        EventId = updatedBooking.event_id,
                        EventName = eventDetails?.event_name,
                        TotalAmount = updatedBooking.total_amount,
                        Status = "confirmed",
                        CreatedOn = updatedBooking.created_on
                    };
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to confirm booking";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error confirming booking: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<BookingResponse>> CancelBookingAsync(int bookingId, string userEmail)
        {
            var response = new CommonResponseModel<BookingResponse>();

            try
            {
                if (bookingId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid booking ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    response.Status = "Failure";
                    response.Message = "Booking not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Get booking seats to release seats
                var bookingDetails = await _bookingRepository.GetBookingDetailsAsync(bookingId);
                if (bookingDetails == null)
                {
                    response.Status = "Failure";
                    response.Message = "Booking details not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Update booking status to cancelled
                var affectedRows = await _bookingRepository.UpdateBookingStatusAsync(bookingId, "cancelled", userEmail);

                if (affectedRows > 0)
                {
                    // Release seats
                    foreach (var seat in bookingDetails.BookingSeats)
                    {
                        await _bookingRepository.ReleaseSeatsAsync(
                            seat.event_seat_type_inventory_id, seat.quantity, userEmail);
                    }

                    var updatedBooking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                    var eventDetails = await _eventDetailsRepository.GetEventByIdAsync(updatedBooking.event_id);

                    response.Status = "Success";
                    response.Message = "Booking cancelled successfully";
                    response.ErrorCode = "0";
                    response.Data = new BookingResponse
                    {
                        BookingId = bookingId,
                        BookingCode = updatedBooking.booking_code,
                        EventId = updatedBooking.event_id,
                        EventName = eventDetails?.event_name,
                        TotalAmount = updatedBooking.total_amount,
                        Status = "cancelled",
                        CreatedOn = updatedBooking.created_on
                    };
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to cancel booking";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error cancelling booking: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<BookingDetailsResponse>> GetBookingDetailsAsync(int bookingId)
        {
            var response = new CommonResponseModel<BookingDetailsResponse>();

            try
            {
                if (bookingId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid booking ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                var bookingDetails = await _bookingRepository.GetBookingDetailsAsync(bookingId);

                if (bookingDetails != null)
                {
                    response.Status = "Success";
                    response.Message = "Booking details fetched successfully";
                    response.ErrorCode = "0";
                    response.Data = bookingDetails;
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Booking not found";
                    response.ErrorCode = "404";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error fetching booking details: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<BookingDetailsResponse>> GetBookingDetailsByCodeAsync(string bookingCode)
        {
            var response = new CommonResponseModel<BookingDetailsResponse>();

            try
            {
                if (string.IsNullOrEmpty(bookingCode))
                {
                    response.Status = "Failure";
                    response.Message = "Booking code is required";
                    response.ErrorCode = "400";
                    return response;
                }

                var bookingDetails = await _bookingRepository.GetBookingDetailsByCodeAsync(bookingCode);

                if (bookingDetails != null)
                {
                    response.Status = "Success";
                    response.Message = "Booking details fetched successfully";
                    response.ErrorCode = "0";
                    response.Data = bookingDetails;
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Booking not found";
                    response.ErrorCode = "404";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error fetching booking details: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<List<BookingResponse>>> GetUserBookingsAsync(Guid userId)
        {
            var response = new CommonResponseModel<List<BookingResponse>>();

            try
            {
                var bookings = await _bookingRepository.GetBookingsByUserIdAsync(userId);
                var bookingResponses = new List<BookingResponse>();

                foreach (var booking in bookings)
                {
                    var eventDetails = await _eventDetailsRepository.GetEventByIdAsync(booking.event_id);

                    bookingResponses.Add(new BookingResponse
                    {
                        BookingId = booking.booking_id,
                        BookingCode = booking.booking_code,
                        EventId = booking.event_id,
                        EventName = eventDetails?.event_name,
                        TotalAmount = booking.total_amount,
                        Status = booking.status,
                        CreatedOn = booking.created_on
                    });
                }

                response.Status = "Success";
                response.Message = "User bookings fetched successfully";
                response.ErrorCode = "0";
                response.Data = bookingResponses;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error fetching user bookings: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<bool>> CheckSeatAvailabilityAsync(SeatAvailabilityRequest request)
        {
            var response = new CommonResponseModel<bool>();

            try
            {
                if (request == null || !request.SeatSelections.Any())
                {
                    response.Status = "Failure";
                    response.Message = "Seat selection data is required";
                    response.ErrorCode = "400";
                    response.Data = false;
                    return response;
                }

                bool allAvailable = true;

                foreach (var seatSelection in request.SeatSelections)
                {
                    var isAvailable = await _bookingRepository.CheckSeatAvailabilityAsync(
                        seatSelection.SeatTypeId, seatSelection.Quantity);

                    if (!isAvailable)
                    {
                        allAvailable = false;
                        break;
                    }
                }

                response.Status = "Success";
                response.Message = allAvailable ? "Seats are available" : "Some seats are no longer available";
                response.ErrorCode = "0";
                response.Data = allAvailable;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error checking seat availability: {ex.Message}";
                response.ErrorCode = "1";
                response.Data = false;
            }

            return response;
        }

        public async Task<CommonResponseModel<List<MyBookingsResponse>>> GetMyBookingsByUserIdAsync(Guid userId)
        {
            var response = new CommonResponseModel<List<MyBookingsResponse>>();

            try
            {
                if (userId == Guid.Empty)
                {
                    response.Status = "Failure";
                    response.Message = "Valid user ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                var bookings = await _bookingRepository.GetMyBookingsByUserIdAsync(userId);

                response.Status = "Success";
                response.Message = "Bookings fetched successfully";
                response.ErrorCode = "0";
                response.Data = bookings.ToList();
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error fetching bookings: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }
    }
}
