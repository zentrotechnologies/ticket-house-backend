using BAL.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MODEL.Response;

namespace TicketHouseBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserEventsController : ControllerBase
    {
        private readonly IUserEventsService _userEventsService;

        public UserEventsController(IUserEventsService userEventsService)
        {
            _userEventsService = userEventsService;
        }

        [HttpPost("GetUpcomingEvents")]
        public async Task<CommonResponseModel<IEnumerable<UpcomingEventResponse>>> GetUpcomingEvents([FromBody] UpcomingEventsRequest request)
        {
            try
            {
                if (request == null)
                {
                    return new CommonResponseModel<IEnumerable<UpcomingEventResponse>>
                    {
                        Status = "Failure",
                        Message = "Request data is required",
                        ErrorCode = "400"
                    };
                }

                return await _userEventsService.GetUpcomingEventsAsync(request);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<IEnumerable<UpcomingEventResponse>>
                {
                    Status = "Error",
                    Message = $"Error in GetUpcomingEvents: {ex.Message}",
                    ErrorCode = "1"
                };
            }
        }

        [HttpPost("GetShowsByArtists")]
        public async Task<CommonResponseModel<IEnumerable<ArtistResponse>>> GetShowsByArtists([FromBody] GetShowsByArtistsRequest request)
        {
            try
            {
                if (request == null)
                {
                    return new CommonResponseModel<IEnumerable<ArtistResponse>>
                    {
                        Status = "Failure",
                        Message = "Request data is required",
                        ErrorCode = "400"
                    };
                }

                return await _userEventsService.GetShowsByArtistsAsync(request);
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<IEnumerable<ArtistResponse>>
                {
                    Status = "Error",
                    Message = $"Error in GetShowsByArtists: {ex.Message}",
                    ErrorCode = "1"
                };
            }
        }

        [HttpGet("GetTestimonialsByArtists")]
        public async Task<CommonResponseModel<IEnumerable<TestimonialResponse>>> GetTestimonialsByArtists()
        {
            try
            {
                return await _userEventsService.GetTestimonialsByArtistsAsync();
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<IEnumerable<TestimonialResponse>>
                {
                    Status = "Error",
                    Message = $"Error in GetTestimonialsByArtists: {ex.Message}",
                    ErrorCode = "1"
                };
            }
        }

        // Optional: Simple GET endpoints with default values
        [HttpGet("GetUpcomingEventsDefault")]
        public async Task<CommonResponseModel<IEnumerable<UpcomingEventResponse>>> GetUpcomingEventsDefault()
        {
            var request = new UpcomingEventsRequest
            {
                Count = 8,
                IncludeLaterEvents = true
            };

            return await GetUpcomingEvents(request);
        }

        [HttpGet("GetShowsByArtistsDefault")]
        public async Task<CommonResponseModel<IEnumerable<ArtistResponse>>> GetShowsByArtistsDefault()
        {
            var request = new GetShowsByArtistsRequest
            {
                Count = 5
            };

            return await GetShowsByArtists(request);
        }
    }
}
