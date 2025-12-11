using DAL.Repository;
using Microsoft.AspNetCore.Http;
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
    public interface IEventDetailsService
    {
        Task<CommonResponseModel<IEnumerable<EventDetailsModel>>> GetAllEventsAsync();
        Task<CommonResponseModel<EventResponse>> GetEventByIdAsync(int eventId);
        Task<CommonResponseModel<EventResponse>> CreateEventAsync(CreateEventRequest eventRequest, string webRootPath, string createdBy);
        Task<CommonResponseModel<EventResponse>> UpdateEventAsync(CreateEventRequest eventRequest, string webRootPath, string updatedBy);
        Task<CommonResponseModel<EventResponse>> DeleteEventAsync(int eventId, string updatedBy);
        Task<CommonResponseModel<string>> SaveEventMediaAsync(IFormFile file, string webRootPath);

        //to fetch events and media together
        Task<CommonResponseModel<IEnumerable<EventWithMediaResponse>>> GetAllEventsWithMediaAsync();
        Task<CommonResponseModel<EventWithMediaResponse>> GetEventWithMediaByIdAsync(int eventId);
        Task<CommonResponseModel<IEnumerable<EventWithMediaResponse>>> GetEventsWithMediaByCategoryAsync(int categoryId);
        Task<CommonResponseModel<IEnumerable<EventWithMediaResponse>>> GetUpcomingEventsWithMediaAsync(int days = 30);
        Task<CommonResponseModel<IEnumerable<EventWithMediaResponse>>> GetFeaturedEventsWithMediaAsync();
    }
    public class EventDetailsService: IEventDetailsService
    {
        private readonly IEventDetailsRepository _eventDetailsRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EventDetailsService(IEventDetailsRepository eventDetailsRepository, IHttpContextAccessor httpContextAccessor)
        {
            _eventDetailsRepository = eventDetailsRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CommonResponseModel<IEnumerable<EventDetailsModel>>> GetAllEventsAsync()
        {
            var response = new CommonResponseModel<IEnumerable<EventDetailsModel>>();
            try
            {
                var events = await _eventDetailsRepository.GetAllEventsAsync();
                response.Status = "Success";
                response.Message = "Events fetched successfully";
                response.ErrorCode = "0";
                response.Data = events;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving events: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<EventResponse>> GetEventByIdAsync(int eventId)
        {
            var response = new CommonResponseModel<EventResponse>();
            try
            {
                var eventDetails = await _eventDetailsRepository.GetEventByIdAsync(eventId);
                if (eventDetails != null)
                {
                    var eventMedia = await _eventDetailsRepository.GetEventMediaByEventIdAsync(eventId);

                    response.Status = "Success";
                    response.Message = "Event fetched successfully";
                    response.ErrorCode = "0";
                    response.Data = new EventResponse
                    {
                        EventDetails = eventDetails,
                        EventMedia = eventMedia.ToList()
                    };
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Event not found";
                    response.ErrorCode = "404";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving event: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<EventResponse>> CreateEventAsync(CreateEventRequest eventRequest, string webRootPath, string createdBy)
        {
            var response = new CommonResponseModel<EventResponse>();
            try
            {
                // Get current user ID from JWT token
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty)
                {
                    response.Status = "Failure";
                    response.Message = "User not authenticated";
                    response.ErrorCode = "401";
                    return response;
                }

                // Get organizer ID for the current user
                var organizerMapping = await _eventDetailsRepository.GetOrganizerByUserIdAsync(userId);
                if (organizerMapping == null)
                {
                    response.Status = "Failure";
                    response.Message = "User is not registered as an organizer";
                    response.ErrorCode = "403";
                    return response;
                }

                // Set organizer ID from the mapping
                eventRequest.EventDetails.organizer_id = organizerMapping.organizer_id;

                // Set created by user
                eventRequest.EventDetails.created_by = createdBy;
                eventRequest.EventDetails.updated_by = createdBy;

                // Calculate total duration
                eventRequest.EventDetails.total_duration_minutes = CalculateDuration(
                    eventRequest.EventDetails.start_time,
                    eventRequest.EventDetails.end_time
                );

                // Create event details
                var eventId = await _eventDetailsRepository.CreateEventAsync(eventRequest.EventDetails);

                if (eventId > 0)
                {
                    eventRequest.EventDetails.event_id = eventId;

                    // Add event media if any
                    var eventMediaList = new List<EventMediaModel>();
                    if (eventRequest.EventMedia != null && eventRequest.EventMedia.Count > 0)
                    {
                        foreach (var media in eventRequest.EventMedia)
                        {
                            media.event_id = eventId;
                            media.created_by = createdBy;
                            media.updated_by = createdBy;

                            var mediaId = await _eventDetailsRepository.AddEventMediaAsync(media);
                            if (mediaId > 0)
                            {
                                media.event_media_id = mediaId;
                                eventMediaList.Add(media);
                            }
                        }
                    }

                    response.Status = "Success";
                    response.Message = "Event created successfully";
                    response.ErrorCode = "0";
                    response.Data = new EventResponse
                    {
                        EventDetails = eventRequest.EventDetails,
                        EventMedia = eventMediaList
                    };
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to create event";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error creating event: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<EventResponse>> UpdateEventAsync(CreateEventRequest eventRequest, string webRootPath, string updatedBy)
        {
            var response = new CommonResponseModel<EventResponse>();
            try
            {
                // Check if event exists
                var existingEvent = await _eventDetailsRepository.GetEventByIdAsync(eventRequest.EventDetails.event_id);
                if (existingEvent == null)
                {
                    response.Status = "Failure";
                    response.Message = "Event not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Set updated by user
                eventRequest.EventDetails.updated_by = updatedBy;

                // Calculate total duration
                eventRequest.EventDetails.total_duration_minutes = CalculateDuration(
                    eventRequest.EventDetails.start_time,
                    eventRequest.EventDetails.end_time
                );

                // Update event details
                var affectedRows = await _eventDetailsRepository.UpdateEventAsync(eventRequest.EventDetails);

                if (affectedRows > 0)
                {
                    // Delete existing media and add new ones
                    if (eventRequest.EventMedia != null && eventRequest.EventMedia.Count > 0)
                    {
                        // Delete all existing media
                        await _eventDetailsRepository.DeleteAllEventMediaAsync(eventRequest.EventDetails.event_id, updatedBy);

                        // Add new media
                        var eventMediaList = new List<EventMediaModel>();
                        foreach (var media in eventRequest.EventMedia)
                        {
                            media.event_id = eventRequest.EventDetails.event_id;
                            media.created_by = updatedBy;
                            media.updated_by = updatedBy;

                            var mediaId = await _eventDetailsRepository.AddEventMediaAsync(media);
                            if (mediaId > 0)
                            {
                                media.event_media_id = mediaId;
                                eventMediaList.Add(media);
                            }
                        }

                        response.Status = "Success";
                        response.Message = "Event updated successfully";
                        response.ErrorCode = "0";
                        response.Data = new EventResponse
                        {
                            EventDetails = eventRequest.EventDetails,
                            EventMedia = eventMediaList
                        };
                    }
                    else
                    {
                        // Get existing media
                        var existingMedia = await _eventDetailsRepository.GetEventMediaByEventIdAsync(eventRequest.EventDetails.event_id);

                        response.Status = "Success";
                        response.Message = "Event updated successfully";
                        response.ErrorCode = "0";
                        response.Data = new EventResponse
                        {
                            EventDetails = eventRequest.EventDetails,
                            EventMedia = existingMedia.ToList()
                        };
                    }
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to update event";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error updating event: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<EventResponse>> DeleteEventAsync(int eventId, string updatedBy)
        {
            var response = new CommonResponseModel<EventResponse>();
            try
            {
                // Check if event exists
                var existingEvent = await _eventDetailsRepository.GetEventByIdAsync(eventId);
                if (existingEvent == null)
                {
                    response.Status = "Failure";
                    response.Message = "Event not found";
                    response.ErrorCode = "404";
                    return response;
                }

                // Delete event (soft delete)
                var affectedRows = await _eventDetailsRepository.DeleteEventAsync(eventId, updatedBy);

                if (affectedRows > 0)
                {
                    // Also delete associated media
                    await _eventDetailsRepository.DeleteAllEventMediaAsync(eventId, updatedBy);

                    response.Status = "Success";
                    response.Message = "Event deleted successfully";
                    response.ErrorCode = "0";
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to delete event";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error deleting event: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<string>> SaveEventMediaAsync(IFormFile file, string webRootPath)
        {
            var response = new CommonResponseModel<string>();
            try
            {
                if (file == null || file.Length == 0)
                {
                    response.Status = "Failure";
                    response.Message = "No file uploaded";
                    response.ErrorCode = "400";
                    return response;
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".avi", ".mov" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                {
                    response.Status = "Failure";
                    response.Message = "Invalid file type. Allowed types: jpg, jpeg, png, gif, mp4, avi, mov";
                    response.ErrorCode = "400";
                    return response;
                }

                // Create directory if it doesn't exist
                var mediaFolder = Path.Combine(webRootPath, "Assets", "Event_Media");
                if (!Directory.Exists(mediaFolder))
                {
                    Directory.CreateDirectory(mediaFolder);
                }

                // Generate unique file name
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(mediaFolder, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative URL for database storage
                var mediaUrl = $"/Assets/Event_Media/{fileName}";

                response.Status = "Success";
                response.Message = "File uploaded successfully";
                response.ErrorCode = "0";
                response.Data = mediaUrl;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error uploading file: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        //to fetch events and media together
        public async Task<CommonResponseModel<IEnumerable<EventWithMediaResponse>>> GetAllEventsWithMediaAsync()
        {
            var response = new CommonResponseModel<IEnumerable<EventWithMediaResponse>>();
            try
            {
                var eventsWithMedia = await _eventDetailsRepository.GetAllEventsWithMediaAsync();

                response.Status = "Success";
                response.Message = "Events with media fetched successfully";
                response.ErrorCode = "0";
                response.Data = eventsWithMedia;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving events with media: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<EventWithMediaResponse>> GetEventWithMediaByIdAsync(int eventId)
        {
            var response = new CommonResponseModel<EventWithMediaResponse>();
            try
            {
                var eventWithMedia = await _eventDetailsRepository.GetEventWithMediaByIdAsync(eventId);

                if (eventWithMedia != null)
                {
                    response.Status = "Success";
                    response.Message = "Event with media fetched successfully";
                    response.ErrorCode = "0";
                    response.Data = eventWithMedia;
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Event not found";
                    response.ErrorCode = "404";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving event with media: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<IEnumerable<EventWithMediaResponse>>> GetEventsWithMediaByCategoryAsync(int categoryId)
        {
            var response = new CommonResponseModel<IEnumerable<EventWithMediaResponse>>();
            try
            {
                if (categoryId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid category ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                var eventsWithMedia = await _eventDetailsRepository.GetEventsWithMediaByCategoryAsync(categoryId);

                response.Status = "Success";
                response.Message = $"Events in category ID '{categoryId}' with media fetched successfully";
                response.ErrorCode = "0";
                response.Data = eventsWithMedia;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving events by category with media: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<IEnumerable<EventWithMediaResponse>>> GetUpcomingEventsWithMediaAsync(int days = 30)
        {
            var response = new CommonResponseModel<IEnumerable<EventWithMediaResponse>>();
            try
            {
                var eventsWithMedia = await _eventDetailsRepository.GetUpcomingEventsWithMediaAsync(days);

                response.Status = "Success";
                response.Message = $"Upcoming events with media fetched successfully";
                response.ErrorCode = "0";
                response.Data = eventsWithMedia;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving upcoming events with media: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<IEnumerable<EventWithMediaResponse>>> GetFeaturedEventsWithMediaAsync()
        {
            var response = new CommonResponseModel<IEnumerable<EventWithMediaResponse>>();
            try
            {
                var eventsWithMedia = await _eventDetailsRepository.GetFeaturedEventsWithMediaAsync();

                response.Status = "Success";
                response.Message = "Featured events with media fetched successfully";
                response.ErrorCode = "0";
                response.Data = eventsWithMedia;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving featured events with media: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        private int CalculateDuration(TimeSpan startTime, TimeSpan endTime)
        {
            if (endTime < startTime)
            {
                // Handle overnight events
                var duration = (TimeSpan.FromHours(24) - startTime) + endTime;
                return (int)duration.TotalMinutes;
            }
            else
            {
                var duration = endTime - startTime;
                return (int)duration.TotalMinutes;
            }
        }

        private Guid GetCurrentUserId()
        {
            // Get user ID from JWT claims
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Guid.Empty;
            }

            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                return userId;
            }

            return Guid.Empty;
        }
    }
}
