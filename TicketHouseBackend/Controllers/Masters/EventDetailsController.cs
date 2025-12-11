using BAL.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MODEL.Entities;
using MODEL.Request;
using MODEL.Response;
using Newtonsoft.Json;

namespace TicketHouseBackend.Controllers.Masters
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventDetailsController : ControllerBase
    {
        private readonly IEventDetailsService _eventDetailsService;
        private readonly IEventArtistGalleryService _eventArtistGalleryService;
        private readonly IWebHostEnvironment _environment;

        public EventDetailsController(IEventDetailsService eventDetailsService, IWebHostEnvironment environment, IEventArtistGalleryService eventArtistGalleryService)
        {
            _eventDetailsService = eventDetailsService;
            _environment = environment;
            _eventArtistGalleryService = eventArtistGalleryService;
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

        // New methods for artist and gallery handling

        [HttpPost("UploadArtistPhoto")]
        public async Task<CommonResponseModel<string>> UploadArtistPhoto([FromForm] ArtistUploadRequest request)
        {
            try
            {
                if (request?.ArtistPhoto == null)
                {
                    return new CommonResponseModel<string>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Artist photo is required"
                    };
                }

                if (string.IsNullOrEmpty(request.ArtistName))
                {
                    return new CommonResponseModel<string>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Artist name is required"
                    };
                }

                if (request.EventId <= 0)
                {
                    return new CommonResponseModel<string>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event ID is required"
                    };
                }

                var result = await _eventArtistGalleryService.UploadArtistPhotoAsync(
                    request.ArtistPhoto,
                    request.EventId,
                    request.ArtistName,
                    _environment.WebRootPath
                );

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

        [HttpPost("UploadGalleryImage")]
        public async Task<CommonResponseModel<string>> UploadGalleryImage([FromForm] GalleryUploadRequest request)
        {
            try
            {
                if (request?.GalleryImage == null)
                {
                    return new CommonResponseModel<string>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "File is required"
                    };
                }

                if (request.EventId <= 0)
                {
                    return new CommonResponseModel<string>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event ID is required"
                    };
                }

                var result = await _eventArtistGalleryService.UploadGalleryImageAsync(
                    request.GalleryImage,
                    request.EventId,
                    _environment.WebRootPath);
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

        [HttpPost("UploadEventBanner")]
        public async Task<CommonResponseModel<string>> UploadEventBanner([FromForm] BannerUploadRequest request)
        {
            try
            {
                if (request?.BannerImage == null)
                {
                    return new CommonResponseModel<string>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Banner image is required"
                    };
                }

                if (request.EventId <= 0)
                {
                    return new CommonResponseModel<string>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event ID is required"
                    };
                }

                var result = await _eventArtistGalleryService.UploadEventBannerAsync(
                    request.BannerImage,
                    request.EventId,
                    _environment.WebRootPath
                );

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

        [HttpPost("CreateEventWithArtistsAndGalleries")]
        //public async Task<CommonResponseModel<EventCompleteResponseModel>> CreateEventWithArtistsAndGalleries()
        //{
        //    try
        //    {
        //        // Get form data manually
        //        var form = await HttpContext.Request.ReadFormAsync();

        //        if (!form.TryGetValue("EventDetails", out var eventDetailsJson) || string.IsNullOrEmpty(eventDetailsJson))
        //        {
        //            return new CommonResponseModel<EventCompleteResponseModel>
        //            {
        //                ErrorCode = "400",
        //                Status = "Error",
        //                Message = "Event details are required"
        //            };
        //        }

        //        // Parse EventDetails from JSON
        //        var eventDetails = JsonConvert.DeserializeObject<EventDetailsModel>(eventDetailsJson);

        //        if (eventDetails == null)
        //        {
        //            return new CommonResponseModel<EventCompleteResponseModel>
        //            {
        //                ErrorCode = "400",
        //                Status = "Error",
        //                Message = "Invalid event details format"
        //            };
        //        }

        //        // Parse EventArtists
        //        var eventArtists = new List<EventArtistModel>();
        //        if (form.TryGetValue("EventArtists", out var eventArtistsJson) && !string.IsNullOrEmpty(eventArtistsJson))
        //        {
        //            eventArtists = JsonConvert.DeserializeObject<List<EventArtistModel>>(eventArtistsJson) ?? new List<EventArtistModel>();
        //        }

        //        // Parse EventGalleries
        //        var eventGalleries = new List<EventGalleryModel>();
        //        if (form.TryGetValue("EventGalleries", out var eventGalleriesJson) && !string.IsNullOrEmpty(eventGalleriesJson))
        //        {
        //            eventGalleries = JsonConvert.DeserializeObject<List<EventGalleryModel>>(eventGalleriesJson) ?? new List<EventGalleryModel>();
        //        }

        //        // Get banner file
        //        var bannerFile = form.Files["BannerImageFile"];

        //        // Create EventCreateRequestModel manually
        //        var eventRequest = new EventCreateRequestModel
        //        {
        //            EventDetails = eventDetails,
        //            EventArtists = eventArtists,
        //            EventGalleries = eventGalleries,
        //            BannerImageFile = bannerFile
        //        };

        //        // Get user from JWT token or form data
        //        var userEmail = User?.Identity?.Name;
        //        var createdBy = string.IsNullOrEmpty(userEmail) ?
        //            (form.TryGetValue("createdBy", out var createdByValue) ? createdByValue.ToString() : "system") :
        //            userEmail;

        //        var result = await _eventArtistGalleryService.CreateEventWithArtistsAndGalleriesAsync(
        //            eventRequest,
        //            _environment.WebRootPath,
        //            createdBy
        //        );

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        return new CommonResponseModel<EventCompleteResponseModel>
        //        {
        //            ErrorCode = "1",
        //            Status = "Error",
        //            Message = ex.Message
        //        };
        //    }
        //}

        public async Task<CommonResponseModel<EventCompleteResponseModel>> CreateEventWithArtistsAndGalleries()
        {
            try
            {
                // Get form data manually
                var form = await HttpContext.Request.ReadFormAsync();

                if (!form.TryGetValue("EventDetails", out var eventDetailsJson) || string.IsNullOrEmpty(eventDetailsJson))
                {
                    return new CommonResponseModel<EventCompleteResponseModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Event details are required"
                    };
                }

                // Parse EventDetails from JSON
                var eventDetails = JsonConvert.DeserializeObject<EventDetailsModel>(eventDetailsJson);

                if (eventDetails == null)
                {
                    return new CommonResponseModel<EventCompleteResponseModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Invalid event details format"
                    };
                }

                // Parse EventArtists
                var eventArtists = new List<EventArtistModel>();
                if (form.TryGetValue("EventArtists", out var eventArtistsJson) && !string.IsNullOrEmpty(eventArtistsJson))
                {
                    eventArtists = JsonConvert.DeserializeObject<List<EventArtistModel>>(eventArtistsJson) ?? new List<EventArtistModel>();
                }

                // Parse EventGalleries
                var eventGalleries = new List<EventGalleryModel>();
                if (form.TryGetValue("EventGalleries", out var eventGalleriesJson) && !string.IsNullOrEmpty(eventGalleriesJson))
                {
                    eventGalleries = JsonConvert.DeserializeObject<List<EventGalleryModel>>(eventGalleriesJson) ?? new List<EventGalleryModel>();
                }

                // Get banner file
                var bannerFile = form.Files["BannerImageFile"];

                // Create EventCreateRequestModel manually
                var eventRequest = new EventCreateRequestModel
                {
                    EventDetails = eventDetails,
                    EventArtists = eventArtists,
                    EventGalleries = eventGalleries,
                    BannerImageFile = bannerFile
                };

                // Get user from form data (passed from frontend)
                string createdBy = "system";
                if (form.TryGetValue("createdBy", out var createdByValue))
                {
                    createdBy = createdByValue.ToString();
                }
                else
                {
                    // Fallback to JWT token user ID
                    var userIdClaim = User?.FindFirst("userId")?.Value;
                    if (!string.IsNullOrEmpty(userIdClaim))
                    {
                        createdBy = userIdClaim;
                    }
                }

                // Validate user ID
                if (string.IsNullOrEmpty(createdBy) || createdBy == "system")
                {
                    return new CommonResponseModel<EventCompleteResponseModel>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required"
                    };
                }

                var result = await _eventArtistGalleryService.CreateEventWithArtistsAndGalleriesAsync(
                    eventRequest,
                    _environment.WebRootPath,
                    createdBy
                );

                return result;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<EventCompleteResponseModel>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("UpdateEventWithArtistsAndGalleries")]
        //public async Task<CommonResponseModel<EventCompleteResponseModel>> UpdateEventWithArtistsAndGalleries()
        //{
        //    try
        //    {
        //        // Get form data manually
        //        var form = await HttpContext.Request.ReadFormAsync();

        //        if (!form.TryGetValue("EventDetails", out var eventDetailsJson) || string.IsNullOrEmpty(eventDetailsJson))
        //        {
        //            return new CommonResponseModel<EventCompleteResponseModel>
        //            {
        //                ErrorCode = "400",
        //                Status = "Error",
        //                Message = "Event details are required"
        //            };
        //        }

        //        // Parse EventDetails from JSON
        //        var eventDetails = JsonConvert.DeserializeObject<EventDetailsModel>(eventDetailsJson);

        //        if (eventDetails == null || eventDetails.event_id <= 0)
        //        {
        //            return new CommonResponseModel<EventCompleteResponseModel>
        //            {
        //                ErrorCode = "400",
        //                Status = "Error",
        //                Message = "Valid event details are required"
        //            };
        //        }

        //        // Parse EventArtists
        //        var eventArtists = new List<EventArtistModel>();
        //        if (form.TryGetValue("EventArtists", out var eventArtistsJson) && !string.IsNullOrEmpty(eventArtistsJson))
        //        {
        //            eventArtists = JsonConvert.DeserializeObject<List<EventArtistModel>>(eventArtistsJson) ?? new List<EventArtistModel>();
        //        }

        //        // Parse EventGalleries
        //        var eventGalleries = new List<EventGalleryModel>();
        //        if (form.TryGetValue("EventGalleries", out var eventGalleriesJson) && !string.IsNullOrEmpty(eventGalleriesJson))
        //        {
        //            eventGalleries = JsonConvert.DeserializeObject<List<EventGalleryModel>>(eventGalleriesJson) ?? new List<EventGalleryModel>();
        //        }

        //        // Get banner file
        //        var bannerFile = form.Files["BannerImageFile"];

        //        // Create EventCreateRequestModel manually
        //        var eventRequest = new EventCreateRequestModel
        //        {
        //            EventDetails = eventDetails,
        //            EventArtists = eventArtists,
        //            EventGalleries = eventGalleries,
        //            BannerImageFile = bannerFile
        //        };

        //        // Get user from JWT token or form data
        //        var userEmail = User?.Identity?.Name;
        //        var updatedBy = string.IsNullOrEmpty(userEmail) ?
        //            (form.TryGetValue("updatedBy", out var updatedByValue) ? updatedByValue.ToString() : "system") :
        //            userEmail;

        //        var result = await _eventArtistGalleryService.UpdateEventWithArtistsAndGalleriesAsync(
        //            eventRequest,
        //            _environment.WebRootPath,
        //            updatedBy
        //        );

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        return new CommonResponseModel<EventCompleteResponseModel>
        //        {
        //            ErrorCode = "1",
        //            Status = "Error",
        //            Message = ex.Message
        //        };
        //    }
        //}

        public async Task<CommonResponseModel<EventCompleteResponseModel>> UpdateEventWithArtistsAndGalleries()
        {
            try
            {
                // Get form data manually
                var form = await HttpContext.Request.ReadFormAsync();

                if (!form.TryGetValue("EventDetails", out var eventDetailsJson) || string.IsNullOrEmpty(eventDetailsJson))
                {
                    return new CommonResponseModel<EventCompleteResponseModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Event details are required"
                    };
                }

                // Parse EventDetails from JSON
                var eventDetails = JsonConvert.DeserializeObject<EventDetailsModel>(eventDetailsJson);

                if (eventDetails == null || eventDetails.event_id <= 0)
                {
                    return new CommonResponseModel<EventCompleteResponseModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event details are required"
                    };
                }

                // Parse EventArtists
                var eventArtists = new List<EventArtistModel>();
                if (form.TryGetValue("EventArtists", out var eventArtistsJson) && !string.IsNullOrEmpty(eventArtistsJson))
                {
                    eventArtists = JsonConvert.DeserializeObject<List<EventArtistModel>>(eventArtistsJson) ?? new List<EventArtistModel>();
                }

                // Parse EventGalleries
                var eventGalleries = new List<EventGalleryModel>();
                if (form.TryGetValue("EventGalleries", out var eventGalleriesJson) && !string.IsNullOrEmpty(eventGalleriesJson))
                {
                    eventGalleries = JsonConvert.DeserializeObject<List<EventGalleryModel>>(eventGalleriesJson) ?? new List<EventGalleryModel>();
                }

                // Get banner file
                var bannerFile = form.Files["BannerImageFile"];

                // Create EventCreateRequestModel manually
                var eventRequest = new EventCreateRequestModel
                {
                    EventDetails = eventDetails,
                    EventArtists = eventArtists,
                    EventGalleries = eventGalleries,
                    BannerImageFile = bannerFile
                };

                // Get user from form data
                string updatedBy = "system";
                if (form.TryGetValue("updatedBy", out var updatedByValue))
                {
                    updatedBy = updatedByValue.ToString();
                }
                else
                {
                    // Fallback to JWT token user ID
                    var userIdClaim = User?.FindFirst("userId")?.Value;
                    if (!string.IsNullOrEmpty(userIdClaim))
                    {
                        updatedBy = userIdClaim;
                    }
                }

                // Validate user ID
                if (string.IsNullOrEmpty(updatedBy) || updatedBy == "system")
                {
                    return new CommonResponseModel<EventCompleteResponseModel>
                    {
                        ErrorCode = "401",
                        Status = "Error",
                        Message = "User authentication required"
                    };
                }

                var result = await _eventArtistGalleryService.UpdateEventWithArtistsAndGalleriesAsync(
                    eventRequest,
                    _environment.WebRootPath,
                    updatedBy
                );

                return result;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<EventCompleteResponseModel>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetEventWithArtistsAndGalleries/{eventId}")]
        public async Task<CommonResponseModel<EventCompleteResponseModel>> GetEventWithArtistsAndGalleries(int eventId)
        {
            try
            {
                if (eventId <= 0)
                {
                    return new CommonResponseModel<EventCompleteResponseModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event ID is required"
                    };
                }

                var result = await _eventArtistGalleryService.GetEventWithArtistsAndGalleriesAsync(eventId);
                return result;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<EventCompleteResponseModel>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("GetPaginatedEventsByCreatedBy")]
        public async Task<PagedResponse<List<EventCompleteResponseModel>>> GetPaginatedEventsByCreatedBy(
            [FromBody] EventPaginationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.created_by))
                {
                    // Get user from JWT token
                    var userEmail = User?.Identity?.Name;
                    request.created_by = string.IsNullOrEmpty(userEmail) ? "system" : userEmail;
                }

                var result = await _eventArtistGalleryService.GetPaginatedEventsByCreatedByAsync(request);
                return result;
            }
            catch (Exception ex)
            {
                return new PagedResponse<List<EventCompleteResponseModel>>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("DeleteEventWithArtistsAndGalleries/{eventId}")]
        public async Task<CommonResponseModel<bool>> DeleteEventWithArtistsAndGalleries(
            int eventId, [FromBody] string updatedBy = null)
        {
            try
            {
                if (eventId <= 0)
                {
                    return new CommonResponseModel<bool>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event ID is required",
                        Data = false
                    };
                }

                updatedBy ??= User?.Identity?.Name ?? "system";

                var result = await _eventArtistGalleryService.DeleteEventWithArtistsAndGalleriesAsync(eventId, updatedBy);
                return result;
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
    }
}
