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
    public class CouponController : ControllerBase
    {
        private readonly ICouponService _couponService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CouponController(
            ICouponService couponService,
            IHttpContextAccessor httpContextAccessor)
        {
            _couponService = couponService;
            _httpContextAccessor = httpContextAccessor;
        }

        #region CRUD Operations

        [HttpPost("CreateCoupon")]
        //[Authorize(Roles = "Admin,Organizer")]
        public async Task<CommonResponseModel<CouponResponse>> CreateCoupon([FromBody] CreateCouponRequest request)
        {
            try
            {
                if (request == null)
                {
                    return new CommonResponseModel<CouponResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Coupon data is required"
                    };
                }

                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return new CommonResponseModel<CouponResponse>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required"
                    };
                }

                return await _couponService.CreateCouponAsync(request, userEmail);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<CouponResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPut("UpdateCoupon")]
        //[Authorize(Roles = "Admin,Organizer")]
        public async Task<CommonResponseModel<CouponResponse>> UpdateCoupon([FromBody] UpdateCouponRequest request)
        {
            try
            {
                if (request == null || request.coupon_id <= 0)
                {
                    return new CommonResponseModel<CouponResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid coupon data is required"
                    };
                }

                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return new CommonResponseModel<CouponResponse>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required"
                    };
                }

                return await _couponService.UpdateCouponAsync(request, userEmail);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<CouponResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpDelete("DeleteCoupon/{couponId}")]
        //[Authorize(Roles = "Admin,Organizer")]
        public async Task<CommonResponseModel<bool>> DeleteCoupon(int couponId)
        {
            try
            {
                if (couponId <= 0)
                {
                    return new CommonResponseModel<bool>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid coupon ID is required",
                        Data = false
                    };
                }

                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return new CommonResponseModel<bool>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required",
                        Data = false
                    };
                }

                return await _couponService.DeleteCouponAsync(couponId, userEmail);
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

        [HttpGet("GetCouponById/{couponId}")]
        //[Authorize]
        public async Task<CommonResponseModel<CouponResponse>> GetCouponById(int couponId)
        {
            try
            {
                if (couponId <= 0)
                {
                    return new CommonResponseModel<CouponResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid coupon ID is required"
                    };
                }

                return await _couponService.GetCouponByIdAsync(couponId);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<CouponResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetCouponByCode/{couponCode}")]
        //[Authorize]
        public async Task<CommonResponseModel<CouponResponse>> GetCouponByCode(string couponCode)
        {
            try
            {
                if (string.IsNullOrEmpty(couponCode))
                {
                    return new CommonResponseModel<CouponResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Coupon code is required"
                    };
                }

                return await _couponService.GetCouponByCodeAsync(couponCode);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<CouponResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetAllCoupons")]
        //[Authorize(Roles = "Admin,Organizer")]
        public async Task<CommonResponseModel<List<CouponResponse>>> GetAllCoupons([FromQuery] bool includeInactive = false)
        {
            try
            {
                return await _couponService.GetAllCouponsAsync(includeInactive);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<List<CouponResponse>>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetCouponsByEvent/{eventId}")]
        //[Authorize]
        public async Task<CommonResponseModel<List<CouponResponse>>> GetCouponsByEvent(int eventId)
        {
            try
            {
                if (eventId <= 0)
                {
                    return new CommonResponseModel<List<CouponResponse>>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event ID is required"
                    };
                }

                return await _couponService.GetCouponsByEventIdAsync(eventId);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<List<CouponResponse>>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        #endregion

        #region Business Logic

        [HttpPost("CheckAndApplyBestCoupon")]
        [AllowAnonymous]
        //[Authorize]
        public async Task<CommonResponseModel<CouponResult>> CheckAndApplyBestCoupon([FromBody] CheckCouponRequest request)
        {
            try
            {
                if (request == null || request.event_id <= 0)
                {
                    return new CommonResponseModel<CouponResult>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event data is required"
                    };
                }

                return await _couponService.CheckAndApplyBestCouponAsync(request);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<CouponResult>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("ApplyCouponManually")]
        //[Authorize]
        public async Task<CommonResponseModel<CouponResult>> ApplyCouponManually([FromBody] ApplyCouponRequest request)
        {
            try
            {
                if (request == null || request.booking_id <= 0 || string.IsNullOrEmpty(request.coupon_code))
                {
                    return new CommonResponseModel<CouponResult>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid booking ID and coupon code are required"
                    };
                }

                return await _couponService.ApplyCouponManuallyAsync(request);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<CouponResult>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("RemoveCoupon/{bookingId}")]
        //[Authorize]
        public async Task<CommonResponseModel<CouponResult>> RemoveCoupon(int bookingId)
        {
            try
            {
                if (bookingId <= 0)
                {
                    return new CommonResponseModel<CouponResult>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid booking ID is required"
                    };
                }

                return await _couponService.RemoveCouponAsync(bookingId);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<CouponResult>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("RecalculateCoupon/{bookingId}")]
        //[Authorize]
        public async Task<CommonResponseModel<CouponResult>> RecalculateCoupon(int bookingId)
        {
            try
            {
                if (bookingId <= 0)
                {
                    return new CommonResponseModel<CouponResult>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid booking ID is required"
                    };
                }

                return await _couponService.RecalculateCouponForBookingAsync(bookingId);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<CouponResult>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        #endregion

        private string GetCurrentUserEmail()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ??
                   _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;
        }
    }
}
