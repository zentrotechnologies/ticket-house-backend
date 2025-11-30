using BAL.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MODEL.Entities;
using MODEL.Request;
using MODEL.Response;

namespace TicketHouseBackend.Controllers.Masters
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventDetailsController : ControllerBase
    {
        private readonly IEventDetailsService _eventDetailsService;
        private readonly IWebHostEnvironment _environment;

        public EventDetailsController(IEventDetailsService eventDetailsService, IWebHostEnvironment environment)
        {
            _eventDetailsService = eventDetailsService;
            _environment = environment;
        }

        [HttpGet("GetAllEvents")]
        public async Task<CommonResponseModel<IEnumerable<EventDetailsModel>>> GetAllEvents()
        {
            try
            {
                var events = await _eventDetailsService.GetAllEventsAsync();
                return events;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<IEnumerable<EventDetailsModel>>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetEventById/{eventId}")]
        public async Task<CommonResponseModel<EventResponse>> GetEventById(int eventId)
        {
            try
            {
                var eventData = await _eventDetailsService.GetEventByIdAsync(eventId);
                return eventData;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<EventResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("CreateEvent")]
        public async Task<CommonResponseModel<EventResponse>> CreateEvent([FromBody] CreateEventRequest eventRequest)
        {
            try
            {
                if (eventRequest == null || eventRequest.EventDetails == null)
                {
                    return new CommonResponseModel<EventResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Event data is required"
                    };
                }

                // For now, using a default user - you can get this from JWT token or session
                var createdBy = "system"; // Replace with actual user from authentication

                var result = await _eventDetailsService.CreateEventAsync(eventRequest, _environment.WebRootPath, createdBy);
                return result;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<EventResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("UpdateEvent")]
        public async Task<CommonResponseModel<EventResponse>> UpdateEvent([FromBody] CreateEventRequest eventRequest)
        {
            try
            {
                if (eventRequest == null || eventRequest.EventDetails == null || eventRequest.EventDetails.event_id <= 0)
                {
                    return new CommonResponseModel<EventResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event data is required"
                    };
                }

                // For now, using a default user - you can get this from JWT token or session
                var updatedBy = "system"; // Replace with actual user from authentication

                var result = await _eventDetailsService.UpdateEventAsync(eventRequest, _environment.WebRootPath, updatedBy);
                return result;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<EventResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("DeleteEvent/{eventId}")]
        public async Task<CommonResponseModel<EventResponse>> DeleteEvent(int eventId, [FromBody] string updatedBy = null)
        {
            try
            {
                if (eventId <= 0)
                {
                    return new CommonResponseModel<EventResponse>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event ID is required"
                    };
                }

                // Use provided updatedBy or default to "system"
                updatedBy ??= "system";

                var result = await _eventDetailsService.DeleteEventAsync(eventId, updatedBy);
                return result;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<EventResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("UploadEventMedia")]
        public async Task<CommonResponseModel<string>> UploadEventMedia(IFormFile file)
        {
            try
            {
                if (file == null)
                {
                    return new CommonResponseModel<string>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "File is required"
                    };
                }

                var result = await _eventDetailsService.SaveEventMediaAsync(file, _environment.WebRootPath);
                return result;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<string>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        //to fetch events and media together
        [HttpGet("GetAllEventsWithMedia")]
        public async Task<CommonResponseModel<IEnumerable<EventWithMediaResponse>>> GetAllEventsWithMedia()
        {
            try
            {
                var eventsWithMedia = await _eventDetailsService.GetAllEventsWithMediaAsync();
                return eventsWithMedia;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<IEnumerable<EventWithMediaResponse>>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetEventWithMediaById/{eventId}")]
        public async Task<CommonResponseModel<EventWithMediaResponse>> GetEventWithMediaById(int eventId)
        {
            try
            {
                var eventWithMedia = await _eventDetailsService.GetEventWithMediaByIdAsync(eventId);
                return eventWithMedia;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<EventWithMediaResponse>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetEventsWithMediaByCategory/{categoryId}")]
        public async Task<CommonResponseModel<IEnumerable<EventWithMediaResponse>>> GetEventsWithMediaByCategory(int categoryId)
        {
            try
            {
                if (categoryId <= 0)
                {
                    return new CommonResponseModel<IEnumerable<EventWithMediaResponse>>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid category ID is required"
                    };
                }

                var eventsWithMedia = await _eventDetailsService.GetEventsWithMediaByCategoryAsync(categoryId);
                return eventsWithMedia;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<IEnumerable<EventWithMediaResponse>>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetUpcomingEventsWithMedia")]
        public async Task<CommonResponseModel<IEnumerable<EventWithMediaResponse>>> GetUpcomingEventsWithMedia([FromQuery] int days = 30)
        {
            try
            {
                if (days <= 0)
                {
                    return new CommonResponseModel<IEnumerable<EventWithMediaResponse>>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Days must be greater than 0"
                    };
                }

                var eventsWithMedia = await _eventDetailsService.GetUpcomingEventsWithMediaAsync(days);
                return eventsWithMedia;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<IEnumerable<EventWithMediaResponse>>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetFeaturedEventsWithMedia")]
        public async Task<CommonResponseModel<IEnumerable<EventWithMediaResponse>>> GetFeaturedEventsWithMedia()
        {
            try
            {
                var eventsWithMedia = await _eventDetailsService.GetFeaturedEventsWithMediaAsync();
                return eventsWithMedia;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<IEnumerable<EventWithMediaResponse>>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }
    }
}
