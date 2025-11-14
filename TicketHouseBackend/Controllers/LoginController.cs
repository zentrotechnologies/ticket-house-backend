using BAL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MODEL.Request;
using MODEL.Response;

namespace TicketHouseBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IOTPAuthService _otpService;

        public LoginController(IUserService userService, IOTPAuthService otpService)
        {
            _userService = userService;
            _otpService = otpService;
        }

        [AllowAnonymous]
        [HttpPost("SignUp")]
        public async Task<ActionResult<LoginResponse>> SignUp([FromBody] SignUpRequest signUpRequest)
        {
            var response = await _userService.RegisterUser(signUpRequest);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest loginRequest)
        {
            var response = await _userService.ValidateUser(loginRequest);

            if (response.Response.Status == "Success")
            {
                // Set cookies for token and refresh token
                SetTokenCookie("jwt", response.token, DateTime.UtcNow.AddMinutes(30));
                SetTokenCookie("refreshToken", response.refresh_token, DateTime.UtcNow.AddDays(7));
            }

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("GenerateOTP")]
        public async Task<ActionResult<OTPResponse>> GenerateOTP([FromBody] GenerateOTPRequest request)
        {
            var response = await _otpService.GenerateOTP(request);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("VerifyOTP")]
        public async Task<ActionResult<CommonResponseModel<string>>> VerifyOTP([FromBody] VerifyOTPRequest request)
        {
            var response = await _otpService.VerifyOTP(request);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("ResendOTP")]
        public async Task<ActionResult<ResendOTPResponse>> ResendOTP([FromBody] ResendOTPRequest request)
        {
            var response = await _otpService.ResendOTP(request.otp_id);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("GoogleSignIn")]
        public async Task<ActionResult<LoginResponse>> GoogleSignIn([FromBody] GoogleSSORequest request)
        {
            // Implement Google Sign-In logic here
            // This would validate the Google token and create/login user
            return Ok(new LoginResponse
            {
                Response = new CommonResponseModel<string>
                {
                    Status = "Success",
                    Message = "Google sign-in successful"
                }
            });
        }

        [HttpPost("RefreshToken")]
        public async Task<ActionResult<CommonResponseModel<string>>> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new CommonResponseModel<string>
                {
                    Status = "Failure",
                    Message = "Refresh token not found",
                    ErrorCode = "REFRESH_TOKEN_MISSING"
                });
            }

            var response = await _userService.VerifyToken(refreshToken);
            return Ok(response);
        }

        [HttpPost("Logout")]
        public async Task<ActionResult<CommonResponseModel<string>>> Logout()
        {
            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("refreshToken");

            return Ok(new CommonResponseModel<string>
            {
                Status = "Success",
                Message = "Logged out successfully"
            });
        }

        private void SetTokenCookie(string key, string token, DateTime expiry)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = expiry,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };
            Response.Cookies.Append(key, token, cookieOptions);
        }
    }
}
