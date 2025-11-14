using DAL.Repository;
using DAL.Utilities;
using Microsoft.IdentityModel.Tokens;
using MODEL.Configuration;
using MODEL.Entities;
using MODEL.Request;
using MODEL.Response;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BAL.Services
{
    public interface IUserService
    {
        Task<LoginResponse> RegisterUser(SignUpRequest signUpRequest);
        Task<LoginResponse> ValidateUser(LoginRequest loginRequest);
        Task<string> GenerateToken(UserModel user, string sessionId);
        Task<string> GenerateRefreshToken(string email, string sessionId);
        Task<CommonResponseModel<string>> VerifyToken(string token);
    }
    public class UserService: IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEncryptionDecryption _encryption;
        private readonly THConfiguration _configuration;

        public UserService(IUserRepository userRepository, IEncryptionDecryption encryption, THConfiguration configuration)
        {
            _userRepository = userRepository;
            _encryption = encryption;
            _configuration = configuration;
        }

        public async Task<LoginResponse> RegisterUser(SignUpRequest signUpRequest)
        {
            var response = new LoginResponse();

            try
            {
                // Check if email already exists
                if (await _userRepository.CheckEmailExists(signUpRequest.email))
                {
                    response.Response.Status = "Failure";
                    response.Response.Message = "Email already registered";
                    response.Response.ErrorCode = "EMAIL_EXISTS";
                    return response;
                }

                // Check if mobile already exists
                if (!string.IsNullOrEmpty(signUpRequest.mobile) &&
                    await _userRepository.CheckMobileExists(signUpRequest.mobile))
                {
                    response.Response.Status = "Failure";
                    response.Response.Message = "Mobile number already registered";
                    response.Response.ErrorCode = "MOBILE_EXISTS";
                    return response;
                }

                // Create user
                var user = new UserModel
                {
                    first_name = signUpRequest.first_name,
                    last_name = signUpRequest.last_name,
                    email = signUpRequest.email,
                    country_code = signUpRequest.country_code,
                    mobile = signUpRequest.mobile,
                    role_id = signUpRequest.role_id,
                    password = await _encryption.Encryption(signUpRequest.password),
                    created_by = "system",
                    updated_by = "system"
                };

                var userId = await _userRepository.AddUser(user);

                if (userId == Guid.Empty)
                {
                    throw new Exception("Failed to create user account - returned empty GUID");
                }

                user.user_id = userId;

                // If user is Event Organizer (role_id = 2), create organizer record
                if (signUpRequest.role_id == 2 && !string.IsNullOrEmpty(signUpRequest.org_name))
                {
                    var organizer = new EventOrganizerModel
                    {
                        user_id = userId,
                        org_name = signUpRequest.org_name,
                        org_start_date = signUpRequest.org_start_date,
                        bank_account_no = signUpRequest.bank_account_no,
                        bank_ifsc = signUpRequest.bank_ifsc,
                        bank_name = signUpRequest.bank_name,
                        beneficiary_name = signUpRequest.beneficiary_name,
                        aadhar_number = signUpRequest.aadhar_number,
                        pancard_number = signUpRequest.pancard_number,
                        owner_personal_email = signUpRequest.owner_personal_email,
                        owner_mobile = signUpRequest.mobile, // Use the same mobile
                        state = signUpRequest.state,
                        city = signUpRequest.city,
                        country = signUpRequest.country,
                        gst_number = signUpRequest.gst_number,
                        instagram_link = signUpRequest.instagram_link,
                        youtube_link = signUpRequest.youtube_link,
                        facebook_link = signUpRequest.facebook_link,
                        twitter_link = signUpRequest.twitter_link,
                        created_by = "system",
                        updated_by = "system"
                    };

                    var organizerId = await _userRepository.AddEventOrganizer(organizer);

                    if (organizerId == Guid.Empty)
                    {
                        // Log the error but don't fail the entire registration
                        Console.WriteLine("Warning: Organizer profile creation failed but user was created");
                    }
                }

                response.Response.Status = "Success";
                response.Response.Message = signUpRequest.role_id == 2 ?
                    "Registration successful! Your application is under review. You will be able to access the panel within 24-48 hours after verification." :
                    "Registration successful!";
                response.user_id = userId;
                response.first_name = user.first_name;
                response.last_name = user.last_name;
                response.email = user.email;

                return response;
            }
            catch (Exception ex)
            {
                // Log the full exception details for debugging
                Console.WriteLine($"Registration error: {ex}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                response.Response.Status = "Failure";
                response.Response.Message = $"Registration failed: {ex.Message}";
                response.Response.ErrorCode = "REGISTRATION_ERROR";
                return response;
            }
        }

        public async Task<LoginResponse> ValidateUser(LoginRequest loginRequest)
        {
            var response = new LoginResponse();

            try
            {
                var user = await _userRepository.GetUserByEmail(loginRequest.email);
                if (user == null)
                {
                    response.Response.Status = "Failure";
                    response.Response.Message = "Invalid email or password";
                    response.Response.ErrorCode = "INVALID_CREDENTIALS";
                    return response;
                }

                var decryptedPassword = await _encryption.Decryption(user.password);
                if (decryptedPassword != loginRequest.password)
                {
                    response.Response.Status = "Failure";
                    response.Response.Message = "Invalid email or password";
                    response.Response.ErrorCode = "INVALID_CREDENTIALS";
                    return response;
                }

                // Generate tokens
                var sessionId = Guid.NewGuid().ToString();
                var token = await GenerateToken(user, sessionId);
                var refreshToken = await GenerateRefreshToken(user.email, sessionId);

                response.Response.Status = "Success";
                response.Response.Message = "Login successful";
                response.user_id = user.user_id;
                response.token = token;
                response.refresh_token = refreshToken;
                response.token_expiry = DateTime.UtcNow.AddMinutes(_configuration.JwtExpireMinutes);
                response.refresh_token_expiry = DateTime.UtcNow.AddDays(_configuration.RefreshTokenExpireDays);
                response.first_name = user.first_name;
                response.last_name = user.last_name;
                response.email = user.email;
                response.mobile = user.mobile;
                response.country_code = user.country_code;
                response.role_id = user.role_id;

                return response;
            }
            catch (Exception ex)
            {
                response.Response.Status = "Failure";
                response.Response.Message = $"Login failed: {ex.Message}";
                response.Response.ErrorCode = "LOGIN_ERROR";
                return response;
            }
        }

        public async Task<string> GenerateToken(UserModel user, string sessionId)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.JwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.user_id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.email),
                new Claim("role_id", user.role_id.ToString()),
                new Claim("session_id", sessionId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration.JwtIssuer,
                audience: _configuration.JwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_configuration.JwtExpireMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> GenerateRefreshToken(string email, string sessionId)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.JwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("session_id", sessionId),
                new Claim("token_type", "refresh"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration.JwtIssuer,
                audience: _configuration.JwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_configuration.RefreshTokenExpireDays),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<CommonResponseModel<string>> VerifyToken(string token)
        {
            var response = new CommonResponseModel<string>();

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration.JwtKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _configuration.JwtIssuer,
                    ValidAudience = _configuration.JwtAudience,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                response.Status = "Success";
                response.Message = "Token is valid";
                return response;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Token validation failed: {ex.Message}";
                response.ErrorCode = "TOKEN_INVALID";
                return response;
            }
        }
    }
}
