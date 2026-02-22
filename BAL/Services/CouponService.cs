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
    public interface ICouponService
    {
        // CRUD Operations
        Task<CommonResponseModel<CouponResponse>> CreateCouponAsync(CreateCouponRequest request, string userEmail);
        Task<CommonResponseModel<CouponResponse>> UpdateCouponAsync(UpdateCouponRequest request, string userEmail);
        Task<CommonResponseModel<bool>> DeleteCouponAsync(int couponId, string userEmail);
        Task<CommonResponseModel<CouponResponse>> GetCouponByIdAsync(int couponId);
        Task<CommonResponseModel<CouponResponse>> GetCouponByCodeAsync(string couponCode);
        Task<CommonResponseModel<List<CouponResponse>>> GetAllCouponsAsync(bool includeInactive = false);
        Task<CommonResponseModel<List<CouponResponse>>> GetCouponsByEventIdAsync(int eventId);

        // Business Logic
        Task<CommonResponseModel<CouponResult>> CheckAndApplyBestCouponAsync(CheckCouponRequest request);
        Task<CommonResponseModel<CouponResult>> ApplyCouponManuallyAsync(ApplyCouponRequest request);
        Task<CommonResponseModel<CouponResult>> RemoveCouponAsync(int bookingId);
        Task<CommonResponseModel<CouponResult>> RecalculateCouponForBookingAsync(int bookingId);
    }
    public class CouponService: ICouponService
    {
        private readonly ICouponRepository _couponRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IUserRepository _userRepository;

        public CouponService(
            ICouponRepository couponRepository,
            IBookingRepository bookingRepository,
            IUserRepository userRepository)
        {
            _couponRepository = couponRepository;
            _bookingRepository = bookingRepository;
            _userRepository = userRepository;
        }

        public async Task<CommonResponseModel<CouponResponse>> CreateCouponAsync(CreateCouponRequest request, string userEmail)
        {
            var response = new CommonResponseModel<CouponResponse>();

            try
            {
                // Validate request
                if (request.event_ids == null || !request.event_ids.Any())
                {
                    response.Status = "Failure";
                    response.Message = "At least one event ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                if (request.valid_from >= request.valid_to)
                {
                    response.Status = "Failure";
                    response.Message = "Valid from date must be before valid to date";
                    response.ErrorCode = "400";
                    return response;
                }

                // Get user
                var user = await _userRepository.GetUserByEmail(userEmail);
                if (user == null)
                {
                    response.Status = "Failure";
                    response.Message = "User not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Generate coupon code
                string couponCode = GenerateCouponCode();

                // Create coupon model
                var coupon = new CouponModel
                {
                    event_ids = string.Join(",", request.event_ids),
                    coupon_name = request.coupon_name,
                    coupon_code = couponCode,
                    discount_type = request.discount_type,
                    discount_amount = request.discount_amount,
                    min_order_amount = request.min_order_amount,
                    max_order_amount = request.max_order_amount,
                    min_seats = request.min_seats,
                    max_seats = request.max_seats,
                    min_ticket_price = request.min_ticket_price,
                    max_ticket_price = request.max_ticket_price,
                    valid_from = request.valid_from,
                    valid_to = request.valid_to,
                    created_by = user.user_id.ToString(),
                    updated_by = user.user_id.ToString(),
                    active = 1
                };

                var couponId = await _couponRepository.CreateCouponAsync(coupon);

                if (couponId > 0)
                {
                    response.Status = "Success";
                    response.Message = "Coupon created successfully";
                    response.ErrorCode = "0";
                    response.Data = MapToResponse(coupon);
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to create coupon";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error creating coupon: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<CouponResponse>> UpdateCouponAsync(UpdateCouponRequest request, string userEmail)
        {
            var response = new CommonResponseModel<CouponResponse>();

            try
            {
                // Get existing coupon
                var existingCoupon = await _couponRepository.GetCouponByIdAsync(request.coupon_id);
                if (existingCoupon == null)
                {
                    response.Status = "Failure";
                    response.Message = "Coupon not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Validate request
                if (request.event_ids == null || !request.event_ids.Any())
                {
                    response.Status = "Failure";
                    response.Message = "At least one event ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                if (request.valid_from >= request.valid_to)
                {
                    response.Status = "Failure";
                    response.Message = "Valid from date must be before valid to date";
                    response.ErrorCode = "400";
                    return response;
                }

                // Get user
                var user = await _userRepository.GetUserByEmail(userEmail);
                if (user == null)
                {
                    response.Status = "Failure";
                    response.Message = "User not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Update coupon
                existingCoupon.event_ids = string.Join(",", request.event_ids);
                existingCoupon.coupon_name = request.coupon_name;
                existingCoupon.discount_type = request.discount_type;
                existingCoupon.discount_amount = request.discount_amount;
                existingCoupon.min_order_amount = request.min_order_amount;
                existingCoupon.max_order_amount = request.max_order_amount;
                existingCoupon.min_seats = request.min_seats;
                existingCoupon.max_seats = request.max_seats;
                existingCoupon.min_ticket_price = request.min_ticket_price;
                existingCoupon.max_ticket_price = request.max_ticket_price;
                existingCoupon.valid_from = request.valid_from;
                existingCoupon.valid_to = request.valid_to;
                existingCoupon.updated_by = user.user_id.ToString();
                existingCoupon.active = request.active;

                var rowsAffected = await _couponRepository.UpdateCouponAsync(existingCoupon);

                if (rowsAffected > 0)
                {
                    response.Status = "Success";
                    response.Message = "Coupon updated successfully";
                    response.ErrorCode = "0";
                    response.Data = MapToResponse(existingCoupon);
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to update coupon";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error updating coupon: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<bool>> DeleteCouponAsync(int couponId, string userEmail)
        {
            var response = new CommonResponseModel<bool>();

            try
            {
                // Get user
                var user = await _userRepository.GetUserByEmail(userEmail);
                if (user == null)
                {
                    response.Status = "Failure";
                    response.Message = "User not found";
                    response.ErrorCode = "404";
                    response.Data = false;
                    return response;
                }

                var rowsAffected = await _couponRepository.DeleteCouponAsync(couponId, user.user_id.ToString());

                response.Status = rowsAffected > 0 ? "Success" : "Failure";
                response.Message = rowsAffected > 0 ? "Coupon deleted successfully" : "Coupon not found";
                response.ErrorCode = rowsAffected > 0 ? "0" : "404";
                response.Data = rowsAffected > 0;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error deleting coupon: {ex.Message}";
                response.ErrorCode = "1";
                response.Data = false;
            }

            return response;
        }

        public async Task<CommonResponseModel<CouponResponse>> GetCouponByIdAsync(int couponId)
        {
            var response = new CommonResponseModel<CouponResponse>();

            try
            {
                var coupon = await _couponRepository.GetCouponByIdAsync(couponId);

                if (coupon != null)
                {
                    response.Status = "Success";
                    response.Message = "Coupon retrieved successfully";
                    response.ErrorCode = "0";
                    response.Data = MapToResponse(coupon);
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Coupon not found";
                    response.ErrorCode = "404";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving coupon: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<CouponResponse>> GetCouponByCodeAsync(string couponCode)
        {
            var response = new CommonResponseModel<CouponResponse>();

            try
            {
                var coupon = await _couponRepository.GetCouponByCodeAsync(couponCode);

                if (coupon != null)
                {
                    response.Status = "Success";
                    response.Message = "Coupon retrieved successfully";
                    response.ErrorCode = "0";
                    response.Data = MapToResponse(coupon);
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Coupon not found";
                    response.ErrorCode = "404";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving coupon: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<List<CouponResponse>>> GetAllCouponsAsync(bool includeInactive = false)
        {
            var response = new CommonResponseModel<List<CouponResponse>>();

            try
            {
                var coupons = await _couponRepository.GetAllCouponsAsync(includeInactive);

                var couponResponses = coupons.Select(c => MapToResponse(c)).ToList();

                response.Status = "Success";
                response.Message = "Coupons retrieved successfully";
                response.ErrorCode = "0";
                response.Data = couponResponses;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving coupons: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<List<CouponResponse>>> GetCouponsByEventIdAsync(int eventId)
        {
            var response = new CommonResponseModel<List<CouponResponse>>();

            try
            {
                var coupons = await _couponRepository.GetCouponsByEventIdAsync(eventId);

                var couponResponses = coupons.Select(c => MapToResponse(c)).ToList();

                response.Status = "Success";
                response.Message = "Coupons retrieved successfully";
                response.ErrorCode = "0";
                response.Data = couponResponses;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving coupons: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<CouponResult>> CheckAndApplyBestCouponAsync(CheckCouponRequest request)
        {
            var response = new CommonResponseModel<CouponResult>();

            try
            {
                // Validate request
                if (request.event_id <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Invalid event ID";
                    response.ErrorCode = "400";
                    return response;
                }

                if (request.seat_selections == null || !request.seat_selections.Any())
                {
                    response.Status = "Failure";
                    response.Message = "No seat selections provided";
                    response.ErrorCode = "400";
                    return response;
                }

                // Log the request for debugging
                Console.WriteLine($"Checking coupon for Event ID: {request.event_id}, Total Seats: {request.total_seats}, Subtotal: {request.subtotal}");

                // Calculate average ticket price per seat (if multiple seat types)
                decimal avgTicketPrice = request.ticket_base_price;

                // Find best matching coupon
                var bestCoupon = await _couponRepository.FindBestMatchingCouponAsync(
                    request.event_id,
                    avgTicketPrice,
                    request.total_seats,
                    request.subtotal);

                if (bestCoupon == null)
                {
                    Console.WriteLine("No matching coupon found");
                    response.Status = "Success";
                    response.Message = "No coupon applicable";
                    response.ErrorCode = "0";
                    response.Data = new CouponResult
                    {
                        is_applied = false,
                        message = "No coupon applicable for this booking",
                        original_amount = request.subtotal,
                        final_amount = request.subtotal,
                        discount_value = 0
                    };
                    return response;
                }

                // Calculate discount value
                decimal discountValue = 0;

                if (bestCoupon.discount_type.ToLower() == "fixed")
                {
                    discountValue = bestCoupon.discount_amount;
                }
                else // percentage
                {
                    discountValue = (bestCoupon.discount_amount * request.subtotal) / 100;
                }

                decimal finalAmount = request.subtotal - discountValue;

                Console.WriteLine($"Coupon found: {bestCoupon.coupon_code}, Discount: {discountValue}");

                response.Status = "Success";
                response.Message = "Coupon applied successfully";
                response.ErrorCode = "0";
                response.Data = new CouponResult
                {
                    coupon_id = bestCoupon.coupon_id,
                    coupon_code = bestCoupon.coupon_code,
                    coupon_name = bestCoupon.coupon_name,
                    discount_type = bestCoupon.discount_type,
                    discount_amount = bestCoupon.discount_amount,
                    original_amount = request.subtotal,
                    discount_value = discountValue,
                    final_amount = finalAmount,
                    is_applied = true,
                    message = $"Coupon {bestCoupon.coupon_code} applied successfully"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking coupon: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                response.Status = "Failure";
                response.Message = $"Error checking coupon: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<CouponResult>> ApplyCouponManuallyAsync(ApplyCouponRequest request)
        {
            var response = new CommonResponseModel<CouponResult>();

            try
            {
                // Get booking
                var booking = await _bookingRepository.GetBookingByIdAsync(request.booking_id);
                if (booking == null)
                {
                    response.Status = "Failure";
                    response.Message = "Booking not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Get coupon by code
                var coupon = await _couponRepository.GetCouponByCodeAsync(request.coupon_code);
                if (coupon == null)
                {
                    response.Status = "Failure";
                    response.Message = "Invalid coupon code";
                    response.ErrorCode = "404";
                    return response;
                }

                // Check if coupon is valid for this booking
                if (booking.final_amount < coupon.min_order_amount || booking.final_amount > coupon.max_order_amount)
                {
                    response.Status = "Failure";
                    response.Message = "Coupon not applicable for this order amount";
                    response.ErrorCode = "400";
                    return response;
                }

                // Check if coupon is valid for this event
                bool isValidForEvent = await _couponRepository.IsCouponValidForEventAsync(coupon.coupon_id, booking.event_id);
                if (!isValidForEvent)
                {
                    response.Status = "Failure";
                    response.Message = "Coupon not applicable for this event";
                    response.ErrorCode = "400";
                    return response;
                }

                // Calculate discount
                decimal discountValue = 0;
                if (coupon.discount_type.ToLower() == "fixed")
                {
                    discountValue = coupon.discount_amount;
                }
                else // percentage
                {
                    discountValue = (coupon.discount_amount * booking.final_amount) / 100;
                }

                decimal finalAmount = booking.final_amount - discountValue;

                // Apply coupon to booking
                await _couponRepository.ApplyCouponToBookingAsync(
                    booking.booking_id,
                    coupon.coupon_id,
                    discountValue,
                    coupon.coupon_code);

                response.Status = "Success";
                response.Message = "Coupon applied successfully";
                response.ErrorCode = "0";
                response.Data = new CouponResult
                {
                    coupon_id = coupon.coupon_id,
                    coupon_code = coupon.coupon_code,
                    coupon_name = coupon.coupon_name,
                    discount_type = coupon.discount_type,
                    discount_amount = coupon.discount_amount,
                    original_amount = booking.final_amount,
                    discount_value = discountValue,
                    final_amount = finalAmount,
                    is_applied = true,
                    message = $"Coupon {coupon.coupon_code} applied successfully"
                };
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error applying coupon: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<CouponResult>> RemoveCouponAsync(int bookingId)
        {
            var response = new CommonResponseModel<CouponResult>();

            try
            {
                // Get booking
                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    response.Status = "Failure";
                    response.Message = "Booking not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Remove coupon
                var rowsAffected = await _couponRepository.RemoveCouponFromBookingAsync(bookingId);

                if (rowsAffected > 0)
                {
                    response.Status = "Success";
                    response.Message = "Coupon removed successfully";
                    response.ErrorCode = "0";
                    response.Data = new CouponResult
                    {
                        is_applied = false,
                        message = "Coupon removed",
                        original_amount = booking.final_amount + (booking.discount_amount),
                        final_amount = booking.final_amount,
                        discount_value = 0
                    };
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "No coupon found on this booking";
                    response.ErrorCode = "404";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error removing coupon: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<CouponResult>> RecalculateCouponForBookingAsync(int bookingId)
        {
            var response = new CommonResponseModel<CouponResult>();

            try
            {
                // Get booking details
                var bookingDetails = await _bookingRepository.GetBookingDetailsAsync(bookingId);
                if (bookingDetails == null)
                {
                    response.Status = "Failure";
                    response.Message = "Booking not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Calculate total seats and average price
                int totalSeats = bookingDetails.BookingSeats.Sum(s => s.quantity);
                decimal avgTicketPrice = bookingDetails.total_amount / totalSeats;

                // Check if coupon is still applicable
                var checkRequest = new CheckCouponRequest
                {
                    event_id = bookingDetails.event_id,
                    seat_selections = bookingDetails.BookingSeats.Select(s => new SeatSelection
                    {
                        SeatTypeId = s.event_seat_type_inventory_id,
                        Quantity = s.quantity
                    }).ToList(),
                    ticket_base_price = avgTicketPrice,
                    total_seats = totalSeats,
                    subtotal = bookingDetails.total_amount
                };

                var bestCouponResult = await CheckAndApplyBestCouponAsync(checkRequest);

                // If no coupon applicable or different coupon, update booking
                if (bestCouponResult.Data?.is_applied == true &&
                    bestCouponResult.Data.coupon_id != bookingDetails.coupon_id)
                {
                    // Apply new best coupon
                    await _couponRepository.ApplyCouponToBookingAsync(
                        bookingId,
                        bestCouponResult.Data.coupon_id.Value,
                        bestCouponResult.Data.discount_value,
                        bestCouponResult.Data.coupon_code);

                    response.Data = bestCouponResult.Data;
                }
                else if (bookingDetails.coupon_id.HasValue)
                {
                    // Keep existing coupon
                    var coupon = await _couponRepository.GetCouponByIdAsync(bookingDetails.coupon_id.Value);

                    if (coupon != null)
                    {
                        response.Data = new CouponResult
                        {
                            coupon_id = coupon.coupon_id,
                            coupon_code = coupon.coupon_code,
                            coupon_name = coupon.coupon_name,
                            discount_type = coupon.discount_type,
                            discount_amount = coupon.discount_amount,
                            original_amount = bookingDetails.total_amount,
                            discount_value = bookingDetails.discount_amount,
                            final_amount = bookingDetails.final_amount,
                            is_applied = true,
                            message = "Existing coupon applied"
                        };
                    }
                    else
                    {
                        response.Data = new CouponResult
                        {
                            is_applied = false,
                            message = "No coupon applicable",
                            original_amount = bookingDetails.total_amount,
                            final_amount = bookingDetails.final_amount,
                            discount_value = 0
                        };
                    }
                }
                else
                {
                    response.Data = bestCouponResult.Data ?? new CouponResult
                    {
                        is_applied = false,
                        message = "No coupon applicable",
                        original_amount = bookingDetails.total_amount,
                        final_amount = bookingDetails.final_amount,
                        discount_value = 0
                    };
                }

                response.Status = "Success";
                response.Message = "Coupon recalculated successfully";
                response.ErrorCode = "0";
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error recalculating coupon: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        #region Helper Methods

        private string GenerateCouponCode()
        {
            // Format: th_ + random string + timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string randomPart = GenerateRandomString(6);
            return $"th_{randomPart}_{timestamp}";
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private CouponResponse MapToResponse(CouponModel coupon)
        {
            if (coupon == null) return null;

            return new CouponResponse
            {
                coupon_id = coupon.coupon_id,
                event_ids = coupon.GetEventIdsList(),
                coupon_name = coupon.coupon_name,
                coupon_code = coupon.coupon_code,
                discount_type = coupon.discount_type,
                discount_amount = coupon.discount_amount,
                min_order_amount = coupon.min_order_amount,
                max_order_amount = coupon.max_order_amount,
                min_seats = coupon.min_seats,
                max_seats = coupon.max_seats,
                min_ticket_price = coupon.min_ticket_price,
                max_ticket_price = coupon.max_ticket_price,
                valid_from = coupon.valid_from,
                valid_to = coupon.valid_to,
                is_valid_now = coupon.valid_from <= DateTime.Now && coupon.valid_to >= DateTime.Now
            };
        }

        #endregion
    }
}
