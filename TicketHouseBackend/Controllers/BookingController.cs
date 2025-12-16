using BAL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MODEL.Entities;
using MODEL.Request;
using MODEL.Response;
using System.Security.Claims;

namespace TicketHouseBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BookingController(
            IBookingService bookingService,
            IHttpContextAccessor httpContextAccessor)
        {
            _bookingService = bookingService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet("GetAvailableSeats/{eventId}")]
        public async Task<CommonResponseModel<List<EventSeatTypeInventoryModel>>> GetAvailableSeats(int eventId)
        {
            try
            {
                if (eventId <= 0)
                {
                    return new CommonResponseModel<List<EventSeatTypeInventoryModel>>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event ID is required"
                    };
                }

                return await _bookingService.GetAvailableSeatsByEventIdAsync(eventId);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<List<EventSeatTypeInventoryModel>>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("SelectSeats")]
        public async Task<CommonResponseModel<SeatSelectionResponse>> SelectSeats([FromBody] SeatSelectionRequest request)
        {
            try
            {
                if (request == null)
                {
                    return new CommonResponseModel<SeatSelectionResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Request data is required"
                    };
                }

                // Get user email from JWT token if available
                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    userEmail = "guest"; // Temporary user for seat selection
                }

                return await _bookingService.SelectSeatsAsync(request, userEmail);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<SeatSelectionResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("CreateBooking")]
        [Authorize]
        public async Task<CommonResponseModel<BookingResponse>> CreateBooking([FromBody] CreateBookingRequest request)
        {
            try
            {
                if (request == null)
                {
                    return new CommonResponseModel<BookingResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Booking data is required"
                    };
                }

                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return new CommonResponseModel<BookingResponse>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required"
                    };
                }

                return await _bookingService.CreateBookingAsync(request, userEmail);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<BookingResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("ConfirmBooking/{bookingId}")]
        [Authorize]
        public async Task<CommonResponseModel<BookingResponse>> ConfirmBooking(int bookingId)
        {
            try
            {
                if (bookingId <= 0)
                {
                    return new CommonResponseModel<BookingResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid booking ID is required"
                    };
                }

                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return new CommonResponseModel<BookingResponse>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required"
                    };
                }

                return await _bookingService.ConfirmBookingAsync(bookingId, userEmail);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<BookingResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("CancelBooking/{bookingId}")]
        [Authorize]
        public async Task<CommonResponseModel<BookingResponse>> CancelBooking(int bookingId)
        {
            try
            {
                if (bookingId <= 0)
                {
                    return new CommonResponseModel<BookingResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid booking ID is required"
                    };
                }

                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return new CommonResponseModel<BookingResponse>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required"
                    };
                }

                return await _bookingService.CancelBookingAsync(bookingId, userEmail);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<BookingResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetBookingDetails/{bookingId}")]
        [Authorize]
        public async Task<CommonResponseModel<BookingDetailsResponse>> GetBookingDetails(int bookingId)
        {
            try
            {
                if (bookingId <= 0)
                {
                    return new CommonResponseModel<BookingDetailsResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid booking ID is required"
                    };
                }

                return await _bookingService.GetBookingDetailsAsync(bookingId);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<BookingDetailsResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetBookingDetailsByCode/{bookingCode}")]
        [Authorize]
        public async Task<CommonResponseModel<BookingDetailsResponse>> GetBookingDetailsByCode(string bookingCode)
        {
            try
            {
                if (string.IsNullOrEmpty(bookingCode))
                {
                    return new CommonResponseModel<BookingDetailsResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Booking code is required"
                    };
                }

                return await _bookingService.GetBookingDetailsByCodeAsync(bookingCode);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<BookingDetailsResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetUserBookings")]
        [Authorize]
        public async Task<CommonResponseModel<List<BookingResponse>>> GetUserBookings()
        {
            try
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
                {
                    return new CommonResponseModel<List<BookingResponse>>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required"
                    };
                }

                return await _bookingService.GetUserBookingsAsync(userId);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<List<BookingResponse>>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("CheckSeatAvailability")]
        public async Task<CommonResponseModel<bool>> CheckSeatAvailability([FromBody] SeatAvailabilityRequest request)
        {
            try
            {
                if (request == null)
                {
                    return new CommonResponseModel<bool>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Request data is required",
                        Data = false
                    };
                }

                return await _bookingService.CheckSeatAvailabilityAsync(request);
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
    }
}
