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

        Task<CommonResponseModel<OrganizerResponse>> AddOrganizer(OrganizerRequest request);
        Task<CommonResponseModel<OrganizerResponse>> UpdateOrganizer(OrganizerRequest request, Guid organizerId);
        Task<CommonResponseModel<bool>> DeleteOrganizer(Guid organizerId, string updatedBy);
        Task<CommonResponseModel<bool>> UpdateOrganizerStatus(Guid organizerId, int status, string updatedBy);
        Task<OrganizerPagedResponse> GetPaginatedOrganizers(PaginationRequest request);
        Task<CommonResponseModel<OrganizerResponse>> GetOrganizerById(Guid organizerId);
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

        public async Task<CommonResponseModel<OrganizerResponse>> AddOrganizer(OrganizerRequest request)
        {
            var response = new CommonResponseModel<OrganizerResponse>();

            try
            {
                // Check if email already exists
                if (await _userRepository.CheckEmailExists(request.email))
                {
                    response.Status = "Failure";
                    response.Success = false;
                    response.Message = "Email already registered";
                    response.ErrorCode = "1";  // Changed to "1" for error
                    return response;
                }

                // Check if mobile already exists
                if (!string.IsNullOrEmpty(request.mobile) &&
                    await _userRepository.CheckMobileExists(request.mobile))
                {
                    response.Status = "Failure";
                    response.Success = false;
                    response.Message = "Mobile number already registered";
                    response.ErrorCode = "1";
                    return response;
                }

                // Create user
                var user = new UserModel
                {
                    first_name = request.first_name,
                    last_name = request.last_name,
                    email = request.email,
                    country_code = request.country_code,
                    mobile = request.mobile,
                    role_id = request.role_id,
                    password = await _encryption.Encryption(request.password),
                    created_by = request.created_by,
                    updated_by = request.updated_by
                };

                var userId = await _userRepository.AddUser(user);

                if (userId == Guid.Empty)
                {
                    response.Status = "Failure";
                    response.Success = false;
                    response.Message = "Failed to create user account";
                    response.ErrorCode = "1";
                    return response;
                }

                // Create organizer with the generated user_id
                var organizer = new EventOrganizerModel
                {
                    user_id = userId,
                    org_name = request.org_name,
                    org_start_date = request.org_start_date,
                    bank_account_no = request.bank_account_no,
                    bank_ifsc = request.bank_ifsc,
                    bank_name = request.bank_name,
                    beneficiary_name = request.beneficiary_name,
                    aadhar_number = request.aadhar_number,
                    pancard_number = request.pancard_number,
                    owner_personal_email = request.owner_personal_email,
                    owner_mobile = request.owner_mobile ?? request.mobile,
                    state = request.state,
                    city = request.city,
                    country = request.country,
                    gst_number = request.gst_number,
                    instagram_link = request.instagram_link,
                    youtube_link = request.youtube_link,
                    facebook_link = request.facebook_link,
                    twitter_link = request.twitter_link,
                    verification_status = "pending",
                    created_by = request.created_by,
                    updated_by = request.updated_by
                };

                var organizerId = await _userRepository.AddEventOrganizer(organizer);

                if (organizerId == Guid.Empty)
                {
                    response.Status = "Failure";
                    response.Success = false;
                    response.Message = "Failed to create organizer profile";
                    response.ErrorCode = "1";
                    return response;
                }

                // Get the created organizer with user details
                var createdOrganizer = await _userRepository.GetOrganizerById(organizerId);

                response.Status = "Success";
                response.Success = true;
                response.Message = "Organizer added successfully";
                response.ErrorCode = "0";  // Success code
                response.Data = createdOrganizer;

                return response;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Success = false;
                response.Message = $"Failed to add organizer: {ex.Message}";
                response.ErrorCode = "1";  // Error code
                return response;
            }
        }

        public async Task<CommonResponseModel<OrganizerResponse>> UpdateOrganizer(OrganizerRequest request, Guid organizerId)
        {
            var response = new CommonResponseModel<OrganizerResponse>();

            try
            {
                // Get existing organizer
                var existingOrganizer = await _userRepository.GetOrganizerById(organizerId);
                if (existingOrganizer == null)
                {
                    response.Status = "Failure";
                    response.Success = false;
                    response.Message = "Organizer not found";
                    response.ErrorCode = "1";
                    return response;
                }

                // Update organizer details
                var organizer = new EventOrganizerModel
                {
                    organizer_id = organizerId,
                    user_id = existingOrganizer.user_id.Value,
                    org_name = request.org_name,
                    org_start_date = request.org_start_date,
                    bank_account_no = request.bank_account_no,
                    bank_ifsc = request.bank_ifsc,
                    bank_name = request.bank_name,
                    beneficiary_name = request.beneficiary_name,
                    aadhar_number = request.aadhar_number,
                    pancard_number = request.pancard_number,
                    owner_personal_email = request.owner_personal_email,
                    owner_mobile = request.owner_mobile,
                    state = request.state,
                    city = request.city,
                    country = request.country,
                    gst_number = request.gst_number,
                    instagram_link = request.instagram_link,
                    youtube_link = request.youtube_link,
                    facebook_link = request.facebook_link,
                    twitter_link = request.twitter_link,
                    verification_status = existingOrganizer.verification_status ?? "pending",
                    updated_by = request.updated_by
                };

                var isUpdated = await _userRepository.UpdateEventOrganizer(organizer);

                if (!isUpdated)
                {
                    response.Status = "Failure";
                    response.Success = false;
                    response.Message = "Failed to update organizer";
                    response.ErrorCode = "1";
                    return response;
                }

                // Get updated organizer
                var updatedOrganizer = await _userRepository.GetOrganizerById(organizerId);

                response.Status = "Success";
                response.Success = true;
                response.Message = "Organizer updated successfully";
                response.ErrorCode = "0";
                response.Data = updatedOrganizer;

                return response;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Success = false;
                response.Message = $"Failed to update organizer: {ex.Message}";
                response.ErrorCode = "1";
                return response;
            }
        }

        public async Task<CommonResponseModel<bool>> DeleteOrganizer(Guid organizerId, string updatedBy)
        {
            var response = new CommonResponseModel<bool>();

            try
            {
                var isDeleted = await _userRepository.DeleteEventOrganizer(organizerId, updatedBy);

                response.Status = isDeleted ? "Success" : "Failure";
                response.Success = isDeleted;
                response.Message = isDeleted ? "Organizer deleted successfully" : "Failed to delete organizer";
                response.ErrorCode = isDeleted ? "0" : "1";
                response.Data = isDeleted;

                return response;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Success = false;
                response.Message = $"Failed to delete organizer: {ex.Message}";
                response.ErrorCode = "1";
                response.Data = false;
                return response;
            }
        }

        public async Task<CommonResponseModel<bool>> UpdateOrganizerStatus(Guid organizerId, int status, string updatedBy)
        {
            var response = new CommonResponseModel<bool>();

            try
            {
                var isUpdated = await _userRepository.UpdateOrganizerStatus(organizerId, status, updatedBy);

                response.Status = isUpdated ? "Success" : "Failure";
                response.Success = isUpdated;
                response.Message = isUpdated ? "Organizer status updated successfully" : "Failed to update organizer status";
                response.Data = isUpdated;

                if (!isUpdated)
                {
                    response.ErrorCode = "UPDATE_STATUS_FAILED";
                }

                return response;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Success = false;
                response.Message = $"Failed to update organizer status: {ex.Message}";
                response.ErrorCode = "UPDATE_STATUS_ERROR";
                response.Data = false;
                return response;
            }
        }

        public async Task<OrganizerPagedResponse> GetPaginatedOrganizers(PaginationRequest request)
        {
            return await _userRepository.GetPaginatedOrganizers(request);
        }

        public async Task<CommonResponseModel<OrganizerResponse>> GetOrganizerById(Guid organizerId)
        {
            var response = new CommonResponseModel<OrganizerResponse>();

            try
            {
                var organizer = await _userRepository.GetOrganizerById(organizerId);

                if (organizer == null)
                {
                    response.Status = "Failure";
                    response.Success = false;
                    response.Message = "Organizer not found";
                    response.ErrorCode = "ORGANIZER_NOT_FOUND";
                    return response;
                }

                response.Status = "Success";
                response.Success = true;
                response.Message = "Organizer fetched successfully";
                response.Data = organizer;

                return response;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Success = false;
                response.Message = $"Failed to fetch organizer: {ex.Message}";
                response.ErrorCode = "GET_ORGANIZER_ERROR";
                return response;
            }
        }
    }
}
