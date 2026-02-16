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
        private readonly IEmailService _emailService;

        public BookingController(
            IBookingService bookingService,
            IHttpContextAccessor httpContextAccessor,
            IEmailService emailService)
        {
            _bookingService = bookingService;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
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

        [HttpGet("GetMyBookingsByUserId/{userId}")]
        public async Task<CommonResponseModel<List<MyBookingsResponse>>> GetMyBookingsByUserId(Guid userId)
        {
            try
            {
                if (userId == Guid.Empty)
                {
                    return new CommonResponseModel<List<MyBookingsResponse>>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid user ID is required"
                    };
                }

                // Optional: Verify the requesting user has access to these bookings
                var currentUserIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
                {
                    return new CommonResponseModel<List<MyBookingsResponse>>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required"
                    };
                }

                // Optional: Check if user is requesting their own bookings (for security)
                if (currentUserId != userId)
                {
                    return new CommonResponseModel<List<MyBookingsResponse>>
                    {
                        ErrorCode = "403",
                        Status = "Error",
                        Message = "Access denied"
                    };
                }

                return await _bookingService.GetMyBookingsByUserIdAsync(userId);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<List<MyBookingsResponse>>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        //bookings with QR

        [HttpPost("ConfirmBookingWithQR/{bookingId}")]
        [Authorize]
        public async Task<CommonResponseModel<BookingQRResponse>> ConfirmBookingWithQR(int bookingId)
        {
            try
            {
                // Add validation at the beginning
                if (bookingId <= 0)
                {
                    return new CommonResponseModel<BookingQRResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid booking ID is required. Received: " + bookingId
                    };
                }

                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return new CommonResponseModel<BookingQRResponse>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required"
                    };
                }

                return await _bookingService.ConfirmBookingWithQRAsync(bookingId, userEmail);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<BookingQRResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("DecodeQRCode")]
        [AllowAnonymous]
        public async Task<CommonResponseModel<QRCodeDataResponse>> DecodeQRCode([FromBody] QRCodeDecodeRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.QRCodeBase64))
                {
                    return new CommonResponseModel<QRCodeDataResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "QR code data is required"
                    };
                }

                return await _bookingService.DecodeQRCodeDataAsync(request.QRCodeBase64);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<QRCodeDataResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("test-smtp")]
        [AllowAnonymous]
        public async Task<IActionResult> TestSmtp()
        {
            var result = await _emailService.TestSmtpConnectionAsync();
            return Ok(new { success = result, message = result ? "SMTP working" : "SMTP failed" });
        }

        [HttpGet("test-smtp-connection-only")]
        [AllowAnonymous]
        public async Task<IActionResult> TestSmtpConnectionOnly()
        {
            try
            {
                // Just test connection without authentication
                using var client = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587);
                client.EnableSsl = true;
                client.Timeout = 5000;

                // This will fail at authentication but will tell us if we can reach the server
                await client.SendMailAsync(
                    new System.Net.Mail.MailMessage("pranjal.kalokhe@zentrotechnologies.com", "pranjal.kalokhe@zentrotechnologies.com")
                    {
                        Subject = "Test",
                        Body = "Test"
                    }
                );

                return Ok(new { success = false, message = "Unexpected - authentication should have failed" });
            }
            catch (System.Net.Mail.SmtpException ex) when (ex.Message.Contains("Authentication"))
            {
                // This is GOOD - means we connected to server but auth failed
                return Ok(new
                {
                    success = true,
                    message = "SMTP server is reachable, authentication failed (expected)"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = $"Connection test failed: {ex.Message}"
                });
            }
        }

        [HttpGet("test-email-service")]
        [AllowAnonymous]
        public async Task<IActionResult> TestEmailService()
        {
            try
            {
                // Test 1: Using your EmailService
                var emailSent = await _emailService.SendEmailAsync(
                    "bingwatchallthetime@gmail.com",
                    "Test from EmailService",
                    $"Test email at {DateTime.UtcNow}. If you receive this, EmailService is working."
                );

                if (emailSent)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Email sent successfully via EmailService",
                        service = "Your EmailService"
                    });
                }

                // Test 2: Direct SMTP with same settings
                var config = new
                {
                    Server = "tickethouse.in",
                    Port = 587,
                    Username = "no-reply@tickethouse.in",
                    Password = "Tickethouse@zentro7888",
                    FromEmail = "no-reply@tickethouse.in",
                    FromName = "TicketHouse"
                };

                try
                {
                    using var client = new System.Net.Mail.SmtpClient(config.Server, config.Port);
                    client.EnableSsl = true;
                    client.Credentials = new System.Net.NetworkCredential(config.Username, config.Password);
                    client.Timeout = 15000;

                    await client.SendMailAsync(
                        new System.Net.Mail.MailMessage(config.FromEmail, "bingwatchallthetime@gmail.com")
                        {
                            Subject = "Direct SMTP Test",
                            Body = $"Direct test at {DateTime.UtcNow}",
                            IsBodyHtml = false
                        }
                    );

                    return Ok(new
                    {
                        success = true,
                        message = "Email sent successfully via direct SMTP",
                        service = "Direct SMTP Client",
                        note = "Your EmailService might have different configuration"
                    });
                }
                catch (Exception ex)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Both EmailService and direct SMTP failed",
                        emailServiceResult = emailSent,
                        directSmtpError = ex.Message,
                        suggestion = "Check if THConfiguration is being loaded correctly in EmailService"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Test failed with exception",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("GetBookingForScan/{bookingCode}")]
        public async Task<CommonResponseModel<BookingDetailsResponse>> GetBookingForScan(string bookingCode)
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

                return await _bookingService.GetBookingForScanningAsync(bookingCode);
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

        [HttpPost("ScanTicket")]
        public async Task<CommonResponseModel<TicketScanResponse>> ScanTicket([FromBody] ScanTicketRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.BookingCode))
                {
                    return new CommonResponseModel<TicketScanResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Booking code is required"
                    };
                }

                var adminEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(adminEmail))
                {
                    return new CommonResponseModel<TicketScanResponse>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "Admin authentication required"
                    };
                }

                return await _bookingService.ScanTicketAsync(request, adminEmail);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<TicketScanResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("PartialScan")]
        public async Task<CommonResponseModel<TicketScanResponse>> PartialScan([FromBody] PartialScanRequest request)
        {
            try
            {
                if (request == null)
                {
                    return new CommonResponseModel<TicketScanResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Request data is required"
                    };
                }

                var adminEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(adminEmail))
                {
                    return new CommonResponseModel<TicketScanResponse>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "Admin authentication required"
                    };
                }

                return await _bookingService.PartialScanTicketAsync(request, adminEmail);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<TicketScanResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetScanSummary/{bookingId}")]
        public async Task<CommonResponseModel<BookingScanSummaryResponse>> GetScanSummary(int bookingId)
        {
            try
            {
                if (bookingId <= 0)
                {
                    return new CommonResponseModel<BookingScanSummaryResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid booking ID is required"
                    };
                }

                return await _bookingService.GetBookingScanSummaryAsync(bookingId);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<BookingScanSummaryResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("ResetScan/{bookingId}")]
        public async Task<CommonResponseModel<bool>> ResetScan(int bookingId)
        {
            try
            {
                if (bookingId <= 0)
                {
                    return new CommonResponseModel<bool>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid booking ID is required",
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

                return await _bookingService.ResetScanCountAsync(bookingId, adminEmail);
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

        //private string GetCurrentUserEmail()
        //{
        //    return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ??
        //           _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;
        //}
        [AllowAnonymous]
        [HttpGet("GetBookingDetailsById/{bookingId}")]
        [Authorize]
        public async Task<CommonResponseModel<BookingDetailedResponse>> GetBookingDetailsById(int bookingId)
        {
            try
            {
                if (bookingId <= 0)
                {
                    return new CommonResponseModel<BookingDetailedResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid booking ID is required. Received: " + bookingId
                    };
                }

                // Optional: Check if the user has permission to view this booking
                var currentUserIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value;
                var currentUserRole = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;

                // For non-admin users, you might want to verify they own this booking
                if (currentUserRole != "Admin" && currentUserRole != "Organizer")
                {
                    // You could add additional check here to verify the booking belongs to the user
                    // This would require fetching the booking first or passing userId to the service
                }

                return await _bookingService.GetBookingDetailsByIdAsync(bookingId);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<BookingDetailedResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = $"An error occurred: {ex.Message}"
                };
            }
        }

        [HttpGet("GetEventSummary/{eventId}")]
        public async Task<CommonResponseModel<EventSummaryResponse>> GetEventSummaryByEventId(int eventId)
        {
            try
            {
                if (eventId <= 0)
                {
                    return new CommonResponseModel<EventSummaryResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event ID is required. Received: " + eventId
                    };
                }

                // Optional: Check if the user has permission to view this event's summary
                var currentUserRole = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
                var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value;

                // For non-admin users (organizers), you might want to verify they own this event
                // This would require additional logic to check if the organizer_id matches the current user

                return await _bookingService.GetEventSummaryByEventIdAsync(eventId);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<EventSummaryResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = $"An error occurred: {ex.Message}"
                };
            }
        }

        [HttpPost("GetPagedBookingHistoryByUserId")]
        public async Task<ActionResult<PagedBookingHistoryResponse>> GetPagedBookingHistoryByUserId([FromBody] BookingHistoryRequest request)
        {
            try
            {
                if (request.UserId == Guid.Empty)
                {
                    return BadRequest(new PagedBookingHistoryResponse
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid user ID is required",
                        Data = new List<BookingHistoryResponse>(),
                        TotalCount = 0,
                        TotalPages = 0,
                        CurrentPage = request.PageNumber,
                        PageSize = request.PageSize
                    });
                }

                // Validate pagination parameters
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1) request.PageSize = 10;
                if (request.PageSize > 100) request.PageSize = 100; // Max page size

                // Authorization check (optional - uncomment if needed)
                // var currentUserId = GetCurrentUserId();
                // var currentUserRole = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
                // if (currentUserRole != "Admin" && currentUserRole != "Organizer" && currentUserId != request.UserId)
                // {
                //     return Forbid();
                // }

                var result = await _bookingService.GetPagedBookingHistoryByUserIdAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new PagedBookingHistoryResponse
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message,
                    Data = new List<BookingHistoryResponse>(),
                    TotalCount = 0,
                    TotalPages = 0,
                    CurrentPage = request.PageNumber,
                    PageSize = request.PageSize
                });
            }
        }

        // Helper method to get current user ID from claims
        private Guid GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                              _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                return userId;
            }

            return Guid.Empty;
        }

        [HttpGet("diagnose-email-issue")]
        [AllowAnonymous]
        public async Task<IActionResult> DiagnoseEmailIssue()
        {
            var results = new Dictionary<string, object>();

            try
            {
                // Test 1: Check DNS records
                results["Domain"] = "tickethouse.in";

                // Test 2: Send a very simple text email first
                var simpleEmailResult = await _emailService.SendEmailAsync(
                    "kalokhepranjal@gmail.com",
                    "Simple Test - No HTML, No Attachment",
                    "This is a plain text test email. If you receive this, the issue is with HTML content or attachments."
                );
                results["SimpleTextEmail"] = simpleEmailResult ? "Sent" : "Failed";

                // Test 3: Send email with minimal HTML
                var minimalHtml = "<html><body><h1>Test</h1><p>Minimal HTML test</p></body></html>";
                var htmlEmailResult = await _emailService.SendEmailAsync(
                    "kalokhepranjal@gmail.com",
                    "Minimal HTML Test",
                    minimalHtml
                );
                results["MinimalHTMLEmail"] = htmlEmailResult ? "Sent" : "Failed";

                // Test 4: Check SPF/DKIM/DMARC (you'll need to implement these checks)
                results["SPF_Status"] = "Check your DNS: v=spf1 mx ~all";
                results["DKIM_Status"] = "Configure DKIM in your domain";
                results["DMARC_Status"] = "Check: _dmarc.tickethouse.in";

                // Test 5: Check if sending IP is blacklisted
                var ip = await GetPublicIPAsync();
                results["SendingServerIP"] = ip;
                results["BlacklistCheck"] = "Check at: https://mxtoolbox.com/blacklists.aspx";

                return Ok(new
                {
                    Success = true,
                    Message = "Diagnosis complete. See results below.",
                    Results = results,
                    Recommendations = new[]
                    {
                "1. Add SPF record: v=spf1 mx include:tickethouse.in ~all",
                "2. Set up DKIM signing for tickethouse.in",
                "3. Add DMARC record: v=DMARC1; p=none; rua=mailto:dmarc@tickethouse.in",
                "4. Ensure reverse DNS (PTR) matches your sending IP",
                "5. Warm up your sending IP by sending emails gradually",
                "6. Check if your IP is on any blacklist"
            }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        private async Task<string> GetPublicIPAsync()
        {
            using var client = new HttpClient();
            return await client.GetStringAsync("https://api.ipify.org");
        }
    }
}
