using DAL.Repository;
using DAL.Utilities;
using MODEL.Configuration;
using MODEL.Entities;
using MODEL.Request;
using MODEL.Response;
using Razorpay.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BAL.Services
{
    public interface IPaymentService
    {
        Task<CommonResponseModel<PaymentOrderResponse>> CreatePaymentOrderAsync(CreatePaymentOrderRequest request, string userEmail);
        Task<CommonResponseModel<PaymentVerificationResponse>> VerifyPaymentAsync(VerifyPaymentRequest request, string userEmail);
        Task<CommonResponseModel<PaymentStatusResponse>> GetPaymentStatusAsync(int bookingId, string userEmail);
        //Task<CommonResponseModel<RefundResponse>> ProcessRefundAsync(PaymentRefundRequest request, string adminEmail);
        Task<CommonResponseModel<bool>> UpdatePaymentStatusAsync(int bookingId, string paymentStatus, string updatedBy);
        Task<CommonResponseModel<PaymentOrderResponse>> CreateBookingWithPaymentAsync(CreateBookingWithPaymentRequest request, string userEmail);
    }
    public class PaymentService: IPaymentService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventDetailsRepository _eventDetailsRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ICouponRepository _couponRepository;
        private readonly THConfiguration _configuration;
        private readonly RazorpayClient _razorpayClient;

        public PaymentService(
            IBookingRepository bookingRepository,
            IUserRepository userRepository,
            IPaymentRepository paymentRepository,
            THConfiguration configuration,
            IEventDetailsRepository eventDetailsRepository,
            ICouponRepository couponRepository)
        {
            _bookingRepository = bookingRepository;
            _userRepository = userRepository;
            _paymentRepository = paymentRepository;
            _configuration = configuration;

            // Initialize Razorpay client
            _razorpayClient = new RazorpayClient(
                _configuration.Razorpay.KeyId,
                _configuration.Razorpay.KeySecret);
            _eventDetailsRepository = eventDetailsRepository;
            _couponRepository = couponRepository;
        }

        //public async Task<CommonResponseModel<PaymentOrderResponse>> CreatePaymentOrderAsync(CreatePaymentOrderRequest request, string userEmail)
        //{
        //    var response = new CommonResponseModel<PaymentOrderResponse>();

        //    try
        //    {
        //        // Validate booking
        //        var booking = await _bookingRepository.GetBookingByIdAsync(request.BookingId);
        //        if (booking == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Booking not found";
        //            response.ErrorCode = "404";
        //            return response;
        //        }

        //        // Get user details
        //        var user = await _userRepository.GetUserByEmail(userEmail);
        //        if (user == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "User not found";
        //            response.ErrorCode = "404";
        //            return response;
        //        }

        //        // Ensure booking belongs to the user
        //        if (booking.user_id != user.user_id)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Unauthorized access to booking";
        //            response.ErrorCode = "403";
        //            return response;
        //        }

        //        // Validate amount
        //        if (booking.total_amount != request.Amount)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Amount mismatch";
        //            response.ErrorCode = "400";
        //            return response;
        //        }

        //        // Create Razorpay order
        //        //var options = new Dictionary<string, object>
        //        //{
        //        //    { "amount", Convert.ToInt32(request.Amount * 100) }, // Convert to paise
        //        //    { "currency", request.Currency },
        //        //    { "receipt", $"receipt_{booking.booking_code}" },
        //        //    { "notes", request.Notes ?? new Dictionary<string, string>() }
        //        //};

        //        // IMPORTANT: Use final_amount for Razorpay payment (not total_amount)
        //        if (booking.final_amount != request.Amount)
        //        {
        //            response.Status = "Failure";
        //            response.Message = $"Amount mismatch. Expected: {booking.final_amount}, Received: {request.Amount}";
        //            response.ErrorCode = "400";
        //            return response;
        //        }

        //        // Create Razorpay order with final_amount
        //        var options = new Dictionary<string, object>
        //        {
        //            { "amount", Convert.ToInt32(booking.final_amount * 100) }, // Convert to paise
        //            { "currency", booking.currency },
        //            { "receipt", $"receipt_{booking.booking_code}" },
        //            { "notes", request.Notes ?? new Dictionary<string, string>() }
        //        };

        //        var razorpayOrder = _razorpayClient.Order.Create(options);

        //        // Update booking with Razorpay order ID
        //        await _paymentRepository.UpdateBookingPaymentDetailsAsync(booking.booking_id,
        //            razorpayOrder["id"].ToString(),
        //            null, null, null, "created", user.user_id.ToString());

        //        // Create response
        //        var orderResponse = new PaymentOrderResponse
        //        {
        //            OrderId = razorpayOrder["id"].ToString(),
        //            KeyId = _configuration.Razorpay.KeyId,
        //            Amount = request.Amount,
        //            Currency = request.Currency,
        //            CompanyName = _configuration.Razorpay.CompanyName,
        //            CustomerName = $"{user.first_name} {user.last_name}",
        //            CustomerEmail = userEmail,
        //            Notes = request.Notes ?? new Dictionary<string, string>()
        //        };

        //        response.Status = "Success";
        //        response.Message = "Payment order created successfully";
        //        response.ErrorCode = "0";
        //        response.Data = orderResponse;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Status = "Failure";
        //        response.Message = $"Error creating payment order: {ex.Message}";
        //        response.ErrorCode = "1";
        //    }

        //    return response;
        //}

        public async Task<CommonResponseModel<PaymentOrderResponse>> CreatePaymentOrderAsync(CreatePaymentOrderRequest request, string userEmail)
        {
            var response = new CommonResponseModel<PaymentOrderResponse>();

            try
            {
                // Validate booking
                var booking = await _bookingRepository.GetBookingByIdAsync(request.BookingId);
                if (booking == null)
                {
                    response.Status = "Failure";
                    response.Message = "Booking not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Get user details
                var user = await _userRepository.GetUserByEmail(userEmail);
                if (user == null)
                {
                    response.Status = "Failure";
                    response.Message = "User not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Ensure booking belongs to the user
                if (booking.user_id != user.user_id)
                {
                    response.Status = "Failure";
                    response.Message = "Unauthorized access to booking";
                    response.ErrorCode = "403";
                    return response;
                }

                // FIXED: Validate amount using final_amount instead of total_amount
                if (booking.final_amount != request.Amount)
                {
                    response.Status = "Failure";
                    response.Message = $"Amount mismatch. Expected final amount: {booking.final_amount}, Received: {request.Amount}";
                    response.ErrorCode = "400";
                    return response;
                }

                // Create Razorpay order using final_amount
                var options = new Dictionary<string, object>
                {
                    { "amount", Convert.ToInt32(booking.final_amount * 100) }, // Convert to paise
                    { "currency", booking.currency },
                    { "receipt", $"receipt_{booking.booking_code}" },
                    { "notes", request.Notes ?? new Dictionary<string, string>() }
                };

                var razorpayOrder = _razorpayClient.Order.Create(options);

                // Update booking with Razorpay order ID
                await _paymentRepository.UpdateBookingPaymentDetailsAsync(booking.booking_id,
                    razorpayOrder["id"].ToString(),
                    null, null, null, "created", user.user_id.ToString());

                // Create response
                var orderResponse = new PaymentOrderResponse
                {
                    OrderId = razorpayOrder["id"].ToString(),
                    KeyId = _configuration.Razorpay.KeyId,
                    Amount = booking.final_amount, // Use final amount
                    Currency = booking.currency,
                    CompanyName = _configuration.Razorpay.CompanyName,
                    CustomerName = $"{user.first_name} {user.last_name}",
                    CustomerEmail = userEmail,
                    Notes = request.Notes ?? new Dictionary<string, string>(),
                    // Add booking info for reference
                    BookingId = booking.booking_id,
                    BookingCode = booking.booking_code,
                    BaseAmount = booking.total_amount,
                    ConvenienceFee = booking.convenience_fee,
                    GstAmount = booking.gst_amount,
                    FinalAmount = booking.final_amount
                };

                response.Status = "Success";
                response.Message = "Payment order created successfully";
                response.ErrorCode = "0";
                response.Data = orderResponse;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error creating payment order: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }


        //public async Task<CommonResponseModel<PaymentVerificationResponse>> VerifyPaymentAsync(VerifyPaymentRequest request, string userEmail)
        //{
        //    var response = new CommonResponseModel<PaymentVerificationResponse>();

        //    try
        //    {
        //        // Validate booking
        //        var booking = await _bookingRepository.GetBookingByIdAsync(request.BookingId);
        //        if (booking == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Booking not found";
        //            response.ErrorCode = "404";
        //            return response;
        //        }

        //        // Get user details
        //        var user = await _userRepository.GetUserByEmail(userEmail);
        //        if (user == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "User not found";
        //            response.ErrorCode = "404";
        //            return response;
        //        }

        //        // Verify payment signature manually
        //        var isValidSignature = VerifyRazorpaySignature(
        //            request.RazorpayOrderId,
        //            request.RazorpayPaymentId,
        //            request.RazorpaySignature
        //        );

        //        if (!isValidSignature)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Payment signature verification failed";
        //            response.ErrorCode = "400";
        //            return response;
        //        }

        //        // Get payment details from Razorpay
        //        var payment = _razorpayClient.Payment.Fetch(request.RazorpayPaymentId);

        //        // Parse payment method details
        //        string paymentMethod = payment["method"].ToString();
        //        string cardLast4 = string.Empty;
        //        string cardNetwork = string.Empty;
        //        string bankName = string.Empty;
        //        string wallet = string.Empty;
        //        string vpa = string.Empty;

        //        // Safely extract payment details
        //        if (payment["method"].ToString() == "card")
        //        {
        //            var cardData = payment["card"];
        //            cardLast4 = cardData?["last4"]?.ToString() ?? "";
        //            cardNetwork = cardData?["network"]?.ToString() ?? "";
        //        }
        //        else if (payment["method"].ToString() == "netbanking")
        //        {
        //            bankName = payment["bank"]?.ToString() ?? "";
        //        }
        //        else if (payment["method"].ToString() == "wallet")
        //        {
        //            wallet = payment["wallet"]?.ToString() ?? "";
        //        }
        //        else if (payment["method"].ToString() == "upi")
        //        {
        //            vpa = payment["vpa"]?.ToString() ?? "";
        //        }

        //        // Update booking payment status
        //        var updatedRows = await _paymentRepository.UpdateBookingPaymentDetailsAsync(
        //            booking.booking_id,
        //            request.RazorpayOrderId,
        //            request.RazorpayPaymentId,
        //            request.RazorpaySignature,
        //            paymentMethod,
        //            payment["status"].ToString().ToLower(),
        //            user.user_id.ToString());

        //        if (updatedRows > 0)
        //        {
        //            // Only confirm booking if payment is successful
        //            if (payment["status"].ToString().ToLower() == "captured")
        //            {
        //                await ConfirmBookingAfterPayment(booking.booking_id, user.user_id.ToString());
        //            }

        //            // Create payment history record
        //            var paymentHistory = new PaymentHistoryModel
        //            {
        //                booking_id = booking.booking_id,
        //                razorpay_order_id = request.RazorpayOrderId,
        //                razorpay_payment_id = request.RazorpayPaymentId,
        //                razorpay_signature = request.RazorpaySignature,
        //                amount = booking.total_amount,
        //                currency = booking.currency,
        //                payment_method = paymentMethod,
        //                payment_status = payment["status"].ToString().ToLower(),
        //                payment_date = DateTime.UtcNow,
        //                card_last4 = cardLast4,
        //                card_network = cardNetwork,
        //                bank_name = bankName,
        //                wallet = wallet,
        //                vpa = vpa,
        //                razorpay_notes = booking.razorpay_notes,
        //                created_by = user.user_id.ToString(),
        //                updated_by = user.user_id.ToString()
        //            };

        //            await _paymentRepository.CreatePaymentHistoryAsync(paymentHistory);

        //            // FIXED: Convert payment details to dictionary - different approach
        //            var paymentDetails = new Dictionary<string, string>();

        //            // Convert payment object to string and parse as JSON if needed
        //            // Or access properties directly based on Razorpay API documentation
        //            // For now, add common properties
        //            var paymentProperties = new List<string> { "id", "amount", "currency", "status", "method", "description" };

        //            foreach (var prop in paymentProperties)
        //            {
        //                if (payment[prop] != null)
        //                {
        //                    paymentDetails[prop] = payment[prop].ToString();
        //                }
        //            }

        //            // Add specific details based on payment method
        //            if (!string.IsNullOrEmpty(cardLast4)) paymentDetails["card_last4"] = cardLast4;
        //            if (!string.IsNullOrEmpty(cardNetwork)) paymentDetails["card_network"] = cardNetwork;
        //            if (!string.IsNullOrEmpty(bankName)) paymentDetails["bank_name"] = bankName;
        //            if (!string.IsNullOrEmpty(wallet)) paymentDetails["wallet"] = wallet;
        //            if (!string.IsNullOrEmpty(vpa)) paymentDetails["vpa"] = vpa;

        //            // Create verification response
        //            var verificationResponse = new PaymentVerificationResponse
        //            {
        //                IsSuccess = true,
        //                Message = "Payment verified successfully",
        //                PaymentId = request.RazorpayPaymentId,
        //                OrderId = request.RazorpayOrderId,
        //                PaymentMethod = paymentMethod,
        //                CardLast4 = cardLast4,
        //                CardNetwork = cardNetwork,
        //                BankName = bankName,
        //                Wallet = wallet,
        //                VPA = vpa,
        //                PaymentStatus = payment["status"].ToString(),
        //                PaymentDate = DateTime.UtcNow,
        //                PaymentDetails = paymentDetails
        //            };

        //            response.Status = "Success";
        //            response.Message = "Payment verified successfully";
        //            response.ErrorCode = "0";
        //            response.Data = verificationResponse;
        //        }
        //        else
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Failed to update payment details";
        //            response.ErrorCode = "1";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Status = "Failure";
        //        response.Message = $"Error verifying payment: {ex.Message}";
        //        response.ErrorCode = "1";
        //    }

        //    return response;
        //}

        public async Task<CommonResponseModel<PaymentVerificationResponse>> VerifyPaymentAsync(VerifyPaymentRequest request, string userEmail)
        {
            var response = new CommonResponseModel<PaymentVerificationResponse>();

            try
            {
                // Validate booking
                var booking = await _bookingRepository.GetBookingByIdAsync(request.BookingId);
                if (booking == null)
                {
                    response.Status = "Failure";
                    response.Message = "Booking not found";
                    response.ErrorCode = "404";
                    return response;
                }

                Console.WriteLine($"Verifying payment for booking {request.BookingId}. Current status: {booking.status}, Payment status: {booking.payment_status}");

                // Verify payment signature
                var isValidSignature = VerifyRazorpaySignature(
                    request.RazorpayOrderId,
                    request.RazorpayPaymentId,
                    request.RazorpaySignature
                );

                if (!isValidSignature)
                {
                    response.Status = "Failure";
                    response.Message = "Payment signature verification failed";
                    response.ErrorCode = "400";
                    return response;
                }

                // Get payment details from Razorpay
                var payment = _razorpayClient.Payment.Fetch(request.RazorpayPaymentId);
                var paymentStatus = payment["status"].ToString().ToLower();

                Console.WriteLine($"Razorpay payment status: {paymentStatus}");

                // Parse payment method details
                string paymentMethod = payment["method"].ToString();
                string cardLast4 = string.Empty;
                string cardNetwork = string.Empty;
                string bankName = string.Empty;
                string wallet = string.Empty;
                string vpa = string.Empty;

                // Safely extract payment details
                if (payment["method"].ToString() == "card")
                {
                    var cardData = payment["card"];
                    cardLast4 = cardData?["last4"]?.ToString() ?? "";
                    cardNetwork = cardData?["network"]?.ToString() ?? "";
                }
                else if (payment["method"].ToString() == "netbanking")
                {
                    bankName = payment["bank"]?.ToString() ?? "";
                }
                else if (payment["method"].ToString() == "wallet")
                {
                    wallet = payment["wallet"]?.ToString() ?? "";
                }
                else if (payment["method"].ToString() == "upi")
                {
                    vpa = payment["vpa"]?.ToString() ?? "";
                }

                // Update booking payment details (but NOT the status to confirmed yet)
                var updatedRows = await _paymentRepository.UpdateBookingPaymentDetailsAsync(
                    booking.booking_id,
                    request.RazorpayOrderId,
                    request.RazorpayPaymentId,
                    request.RazorpaySignature,
                    paymentMethod,
                    paymentStatus,
                    booking.user_id.ToString());

                Console.WriteLine($"Updated payment details, rows affected: {updatedRows}");

                if (updatedRows > 0)
                {
                    // CRITICAL: Only confirm booking and DEDUCT SEATS if payment is SUCCESSFUL (captured)
                    // AND booking is not already confirmed
                    if (paymentStatus == "captured" && booking.status?.ToLower() != "confirmed")
                    {
                        Console.WriteLine($"Payment captured for booking {booking.booking_id}, calling ConfirmBookingAfterPayment");

                        // This method will deduct the seats and set status to confirmed
                        await ConfirmBookingAfterPayment(booking.booking_id, booking.user_id.ToString());

                        response.Status = "Success";
                        response.Message = "Payment verified and booking confirmed successfully";
                    }
                    else if (paymentStatus == "failed")
                    {
                        // Payment failed - update booking status to failed
                        await _bookingRepository.UpdateBookingStatusAsync(
                            booking.booking_id,
                            "payment_failed",
                            booking.user_id.ToString());

                        response.Status = "Failure";
                        response.Message = "Payment failed. Booking not confirmed.";
                        response.ErrorCode = "400";
                    }
                    else
                    {
                        // Other payment statuses (authorized, etc.) or already confirmed
                        if (booking.status?.ToLower() != "confirmed")
                        {
                            await _bookingRepository.UpdateBookingStatusAsync(
                                booking.booking_id,
                                "payment_pending",
                                booking.user_id.ToString());
                        }

                        response.Status = "Success";
                        response.Message = booking.status?.ToLower() == "confirmed"
                            ? "Booking already confirmed"
                            : "Payment verified. Booking pending confirmation.";
                    }

                    // Create payment history record (always create regardless of status)
                    var paymentHistory = new PaymentHistoryModel
                    {
                        booking_id = booking.booking_id,
                        razorpay_order_id = request.RazorpayOrderId,
                        razorpay_payment_id = request.RazorpayPaymentId,
                        razorpay_signature = request.RazorpaySignature,
                        amount = booking.total_amount,
                        currency = booking.currency,
                        payment_method = paymentMethod,
                        payment_status = paymentStatus,
                        payment_date = DateTime.UtcNow,
                        card_last4 = cardLast4,
                        card_network = cardNetwork,
                        bank_name = bankName,
                        wallet = wallet,
                        vpa = vpa,
                        razorpay_notes = booking.razorpay_notes,
                        created_by = booking.user_id.ToString(),
                        updated_by = booking.user_id.ToString()
                    };

                    await _paymentRepository.CreatePaymentHistoryAsync(paymentHistory);

                    // Create verification response
                    var verificationResponse = new PaymentVerificationResponse
                    {
                        IsSuccess = paymentStatus == "captured",
                        Message = paymentStatus == "captured" ? "Payment verified successfully" : $"Payment {paymentStatus}",
                        PaymentId = request.RazorpayPaymentId,
                        OrderId = request.RazorpayOrderId,
                        PaymentMethod = paymentMethod,
                        CardLast4 = cardLast4,
                        CardNetwork = cardNetwork,
                        BankName = bankName,
                        Wallet = wallet,
                        VPA = vpa,
                        PaymentStatus = paymentStatus,
                        PaymentDate = DateTime.UtcNow,
                        PaymentDetails = new Dictionary<string, string>
                {
                    { "id", payment["id"].ToString() },
                    { "amount", payment["amount"].ToString() },
                    { "currency", payment["currency"].ToString() },
                    { "status", paymentStatus },
                    { "method", paymentMethod }
                }
                    };

                    response.Status = paymentStatus == "captured" ? "Success" : "Failure";
                    response.Message = verificationResponse.Message;
                    response.ErrorCode = paymentStatus == "captured" ? "0" : "400";
                    response.Data = verificationResponse;
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to update payment details";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying payment: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                response.Status = "Failure";
                response.Message = $"Error verifying payment: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        //-------correct before seats deduction logic when failed payment
        //public async Task<CommonResponseModel<PaymentVerificationResponse>> VerifyPaymentAsync(VerifyPaymentRequest request, string userEmail)
        //{
        //    var response = new CommonResponseModel<PaymentVerificationResponse>();

        //    try
        //    {
        //        // Validate booking
        //        var booking = await _bookingRepository.GetBookingByIdAsync(request.BookingId);
        //        if (booking == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Booking not found";
        //            response.ErrorCode = "404";
        //            return response;
        //        }

        //        // Verify payment signature
        //        var isValidSignature = VerifyRazorpaySignature(
        //            request.RazorpayOrderId,
        //            request.RazorpayPaymentId,
        //            request.RazorpaySignature
        //        );

        //        if (!isValidSignature)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Payment signature verification failed";
        //            response.ErrorCode = "400";
        //            return response;
        //        }

        //        // Get payment details from Razorpay
        //        var payment = _razorpayClient.Payment.Fetch(request.RazorpayPaymentId);
        //        var paymentStatus = payment["status"].ToString().ToLower();

        //        // Update booking payment status
        //        var updatedRows = await _paymentRepository.UpdateBookingPaymentDetailsAsync(
        //            booking.booking_id,
        //            request.RazorpayOrderId,
        //            request.RazorpayPaymentId,
        //            request.RazorpaySignature,
        //            payment["method"].ToString(),
        //            paymentStatus,
        //            booking.user_id.ToString());

        //        if (updatedRows > 0)
        //        {
        //            // CRITICAL: Only confirm booking if payment is SUCCESSFUL
        //            if (paymentStatus == "captured")
        //            {
        //                // Confirm the booking
        //                await ConfirmBookingAfterPayment(booking.booking_id, booking.user_id.ToString());

        //                response.Status = "Success";
        //                response.Message = "Payment verified and booking confirmed successfully";
        //            }
        //            else if (paymentStatus == "failed")
        //            {
        //                // Payment failed - update booking status to failed
        //                await _bookingRepository.UpdateBookingStatusAsync(
        //                    booking.booking_id,
        //                    "payment_failed",
        //                    booking.user_id.ToString());

        //                // Release reserved seats
        //                await ReleaseSeatsForFailedPayment(booking.booking_id);

        //                response.Status = "Failure";
        //                response.Message = "Payment failed. Booking not confirmed.";
        //                response.ErrorCode = "400";
        //            }
        //            else
        //            {
        //                // Other payment statuses (authorized, etc.)
        //                response.Status = "Success";
        //                response.Message = "Payment verified. Booking pending confirmation.";
        //            }
        //        }

        //        // ... rest of payment history creation code ...
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Status = "Failure";
        //        response.Message = $"Error verifying payment: {ex.Message}";
        //        response.ErrorCode = "1";
        //    }

        //    return response;
        //}

        private bool VerifyRazorpaySignature(string orderId, string paymentId, string signature)
        {
            try
            {
                string payload = $"{orderId}|{paymentId}";
                string secret = _configuration.Razorpay.KeySecret;

                using (var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret)))
                {
                    var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
                    var generatedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

                    return generatedSignature == signature.ToLower();
                }
            }
            catch
            {
                return false;
            }
        }

        //private async Task ConfirmBookingAfterPayment(int bookingId, string userId)
        //{
        //    try
        //    {
        //        var bookingDetails = await _bookingRepository.GetBookingDetailsAsync(bookingId);
        //        if (bookingDetails == null) return;

        //        var seatUpdates = new List<SeatUpdateRequest>();
        //        foreach (var seat in bookingDetails.BookingSeats)
        //        {
        //            seatUpdates.Add(new SeatUpdateRequest
        //            {
        //                SeatTypeId = seat.event_seat_type_inventory_id,
        //                Quantity = seat.quantity
        //            });
        //        }

        //        // Update booking status to confirmed and reduce seat availability
        //        await _bookingRepository.UpdateBookingStatusAndSeatsAsync(
        //            bookingId, "confirmed", seatUpdates, userId);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log error but don't fail the payment verification
        //        Console.WriteLine($"Error confirming booking after payment: {ex.Message}");
        //    }
        //}

        private async Task ConfirmBookingAfterPayment(int bookingId, string userId)
        {
            try
            {
                Console.WriteLine($"Starting ConfirmBookingAfterPayment for BookingId: {bookingId}");

                var bookingDetails = await _bookingRepository.GetBookingDetailsAsync(bookingId);
                if (bookingDetails == null)
                {
                    Console.WriteLine($"Booking details not found for {bookingId}");
                    return;
                }

                Console.WriteLine($"Found booking details with {bookingDetails.BookingSeats?.Count} seats");

                var seatUpdates = new List<SeatUpdateRequest>();
                foreach (var seat in bookingDetails.BookingSeats)
                {
                    Console.WriteLine($"Processing seat - TypeId: {seat.event_seat_type_inventory_id}, Quantity: {seat.quantity}");
                    seatUpdates.Add(new SeatUpdateRequest
                    {
                        SeatTypeId = seat.event_seat_type_inventory_id,
                        Quantity = seat.quantity
                    });
                }

                // Update booking status to confirmed and reduce seat availability
                var result = await _bookingRepository.UpdateBookingStatusAndSeatsAsync(
                    bookingId, "confirmed", seatUpdates, userId);

                Console.WriteLine($"UpdateBookingStatusAndSeatsAsync returned: {result}");
                Console.WriteLine($"Successfully confirmed booking {bookingId} and deducted {seatUpdates.Count} seat types");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error confirming booking after payment: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Rethrow so we know it failed
            }
        }

        public async Task<CommonResponseModel<PaymentStatusResponse>> GetPaymentStatusAsync(int bookingId, string userEmail)
        {
            var response = new CommonResponseModel<PaymentStatusResponse>();

            try
            {
                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    response.Status = "Failure";
                    response.Message = "Booking not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Get user details
                var user = await _userRepository.GetUserByEmail(userEmail);
                if (user == null)
                {
                    response.Status = "Failure";
                    response.Message = "User not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Ensure booking belongs to the user
                if (booking.user_id != user.user_id)
                {
                    response.Status = "Failure";
                    response.Message = "Unauthorized access to booking";
                    response.ErrorCode = "403";
                    return response;
                }

                if (string.IsNullOrEmpty(booking.razorpay_payment_id))
                {
                    response.Status = "Failure";
                    response.Message = "No payment found for this booking";
                    response.ErrorCode = "404";
                    return response;
                }

                try
                {
                    // Get payment details from Razorpay
                    var payment = _razorpayClient.Payment.Fetch(booking.razorpay_payment_id);

                    Dictionary<string, string> notesDictionary = null;
                    if (booking.razorpay_notes != null)
                    {
                        try
                        {
                            // FIXED: Use JsonSerializer from System.Text.Json
                            notesDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(booking.razorpay_notes);
                        }
                        catch
                        {
                            notesDictionary = new Dictionary<string, string>();
                        }
                    }

                    var statusResponse = new PaymentStatusResponse
                    {
                        PaymentId = booking.razorpay_payment_id,
                        Status = payment["status"].ToString(),
                        Amount = booking.total_amount,
                        Currency = booking.currency,
                        Method = booking.payment_method,
                        CreatedAt = booking.payment_date ?? booking.created_on,
                        Notes = notesDictionary
                    };

                    response.Status = "Success";
                    response.Message = "Payment status retrieved successfully";
                    response.ErrorCode = "0";
                    response.Data = statusResponse;
                }
                catch (Exception ex)
                {
                    // If Razorpay API fails, return status from database
                    Dictionary<string, string> notesDictionary = null;
                    if (booking.razorpay_notes != null)
                    {
                        try
                        {
                            // FIXED: Use JsonSerializer from System.Text.Json
                            notesDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(booking.razorpay_notes);
                        }
                        catch
                        {
                            notesDictionary = new Dictionary<string, string>();
                        }
                    }

                    var statusResponse = new PaymentStatusResponse
                    {
                        PaymentId = booking.razorpay_payment_id,
                        Status = booking.payment_status,
                        Amount = booking.total_amount,
                        Currency = booking.currency,
                        Method = booking.payment_method,
                        CreatedAt = booking.payment_date ?? booking.created_on,
                        Notes = notesDictionary
                    };

                    response.Status = "Success";
                    response.Message = "Payment status retrieved from database";
                    response.ErrorCode = "0";
                    response.Data = statusResponse;
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error getting payment status: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        //public async Task<CommonResponseModel<RefundResponse>> ProcessRefundAsync(PaymentRefundRequest request, string adminEmail)
        //{
        //    var response = new CommonResponseModel<RefundResponse>();

        //    try
        //    {
        //        var booking = await _bookingRepository.GetBookingByIdAsync(request.BookingId);
        //        if (booking == null || string.IsNullOrEmpty(booking.razorpay_payment_id))
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Booking or payment not found";
        //            response.ErrorCode = "404";
        //            return response;
        //        }

        //        // Get user/admin details
        //        var user = await _userRepository.GetUserByEmail(adminEmail);
        //        if (user == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "User not found";
        //            response.ErrorCode = "404";
        //            return response;
        //        }

        //        // Create refund in Razorpay
        //        var options = new Dictionary<string, object>
        //        {
        //            { "amount", Convert.ToInt32(request.Amount * 100) }
        //        };

        //        // Add notes if reason is provided
        //        if (!string.IsNullOrEmpty(request.Reason))
        //        {
        //            options.Add("notes", new Dictionary<string, string> { { "reason", request.Reason } });
        //        }

        //        // FIXED: Check Razorpay API documentation for correct method signature
        //        // The refund might need different parameters. Try these options:

        //        // Option 1: Try single parameter with options dictionary
        //        try
        //        {
        //            // Some Razorpay versions expect payment ID and options
        //            var refund = _razorpayClient.Refund.Create(booking.razorpay_payment_id, options);

        //            var refundResponse = new RefundResponse
        //            {
        //                RefundId = refund["id"].ToString(),
        //                Amount = request.Amount,
        //                Status = refund["status"].ToString(),
        //                CreatedAt = DateTime.UtcNow
        //            };

        //            // Update booking status to refunded
        //            await _bookingRepository.UpdateBookingStatusAsync(request.BookingId, "refunded", user.user_id.ToString());

        //            response.Status = "Success";
        //            response.Message = "Refund processed successfully";
        //            response.ErrorCode = "0";
        //            response.Data = refundResponse;
        //        }
        //        catch (Exception ex)
        //        {
        //            // Option 2: Try with amount as separate parameter
        //            try
        //            {
        //                var refund = _razorpayClient.Refund.Create(
        //                    booking.razorpay_payment_id,
        //                    Convert.ToInt32(request.Amount * 100),
        //                    options
        //                );

        //                var refundResponse = new RefundResponse
        //                {
        //                    RefundId = refund["id"].ToString(),
        //                    Amount = request.Amount,
        //                    Status = refund["status"].ToString(),
        //                    CreatedAt = DateTime.UtcNow
        //                };

        //                // Update booking status to refunded
        //                await _bookingRepository.UpdateBookingStatusAsync(request.BookingId, "refunded", user.user_id.ToString());

        //                response.Status = "Success";
        //                response.Message = "Refund processed successfully";
        //                response.ErrorCode = "0";
        //                response.Data = refundResponse;
        //            }
        //            catch (Exception innerEx)
        //            {
        //                response.Status = "Failure";
        //                response.Message = $"Error processing refund: {innerEx.Message}";
        //                response.ErrorCode = "1";
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Status = "Failure";
        //        response.Message = $"Error processing refund: {ex.Message}";
        //        response.ErrorCode = "1";
        //    }

        //    return response;
        //}

        public async Task<CommonResponseModel<bool>> UpdatePaymentStatusAsync(int bookingId, string paymentStatus, string updatedBy)
        {
            var response = new CommonResponseModel<bool>();

            try
            {
                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    response.Status = "Failure";
                    response.Message = "Booking not found";
                    response.ErrorCode = "404";
                    response.Data = false;
                    return response;
                }

                await _paymentRepository.UpdatePaymentStatusAsync(bookingId, paymentStatus, updatedBy);

                response.Status = "Success";
                response.Message = "Payment status updated successfully";
                response.ErrorCode = "0";
                response.Data = true;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error updating payment status: {ex.Message}";
                response.ErrorCode = "1";
                response.Data = false;
            }

            return response;
        }

        // In PaymentService.cs
        //---------correct before seats deduction even it failed
        //public async Task<CommonResponseModel<PaymentOrderResponse>> CreateBookingWithPaymentAsync(CreateBookingWithPaymentRequest request, string userEmail)
        //{
        //    var response = new CommonResponseModel<PaymentOrderResponse>();

        //    try
        //    {
        //        // Get user
        //        var user = await _userRepository.GetUserByEmail(userEmail);
        //        if (user == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "User not found";
        //            response.ErrorCode = "404";
        //            return response;
        //        }

        //        // Check event exists
        //        var eventDetails = await _eventDetailsRepository.GetEventByIdAsync(request.EventId);
        //        if (eventDetails == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Event not found";
        //            response.ErrorCode = "404";
        //            return response;
        //        }

        //        // Check seat availability
        //        foreach (var seatSelection in request.SeatSelections)
        //        {
        //            var isAvailable = await _bookingRepository.CheckSeatAvailabilityAsync(
        //                seatSelection.SeatTypeId, seatSelection.Quantity);

        //            if (!isAvailable)
        //            {
        //                var seatType = await _bookingRepository.GetSeatTypeByIdAsync(seatSelection.SeatTypeId);
        //                response.Status = "Failure";
        //                response.Message = $"Not enough seats available for {seatType?.seat_name}";
        //                response.ErrorCode = "400";
        //                return response;
        //            }
        //        }

        //        // Temporarily reserve seats
        //        string userIdString = user.user_id.ToString();
        //        foreach (var seatSelection in request.SeatSelections)
        //        {
        //            await _bookingRepository.ReserveSeatsAsync(
        //                seatSelection.SeatTypeId, seatSelection.Quantity, userIdString);
        //        }

        //        // Calculate amounts
        //        decimal totalAmount = 0;
        //        foreach (var seatSelection in request.SeatSelections)
        //        {
        //            var seatType = await _bookingRepository.GetSeatTypeByIdAsync(seatSelection.SeatTypeId);
        //            var subtotal = seatType.price * seatSelection.Quantity;
        //            totalAmount += subtotal;
        //        }

        //        // Calculate fees
        //        var fees = FeeCalculator.CalculateFees(totalAmount);

        //        // Generate booking code
        //        var bookingCode = $"ZTH{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";

        //        // Create booking in PENDING status
        //        var booking = new BookingModel
        //        {
        //            booking_code = bookingCode,
        //            user_id = user.user_id,
        //            event_id = request.EventId,
        //            total_amount = totalAmount,
        //            booking_amount = totalAmount,
        //            convenience_fee = fees.convenienceFee,
        //            gst_amount = fees.gstAmount,
        //            final_amount = fees.finalAmount, // This is what will be sent to Razorpay
        //            status = "pending_payment",
        //            currency = "INR",
        //            created_by = userIdString,
        //            updated_by = userIdString
        //        };

        //        var bookingId = await _bookingRepository.CreateBookingAsync(booking);

        //        if (bookingId > 0)
        //        {
        //            // Create booking seats
        //            var bookingSeats = new List<BookingSeatModel>();
        //            foreach (var seatSelection in request.SeatSelections)
        //            {
        //                var seatType = await _bookingRepository.GetSeatTypeByIdAsync(seatSelection.SeatTypeId);
        //                var subtotal = seatType.price * seatSelection.Quantity;

        //                var bookingSeat = new BookingSeatModel
        //                {
        //                    booking_id = bookingId,
        //                    event_seat_type_inventory_id = seatSelection.SeatTypeId,
        //                    quantity = seatSelection.Quantity,
        //                    remaining_quantity = seatSelection.Quantity,
        //                    price_per_seat = seatType.price,
        //                    subtotal = subtotal,
        //                    created_by = userIdString,
        //                    updated_by = userIdString
        //                };

        //                bookingSeats.Add(bookingSeat);
        //            }

        //            await _bookingRepository.CreateBookingSeatsAsync(bookingSeats);

        //            // Now create Razorpay order - IMPORTANT: Use final_amount
        //            var paymentRequest = new CreatePaymentOrderRequest
        //            {
        //                BookingId = bookingId,
        //                Amount = fees.finalAmount, // Use final amount, not total amount
        //                Currency = "INR",
        //                Notes = new Dictionary<string, string>
        //        {
        //            { "eventId", request.EventId.ToString() },
        //            { "eventName", eventDetails.event_name },
        //            { "userId", user.user_id.ToString() },
        //            { "bookingCode", bookingCode }
        //        }
        //            };

        //            // Call the fixed CreatePaymentOrderAsync method
        //            return await CreatePaymentOrderAsync(paymentRequest, userEmail);
        //        }
        //        else
        //        {
        //            // Release reserved seats if booking creation failed
        //            foreach (var seatSelection in request.SeatSelections)
        //            {
        //                await _bookingRepository.ReleaseSeatsAsync(
        //                    seatSelection.SeatTypeId, seatSelection.Quantity, userIdString);
        //            }

        //            response.Status = "Failure";
        //            response.Message = "Failed to create booking";
        //            response.ErrorCode = "1";
        //            return response;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Status = "Failure";
        //        response.Message = $"Error creating booking with payment: {ex.Message}";
        //        response.ErrorCode = "1";
        //        return response;
        //    }
        //}


        // Update the CreateBookingWithPaymentAsync method in PaymentService.cs

        public async Task<CommonResponseModel<PaymentOrderResponse>> CreateBookingWithPaymentAsync(CreateBookingWithPaymentRequest request, string userEmail)
        {
            var response = new CommonResponseModel<PaymentOrderResponse>();

            try
            {
                // Get user
                var user = await _userRepository.GetUserByEmail(userEmail);
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

                // Check seat availability
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

                // Calculate amounts
                decimal totalAmount = 0;
                foreach (var seatSelection in request.SeatSelections)
                {
                    var seatType = await _bookingRepository.GetSeatTypeByIdAsync(seatSelection.SeatTypeId);
                    var subtotal = seatType.price * seatSelection.Quantity;
                    totalAmount += subtotal;
                }

                // Get convenience fee percentage from event
                decimal convenienceFeePercentage = eventDetails.convenience_fee ?? 6m;

                // Calculate fees
                var fees = FeeCalculator.CalculateFees(totalAmount, convenienceFeePercentage);

                // Apply coupon discount if provided - FIX: Check for null or empty string
                decimal finalAmount = fees.finalAmount;
                decimal discountAmount = 0;
                int? couponId = null;
                string couponCode = null;

                if (!string.IsNullOrEmpty(request.CouponCode) && request.DiscountAmount > 0)
                {
                    discountAmount = request.DiscountAmount;
                    finalAmount = fees.finalAmount - discountAmount;

                    // Get the coupon ID from the code
                    var coupon = await _couponRepository.GetCouponByCodeAsync(request.CouponCode);
                    if (coupon != null)
                    {
                        couponId = coupon.coupon_id;
                        couponCode = coupon.coupon_code;

                        // Validate that the coupon is applicable for this booking
                        // (You may want to add additional validation here)
                    }
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
                    booking_amount = totalAmount,
                    convenience_fee = fees.convenienceFee,
                    gst_amount = fees.gstAmount,
                    final_amount = finalAmount, // Amount after coupon discount
                    status = "pending_payment",
                    currency = "INR",
                    created_by = user.user_id.ToString(),
                    updated_by = user.user_id.ToString(),
                    // Coupon fields - set to null if no coupon
                    coupon_id = couponId,
                    discount_amount = discountAmount,
                    coupon_code_applied = couponCode
                };

                var bookingId = await _bookingRepository.CreateBookingAsync(booking);

                if (bookingId > 0)
                {
                    // Create booking seats
                    var bookingSeats = new List<BookingSeatModel>();
                    foreach (var seatSelection in request.SeatSelections)
                    {
                        var seatType = await _bookingRepository.GetSeatTypeByIdAsync(seatSelection.SeatTypeId);
                        var subtotal = seatType.price * seatSelection.Quantity;

                        var bookingSeat = new BookingSeatModel
                        {
                            booking_id = bookingId,
                            event_seat_type_inventory_id = seatSelection.SeatTypeId,
                            quantity = seatSelection.Quantity,
                            remaining_quantity = seatSelection.Quantity,
                            price_per_seat = seatType.price,
                            subtotal = subtotal,
                            created_by = user.user_id.ToString(),
                            updated_by = user.user_id.ToString()
                        };

                        bookingSeats.Add(bookingSeat);
                    }

                    await _bookingRepository.CreateBookingSeatsAsync(bookingSeats);

                    // Create Razorpay order with final amount (after coupon)
                    var paymentRequest = new CreatePaymentOrderRequest
                    {
                        BookingId = bookingId,
                        Amount = finalAmount, // Use amount after coupon
                        Currency = "INR",
                        Notes = new Dictionary<string, string>
                {
                    { "eventId", request.EventId.ToString() },
                    { "eventName", eventDetails.event_name },
                    { "userId", user.user_id.ToString() },
                    { "bookingCode", bookingCode },
                    { "originalAmount", totalAmount.ToString() },
                    { "discountAmount", discountAmount.ToString() },
                    { "couponCode", couponCode ?? "" }
                }
                    };

                    return await CreatePaymentOrderAsync(paymentRequest, userEmail);
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to create booking";
                    response.ErrorCode = "1";
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error creating booking with payment: {ex.Message}";
                response.ErrorCode = "1";
                return response;
            }
        }

        //---------correct before coupon
        //public async Task<CommonResponseModel<PaymentOrderResponse>> CreateBookingWithPaymentAsync(CreateBookingWithPaymentRequest request, string userEmail)
        //{
        //    var response = new CommonResponseModel<PaymentOrderResponse>();

        //    try
        //    {
        //        // Get user
        //        var user = await _userRepository.GetUserByEmail(userEmail);
        //        if (user == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "User not found";
        //            response.ErrorCode = "404";
        //            return response;
        //        }

        //        // Check event exists
        //        var eventDetails = await _eventDetailsRepository.GetEventByIdAsync(request.EventId);
        //        if (eventDetails == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Event not found";
        //            response.ErrorCode = "404";
        //            return response;
        //        }

        //        // Get convenience fee percentage from event (default to 6 if not set)
        //        decimal convenienceFeePercentage = eventDetails.convenience_fee ?? 6m;

        //        // Check seat availability (BUT DON'T RESERVE YET)
        //        foreach (var seatSelection in request.SeatSelections)
        //        {
        //            var isAvailable = await _bookingRepository.CheckSeatAvailabilityAsync(
        //                seatSelection.SeatTypeId, seatSelection.Quantity);

        //            if (!isAvailable)
        //            {
        //                var seatType = await _bookingRepository.GetSeatTypeByIdAsync(seatSelection.SeatTypeId);
        //                response.Status = "Failure";
        //                response.Message = $"Not enough seats available for {seatType?.seat_name}";
        //                response.ErrorCode = "400";
        //                return response;
        //            }
        //        }

        //        // REMOVED: Temporarily reserve seats - DON'T RESERVE UNTIL PAYMENT IS CONFIRMED
        //        // string userIdString = user.user_id.ToString();
        //        // foreach (var seatSelection in request.SeatSelections)
        //        // {
        //        //     await _bookingRepository.ReserveSeatsAsync(
        //        //         seatSelection.SeatTypeId, seatSelection.Quantity, userIdString);
        //        // }

        //        // Calculate amounts
        //        decimal totalAmount = 0;
        //        foreach (var seatSelection in request.SeatSelections)
        //        {
        //            var seatType = await _bookingRepository.GetSeatTypeByIdAsync(seatSelection.SeatTypeId);
        //            var subtotal = seatType.price * seatSelection.Quantity;
        //            totalAmount += subtotal;
        //        }

        //        // Calculate fees
        //        //var fees = FeeCalculator.CalculateFees(totalAmount);

        //        // Calculate fees using dynamic percentage from event
        //        var fees = FeeCalculator.CalculateFees(totalAmount, convenienceFeePercentage);

        //        // Generate booking code
        //        var bookingCode = $"ZTH{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";

        //        // Create booking in PENDING_PAYMENT status
        //        var booking = new BookingModel
        //        {
        //            booking_code = bookingCode,
        //            user_id = user.user_id,
        //            event_id = request.EventId,
        //            total_amount = totalAmount,
        //            booking_amount = totalAmount,
        //            convenience_fee = fees.convenienceFee,
        //            gst_amount = fees.gstAmount,
        //            final_amount = fees.finalAmount,
        //            status = "pending_payment", // Status is pending payment, not confirmed
        //            currency = "INR",
        //            created_by = user.user_id.ToString(),
        //            updated_by = user.user_id.ToString()
        //        };

        //        var bookingId = await _bookingRepository.CreateBookingAsync(booking);

        //        if (bookingId > 0)
        //        {
        //            // Create booking seats (but these are just records, seats not deducted yet)
        //            var bookingSeats = new List<BookingSeatModel>();
        //            foreach (var seatSelection in request.SeatSelections)
        //            {
        //                var seatType = await _bookingRepository.GetSeatTypeByIdAsync(seatSelection.SeatTypeId);
        //                var subtotal = seatType.price * seatSelection.Quantity;

        //                var bookingSeat = new BookingSeatModel
        //                {
        //                    booking_id = bookingId,
        //                    event_seat_type_inventory_id = seatSelection.SeatTypeId,
        //                    quantity = seatSelection.Quantity,
        //                    remaining_quantity = seatSelection.Quantity, // Initially same as quantity
        //                    price_per_seat = seatType.price,
        //                    subtotal = subtotal,
        //                    created_by = user.user_id.ToString(),
        //                    updated_by = user.user_id.ToString()
        //                };

        //                bookingSeats.Add(bookingSeat);
        //            }

        //            await _bookingRepository.CreateBookingSeatsAsync(bookingSeats);

        //            // Now create Razorpay order - IMPORTANT: Use final_amount
        //            var paymentRequest = new CreatePaymentOrderRequest
        //            {
        //                BookingId = bookingId,
        //                Amount = fees.finalAmount,
        //                Currency = "INR",
        //                Notes = new Dictionary<string, string>
        //                {
        //                    { "eventId", request.EventId.ToString() },
        //                    { "eventName", eventDetails.event_name },
        //                    { "userId", user.user_id.ToString() },
        //                    { "bookingCode", bookingCode },
        //                    { "convenienceFeePercentage", convenienceFeePercentage.ToString() }
        //                }
        //            };

        //            // Call the fixed CreatePaymentOrderAsync method
        //            return await CreatePaymentOrderAsync(paymentRequest, userEmail);
        //        }
        //        else
        //        {
        //            // No need to release seats since we never reserved them
        //            response.Status = "Failure";
        //            response.Message = "Failed to create booking";
        //            response.ErrorCode = "1";
        //            return response;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Status = "Failure";
        //        response.Message = $"Error creating booking with payment: {ex.Message}";
        //        response.ErrorCode = "1";
        //        return response;
        //    }
        //}

        private async Task ReleaseSeatsForFailedPayment(int bookingId)
        {
            try
            {
                var bookingDetails = await _bookingRepository.GetBookingDetailsAsync(bookingId);
                if (bookingDetails == null) return;

                foreach (var seat in bookingDetails.BookingSeats)
                {
                    await _bookingRepository.ReleaseSeatsAsync(
                        seat.event_seat_type_inventory_id,
                        seat.quantity,
                        bookingDetails.user_id.ToString());
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail
                Console.WriteLine($"Error releasing seats for failed payment: {ex.Message}");
            }
        }
    }
}
