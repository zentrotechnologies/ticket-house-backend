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
    public class BannerManagementController : ControllerBase
    {
        private readonly IBannerManagementService _bannerService;

        public BannerManagementController(IBannerManagementService bannerService)
        {
            _bannerService = bannerService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        }

        private string GetCurrentUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? "system";
        }

        [HttpGet("GetAllBanners")]
        [AllowAnonymous]
        public async Task<ActionResult<CommonResponseModel<IEnumerable<object>>>> GetAllBanners()
        {
            var response = await _bannerService.GetAllBanners();
            return Ok(response);
        }

        [HttpGet("GetBannerById/{bannerId}")]
        [AllowAnonymous]
        public async Task<ActionResult<CommonResponseModel<object>>> GetBannerById(int bannerId)
        {
            var response = await _bannerService.GetBannerById(bannerId);
            return Ok(response);
        }

        [HttpPost("CreateBanner")]
        public async Task<ActionResult<CommonResponseModel<int>>> CreateBanner([FromBody] CreateBannerRequest request)
        {
            // Set the current user as creator
            request.created_by = GetCurrentUserName();

            var response = await _bannerService.CreateBanner(request);
            return Ok(response);
        }

        [HttpPost("UpdateBanner/{bannerId}")]
        public async Task<ActionResult<CommonResponseModel<bool>>> UpdateBanner(int bannerId, [FromBody] UpdateBannerRequest request)
        {
            // Set the current user as updater
            request.updated_by = GetCurrentUserName();

            var response = await _bannerService.UpdateBanner(bannerId, request);
            return Ok(response);
        }

        [HttpPost("DeleteBanner/{bannerId}")]
        public async Task<ActionResult<CommonResponseModel<bool>>> DeleteBanner(int bannerId)
        {
            var updatedBy = GetCurrentUserName();
            var response = await _bannerService.DeleteBanner(bannerId, updatedBy);
            return Ok(response);
        }
    }
}
