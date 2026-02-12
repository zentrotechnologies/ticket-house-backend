using BAL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MODEL.Request;
using MODEL.Response;
using System.Security.Claims;

namespace TicketHouseBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentController(
            IPaymentService paymentService,
            IHttpContextAccessor httpContextAccessor)
        {
            _paymentService = paymentService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("CreateOrder")]
        [Authorize]
        public async Task<CommonResponseModel<PaymentOrderResponse>> CreateOrder([FromBody] CreatePaymentOrderRequest request)
        {
            try
            {
                if (request == null || request.BookingId <= 0 || request.Amount <= 0)
                {
                    return new CommonResponseModel<PaymentOrderResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid booking ID and amount are required"
                    };
                }

                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return new CommonResponseModel<PaymentOrderResponse>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required"
                    };
                }

                return await _paymentService.CreatePaymentOrderAsync(request, userEmail);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<PaymentOrderResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("VerifyPayment")]
        [Authorize]
        public async Task<CommonResponseModel<PaymentVerificationResponse>> VerifyPayment([FromBody] VerifyPaymentRequest request)
        {
            try
            {
                if (request == null || request.BookingId <= 0 ||
                    string.IsNullOrEmpty(request.RazorpayOrderId) ||
                    string.IsNullOrEmpty(request.RazorpayPaymentId) ||
                    string.IsNullOrEmpty(request.RazorpaySignature))
                {
                    return new CommonResponseModel<PaymentVerificationResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "All payment verification fields are required"
                    };
                }

                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return new CommonResponseModel<PaymentVerificationResponse>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required"
                    };
                }

                return await _paymentService.VerifyPaymentAsync(request, userEmail);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<PaymentVerificationResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetPaymentStatus/{bookingId}")]
        [Authorize]
        public async Task<CommonResponseModel<PaymentStatusResponse>> GetPaymentStatus(int bookingId)
        {
            try
            {
                if (bookingId <= 0)
                {
                    return new CommonResponseModel<PaymentStatusResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid booking ID is required"
                    };
                }

                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return new CommonResponseModel<PaymentStatusResponse>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required"
                    };
                }

                return await _paymentService.GetPaymentStatusAsync(bookingId, userEmail);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<PaymentStatusResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        //[HttpPost("ProcessRefund")]
        //[Authorize(Roles = "Admin")]
        //public async Task<CommonResponseModel<RefundResponse>> ProcessRefund([FromBody] PaymentRefundRequest request)
        //{
        //    try
        //    {
        //        if (request == null || request.BookingId <= 0 || request.Amount <= 0)
        //        {
        //            return new CommonResponseModel<RefundResponse>
        //            {
        //                ErrorCode = "400",
        //                Status = "Error",
        //                Message = "Valid booking ID and amount are required"
        //            };
        //        }

        //        var adminEmail = GetCurrentUserEmail();
        //        if (string.IsNullOrEmpty(adminEmail))
        //        {
        //            return new CommonResponseModel<RefundResponse>
        //            {
        //                ErrorCode = "401",
        //                Status = "Error",
        //                Message = "Admin authentication required"
        //            };
        //        }

        //        return await _paymentService.ProcessRefundAsync(request, adminEmail);
        //    }
        //    catch (Exception ex)
        //    {
        //        return new CommonResponseModel<RefundResponse>
        //        {
        //            ErrorCode = "1",
        //            Status = "Error",
        //            Message = ex.Message
        //        };
        //    }
        //}

        [HttpPost("UpdatePaymentStatus/{bookingId}/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<CommonResponseModel<bool>> UpdatePaymentStatus(int bookingId, string status)
        {
            try
            {
                if (bookingId <= 0 || string.IsNullOrEmpty(status))
                {
                    return new CommonResponseModel<bool>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid booking ID and status are required",
                        Data = false
                    };
                }

                var adminEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(adminEmail))
                {
                    return new CommonResponseModel<bool>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "Admin authentication required",
                        Data = false
                    };
                }

                return await _paymentService.UpdatePaymentStatusAsync(bookingId, status, adminEmail);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<bool>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message,
                    Data = false
                };
            }
        }

        private string GetCurrentUserEmail()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ??
                   _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;
        }

        // In PaymentController.cs
        [HttpPost("CreateBookingWithPayment")]
        [Authorize]
        public async Task<CommonResponseModel<PaymentOrderResponse>> CreateBookingWithPayment([FromBody] CreateBookingWithPaymentRequest request)
        {
            try
            {
                if (request == null || request.EventId <= 0 || request.SeatSelections == null)
                {
                    return new CommonResponseModel<PaymentOrderResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid booking data is required"
                    };
                }

                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return new CommonResponseModel<PaymentOrderResponse>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required"
                    };
                }

                return await _paymentService.CreateBookingWithPaymentAsync(request, userEmail);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<PaymentOrderResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }
    }
}
