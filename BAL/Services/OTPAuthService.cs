using DAL.Repository;
using Microsoft.Extensions.Logging;
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
    public interface IOTPAuthService
    {
        Task<OTPResponse> GenerateOTP(GenerateOTPRequest request);
        Task<CommonResponseModel<string>> VerifyOTP(VerifyOTPRequest request);
        Task<ResendOTPResponse> ResendOTP(int otpId);
    }
    public class OTPAuthService: IOTPAuthService
    {
        private readonly ILoginRepository _loginRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<OTPAuthService> _logger;

        public OTPAuthService(ILoginRepository loginRepository, IUserRepository userRepository, IEmailService emailService, ILogger<OTPAuthService> logger)
        {
            _loginRepository = loginRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<OTPResponse> GenerateOTP(GenerateOTPRequest request)
        {
            var response = new OTPResponse();

            try
            {
                // Validate request
                if (string.IsNullOrEmpty(request.email) && string.IsNullOrEmpty(request.mobile))
                {
                    response.Response.Status = "Failure";
                    response.Response.Message = "Email or mobile is required";
                    response.Response.ErrorCode = "CONTACT_REQUIRED";
                    return response;
                }

                if (string.IsNullOrEmpty(request.contact_type))
                {
                    response.Response.Status = "Failure";
                    response.Response.Message = "Contact type (email/mobile) is required";
                    response.Response.ErrorCode = "CONTACT_TYPE_REQUIRED";
                    return response;
                }

                // Generate random 4-digit OTP
                var random = new Random();
                var otp = random.Next(1000, 9999).ToString();

                var otpVerification = new OtpVerificationModel
                {
                    email = request.email,
                    mobile = request.mobile,
                    country_code = request.country_code,
                    type = request.contact_type.ToLower(),
                    otp = otp,
                    status = "not verified",
                    created_by = "system",
                    updated_by = "system"
                };

                // If not a new user, get user_id
                if (!request.newUser && !string.IsNullOrEmpty(request.email))
                {
                    var user = await _userRepository.GetUserByEmail(request.email);
                    if (user != null)
                    {
                        _logger.LogInformation($"Found existing user: {user.user_id} for email: {request.email}");
                        otpVerification.user_id = user.user_id;
                    }
                }

                var otpId = await _loginRepository.AddOtpVerification(otpVerification);
                _logger.LogInformation($"OTP saved to database with ID: {otpId}");

                // Send OTP via email if email is provided and type is email
                if (!string.IsNullOrEmpty(request.email) && request.contact_type.ToLower() == "email")
                {
                    var user = await _userRepository.GetUserByEmail(request.email);
                    var userName = user?.first_name ?? "User";

                    _logger.LogInformation($"Attempting to send OTP email to: {request.email}");

                    var emailSent = await _emailService.SendOTPEmailAsync(request.email, otp, userName);

                    if (!emailSent)
                    {
                        // Don't fail the request if email fails, just log it
                        Console.WriteLine($"Warning: Failed to send OTP email to {request.email}");
                        _logger.LogError($"Failed to send OTP email to {request.email}");
                    }
                }

                response.Response.Status = "Success";
                response.Response.Message = $"OTP sent successfully to {request.contact_type}";
                response.validationotp_id = otpId;

                return response;
            }
            catch (Exception ex)
            {
                response.Response.Status = "Failure";
                response.Response.Message = $"Failed to generate OTP: {ex.Message}";
                response.Response.ErrorCode = "OTP_GENERATION_ERROR";
                return response;
            }
        }

        public async Task<CommonResponseModel<string>> VerifyOTP(VerifyOTPRequest request)
        {
            var response = new CommonResponseModel<string>();

            try
            {
                var otpVerification = await _loginRepository.GetOtpVerification(request.otp_id);
                if (otpVerification == null)
                {
                    response.Status = "Failure";
                    response.Message = "Invalid OTP request";
                    response.ErrorCode = "INVALID_OTP_REQUEST";
                    return response;
                }

                // Check if OTP is already verified
                if (otpVerification.status == "verified")
                {
                    response.Status = "Failure";
                    response.Message = "OTP already verified";
                    response.ErrorCode = "OTP_ALREADY_VERIFIED";
                    return response;
                }

                // Check if OTP is expired (120 seconds / 2 minutes)
                if (DateTime.UtcNow > otpVerification.created_on.AddSeconds(120))
                {
                    await _loginRepository.UpdateOtpStatus(request.otp_id, "expired");
                    response.Status = "Failure";
                    response.Message = "OTP has expired";
                    response.ErrorCode = "OTP_EXPIRED";
                    return response;
                }

                // Check if OTP matches
                if (otpVerification.otp != request.otp)
                {
                    response.Status = "Failure";
                    response.Message = "Invalid OTP";
                    response.ErrorCode = "INVALID_OTP";
                    return response;
                }

                // Verify OTP
                await _loginRepository.VerifyOtp(request.otp_id);

                response.Status = "Success";
                response.Message = "OTP verified successfully";
                return response;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"OTP verification failed: {ex.Message}";
                response.ErrorCode = "OTP_VERIFICATION_ERROR";
                return response;
            }
        }

        public async Task<ResendOTPResponse> ResendOTP(int otpId)
        {
            var response = new ResendOTPResponse();

            try
            {
                var existingOtp = await _loginRepository.GetOtpVerification(otpId);
                if (existingOtp == null)
                {
                    response.Response.Status = "Failure";
                    response.Response.Message = "Invalid OTP request";
                    response.Response.ErrorCode = "INVALID_OTP_REQUEST";
                    return response;
                }

                // Mark old OTP as expired
                await _loginRepository.UpdateOtpStatus(otpId, "expired");

                // Generate new OTP
                var generateRequest = new GenerateOTPRequest
                {
                    contact_type = existingOtp.type,
                    email = existingOtp.email,
                    mobile = existingOtp.mobile,
                    country_code = existingOtp.country_code,
                    newUser = existingOtp.user_id == null
                };

                var newOtpResponse = await GenerateOTP(generateRequest);

                if (newOtpResponse.Response.Status == "Success")
                {
                    response.Response.Status = "Success";
                    response.Response.Message = "OTP resent successfully";
                    response.new_otp_id = newOtpResponse.validationotp_id;
                }
                else
                {
                    response.Response.Status = "Failure";
                    response.Response.Message = newOtpResponse.Response.Message;
                    response.Response.ErrorCode = newOtpResponse.Response.ErrorCode;
                }

                return response;
            }
            catch (Exception ex)
            {
                response.Response.Status = "Failure";
                response.Response.Message = $"Failed to resend OTP: {ex.Message}";
                response.Response.ErrorCode = "OTP_RESEND_ERROR";
                return response;
            }
        }
    }
}
