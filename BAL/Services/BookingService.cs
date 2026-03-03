using ClosedXML.Excel;
using DAL.Repository;
using DAL.Utilities;
using Microsoft.Extensions.Logging;
using MODEL.Entities;
using MODEL.Request;
using MODEL.Response;
using OfficeOpenXml;
using PreMailer.Net;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        Task<CommonResponseModel<BookingDetailedResponse>> GetBookingDetailsByIdAsync(int bookingId);
        Task<CommonResponseModel<EventSummaryResponse>> GetEventSummaryByEventIdAsync(int eventId);
        Task<PagedBookingHistoryResponse> GetPagedBookingHistoryByUserIdAsync(BookingHistoryRequest request);
        /// <summary>
        /// Get comprehensive booking history for a specific event
        /// </summary>
        Task<CommonResponseModel<EventBookingHistoryResponse>> GetEventBookingHistoryAsync(int eventId);
        Task<CommonResponseModel<TicketScanHistoryByEventResponse>> GetTicketScanHistoryByEventIdAsync(int eventId, int pageNumber = 1, int pageSize = 10);
        Task<byte[]> ExportEventBookingDetailsToExcelAsync(int eventId);
    }
    public class BookingService: IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventDetailsRepository _eventDetailsRepository;
        private readonly IUserRepository _authRepository;
        private readonly IQRCodeService _qrCodeService;
        private readonly IEmailService _emailService; // Assuming you have an email service
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            IBookingRepository bookingRepository,
            IEventDetailsRepository eventDetailsRepository,
            IUserRepository authRepository,
            IQRCodeService qrCodeService,
            IEmailService emailService,
            ILogger<BookingService> logger)
        {
            _bookingRepository = bookingRepository;
            _eventDetailsRepository = eventDetailsRepository;
            _authRepository = authRepository;
            _qrCodeService = qrCodeService;
            _emailService = emailService;
            _logger = logger;
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

                // Get convenience fee percentage from event (default to 6 if not set)
                decimal convenienceFeePercentage = eventDetails.convenience_fee ?? 6m;

                // Calculate total amount
                decimal totalAmount = 0;
                var bookingSeats = new List<BookingSeatModel>();

                foreach (var seatSelection in request.SeatSelections)
                {
                    var seatType = await _bookingRepository.GetSeatTypeByIdAsync(seatSelection.SeatTypeId);
                    var subtotal = seatType.price * seatSelection.Quantity;
                    totalAmount += subtotal;
                }

                // Calculate fees using helper
                //var fees = FeeCalculator.CalculateFees(totalAmount);

                // Calculate fees using dynamic percentage from event
                var fees = FeeCalculator.CalculateFees(totalAmount, convenienceFeePercentage);

                // Generate booking code
                var bookingCode = $"ZTH{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";

                // Create booking
                var booking = new BookingModel
                {
                    booking_code = bookingCode,
                    user_id = user.user_id,
                    event_id = request.EventId,
                    total_amount = totalAmount, //base amount
                    booking_amount = totalAmount, // Same as total_amount (for clarity)
                    convenience_fee = fees.convenienceFee,
                    gst_amount = fees.gstAmount,
                    final_amount = fees.finalAmount, // This is what Razorpay will use
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
                        //TotalAmount = totalAmount,
                        TotalAmount = totalAmount, // Base amount
                        FinalAmount = fees.finalAmount, // Add this to response model
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
        ////-----correct before qr save
        //public async Task<CommonResponseModel<BookingQRResponse>> ConfirmBookingWithQRAsync(int bookingId, string userEmail)
        //{
        //    var response = new CommonResponseModel<BookingQRResponse>();

        //    try
        //    {
        //        if (bookingId <= 0)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Valid booking ID is required";
        //            response.ErrorCode = "400";
        //            return response;
        //        }

        //        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        //        if (booking == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Booking not found";
        //            response.ErrorCode = "404";
        //            return response;
        //        }

        //        // Get booking details
        //        var bookingDetails = await _bookingRepository.GetBookingDetailsAsync(bookingId);
        //        if (bookingDetails == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Booking details not found";
        //            response.ErrorCode = "404";
        //            return response;
        //        }

        //        var seatUpdates = new List<SeatUpdateRequest>();
        //        foreach (var seat in bookingDetails.BookingSeats)
        //        {
        //            seatUpdates.Add(new SeatUpdateRequest
        //            {
        //                SeatTypeId = seat.event_seat_type_inventory_id,
        //                Quantity = seat.quantity
        //            });
        //        }

        //        // Update booking status and reduce seat availability
        //        var affectedRows = await _bookingRepository.UpdateBookingStatusAndSeatsAsync(
        //            bookingId, "confirmed", seatUpdates, userEmail);

        //        if (affectedRows > 0)
        //        {
        //            // Get updated booking details
        //            var updatedBookingDetails = await _bookingRepository.GetBookingDetailsAsync(bookingId);

        //            // Generate QR code
        //            string qrCodeBase64 = await _qrCodeService.GenerateBookingQRCodeAsync(bookingId, updatedBookingDetails);

        //            // Get event details
        //            var eventDetails = await _eventDetailsRepository.GetEventByIdAsync(updatedBookingDetails.event_id);

        //            // Prepare thank you message
        //            string thankYouMessage = $"Thank you for booking {updatedBookingDetails.event_name}!\n\n" +
        //                                   $"Your booking #{updatedBookingDetails.booking_code} has been confirmed.\n" +
        //                                   $"Date: {updatedBookingDetails.event_date:dd MMM yyyy}\n" +
        //                                   $"Time: {updatedBookingDetails.start_time} - {updatedBookingDetails.end_time}\n" +
        //                                   $"Venue: {updatedBookingDetails.location}\n\n" +
        //                                   $"Please present this QR code at the venue entry.";

        //            var qrResponse = new BookingQRResponse
        //            {
        //                BookingId = bookingId,
        //                BookingCode = updatedBookingDetails.booking_code,
        //                EventId = updatedBookingDetails.event_id,
        //                EventName = eventDetails?.event_name,
        //                TotalAmount = updatedBookingDetails.total_amount,
        //                Status = "confirmed",
        //                CreatedOn = updatedBookingDetails.created_on,
        //                QRCodeBase64 = qrCodeBase64,
        //                ThankYouMessage = thankYouMessage,
        //                BookingDetails = updatedBookingDetails
        //            };

        //            // Send email with QR code
        //            await SendBookingConfirmationEmailAsync(updatedBookingDetails, qrCodeBase64, thankYouMessage);
        //            //await SendBookingConfirmationEmailSimpleAsync(updatedBookingDetails, qrCodeBase64, thankYouMessage);

        //            response.Status = "Success";
        //            response.Message = "Booking confirmed successfully! QR code generated.";
        //            response.ErrorCode = "0";
        //            response.Data = qrResponse;
        //        }
        //        else
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Failed to confirm booking";
        //            response.ErrorCode = "1";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Status = "Failure";
        //        response.Message = $"Error confirming booking: {ex.Message}";
        //        response.ErrorCode = "1";
        //    }

        //    return response;
        //}

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

                    // STORE QR CODE IN DATABASE - THIS IS THE KEY CHANGE
                    await _bookingRepository.UpdateBookingQRCodeAsync(bookingId, qrCodeBase64, booking.user_id.ToString());

                    // Get event details
                    var eventDetails = await _eventDetailsRepository.GetEventByIdAsync(updatedBookingDetails.event_id);

                    // Prepare thank you message
                    //string thankYouMessage = $"Thank you for booking {updatedBookingDetails.event_name}!\n\n" +
                    //                       $"Your booking #{updatedBookingDetails.booking_code} has been confirmed.\n" +
                    //                       $"Date: {updatedBookingDetails.event_date:dd MMM yyyy}\n" +
                    //                       $"Time: {updatedBookingDetails.start_time} - {updatedBookingDetails.end_time}\n" +
                    //                       $"Venue: {updatedBookingDetails.location}\n\n" +
                    //                       $"Please present this QR code at the venue entry.";

                    // Prepare thank you message (include coupon info if applied)
                    string thankYouMessage = $"Thank you for booking {updatedBookingDetails.event_name}!\n\n" +
                                           $"Your booking #{updatedBookingDetails.booking_code} has been confirmed.\n" +
                                           $"Date: {updatedBookingDetails.event_date:dd MMM yyyy}\n" +
                                           $"Time: {updatedBookingDetails.start_time} - {updatedBookingDetails.end_time}\n" +
                                           $"Venue: {updatedBookingDetails.location}\n\n" +
                                           (updatedBookingDetails.discount_amount > 0
                                            ? $"Coupon Applied: {updatedBookingDetails.coupon_code_applied} (Discount: ₹{updatedBookingDetails.discount_amount})\n\n"
                                            : "") +
                                           $"Please present this QR code at the venue entry.";

                    var qrResponse = new BookingQRResponse
                    {
                        BookingId = bookingId,
                        BookingCode = updatedBookingDetails.booking_code,
                        EventId = updatedBookingDetails.event_id,
                        EventName = eventDetails?.event_name,
                        //TotalAmount = updatedBookingDetails.total_amount,
                        TotalAmount = updatedBookingDetails.final_amount,
                        Status = "confirmed",
                        CreatedOn = updatedBookingDetails.created_on,
                        QRCodeBase64 = qrCodeBase64,
                        ThankYouMessage = thankYouMessage,
                        BookingDetails = updatedBookingDetails
                    };

                    // Send email with QR code
                    await SendBookingConfirmationEmailAsync(updatedBookingDetails, qrCodeBase64, thankYouMessage);

                    response.Status = "Success";
                    response.Message = "Booking confirmed successfully! QR code generated and stored.";
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
        //private async Task SendBookingConfirmationEmailAsync(BookingDetailsResponse bookingDetails, string qrCodeBase64, string message)
        //{
        //    try
        //    {
        //        string subject = $"Booking Confirmation - {bookingDetails.event_name}";

        //        // Create HTML email body
        //        string htmlBody = CreateBookingConfirmationEmailHtml(bookingDetails, qrCodeBase64, message);

        //        // Send email using the EmailService
        //        await _emailService.SendEmailAsync(
        //            bookingDetails.email,
        //            subject,
        //            htmlBody
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log email sending error but don't fail the booking
        //        // Use proper logging instead of Console.WriteLine in production
        //        // Consider injecting ILogger<BookingService> for proper logging
        //        // For now, we'll just swallow the exception to not break booking confirmation
        //        // In production, implement proper logging
        //        var errorMessage = $"Error sending confirmation email: {ex.Message}";
        //        // Log error here
        //    }
        //}

        //private string CreateBookingConfirmationEmailHtml(BookingDetailsResponse bookingDetails, string qrCodeBase64, string message)
        //{
        //    return $@"
        //    <!DOCTYPE html>
        //    <html>
        //    <head>
        //        <meta charset='UTF-8'>
        //        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        //        <title>Booking Confirmation</title>
        //        <style>
        //            body {{ 
        //                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
        //                line-height: 1.6; 
        //                color: #333; 
        //                margin: 0;
        //                padding: 20px;
        //                background-color: #f5f5f5;
        //            }}
        //            .container {{ 
        //                max-width: 600px; 
        //                margin: 0 auto; 
        //                background: white;
        //                border-radius: 12px;
        //                overflow: hidden;
        //                box-shadow: 0 4px 20px rgba(0,0,0,0.1);
        //            }}
        //            .header {{ 
        //                background: linear-gradient(135deg, #2c2e72 0%, #4896d1 100%); 
        //                color: white; 
        //                padding: 30px 20px; 
        //                text-align: center; 
        //            }}
        //            .header h1 {{
        //                margin: 0;
        //                font-size: 28px;
        //                font-weight: 700;
        //            }}
        //            .header .subtitle {{
        //                font-size: 16px;
        //                opacity: 0.9;
        //                margin-top: 10px;
        //            }}
        //            .content {{ 
        //                padding: 30px; 
        //            }}
        //            .greeting {{
        //                font-size: 18px;
        //                margin-bottom: 20px;
        //                color: #2c2e72;
        //            }}
        //            .message-box {{
        //                background: #eef3ff;
        //                padding: 20px;
        //                border-radius: 8px;
        //                margin-bottom: 25px;
        //                border-left: 4px solid #4896d1;
        //            }}
        //            .message-box p {{
        //                margin: 0;
        //                white-space: pre-line;
        //                line-height: 1.8;
        //            }}
        //            .qr-section {{
        //                text-align: center; 
        //                margin: 30px 0; 
        //                padding: 25px;
        //                background: #f8f9fa;
        //                border-radius: 12px;
        //                border: 2px dashed #4896d1;
        //            }}
        //            .qr-section h3 {{
        //                color: #2c2e72;
        //                margin-bottom: 15px;
        //                font-size: 20px;
        //            }}
        //            .qr-code-container {{
        //                display: inline-block;
        //                padding: 20px;
        //                background: white;
        //                border-radius: 10px;
        //                box-shadow: 0 2px 15px rgba(0,0,0,0.1);
        //                margin: 15px 0;
        //            }}
        //            .qr-code-container img {{
        //                width: 200px;
        //                height: 200px;
        //                display: block;
        //            }}
        //            .qr-instruction {{
        //                margin-top: 15px;
        //                color: #666;
        //                font-size: 14px;
        //                font-style: italic;
        //            }}
        //            .details-section {{
        //                margin: 25px 0;
        //            }}
        //            .details-section h4 {{
        //                color: #2c2e72;
        //                margin-bottom: 15px;
        //                font-size: 18px;
        //                border-bottom: 2px solid #e9ecef;
        //                padding-bottom: 8px;
        //            }}
        //            .detail-grid {{
        //                display: grid;
        //                grid-template-columns: 1fr 1fr;
        //                gap: 15px;
        //                margin-bottom: 20px;
        //            }}
        //            @media (max-width: 480px) {{
        //                .detail-grid {{
        //                    grid-template-columns: 1fr;
        //                }}
        //            }}
        //            .detail-item {{
        //                margin-bottom: 12px;
        //            }}
        //            .detail-label {{
        //                font-weight: 600;
        //                color: #2c2e72;
        //                font-size: 14px;
        //                display: block;
        //                margin-bottom: 5px;
        //            }}
        //            .detail-value {{
        //                color: #333;
        //                font-size: 15px;
        //            }}
        //            .seat-details {{
        //                margin-top: 20px;
        //                padding-top: 20px;
        //                border-top: 1px solid #e9ecef;
        //            }}
        //            .seat-item {{
        //                display: flex;
        //                justify-content: space-between;
        //                padding: 10px 0;
        //                border-bottom: 1px solid #f0f0f0;
        //            }}
        //            .seat-item:last-child {{
        //                border-bottom: none;
        //            }}
        //            .footer {{ 
        //                margin-top: 30px; 
        //                padding-top: 20px; 
        //                border-top: 1px solid #ddd; 
        //                text-align: center; 
        //                color: #666;
        //                font-size: 14px;
        //            }}
        //            .footer p {{
        //                margin: 5px 0;
        //            }}
        //            .highlight {{
        //                color: #2c2e72;
        //                font-weight: 600;
        //            }}
        //            .total-amount {{
        //                background: #2c2e72;
        //                color: white;
        //                padding: 15px;
        //                border-radius: 8px;
        //                margin: 20px 0;
        //                text-align: center;
        //                font-size: 20px;
        //                font-weight: 700;
        //            }}
        //        </style>
        //    </head>
        //    <body>
        //        <div class='container'>
        //            <div class='header'>
        //                <h1>🎉 Booking Confirmed!</h1>
        //                <div class='subtitle'>Your TicketHouse Booking #{bookingDetails.booking_code}</div>
        //            </div>

        //            <div class='content'>
        //                <div class='greeting'>
        //                    Dear <span class='highlight'>{bookingDetails.first_name} {bookingDetails.last_name}</span>,
        //                </div>

        //                <div class='message-box'>
        //                    <p>{message}</p>
        //                </div>

        //                <div class='qr-section'>
        //                    <h3>Your Entry QR Code</h3>
        //                    <div class='qr-code-container'>
        //                        <img src='data:image/png;base64,{qrCodeBase64}' alt='Booking QR Code' />
        //                    </div>
        //                    <p class='qr-instruction'>Please present this QR code at the venue for entry</p>
        //                </div>

        //                <div class='details-section'>
        //                    <h4>Booking Details</h4>
        //                    <div class='detail-grid'>
        //                        <div class='detail-item'>
        //                            <span class='detail-label'>Booking Code:</span>
        //                            <span class='detail-value highlight'>{bookingDetails.booking_code}</span>
        //                        </div>
        //                        <div class='detail-item'>
        //                            <span class='detail-label'>Event:</span>
        //                            <span class='detail-value'>{bookingDetails.event_name}</span>
        //                        </div>
        //                        <div class='detail-item'>
        //                            <span class='detail-label'>Date:</span>
        //                            <span class='detail-value'>{bookingDetails.event_date:dddd, dd MMMM yyyy}</span>
        //                        </div>
        //                        <div class='detail-item'>
        //                            <span class='detail-label'>Time:</span>
        //                            <span class='detail-value'>{bookingDetails.start_time} - {bookingDetails.end_time}</span>
        //                        </div>
        //                        <div class='detail-item'>
        //                            <span class='detail-label'>Venue:</span>
        //                            <span class='detail-value'>{bookingDetails.location}</span>
        //                        </div>
        //                        <div class='detail-item'>
        //                            <span class='detail-label'>Status:</span>
        //                            <span class='detail-value highlight' style='color: #4CAF50;'>{bookingDetails.status.ToUpper()}</span>
        //                        </div>
        //                    </div>

        //                    <div class='total-amount'>
        //                        Total Amount: ₹{bookingDetails.total_amount}
        //                    </div>

        //                    <div class='seat-details'>
        //                        <h4>Seat Details</h4>
        //                        {string.Join("", bookingDetails.BookingSeats.Select(bs =>
        //                                    $"<div class='seat-item'>" +
        //                                    $"<span>{bs.seat_name} × {bs.quantity}</span>" +
        //                                    $"<span class='highlight'>₹{bs.subtotal}</span>" +
        //                                    $"</div>"
        //                                ))}
        //                    </div>
        //                </div>

        //                <div class='footer'>
        //                    <p>Thank you for choosing <span class='highlight'>TicketHouse</span>!</p>
        //                    <p>For any queries, please contact support@tickethouse.in</p>
        //                    <p>© {DateTime.Now.Year} TicketHouse. All rights reserved.</p>
        //                </div>
        //            </div>
        //        </div>
        //    </body>
        //    </html>";
        //}

        // Replace the existing SendBookingConfirmationEmailAsync method with this:

        //private async Task SendBookingConfirmationEmailAsync(BookingDetailsResponse bookingDetails, string qrCodeBase64, string message)
        //{
        //    try
        //    {
        //        string subject = $"Your Booking Confirmation - {bookingDetails.event_name}";

        //        // Convert base64 string to byte array for attachment
        //        byte[] qrCodeBytes = Convert.FromBase64String(qrCodeBase64);

        //        // Create HTML email body WITHOUT embedded QR code
        //        string htmlBody = CreateBookingConfirmationEmailHtml(bookingDetails, message);

        //        // Send email with QR code as attachment
        //        string fileName = $"Ticket_{bookingDetails.booking_code}.png";

        //        await _emailService.SendEmailWithAttachmentAsync(
        //            bookingDetails.email,
        //            subject,
        //            htmlBody,
        //            qrCodeBytes,
        //            fileName
        //        );

        //        _logger.LogInformation($"Confirmation email sent to {bookingDetails.email} with QR attachment");
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log error but don't throw - booking is already confirmed
        //        _logger.LogError(ex, $"Error sending confirmation email to {bookingDetails.email}");
        //    }
        //}

        // Update the CreateBookingConfirmationEmailHtml method - REMOVE the embedded QR code
        //    private string CreateBookingConfirmationEmailHtml(BookingDetailsResponse bookingDetails, string message)
        //    {
        //        return $@"
        //<!DOCTYPE html>
        //<html>
        //<head>
        //    <meta charset='UTF-8'>
        //    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        //    <title>Booking Confirmation</title>
        //    <style>
        //        body {{ 
        //            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
        //            line-height: 1.6; 
        //            color: #333; 
        //            margin: 0;
        //            padding: 20px;
        //            background-color: #f5f5f5;
        //        }}
        //        .container {{ 
        //            max-width: 600px; 
        //            margin: 0 auto; 
        //            background: white;
        //            border-radius: 12px;
        //            overflow: hidden;
        //            box-shadow: 0 4px 20px rgba(0,0,0,0.1);
        //        }}
        //        .header {{ 
        //            background: linear-gradient(135deg, #2c2e72 0%, #4896d1 100%); 
        //            color: white; 
        //            padding: 30px 20px; 
        //            text-align: center; 
        //        }}
        //        .header h1 {{
        //            margin: 0;
        //            font-size: 28px;
        //            font-weight: 700;
        //        }}
        //        .header .subtitle {{
        //            font-size: 16px;
        //            opacity: 0.9;
        //            margin-top: 10px;
        //        }}
        //        .content {{ 
        //            padding: 30px; 
        //        }}
        //        .greeting {{
        //            font-size: 18px;
        //            margin-bottom: 20px;
        //            color: #2c2e72;
        //        }}
        //        .message-box {{
        //            background: #eef3ff;
        //            padding: 20px;
        //            border-radius: 8px;
        //            margin-bottom: 25px;
        //            border-left: 4px solid #4896d1;
        //        }}
        //        .message-box p {{
        //            margin: 0;
        //            white-space: pre-line;
        //            line-height: 1.8;
        //        }}
        //        .qr-section {{
        //            text-align: center; 
        //            margin: 30px 0; 
        //            padding: 25px;
        //            background: #f8f9fa;
        //            border-radius: 12px;
        //            border: 2px dashed #4896d1;
        //        }}
        //        .qr-section h3 {{
        //            color: #2c2e72;
        //            margin-bottom: 15px;
        //            font-size: 20px;
        //        }}
        //        .qr-instruction {{
        //            margin-top: 15px;
        //            color: #666;
        //            font-size: 14px;
        //            font-style: italic;
        //        }}
        //        .details-section {{
        //            margin: 25px 0;
        //        }}
        //        .details-section h4 {{
        //            color: #2c2e72;
        //            margin-bottom: 15px;
        //            font-size: 18px;
        //            border-bottom: 2px solid #e9ecef;
        //            padding-bottom: 8px;
        //        }}
        //        .detail-grid {{
        //            display: grid;
        //            grid-template-columns: 1fr 1fr;
        //            gap: 15px;
        //            margin-bottom: 20px;
        //        }}
        //        @media (max-width: 480px) {{
        //            .detail-grid {{
        //                grid-template-columns: 1fr;
        //            }}
        //        }}
        //        .detail-item {{
        //            margin-bottom: 12px;
        //        }}
        //        .detail-label {{
        //            font-weight: 600;
        //            color: #2c2e72;
        //            font-size: 14px;
        //            display: block;
        //            margin-bottom: 5px;
        //        }}
        //        .detail-value {{
        //            color: #333;
        //            font-size: 15px;
        //        }}
        //        .seat-details {{
        //            margin-top: 20px;
        //            padding-top: 20px;
        //            border-top: 1px solid #e9ecef;
        //        }}
        //        .seat-item {{
        //            display: flex;
        //            justify-content: space-between;
        //            padding: 10px 0;
        //            border-bottom: 1px solid #f0f0f0;
        //        }}
        //        .seat-item:last-child {{
        //            border-bottom: none;
        //        }}
        //        .footer {{ 
        //            margin-top: 30px; 
        //            padding-top: 20px; 
        //            border-top: 1px solid #ddd; 
        //            text-align: center; 
        //            color: #666;
        //            font-size: 14px;
        //        }}
        //        .footer p {{
        //            margin: 5px 0;
        //        }}
        //        .highlight {{
        //            color: #2c2e72;
        //            font-weight: 600;
        //        }}
        //        .total-amount {{
        //            background: #2c2e72;
        //            color: white;
        //            padding: 15px;
        //            border-radius: 8px;
        //            margin: 20px 0;
        //            text-align: center;
        //            font-size: 20px;
        //            font-weight: 700;
        //        }}
        //        .attachment-note {{
        //            background: #e8f5e9;
        //            padding: 15px;
        //            border-radius: 8px;
        //            margin: 20px 0;
        //            text-align: center;
        //            border: 1px solid #4caf50;
        //        }}
        //        .attachment-note p {{
        //            margin: 5px 0;
        //            color: #2e7d32;
        //        }}
        //    </style>
        //</head>
        //<body>
        //    <div class='container'>
        //        <div class='header'>
        //            <h1>🎉 Booking Confirmed!</h1>
        //            <div class='subtitle'>Your TicketHouse Booking #{bookingDetails.booking_code}</div>
        //        </div>

        //        <div class='content'>
        //            <div class='greeting'>
        //                Dear <span class='highlight'>{bookingDetails.first_name} {bookingDetails.last_name}</span>,
        //            </div>

        //            <div class='message-box'>
        //                <p>{message}</p>
        //            </div>

        //            <div class='attachment-note'>
        //                <p>📎 <strong>QR Code Attached</strong></p>
        //                <p>Please find your entry QR code attached to this email as 'Ticket_{bookingDetails.booking_code}.png'</p>
        //                <p>Present this QR code at the venue for quick entry</p>
        //            </div>

        //            <div class='details-section'>
        //                <h4>Booking Details</h4>
        //                <div class='detail-grid'>
        //                    <div class='detail-item'>
        //                        <span class='detail-label'>Booking Code:</span>
        //                        <span class='detail-value highlight'>{bookingDetails.booking_code}</span>
        //                    </div>
        //                    <div class='detail-item'>
        //                        <span class='detail-label'>Event:</span>
        //                        <span class='detail-value'>{bookingDetails.event_name}</span>
        //                    </div>
        //                    <div class='detail-item'>
        //                        <span class='detail-label'>Date:</span>
        //                        <span class='detail-value'>{bookingDetails.event_date:dddd, dd MMMM yyyy}</span>
        //                    </div>
        //                    <div class='detail-item'>
        //                        <span class='detail-label'>Time:</span>
        //                        <span class='detail-value'>{bookingDetails.start_time} - {bookingDetails.end_time}</span>
        //                    </div>
        //                    <div class='detail-item'>
        //                        <span class='detail-label'>Venue:</span>
        //                        <span class='detail-value'>{bookingDetails.location}</span>
        //                    </div>
        //                    <div class='detail-item'>
        //                        <span class='detail-label'>Status:</span>
        //                        <span class='detail-value highlight' style='color: #4CAF50;'>{bookingDetails.status.ToUpper()}</span>
        //                    </div>
        //                </div>

        //                <div class='total-amount'>
        //                    Total Amount: ₹{bookingDetails.final_amount}
        //                </div>

        //                <div class='seat-details'>
        //                    <h4>Seat Details</h4>
        //                    {string.Join("", bookingDetails.BookingSeats.Select(bs =>
        //                                        $"<div class='seat-item'>" +
        //                                        $"<span>{bs.seat_name} × {bs.quantity}</span>" +
        //                                        $"<span class='highlight'>₹{bs.subtotal}</span>" +
        //                                        $"</div>"
        //                                    ))}
        //                </div>
        //            </div>

        //            <div class='footer'>
        //                <p>Thank you for choosing <span class='highlight'>TicketHouse</span>!</p>
        //                <p>For any queries, please contact support@tickethouse.in</p>
        //                <p>© {DateTime.Now.Year} TicketHouse. All rights reserved.</p>
        //            </div>
        //        </div>
        //    </div>
        //</body>
        //</html>";
        //    }

        //private async Task SendBookingConfirmationEmailAsync(BookingDetailsResponse bookingDetails, string qrCodeBase64, string message)
        //{
        //    try
        //    {
        //        string subject = $"Booking Confirmed! {bookingDetails.event_name} - TicketHouse";

        //        // Convert base64 string to byte array for attachment
        //        byte[] qrCodeBytes = Convert.FromBase64String(qrCodeBase64);

        //        // Create HTML email body
        //        string htmlBody = CreateBookingConfirmationEmailHtml(bookingDetails, message);

        //        // Send email with QR code as attachment
        //        string fileName = $"Ticket_{bookingDetails.booking_code}.png";

        //        // Ensure we're sending HTML content
        //        await _emailService.SendEmailWithAttachmentAsync(
        //            bookingDetails.email,
        //            subject,
        //            htmlBody,
        //            qrCodeBytes,
        //            fileName
        //        );

        //        _logger.LogInformation($"✓ Confirmation email sent to {bookingDetails.email} with QR attachment");
        //    }
        //    catch (FormatException fex)
        //    {
        //        _logger.LogError(fex, $"Invalid QR code base64 format for booking {bookingDetails.booking_code}");
        //        // Try to send email without QR code attachment as fallback
        //        await SendEmailWithoutQRAsync(bookingDetails, message);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Error sending confirmation email to {bookingDetails.email}");
        //        // Log but don't throw - booking is already confirmed
        //    }
        //}

        // Fallback method if QR code attachment fails
        private async Task SendEmailWithoutQRAsync(BookingDetailsResponse bookingDetails, string message)
        {
            try
            {
                string subject = $"Booking Confirmed! {bookingDetails.event_name} - TicketHouse";
                string htmlBody = await CreateBookingConfirmationEmailHtml(bookingDetails, message);

                await _emailService.SendEmailAsync(
                    bookingDetails.email,
                    subject,
                    htmlBody
                );

                _logger.LogInformation($"✓ Confirmation email sent to {bookingDetails.email} (without QR attachment)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending fallback email to {bookingDetails.email}");
            }
        }

        private async Task SendBookingConfirmationEmailAsync(BookingDetailsResponse bookingDetails, string qrCodeBase64, string message)
        {
            try
            {
                string subject = $"Booking Confirmed! {bookingDetails.event_name} - TicketHouse";

                // Convert base64 string to byte array for attachment
                byte[] qrCodeBytes = Convert.FromBase64String(qrCodeBase64);

                // Create HTML email body using template
                string htmlBody = await CreateBookingConfirmationEmailHtml(bookingDetails, message);

                // Send email with QR code as attachment
                string fileName = $"Ticket_{bookingDetails.booking_code}.png";

                await _emailService.SendEmailWithAttachmentAsync(
                    bookingDetails.email,
                    subject,
                    htmlBody,
                    qrCodeBytes,
                    fileName
                );

                _logger.LogInformation($"✓ Confirmation email sent to {bookingDetails.email} with QR attachment");
            }
            catch (FormatException fex)
            {
                _logger.LogError(fex, $"Invalid QR code base64 format for booking {bookingDetails.booking_code}");
                await SendEmailWithoutQRAsync(bookingDetails, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending confirmation email to {bookingDetails.email}");
            }
        }

        //        private string CreateBookingConfirmationEmailHtml(BookingDetailsResponse bookingDetails, string message)
        //        {
        //            // Format date and time properly
        //            string eventDate = bookingDetails.event_date.ToString("dddd, MMMM dd, yyyy");
        //            string customerName = $"{bookingDetails.first_name} {bookingDetails.last_name}";

        //            return $@"<!DOCTYPE html>
        //<html lang=""en"">
        //<head>
        //    <meta charset=""UTF-8"">
        //    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        //    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
        //    <title>Booking Confirmation - TicketHouse</title>
        //    <style type=""text/css"">
        //        /* Email-safe reset */
        //        body, table, td, p, a {{
        //            font-family: 'Segoe UI', 'Helvetica Neue', Helvetica, Arial, sans-serif;
        //            line-height: 1.6;
        //            margin: 0;
        //            padding: 0;
        //        }}

        //        /* Client-specific styles */
        //        .ExternalClass, .ReadMsgBody {{
        //            width: 100%;
        //            background-color: #f8f9fa;
        //        }}

        //        /* Main container */
        //        .email-container {{
        //            max-width: 600px;
        //            margin: 0 auto;
        //            background-color: #ffffff;
        //            border-radius: 16px;
        //            overflow: hidden;
        //            box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
        //        }}

        //        /* Header section */
        //        .email-header {{
        //            background: linear-gradient(135deg, #1a1c3c 0%, #2a3f6e 100%);
        //            padding: 40px 30px;
        //            text-align: center;
        //        }}

        //        .header-title {{
        //            color: #ffffff;
        //            font-size: 32px;
        //            font-weight: 700;
        //            margin: 0 0 10px 0;
        //            text-shadow: 0 2px 4px rgba(0,0,0,0.2);
        //        }}

        //        .header-subtitle {{
        //            color: #e0e7ff;
        //            font-size: 16px;
        //            margin: 0;
        //        }}

        //        .booking-badge {{
        //            display: inline-block;
        //            background-color: rgba(255, 255, 255, 0.2);
        //            padding: 8px 20px;
        //            border-radius: 50px;
        //            margin-top: 15px;
        //            font-weight: 600;
        //            color: #ffffff;
        //            font-size: 18px;
        //        }}

        //        /* Content section */
        //        .email-content {{
        //            padding: 40px 30px;
        //            background-color: #ffffff;
        //        }}

        //        /* Greeting */
        //        .greeting {{
        //            font-size: 20px;
        //            color: #1a1c3c;
        //            font-weight: 600;
        //            margin-bottom: 25px;
        //        }}

        //        /* Message box */
        //        .message-box {{
        //            background-color: #f0f4ff;
        //            padding: 25px;
        //            border-radius: 12px;
        //            margin-bottom: 30px;
        //            border-left: 4px solid #2a3f6e;
        //        }}

        //        .message-text {{
        //            font-size: 16px;
        //            color: #2d3748;
        //            margin: 0;
        //            white-space: pre-line;
        //        }}

        //        /* QR Code note */
        //        .qr-note {{
        //            background-color: #f8fafc;
        //            border: 2px dashed #2a3f6e;
        //            padding: 25px;
        //            border-radius: 12px;
        //            margin-bottom: 35px;
        //            text-align: center;
        //        }}

        //        .qr-note-title {{
        //            font-size: 20px;
        //            font-weight: 700;
        //            color: #1a1c3c;
        //            margin: 0 0 10px 0;
        //        }}

        //        .qr-note-text {{
        //            font-size: 15px;
        //            color: #4a5568;
        //            margin: 5px 0;
        //        }}

        //        .qr-note-highlight {{
        //            background-color: #2a3f6e;
        //            color: #ffffff;
        //            padding: 12px 20px;
        //            border-radius: 8px;
        //            font-weight: 600;
        //            font-size: 16px;
        //            margin-top: 15px;
        //            display: inline-block;
        //        }}

        //        /* Details section */
        //        .details-section {{
        //            margin: 35px 0;
        //        }}

        //        .section-title {{
        //            font-size: 20px;
        //            font-weight: 700;
        //            color: #1a1c3c;
        //            margin: 0 0 20px 0;
        //            padding-bottom: 10px;
        //            border-bottom: 3px solid #e2e8f0;
        //        }}

        //        /* Info grid */
        //        .info-grid {{
        //            display: grid;
        //            grid-template-columns: 1fr 1fr;
        //            gap: 20px;
        //            margin-bottom: 30px;
        //        }}

        //        @media screen and (max-width: 480px) {{
        //            .info-grid {{
        //                grid-template-columns: 1fr;
        //            }}
        //        }}

        //        .info-item {{
        //            margin-bottom: 5px;
        //        }}

        //        .info-label {{
        //            display: block;
        //            font-size: 13px;
        //            color: #718096;
        //            text-transform: uppercase;
        //            letter-spacing: 0.5px;
        //            margin-bottom: 4px;
        //        }}

        //        .info-value {{
        //            display: block;
        //            font-size: 16px;
        //            color: #1a202c;
        //            font-weight: 600;
        //        }}

        //        /* Amount box */
        //        .amount-box {{
        //            background: linear-gradient(135deg, #f8f9fa 0%, #edf2f7 100%);
        //            padding: 25px;
        //            border-radius: 12px;
        //            margin: 30px 0;
        //            text-align: center;
        //            border: 1px solid #cbd5e0;
        //        }}

        //        .amount-label {{
        //            font-size: 14px;
        //            color: #4a5568;
        //            text-transform: uppercase;
        //            letter-spacing: 1px;
        //        }}

        //        .amount-value {{
        //            font-size: 36px;
        //            font-weight: 800;
        //            color: #1a1c3c;
        //            line-height: 1.2;
        //            margin: 10px 0 5px;
        //        }}

        //        .amount-currency {{
        //            font-size: 20px;
        //            vertical-align: super;
        //        }}

        //        .amount-subtext {{
        //            font-size: 14px;
        //            color: #718096;
        //            margin: 0;
        //        }}

        //        /* Seat details table */
        //        .seat-table {{
        //            width: 100%;
        //            border-collapse: collapse;
        //            margin: 20px 0;
        //        }}

        //        .seat-table th {{
        //            text-align: left;
        //            padding: 12px 10px;
        //            background-color: #f7fafc;
        //            color: #2d3748;
        //            font-weight: 600;
        //            font-size: 14px;
        //            border-bottom: 2px solid #cbd5e0;
        //        }}

        //        .seat-table td {{
        //            padding: 12px 10px;
        //            border-bottom: 1px solid #e2e8f0;
        //            color: #4a5568;
        //        }}

        //        .seat-table tr:last-child td {{
        //            border-bottom: none;
        //        }}

        //        .seat-name {{
        //            font-weight: 600;
        //            color: #2d3748;
        //        }}

        //        .seat-price {{
        //            font-weight: 600;
        //            color: #1a1c3c;
        //        }}

        //        .text-right {{
        //            text-align: right;
        //        }}

        //        /* Footer */
        //        .email-footer {{
        //            background-color: #f8fafc;
        //            padding: 30px;
        //            text-align: center;
        //            border-top: 1px solid #e2e8f0;
        //        }}

        //        .footer-logo {{
        //            font-size: 24px;
        //            font-weight: 800;
        //            color: #1a1c3c;
        //            margin-bottom: 15px;
        //        }}

        //        .footer-text {{
        //            font-size: 14px;
        //            color: #718096;
        //            margin: 5px 0;
        //        }}

        //        .footer-links {{
        //            margin: 20px 0;
        //        }}

        //        .footer-links a {{
        //            color: #2a3f6e;
        //            text-decoration: none;
        //            margin: 0 10px;
        //            font-size: 13px;
        //        }}

        //        .social-icons {{
        //            margin: 20px 0;
        //        }}

        //        .social-icon {{
        //            display: inline-block;
        //            width: 32px;
        //            height: 32px;
        //            background-color: #e2e8f0;
        //            border-radius: 50%;
        //            margin: 0 5px;
        //            line-height: 32px;
        //            text-align: center;
        //        }}

        //        .copyright {{
        //            font-size: 12px;
        //            color: #a0aec0;
        //            margin-top: 20px;
        //        }}

        //        /* Utility classes */
        //        .highlight {{
        //            color: #2a3f6e;
        //            font-weight: 600;
        //        }}

        //        .text-success {{
        //            color: #38a169;
        //        }}

        //        .text-muted {{
        //            color: #718096;
        //        }}

        //        .divider {{
        //            height: 1px;
        //            background-color: #e2e8f0;
        //            margin: 25px 0;
        //        }}
        //    </style>
        //</head>
        //<body style=""margin: 0; padding: 20px; background-color: #f8f9fa; font-family: 'Segoe UI', 'Helvetica Neue', Helvetica, Arial, sans-serif;"">
        //    <center style=""width: 100%; table-layout: fixed;"">
        //        <div style=""max-width: 600px; margin: 0 auto;"">
        //            <!-- Main Container -->
        //            <div class=""email-container"" style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);"">

        //                <!-- Header -->
        //                <div class=""email-header"" style=""background: linear-gradient(135deg, #1a1c3c 0%, #2a3f6e 100%); padding: 40px 30px; text-align: center;"">
        //                    <h1 class=""header-title"" style=""color: #ffffff; font-size: 32px; font-weight: 700; margin: 0 0 10px 0; text-shadow: 0 2px 4px rgba(0,0,0,0.2);"">🎉 Booking Confirmed!</h1>
        //                    <p class=""header-subtitle"" style=""color: #e0e7ff; font-size: 16px; margin: 0;"">Your ticket is ready for the event</p>
        //                    <div class=""booking-badge"" style=""display: inline-block; background-color: rgba(255, 255, 255, 0.2); padding: 8px 20px; border-radius: 50px; margin-top: 15px; font-weight: 600; color: #ffffff; font-size: 18px;"">
        //                        #{bookingDetails.booking_code}
        //                    </div>
        //                </div>

        //                <!-- Content -->
        //                <div class=""email-content"" style=""padding: 40px 30px; background-color: #ffffff;"">

        //                    <!-- Greeting -->
        //                    <div class=""greeting"" style=""font-size: 20px; color: #1a1c3c; font-weight: 600; margin-bottom: 25px;"">
        //                        Dear <span class=""highlight"" style=""color: #2a3f6e; font-weight: 600;"">{customerName}</span>,
        //                    </div>

        //                    <!-- Message Box -->
        //                    <div class=""message-box"" style=""background-color: #f0f4ff; padding: 25px; border-radius: 12px; margin-bottom: 30px; border-left: 4px solid #2a3f6e;"">
        //                        <p class=""message-text"" style=""font-size: 16px; color: #2d3748; margin: 0; white-space: pre-line;"">{message}</p>
        //                    </div>

        //                    <!-- QR Code Note (since QR is attachment) -->
        //                    <div class=""qr-note"" style=""background-color: #f8fafc; border: 2px dashed #2a3f6e; padding: 25px; border-radius: 12px; margin-bottom: 35px; text-align: center;"">
        //                        <h3 class=""qr-note-title"" style=""font-size: 20px; font-weight: 700; color: #1a1c3c; margin: 0 0 10px 0;"">📎 QR Code Attached</h3>
        //                        <p class=""qr-note-text"" style=""font-size: 15px; color: #4a5568; margin: 5px 0;"">Your entry QR code is attached to this email as:</p>
        //                        <p class=""qr-note-text"" style=""font-size: 15px; color: #4a5568; margin: 5px 0;""><strong>Ticket_{bookingDetails.booking_code}.png</strong></p>
        //                        <div class=""qr-note-highlight"" style=""background-color: #2a3f6e; color: #ffffff; padding: 12px 20px; border-radius: 8px; font-weight: 600; font-size: 16px; margin-top: 15px; display: inline-block;"">
        //                            ⚡ Present this QR code at venue entry
        //                        </div>
        //                    </div>

        //                    <!-- Event Details Section -->
        //                    <div class=""details-section"" style=""margin: 35px 0;"">
        //                        <h2 class=""section-title"" style=""font-size: 20px; font-weight: 700; color: #1a1c3c; margin: 0 0 20px 0; padding-bottom: 10px; border-bottom: 3px solid #e2e8f0;"">Event Details</h2>

        //                        <div class=""info-grid"" style=""display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-bottom: 30px;"">
        //                            <div class=""info-item"" style=""margin-bottom: 5px;"">
        //                                <span class=""info-label"" style=""display: block; font-size: 13px; color: #718096; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px;"">Event Name</span>
        //                                <span class=""info-value"" style=""display: block; font-size: 16px; color: #1a202c; font-weight: 600;"">{bookingDetails.event_name}</span>
        //                            </div>
        //                            <div class=""info-item"" style=""margin-bottom: 5px;"">
        //                                <span class=""info-label"" style=""display: block; font-size: 13px; color: #718096; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px;"">Booking Code</span>
        //                                <span class=""info-value highlight"" style=""display: block; font-size: 16px; color: #2a3f6e; font-weight: 600;"">{bookingDetails.booking_code}</span>
        //                            </div>
        //                            <div class=""info-item"" style=""margin-bottom: 5px;"">
        //                                <span class=""info-label"" style=""display: block; font-size: 13px; color: #718096; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px;"">Date</span>
        //                                <span class=""info-value"" style=""display: block; font-size: 16px; color: #1a202c; font-weight: 600;"">{eventDate}</span>
        //                            </div>
        //                            <div class=""info-item"" style=""margin-bottom: 5px;"">
        //                                <span class=""info-label"" style=""display: block; font-size: 13px; color: #718096; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px;"">Time</span>
        //                                <span class=""info-value"" style=""display: block; font-size: 16px; color: #1a202c; font-weight: 600;"">{bookingDetails.start_time} - {bookingDetails.end_time}</span>
        //                            </div>
        //                            <div class=""info-item"" style=""margin-bottom: 5px;"">
        //                                <span class=""info-label"" style=""display: block; font-size: 13px; color: #718096; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px;"">Venue</span>
        //                                <span class=""info-value"" style=""display: block; font-size: 16px; color: #1a202c; font-weight: 600;"">{bookingDetails.location}</span>
        //                            </div>
        //                            <div class=""info-item"" style=""margin-bottom: 5px;"">
        //                                <span class=""info-label"" style=""display: block; font-size: 13px; color: #718096; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px;"">Status</span>
        //                                <span class=""info-value text-success"" style=""display: block; font-size: 16px; color: #38a169; font-weight: 600;"">{bookingDetails.status.ToUpper()}</span>
        //                            </div>
        //                        </div>
        //                    </div>

        //                    <!-- Amount Box -->
        //                    <div class=""amount-box"" style=""background: linear-gradient(135deg, #f8f9fa 0%, #edf2f7 100%); padding: 25px; border-radius: 12px; margin: 30px 0; text-align: center; border: 1px solid #cbd5e0;"">
        //                        <div class=""amount-label"" style=""font-size: 14px; color: #4a5568; text-transform: uppercase; letter-spacing: 1px;"">Total Amount Paid</div>
        //                        <div class=""amount-value"" style=""font-size: 36px; font-weight: 800; color: #1a1c3c; line-height: 1.2; margin: 10px 0 5px;"">
        //                            <span class=""amount-currency"" style=""font-size: 20px; vertical-align: super;"">₹</span>{bookingDetails.final_amount:N0}
        //                        </div>
        //                        <p class=""amount-subtext"" style=""font-size: 14px; color: #718096; margin: 0;"">(Includes all taxes & fees)</p>
        //                    </div>

        //                    <!-- Seat Details Section -->
        //                    <div class=""details-section"" style=""margin: 35px 0;"">
        //                        <h2 class=""section-title"" style=""font-size: 20px; font-weight: 700; color: #1a1c3c; margin: 0 0 20px 0; padding-bottom: 10px; border-bottom: 3px solid #e2e8f0;"">Ticket Details</h2>

        //                        <table class=""seat-table"" style=""width: 100%; border-collapse: collapse; margin: 20px 0;"">
        //                            <thead>
        //                                <tr>
        //                                    <th style=""text-align: left; padding: 12px 10px; background-color: #f7fafc; color: #2d3748; font-weight: 600; font-size: 14px; border-bottom: 2px solid #cbd5e0;"">Seat Type</th>
        //                                    <th style=""text-align: center; padding: 12px 10px; background-color: #f7fafc; color: #2d3748; font-weight: 600; font-size: 14px; border-bottom: 2px solid #cbd5e0;"">Quantity</th>
        //                                    <th style=""text-align: right; padding: 12px 10px; background-color: #f7fafc; color: #2d3748; font-weight: 600; font-size: 14px; border-bottom: 2px solid #cbd5e0;"">Price</th>
        //                                    <th style=""text-align: right; padding: 12px 10px; background-color: #f7fafc; color: #2d3748; font-weight: 600; font-size: 14px; border-bottom: 2px solid #cbd5e0;"">Subtotal</th>
        //                                </tr>
        //                            </thead>
        //                            <tbody>
        //                                {string.Join("", bookingDetails.BookingSeats.Select(bs => $@"
        //                                <tr>
        //                                    <td style=""padding: 12px 10px; border-bottom: 1px solid #e2e8f0; color: #4a5568;"">
        //                                        <span class=""seat-name"" style=""font-weight: 600; color: #2d3748;"">{bs.seat_name}</span>
        //                                    </td>
        //                                    <td style=""padding: 12px 10px; border-bottom: 1px solid #e2e8f0; color: #4a5568; text-align: center;"">
        //                                        {bs.quantity}
        //                                    </td>
        //                                    <td style=""padding: 12px 10px; border-bottom: 1px solid #e2e8f0; color: #4a5568; text-align: right;"">
        //                                        ₹{bs.price_per_seat:N0}
        //                                    </td>
        //                                    <td style=""padding: 12px 10px; border-bottom: 1px solid #e2e8f0; color: #4a5568; text-align: right;"">
        //                                        <span class=""seat-price"" style=""font-weight: 600; color: #1a1c3c;"">₹{bs.subtotal:N0}</span>
        //                                    </td>
        //                                </tr>
        //                                "))}
        //                            </tbody>
        //                        </table>
        //                    </div>

        //                    <!-- Important Notes -->
        //                    <div class=""details-section"" style=""margin: 35px 0;"">
        //                        <h2 class=""section-title"" style=""font-size: 20px; font-weight: 700; color: #1a1c3c; margin: 0 0 20px 0; padding-bottom: 10px; border-bottom: 3px solid #e2e8f0;"">Important Information</h2>
        //                        <ul style=""padding-left: 20px; margin: 0;"">
        //                            <li style=""margin-bottom: 10px; color: #4a5568;"">Please carry a valid ID proof along with the QR code</li>
        //                            <li style=""margin-bottom: 10px; color: #4a5568;"">Do not share your QR code with anyone</li>
        //                            <li style=""margin-bottom: 10px; color: #4a5568;"">Gate opens 60 minutes before the event start time</li>
        //                            <li style=""margin-bottom: 10px; color: #4a5568;"">This ticket is non-transferable</li>
        //                        </ul>
        //                    </div>

        //                </div>

        //                <!-- Footer -->
        //                <div class=""email-footer"" style=""background-color: #f8fafc; padding: 30px; text-align: center; border-top: 1px solid #e2e8f0;"">
        //                    <div class=""footer-logo"" style=""font-size: 24px; font-weight: 800; color: #1a1c3c; margin-bottom: 15px;"">TicketHouse</div>

        //                    <p class=""footer-text"" style=""font-size: 14px; color: #718096; margin: 5px 0;"">Thank you for choosing TicketHouse!</p>
        //                    <p class=""footer-text"" style=""font-size: 14px; color: #718096; margin: 5px 0;"">For any queries, contact support@tickethouse.in</p>

        //                    <div class=""footer-links"" style=""margin: 20px 0;"">
        //                        <a href=""#"" style=""color: #2a3f6e; text-decoration: none; margin: 0 10px; font-size: 13px;"">Terms</a>
        //                        <a href=""#"" style=""color: #2a3f6e; text-decoration: none; margin: 0 10px; font-size: 13px;"">Privacy</a>
        //                        <a href=""#"" style=""color: #2a3f6e; text-decoration: none; margin: 0 10px; font-size: 13px;"">FAQ</a>
        //                        <a href=""#"" style=""color: #2a3f6e; text-decoration: none; margin: 0 10px; font-size: 13px;"">Contact</a>
        //                    </div>

        //                    <div class=""copyright"" style=""font-size: 12px; color: #a0aec0; margin-top: 20px;"">
        //                        © {DateTime.Now.Year} TicketHouse. All rights reserved.
        //                    </div>
        //                </div>
        //            </div>
        //        </div>
        //    </center>
        //</body>
        //</html>";
        //        }

        private async Task<string> CreateBookingConfirmationEmailHtml(BookingDetailsResponse bookingDetails, string message)
        {
            try
            {
                // Get the path to the template file
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "ConfirmBookingEmail.html");

                // If running in development, try alternate path
                if (!File.Exists(templatePath))
                {
                    string currentDir = Directory.GetCurrentDirectory();
                    templatePath = Path.Combine(currentDir, "Templates", "ConfirmBookingEmail.html");

                    if (!File.Exists(templatePath))
                    {
                        templatePath = Path.Combine(currentDir, "..", "Templates", "ConfirmBookingEmail.html");
                        templatePath = Path.GetFullPath(templatePath);
                    }
                }

                if (!File.Exists(templatePath))
                {
                    _logger.LogError($"Email template not found at: {templatePath}");
                    throw new FileNotFoundException($"Email template not found. Searched paths: {templatePath}");
                }

                // Read the template
                string template = await File.ReadAllTextAsync(templatePath);

                // Format date
                string eventDate = bookingDetails.event_date.ToString("dddd, MMMM dd, yyyy");
                string eventTime = $"{bookingDetails.start_time} - {bookingDetails.end_time}";

                // Calculate subtotal total (sum of all seat subtotals)
                decimal subtotalTotal = bookingDetails.BookingSeats.Sum(bs => bs.subtotal);

                // Build seat details HTML - using "F2" format to show exact decimals without rounding
                string seatDetailsHtml = "";
                foreach (var bs in bookingDetails.BookingSeats)
                {
                    seatDetailsHtml += $@"
                <tr class='ticket-row'>
                    <td style='padding: 12px 8px;'>{bs.seat_name}</td>
                    <td style='padding: 12px 8px; text-align: center;'>{bs.quantity}</td>
                    <td style='padding: 12px 8px; text-align: right;'>₹{bs.price_per_seat:0.00}</td>
                    <td style='padding: 12px 8px; text-align: right; font-weight: 600;'>₹{bs.subtotal:0.00}</td>
                </tr>";
                }

                string geoMapUrl = bookingDetails.geo_map_url ?? "#"; // Fallback if not available

                // GENERATE COUPON SECTION HTML - CONDITIONAL
                string couponSectionHtml = "";

                // Check if coupon was applied (discount_amount > 0)
                if (bookingDetails.discount_amount > 0 && !string.IsNullOrEmpty(bookingDetails.coupon_code_applied))
                {
                    couponSectionHtml = $@"
                    <!-- Coupon Applied Section -->
                    <tr>
                        <td style=""padding: 4px 0 8px 0;"">
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                                <tr>
                                    <td style=""font-size: 14px; color: #10b981; padding-right: 20px;"">
                                        Coupon Applied (<span style=""font-weight: 600;"">{bookingDetails.coupon_code_applied}</span>):
                                    </td>
                                    <td style=""font-size: 14px; color: #10b981; text-align: right; font-weight: 500;"">
                                        - ₹{bookingDetails.discount_amount:0.00}
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
            
                    <!-- Optional spacer row for better visual separation -->
                    <tr>
                        <td style=""padding: 0 0 4px 0;"">
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                                <tr>
                                    <td style=""border-top: 1px dashed #eee;"" colspan=""2""></td>
                                </tr>
                            </table>
                        </td>
                    </tr>";
                }

                // Log the final value being used
                _logger.LogInformation($"Final GeoMapUrl being used in template: {geoMapUrl}");

                // Replace placeholders - using "0.00" format to show exact decimals without rounding off
                string htmlBody = template
                    .Replace("{{BookingCode}}", bookingDetails.booking_code)
                    .Replace("{{username}}", bookingDetails.first_name)
                    .Replace("{{EventName}}", bookingDetails.event_name)
                    .Replace("{{EventDate}}", eventDate)
                    .Replace("{{EventTime}}", eventTime)
                    .Replace("{{Venue}}", bookingDetails.location)
                    .Replace("{{GeoMapUrl}}", geoMapUrl)
                    .Replace("{{SeatDetails}}", seatDetailsHtml)
                    .Replace("{{SubtotalTotal}}", subtotalTotal.ToString("0.00"))  // New placeholder for subtotal total
                    .Replace("{{ConvenienceFee}}", bookingDetails.convenience_fee.ToString("0.00"))
                    .Replace("{{GSTAmount}}", bookingDetails.gst_amount.ToString("0.00"))
                    .Replace("{{CouponAppliedSection}}", couponSectionHtml)  // This will be empty if no coupon
                    .Replace("{{FinalAmount}}", bookingDetails.final_amount.ToString("0.00"))
                    .Replace("{{CurrentYear}}", DateTime.Now.Year.ToString());

                // Move CSS inline for better email client compatibility
                var result = PreMailer.Net.PreMailer.MoveCssInline(htmlBody);
                return result.Html;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating email HTML from template");
                throw;
            }
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
                                // Include coupon details
                                CouponCode = bookingDetails.coupon_code_applied,
                                DiscountAmount = bookingDetails.discount_amount,
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

        public async Task<CommonResponseModel<BookingDetailedResponse>> GetBookingDetailsByIdAsync(int bookingId)
        {
            var response = new CommonResponseModel<BookingDetailedResponse>();

            try
            {
                if (bookingId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid booking ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                var bookingDetails = await _bookingRepository.GetBookingDetailsByIdAsync(bookingId);

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

                // Log the exception
                // _logger.LogError(ex, "Error in GetBookingDetailsByIdAsync for BookingId: {BookingId}", bookingId);
            }

            return response;
        }

        public async Task<CommonResponseModel<EventSummaryResponse>> GetEventSummaryByEventIdAsync(int eventId)
        {
            var response = new CommonResponseModel<EventSummaryResponse>();

            try
            {
                if (eventId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid event ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                // Verify event exists
                var eventExists = await _eventDetailsRepository.GetEventByIdAsync(eventId);
                if (eventExists == null)
                {
                    response.Status = "Failure";
                    response.Message = "Event not found";
                    response.ErrorCode = "404";
                    return response;
                }

                var summary = await _bookingRepository.GetEventSummaryByEventIdAsync(eventId);

                if (summary != null)
                {
                    response.Status = "Success";
                    response.Message = "Event summary fetched successfully";
                    response.ErrorCode = "0";
                    response.Data = summary;
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "No data found for this event";
                    response.ErrorCode = "404";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error fetching event summary: {ex.Message}";
                response.ErrorCode = "1";

                // Log the exception
                // _logger.LogError(ex, "Error in GetEventSummaryByEventIdAsync for EventId: {EventId}", eventId);
            }

            return response;
        }

        public async Task<PagedBookingHistoryResponse> GetPagedBookingHistoryByUserIdAsync(BookingHistoryRequest request)
        {
            try
            {
                if (request.UserId == Guid.Empty)
                {
                    return new PagedBookingHistoryResponse
                    {
                        Status = "Failure",
                        Message = "Valid user ID is required",
                        ErrorCode = "400",
                        Data = new List<BookingHistoryResponse>(),
                        TotalCount = 0,
                        TotalPages = 0,
                        CurrentPage = request.PageNumber,
                        PageSize = request.PageSize
                    };
                }

                return await _bookingRepository.GetPagedBookingHistoryByUserIdAsync(request);
            }
            catch (Exception ex)
            {
                return new PagedBookingHistoryResponse
                {
                    Status = "Failure",
                    Message = $"Error retrieving booking history: {ex.Message}",
                    ErrorCode = "1",
                    Data = new List<BookingHistoryResponse>(),
                    TotalCount = 0,
                    TotalPages = 0,
                    CurrentPage = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
        }

        public async Task<CommonResponseModel<EventBookingHistoryResponse>> GetEventBookingHistoryAsync(int eventId)
        {
            var response = new CommonResponseModel<EventBookingHistoryResponse>();

            try
            {
                // Validate event ID
                if (eventId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid event ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                // Check if event exists
                var eventExists = await _eventDetailsRepository.GetEventByIdAsync(eventId);
                if (eventExists == null)
                {
                    response.Status = "Failure";
                    response.Message = "Event not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Get booking history
                var bookingHistory = await _bookingRepository.GetEventBookingHistoryAsync(eventId);

                if (bookingHistory != null && bookingHistory.bookings.Any())
                {
                    response.Status = "Success";
                    response.Message = "Event booking history retrieved successfully";
                    response.ErrorCode = "0";
                    response.Data = bookingHistory;
                }
                else
                {
                    response.Status = "Success";
                    response.Message = "No bookings found for this event";
                    response.ErrorCode = "0";
                    response.Data = new EventBookingHistoryResponse
                    {
                        event_id = eventId,
                        event_name = eventExists.event_name,
                        event_date = eventExists.event_date,
                        start_time = eventExists.start_time,
                        end_time = eventExists.end_time,
                        location = eventExists.location,
                        bookings = new System.Collections.Generic.List<EventBookingDetail>(),
                        summary = new EventBookingSummary
                        {
                            bookings_by_status = new System.Collections.Generic.Dictionary<string, int>(),
                            bookings_by_payment_status = new System.Collections.Generic.Dictionary<string, int>()
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving booking history for EventId: {EventId}", eventId);

                response.Status = "Failure";
                response.Message = $"Error retrieving booking history: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<TicketScanHistoryByEventResponse>> GetTicketScanHistoryByEventIdAsync(int eventId, int pageNumber = 1, int pageSize = 10)
        {
            var response = new CommonResponseModel<TicketScanHistoryByEventResponse>();

            try
            {
                // Validate inputs
                if (eventId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid event ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100; // Max limit

                // Check if event exists
                var eventExists = await _eventDetailsRepository.GetEventByIdAsync(eventId);
                if (eventExists == null)
                {
                    response.Status = "Failure";
                    response.Message = "Event not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Get scan history
                var scanHistory = await _bookingRepository.GetTicketScanHistoryByEventIdAsync(eventId, pageNumber, pageSize);

                if (scanHistory != null)
                {
                    response.Status = "Success";
                    response.Message = "Ticket scan history retrieved successfully";
                    response.ErrorCode = "0";
                    response.Data = scanHistory;
                }
                else
                {
                    response.Status = "Success";
                    response.Message = "No scan history found for this event";
                    response.ErrorCode = "0";
                    response.Data = new TicketScanHistoryByEventResponse
                    {
                        EventDetails = new EventDetails
                        {
                            event_id = eventExists.event_id,
                            event_name = eventExists.event_name,
                            event_date = eventExists.event_date,
                            start_time = eventExists.start_time,
                            end_time = eventExists.end_time,
                            location = eventExists.location
                        },
                        ScanHistory = new PaginatedScanHistory
                        {
                            current_page = pageNumber,
                            page_size = pageSize,
                            total_pages = 0,
                            total_records = 0,
                            data = new List<TicketScanHistoryDetail>()
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving ticket scan history for EventId: {eventId}");

                response.Status = "Failure";
                response.Message = $"Error retrieving ticket scan history: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<byte[]> ExportEventBookingDetailsToExcelAsync(int eventId)
        {
            try
            {
                // Fetch the data
                var bookingData = await _bookingRepository.GetEventBookingDetailsForExportAsync(eventId);
                var bookingList = bookingData.ToList();

                if (!bookingList.Any())
                {
                    throw new Exception("No confirmed bookings found for this event");
                }

                // Create Excel file using ClosedXML
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Event Bookings");

                    // Get event name for the title
                    var eventName = bookingList.FirstOrDefault()?.EventName ?? "Event";

                    // Title
                    worksheet.Cell("A1").Value = $"Booking Details - {eventName}";
                    worksheet.Range("A1:R1").Merge();
                    worksheet.Cell("A1").Style.Font.Bold = true;
                    worksheet.Cell("A1").Style.Font.FontSize = 14;
                    worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Add headers
                    string[] headers = {
                "Sr No", "Event Name", "Event Date", "Event Time", "Location",
                "Booking Confirmed", "User Name", "Email", "Mobile",
                "Seat Details", "Total Seats", "Scanned", "Remaining",
                "Coupon Applied", "Discount (₹)", "Total Amount (₹)", "Payment Status", "Booking Date"
            };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cell(3, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }

                    // Add data
                    int row = 4;
                    foreach (var booking in bookingList)
                    {
                        worksheet.Cell(row, 1).Value = booking.SrNo;
                        worksheet.Cell(row, 2).Value = booking.EventName;
                        worksheet.Cell(row, 3).Value = booking.EventDate.ToString("dd-MMM-yyyy");
                        worksheet.Cell(row, 4).Value = booking.EventTime;
                        worksheet.Cell(row, 5).Value = booking.Location;
                        worksheet.Cell(row, 6).Value = booking.BookingIsConfirmed;
                        worksheet.Cell(row, 7).Value = booking.UserName;
                        worksheet.Cell(row, 8).Value = booking.Email;
                        worksheet.Cell(row, 9).Value = booking.Mobile;
                        worksheet.Cell(row, 10).Value = booking.SeatDetails;
                        worksheet.Cell(row, 11).Value = booking.TotalSeatsBooked;
                        worksheet.Cell(row, 12).Value = booking.ScannedCount;
                        worksheet.Cell(row, 13).Value = booking.RemainingCount;
                        worksheet.Cell(row, 14).Value = booking.CouponApplied;
                        worksheet.Cell(row, 15).Value = booking.DiscountAmount;
                        worksheet.Cell(row, 16).Value = booking.TotalAmount;
                        worksheet.Cell(row, 17).Value = booking.PaymentStatus;
                        worksheet.Cell(row, 18).Value = booking.BookingDate.ToString("dd-MMM-yyyy HH:mm");

                        // Format number columns
                        worksheet.Cell(row, 11).Style.NumberFormat.Format = "#,##0";
                        worksheet.Cell(row, 12).Style.NumberFormat.Format = "#,##0";
                        worksheet.Cell(row, 13).Style.NumberFormat.Format = "#,##0";
                        worksheet.Cell(row, 15).Style.NumberFormat.Format = "#,##0.00";
                        worksheet.Cell(row, 16).Style.NumberFormat.Format = "#,##0.00";

                        // Color coding for scan status
                        if (booking.RemainingCount == 0)
                        {
                            worksheet.Cell(row, 13).Style.Fill.BackgroundColor = XLColor.LightGreen;
                        }
                        else if (booking.ScannedCount > 0)
                        {
                            worksheet.Cell(row, 13).Style.Fill.BackgroundColor = XLColor.LightYellow;
                        }

                        row++;
                    }

                    // Add summary row
                    row++;
                    worksheet.Cell(row, 1).Value = "SUMMARY";
                    worksheet.Cell(row, 1).Style.Font.Bold = true;
                    worksheet.Range(row, 1, row, 5).Merge();

                    worksheet.Cell(row, 10).Value = "Total Bookings:";
                    worksheet.Cell(row, 11).Value = bookingList.Count;

                    worksheet.Cell(row, 12).Value = "Total Seats:";
                    worksheet.Cell(row, 13).Value = bookingList.Sum(b => b.TotalSeatsBooked);

                    worksheet.Cell(row, 14).Value = "Total Scanned:";
                    worksheet.Cell(row, 15).Value = bookingList.Sum(b => b.ScannedCount);

                    worksheet.Cell(row, 16).Value = "Total Revenue:";
                    worksheet.Cell(row, 17).Value = bookingList.Sum(b => b.TotalAmount);

                    // Make summary row bold
                    worksheet.Range(row, 10, row, 17).Style.Font.Bold = true;

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Set column widths for better readability
                    worksheet.Column(10).Width = 50; // Seat Details column wider
                    worksheet.Column(2).Width = 30; // Event Name
                    worksheet.Column(7).Width = 25; // User Name
                    worksheet.Column(8).Width = 30; // Email

                    // Save to memory stream
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        return stream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error exporting event booking details for EventId: {eventId}");
                throw;
            }
        }
    }
}
