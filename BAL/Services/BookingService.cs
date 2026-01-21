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
        Task<CommonResponseModel<BookingQRResponse>> ConfirmBookingWithQRAsync(int bookingId, string userEmail);
        Task<CommonResponseModel<QRCodeDataResponse>> DecodeQRCodeDataAsync(string qrCodeBase64);

        // Ticket Scanning
        Task<CommonResponseModel<TicketScanResponse>> ScanTicketAsync(ScanTicketRequest request, string adminEmail);
        Task<CommonResponseModel<TicketScanResponse>> PartialScanTicketAsync(PartialScanRequest request, string adminEmail);
        Task<CommonResponseModel<BookingScanSummaryResponse>> GetBookingScanSummaryAsync(int bookingId);
        Task<CommonResponseModel<BookingDetailsResponse>> GetBookingForScanningAsync(string bookingCode);
        Task<CommonResponseModel<List<TicketScanHistoryModel>>> GetScanHistoryAsync(int bookingId);
        Task<CommonResponseModel<bool>> ResetScanCountAsync(int bookingId, string adminEmail);
    }
    public class BookingService: IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventDetailsRepository _eventDetailsRepository;
        private readonly IUserRepository _authRepository;
        private readonly IQRCodeService _qrCodeService;
        private readonly IEmailService _emailService; // Assuming you have an email service

        public BookingService(
            IBookingRepository bookingRepository,
            IEventDetailsRepository eventDetailsRepository,
            IUserRepository authRepository,
            IQRCodeService qrCodeService,
            IEmailService emailService)
        {
            _bookingRepository = bookingRepository;
            _eventDetailsRepository = eventDetailsRepository;
            _authRepository = authRepository;
            _qrCodeService = qrCodeService;
            _emailService = emailService;
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
                            remaining_quantity = seatSelection.Quantity,  // Add this line
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

        // Add this new method for confirming booking with QR code
        public async Task<CommonResponseModel<BookingQRResponse>> ConfirmBookingWithQRAsync(int bookingId, string userEmail)
        {
            var response = new CommonResponseModel<BookingQRResponse>();

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

                // Get booking details
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
                    // Get updated booking details
                    var updatedBookingDetails = await _bookingRepository.GetBookingDetailsAsync(bookingId);

                    // Generate QR code
                    string qrCodeBase64 = await _qrCodeService.GenerateBookingQRCodeAsync(bookingId, updatedBookingDetails);

                    // Get event details
                    var eventDetails = await _eventDetailsRepository.GetEventByIdAsync(updatedBookingDetails.event_id);

                    // Prepare thank you message
                    string thankYouMessage = $"Thank you for booking {updatedBookingDetails.event_name}!\n\n" +
                                           $"Your booking #{updatedBookingDetails.booking_code} has been confirmed.\n" +
                                           $"Date: {updatedBookingDetails.event_date:dd MMM yyyy}\n" +
                                           $"Time: {updatedBookingDetails.start_time} - {updatedBookingDetails.end_time}\n" +
                                           $"Venue: {updatedBookingDetails.location}\n\n" +
                                           $"Please present this QR code at the venue entry.";

                    var qrResponse = new BookingQRResponse
                    {
                        BookingId = bookingId,
                        BookingCode = updatedBookingDetails.booking_code,
                        EventId = updatedBookingDetails.event_id,
                        EventName = eventDetails?.event_name,
                        TotalAmount = updatedBookingDetails.total_amount,
                        Status = "confirmed",
                        CreatedOn = updatedBookingDetails.created_on,
                        QRCodeBase64 = qrCodeBase64,
                        ThankYouMessage = thankYouMessage,
                        BookingDetails = updatedBookingDetails
                    };

                    // Send email with QR code
                    await SendBookingConfirmationEmailAsync(updatedBookingDetails, qrCodeBase64, thankYouMessage);

                    response.Status = "Success";
                    response.Message = "Booking confirmed successfully! QR code generated.";
                    response.ErrorCode = "0";
                    response.Data = qrResponse;
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

        // Add this method for email sending ----> later replace with html template
        // Update the SendBookingConfirmationEmailAsync method to remove the isHtml parameter
        private async Task SendBookingConfirmationEmailAsync(BookingDetailsResponse bookingDetails, string qrCodeBase64, string message)
        {
            try
            {
                string subject = $"Booking Confirmation - {bookingDetails.event_name}";

                // Create HTML email body
                string htmlBody = CreateBookingConfirmationEmailHtml(bookingDetails, qrCodeBase64, message);

                // Send email using the EmailService
                await _emailService.SendEmailAsync(
                    bookingDetails.email,
                    subject,
                    htmlBody
                );
            }
            catch (Exception ex)
            {
                // Log email sending error but don't fail the booking
                // Use proper logging instead of Console.WriteLine in production
                // Consider injecting ILogger<BookingService> for proper logging
                // For now, we'll just swallow the exception to not break booking confirmation
                // In production, implement proper logging
                var errorMessage = $"Error sending confirmation email: {ex.Message}";
                // Log error here
            }
        }

        private string CreateBookingConfirmationEmailHtml(BookingDetailsResponse bookingDetails, string qrCodeBase64, string message)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Booking Confirmation</title>
                <style>
                    body {{ 
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
                        line-height: 1.6; 
                        color: #333; 
                        margin: 0;
                        padding: 20px;
                        background-color: #f5f5f5;
                    }}
                    .container {{ 
                        max-width: 600px; 
                        margin: 0 auto; 
                        background: white;
                        border-radius: 12px;
                        overflow: hidden;
                        box-shadow: 0 4px 20px rgba(0,0,0,0.1);
                    }}
                    .header {{ 
                        background: linear-gradient(135deg, #2c2e72 0%, #4896d1 100%); 
                        color: white; 
                        padding: 30px 20px; 
                        text-align: center; 
                    }}
                    .header h1 {{
                        margin: 0;
                        font-size: 28px;
                        font-weight: 700;
                    }}
                    .header .subtitle {{
                        font-size: 16px;
                        opacity: 0.9;
                        margin-top: 10px;
                    }}
                    .content {{ 
                        padding: 30px; 
                    }}
                    .greeting {{
                        font-size: 18px;
                        margin-bottom: 20px;
                        color: #2c2e72;
                    }}
                    .message-box {{
                        background: #eef3ff;
                        padding: 20px;
                        border-radius: 8px;
                        margin-bottom: 25px;
                        border-left: 4px solid #4896d1;
                    }}
                    .message-box p {{
                        margin: 0;
                        white-space: pre-line;
                        line-height: 1.8;
                    }}
                    .qr-section {{
                        text-align: center; 
                        margin: 30px 0; 
                        padding: 25px;
                        background: #f8f9fa;
                        border-radius: 12px;
                        border: 2px dashed #4896d1;
                    }}
                    .qr-section h3 {{
                        color: #2c2e72;
                        margin-bottom: 15px;
                        font-size: 20px;
                    }}
                    .qr-code-container {{
                        display: inline-block;
                        padding: 20px;
                        background: white;
                        border-radius: 10px;
                        box-shadow: 0 2px 15px rgba(0,0,0,0.1);
                        margin: 15px 0;
                    }}
                    .qr-code-container img {{
                        width: 200px;
                        height: 200px;
                        display: block;
                    }}
                    .qr-instruction {{
                        margin-top: 15px;
                        color: #666;
                        font-size: 14px;
                        font-style: italic;
                    }}
                    .details-section {{
                        margin: 25px 0;
                    }}
                    .details-section h4 {{
                        color: #2c2e72;
                        margin-bottom: 15px;
                        font-size: 18px;
                        border-bottom: 2px solid #e9ecef;
                        padding-bottom: 8px;
                    }}
                    .detail-grid {{
                        display: grid;
                        grid-template-columns: 1fr 1fr;
                        gap: 15px;
                        margin-bottom: 20px;
                    }}
                    @media (max-width: 480px) {{
                        .detail-grid {{
                            grid-template-columns: 1fr;
                        }}
                    }}
                    .detail-item {{
                        margin-bottom: 12px;
                    }}
                    .detail-label {{
                        font-weight: 600;
                        color: #2c2e72;
                        font-size: 14px;
                        display: block;
                        margin-bottom: 5px;
                    }}
                    .detail-value {{
                        color: #333;
                        font-size: 15px;
                    }}
                    .seat-details {{
                        margin-top: 20px;
                        padding-top: 20px;
                        border-top: 1px solid #e9ecef;
                    }}
                    .seat-item {{
                        display: flex;
                        justify-content: space-between;
                        padding: 10px 0;
                        border-bottom: 1px solid #f0f0f0;
                    }}
                    .seat-item:last-child {{
                        border-bottom: none;
                    }}
                    .footer {{ 
                        margin-top: 30px; 
                        padding-top: 20px; 
                        border-top: 1px solid #ddd; 
                        text-align: center; 
                        color: #666;
                        font-size: 14px;
                    }}
                    .footer p {{
                        margin: 5px 0;
                    }}
                    .highlight {{
                        color: #2c2e72;
                        font-weight: 600;
                    }}
                    .total-amount {{
                        background: #2c2e72;
                        color: white;
                        padding: 15px;
                        border-radius: 8px;
                        margin: 20px 0;
                        text-align: center;
                        font-size: 20px;
                        font-weight: 700;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>🎉 Booking Confirmed!</h1>
                        <div class='subtitle'>Your TicketHouse Booking #{bookingDetails.booking_code}</div>
                    </div>
            
                    <div class='content'>
                        <div class='greeting'>
                            Dear <span class='highlight'>{bookingDetails.first_name} {bookingDetails.last_name}</span>,
                        </div>
                
                        <div class='message-box'>
                            <p>{message}</p>
                        </div>
                
                        <div class='qr-section'>
                            <h3>Your Entry QR Code</h3>
                            <div class='qr-code-container'>
                                <img src='data:image/png;base64,{qrCodeBase64}' alt='Booking QR Code' />
                            </div>
                            <p class='qr-instruction'>Please present this QR code at the venue for entry</p>
                        </div>
                
                        <div class='details-section'>
                            <h4>Booking Details</h4>
                            <div class='detail-grid'>
                                <div class='detail-item'>
                                    <span class='detail-label'>Booking Code:</span>
                                    <span class='detail-value highlight'>{bookingDetails.booking_code}</span>
                                </div>
                                <div class='detail-item'>
                                    <span class='detail-label'>Event:</span>
                                    <span class='detail-value'>{bookingDetails.event_name}</span>
                                </div>
                                <div class='detail-item'>
                                    <span class='detail-label'>Date:</span>
                                    <span class='detail-value'>{bookingDetails.event_date:dddd, dd MMMM yyyy}</span>
                                </div>
                                <div class='detail-item'>
                                    <span class='detail-label'>Time:</span>
                                    <span class='detail-value'>{bookingDetails.start_time} - {bookingDetails.end_time}</span>
                                </div>
                                <div class='detail-item'>
                                    <span class='detail-label'>Venue:</span>
                                    <span class='detail-value'>{bookingDetails.location}</span>
                                </div>
                                <div class='detail-item'>
                                    <span class='detail-label'>Status:</span>
                                    <span class='detail-value highlight' style='color: #4CAF50;'>{bookingDetails.status.ToUpper()}</span>
                                </div>
                            </div>
                    
                            <div class='total-amount'>
                                Total Amount: ₹{bookingDetails.total_amount}
                            </div>
                    
                            <div class='seat-details'>
                                <h4>Seat Details</h4>
                                {string.Join("", bookingDetails.BookingSeats.Select(bs =>
                                            $"<div class='seat-item'>" +
                                            $"<span>{bs.seat_name} × {bs.quantity}</span>" +
                                            $"<span class='highlight'>₹{bs.subtotal}</span>" +
                                            $"</div>"
                                        ))}
                            </div>
                        </div>
                
                        <div class='footer'>
                            <p>Thank you for choosing <span class='highlight'>TicketHouse</span>!</p>
                            <p>For any queries, please contact support@tickethouse.in</p>
                            <p>© {DateTime.Now.Year} TicketHouse. All rights reserved.</p>
                        </div>
                    </div>
                </div>
            </body>
            </html>";
        }

        // Add this method to decode QR code data
        public async Task<CommonResponseModel<QRCodeDataResponse>> DecodeQRCodeDataAsync(string qrCodeBase64)
        {
            var response = new CommonResponseModel<QRCodeDataResponse>();

            try
            {
                if (string.IsNullOrEmpty(qrCodeBase64))
                {
                    response.Status = "Failure";
                    response.Message = "QR code data is required";
                    response.ErrorCode = "400";
                    return response;
                }

                // Decode the QR code (in real scenario, you'd decode the base64 and parse JSON)
                // For now, we'll assume the data contains booking ID
                var bookingDetails = await GetBookingDetailsFromQRAsync(qrCodeBase64);

                if (bookingDetails != null)
                {
                    response.Status = "Success";
                    response.Message = "QR code data decoded successfully";
                    response.ErrorCode = "0";
                    response.Data = bookingDetails;
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Invalid QR code data";
                    response.ErrorCode = "404";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error decoding QR code: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        private async Task<QRCodeDataResponse> GetBookingDetailsFromQRAsync(string qrCodeBase64)
        {
            try
            {
                // Decode base64 and parse JSON
                // This is a simplified version - in production, you'd need proper QR code scanning
                // For now, we'll extract booking ID from the data
                var jsonData = DecodeQRCodeBase64(qrCodeBase64);
                if (!string.IsNullOrEmpty(jsonData))
                {
                    var qrData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonData);
                    if (qrData?.BookingId != null)
                    {
                        int bookingId = (int)qrData.BookingId;
                        var bookingDetails = await _bookingRepository.GetBookingDetailsAsync(bookingId);

                        if (bookingDetails != null)
                        {
                            return new QRCodeDataResponse
                            {
                                BookingId = bookingDetails.booking_id,
                                BookingCode = bookingDetails.booking_code,
                                EventName = bookingDetails.event_name,
                                EventDate = bookingDetails.event_date,
                                EventTime = $"{bookingDetails.start_time} - {bookingDetails.end_time}",
                                Location = bookingDetails.location,
                                CustomerName = $"{bookingDetails.first_name} {bookingDetails.last_name}",
                                CustomerEmail = bookingDetails.email,
                                TotalAmount = bookingDetails.total_amount,
                                Status = bookingDetails.status,
                                BookingDate = bookingDetails.created_on,
                                Seats = bookingDetails.BookingSeats.Select(bs => new QRSeatDetail
                                {
                                    SeatType = bs.seat_name,
                                    Quantity = bs.quantity,
                                    Price = bs.price_per_seat,
                                    Subtotal = bs.subtotal
                                }).ToList(),
                                Message = "Booking verified successfully!"
                            };
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private string DecodeQRCodeBase64(string base64Data)
        {
            try
            {
                byte[] data = Convert.FromBase64String(base64Data);
                return System.Text.Encoding.UTF8.GetString(data);
            }
            catch
            {
                return null;
            }
        }

        public async Task<CommonResponseModel<TicketScanResponse>> ScanTicketAsync(ScanTicketRequest request, string adminEmail)
        {
            var response = new CommonResponseModel<TicketScanResponse>();

            try
            {
                if (string.IsNullOrEmpty(request.BookingCode))
                {
                    response.Status = "Failure";
                    response.Message = "Booking code is required";
                    response.ErrorCode = "400";
                    return response;
                }

                // Set scanned by info
                //request.ScannedBy = adminEmail;
                //request.DeviceInfo = $"Web Admin - {DateTime.UtcNow}";

                // Get user by email to get user_id
                var user = await _authRepository.GetUserByEmail(adminEmail);
                if (user == null)
                {
                    response.Status = "Failure";
                    response.Message = "User not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Set scanned by info with user_id instead of email
                request.ScannedBy = user.user_id.ToString(); // Convert GUID to string
                request.DeviceInfo = $"Web Admin - {DateTime.UtcNow}";

                var scanResult = await _bookingRepository.ScanTicketAsync(request);

                response.Status = scanResult.IsSuccess ? "Success" : "Partial";
                response.Message = scanResult.Message;
                response.ErrorCode = "0";
                response.Data = scanResult;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error scanning ticket: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<TicketScanResponse>> PartialScanTicketAsync(PartialScanRequest request, string adminEmail)
        {
            var response = new CommonResponseModel<TicketScanResponse>();

            try
            {
                if (request.BookingId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid booking ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                if (request.SeatScanDetails == null || !request.SeatScanDetails.Any())
                {
                    response.Status = "Failure";
                    response.Message = "Seat scan details are required";
                    response.ErrorCode = "400";
                    return response;
                }

                // Validate quantities
                foreach (var detail in request.SeatScanDetails)
                {
                    if (detail.QuantityToScan <= 0)
                    {
                        response.Status = "Failure";
                        response.Message = "Quantity to scan must be greater than 0";
                        response.ErrorCode = "400";
                        return response;
                    }
                }

                //request.ScannedBy = adminEmail;
                //request.DeviceInfo = $"Web Admin - {DateTime.UtcNow}";

                // Get user by email to get user_id
                var user = await _authRepository.GetUserByEmail(adminEmail);
                if (user == null)
                {
                    response.Status = "Failure";
                    response.Message = "User not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Set scanned by with user_id instead of email
                request.ScannedBy = user.user_id.ToString(); // Convert GUID to string
                request.DeviceInfo = $"Web Scanner - {DateTime.UtcNow}";

                var scanResult = await _bookingRepository.PartialScanTicketAsync(request);

                response.Status = scanResult.IsSuccess ? "Success" : "Partial";
                response.Message = scanResult.Message;
                response.ErrorCode = "0";
                response.Data = scanResult;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error in partial scan: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<BookingScanSummaryResponse>> GetBookingScanSummaryAsync(int bookingId)
        {
            var response = new CommonResponseModel<BookingScanSummaryResponse>();

            try
            {
                if (bookingId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid booking ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                var summary = await _bookingRepository.GetBookingScanSummaryAsync(bookingId);

                if (summary != null)
                {
                    response.Status = "Success";
                    response.Message = "Scan summary fetched successfully";
                    response.ErrorCode = "0";
                    response.Data = summary;
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
                response.Message = $"Error fetching scan summary: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<BookingDetailsResponse>> GetBookingForScanningAsync(string bookingCode)
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

                var bookingDetails = await _bookingRepository.GetBookingForScanningAsync(bookingCode);

                if (bookingDetails != null)
                {
                    response.Status = "Success";
                    response.Message = "Booking details fetched for scanning";
                    response.ErrorCode = "0";
                    response.Data = bookingDetails;
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Booking not found or not confirmed";
                    response.ErrorCode = "404";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error fetching booking for scanning: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<List<TicketScanHistoryModel>>> GetScanHistoryAsync(int bookingId)
        {
            var response = new CommonResponseModel<List<TicketScanHistoryModel>>();

            try
            {
                if (bookingId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid booking ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                var scanHistory = await _bookingRepository.GetScanHistoryAsync(bookingId);

                response.Status = "Success";
                response.Message = "Scan history fetched successfully";
                response.ErrorCode = "0";
                response.Data = scanHistory.ToList();
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error fetching scan history: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<bool>> ValidateTicketForScanAsync(string bookingCode, int seatTypeId, int quantityToScan)
        {
            var response = new CommonResponseModel<bool>();

            try
            {
                if (string.IsNullOrEmpty(bookingCode))
                {
                    response.Status = "Failure";
                    response.Message = "Booking code is required";
                    response.ErrorCode = "400";
                    response.Data = false;
                    return response;
                }

                if (seatTypeId <= 0 || quantityToScan <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid seat type ID and quantity are required";
                    response.ErrorCode = "400";
                    response.Data = false;
                    return response;
                }

                var isValid = await _bookingRepository.ValidateTicketForScanAsync(bookingCode, seatTypeId, quantityToScan);

                response.Status = "Success";
                response.Message = isValid ? "Ticket is valid for scanning" : "Ticket is not valid for scanning";
                response.ErrorCode = "0";
                response.Data = isValid;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error validating ticket: {ex.Message}";
                response.ErrorCode = "1";
                response.Data = false;
            }

            return response;
        }

        public async Task<CommonResponseModel<bool>> ResetScanCountAsync(int bookingId, string adminEmail)
        {
            var response = new CommonResponseModel<bool>();

            try
            {
                if (bookingId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid booking ID is required";
                    response.ErrorCode = "400";
                    response.Data = false;
                    return response;
                }

                if (string.IsNullOrEmpty(adminEmail))
                {
                    response.Status = "Failure";
                    response.Message = "Admin email is required";
                    response.ErrorCode = "400";
                    response.Data = false;
                    return response;
                }

                var affectedRows = await _bookingRepository.ResetScanCountAsync(bookingId, adminEmail);

                response.Status = affectedRows > 0 ? "Success" : "Failure";
                response.Message = affectedRows > 0 ? "Scan count reset successfully" : "Failed to reset scan count";
                response.ErrorCode = "0";
                response.Data = affectedRows > 0;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error resetting scan count: {ex.Message}";
                response.ErrorCode = "1";
                response.Data = false;
            }

            return response;
        }
    }
}
