using BAL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MODEL.Request;
using MODEL.Response;
using System.Security.Claims;

namespace TicketHouseBackend.Controllers.Masters
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("GetPaginatedOrganizers")]
        public async Task<ActionResult<OrganizerPagedResponse>> GetPaginatedOrganizers([FromBody] PaginationRequest request)
        {
            var response = await _userService.GetPaginatedOrganizers(request);
            return Ok(response);
        }

        [HttpGet("GetOrganizerById/{organizerId}")]
        public async Task<ActionResult<CommonResponseModel<OrganizerResponse>>> GetOrganizerById(Guid organizerId)
        {
            var response = await _userService.GetOrganizerById(organizerId);
            return Ok(response);
        }

        [HttpPost("AddOrganizer")]
        //[Authorize(Roles = "1")] // Only Super Admin (role_id = 1) can add organizers
        public async Task<ActionResult<CommonResponseModel<OrganizerResponse>>> AddOrganizer([FromBody] OrganizerRequest request)
        {
            // Get current user from JWT token
            var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserEmail) || string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new CommonResponseModel<OrganizerResponse>
                {
                    Status = "Failure",
                    Success = false,
                    Message = "User not authenticated",
                    ErrorCode = "1"
                });
            }

            request.created_by = currentUserId;  // Use user_id instead of email
            request.updated_by = currentUserId;

            var response = await _userService.AddOrganizer(request);

            if (response.Success == true)
                return Ok(response);
            else
                return BadRequest(response);
        }

        [HttpPost("UpdateOrganizer/{organizerId}")]
        //[Authorize(Roles = "1,2")] // Super Admin and Admin can update
        public async Task<ActionResult<CommonResponseModel<OrganizerResponse>>> UpdateOrganizer(
            Guid organizerId,
            [FromBody] OrganizerRequest request)
        {
            // Get current user from JWT token
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new CommonResponseModel<OrganizerResponse>
                {
                    Status = "Failure",
                    Success = false,
                    Message = "User not authenticated",
                    ErrorCode = "1"
                });
            }

            request.updated_by = currentUserId;

            //var response = await _userService.UpdateOrganizer(request, organizerId);

            // Use the combined update method
            var response = await _userService.UpdateOrganizerWithUserDetails(request, organizerId);

            if (response.Success == true)
                return Ok(response);
            else
                return BadRequest(response);
        }

        [HttpPost("DeleteOrganizer/{organizerId}")]
        //[Authorize(Roles = "1")] // Only Super Admin can delete
        public async Task<ActionResult<CommonResponseModel<bool>>> DeleteOrganizer(Guid organizerId)
        {
            // Get current user from JWT token
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new CommonResponseModel<bool>
                {
                    Status = "Failure",
                    Success = false,
                    Message = "User not authenticated",
                    ErrorCode = "1",
                    Data = false
                });
            }

            var response = await _userService.DeleteOrganizer(organizerId, currentUserId);

            if (response.Success == true)
                return Ok(response);
            else
                return BadRequest(response);
        }

        [HttpPost("UpdateOrganizerStatus/{organizerId}")]
        //[Authorize(Roles = "1,2")] // Super Admin and Admin can update status
        public async Task<ActionResult<CommonResponseModel<bool>>> UpdateOrganizerStatus(
            Guid organizerId,
            [FromBody] UpdateStatusRequest statusRequest)
        {
            // Get current user from JWT token
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new CommonResponseModel<bool>
                {
                    Status = "Failure",
                    Success = false,
                    Message = "User not authenticated",
                    ErrorCode = "1",
                    Data = false
                });
            }

            var response = await _userService.UpdateOrganizerStatus(
                organizerId,
                statusRequest.Status,
                currentUserId);

            if (response.Success == true)
                return Ok(response);
            else
                return BadRequest(response);
        }
    }
}
