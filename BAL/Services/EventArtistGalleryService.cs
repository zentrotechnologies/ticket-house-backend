using DAL.Repository;
using DAL.Utilities;
using Microsoft.AspNetCore.Http;
using MODEL.Request;
using MODEL.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BAL.Services
{
    public interface IEventArtistGalleryService
    {
        // Upload methods with event_id
        Task<CommonResponseModel<string>> UploadArtistPhotoAsync(IFormFile file, int eventId, string artistName, string webRootPath);
        Task<CommonResponseModel<string>> UploadGalleryImageAsync(IFormFile file, int eventId, string webRootPath);
        Task<CommonResponseModel<string>> UploadEventBannerAsync(IFormFile file, int eventId, string webRootPath);

        // Complete event operations
        Task<CommonResponseModel<EventCompleteResponseModel>> CreateEventWithArtistsAndGalleriesAsync(
            EventCreateRequestModel eventRequest, string webRootPath, string createdBy);

        Task<CommonResponseModel<EventCompleteResponseModel>> UpdateEventWithArtistsAndGalleriesAsync(
            EventCreateRequestModel eventRequest, string webRootPath, string updatedBy);

        Task<CommonResponseModel<EventCompleteResponseModel>> GetEventWithArtistsAndGalleriesAsync(int eventId);

        // Paginated events by created_by
        Task<PagedResponse<List<EventCompleteResponseModel>>> GetPaginatedEventsByCreatedByAsync(
            EventPaginationRequest request);

        // Delete operations
        Task<CommonResponseModel<bool>> DeleteEventWithArtistsAndGalleriesAsync(int eventId, string updatedBy);
    }
    public class EventArtistGalleryService: IEventArtistGalleryService
    {
        private readonly IEventDetailsRepository _eventDetailsRepository;
        private readonly IEventArtistGalleryRepository _eventArtistGalleryRepository;
        private readonly IFileUploadHelper _fileUploadHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EventArtistGalleryService(
            IEventDetailsRepository eventDetailsRepository,
            IFileUploadHelper fileUploadHelper,
            IHttpContextAccessor httpContextAccessor,
            IEventArtistGalleryRepository eventArtistGalleryRepository)
        {
            _eventDetailsRepository = eventDetailsRepository;
            _fileUploadHelper = fileUploadHelper;
            _httpContextAccessor = httpContextAccessor;
            _eventArtistGalleryRepository = eventArtistGalleryRepository;
        }

        public async Task<CommonResponseModel<string>> UploadArtistPhotoAsync(IFormFile file, int eventId, string artistName, string webRootPath)
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

                if (string.IsNullOrEmpty(artistName))
                {
                    response.Status = "Failure";
                    response.Message = "Artist name is required";
                    response.ErrorCode = "400";
                    return response;
                }

                if (eventId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid event ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                // Upload the file
                var fileUrl = await _fileUploadHelper.UploadArtistPhoto(file, eventId, artistName, webRootPath);

                response.Status = "Success";
                response.Message = "Artist photo uploaded successfully";
                response.ErrorCode = "0";
                response.Data = fileUrl;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error uploading artist photo: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<string>> UploadGalleryImageAsync(IFormFile file, int eventId, string webRootPath)
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

                if (eventId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid event ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                // Upload the file
                var fileUrl = await _fileUploadHelper.UploadGalleryImage(file, eventId, webRootPath);

                response.Status = "Success";
                response.Message = "Gallery image uploaded successfully";
                response.ErrorCode = "0";
                response.Data = fileUrl;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error uploading gallery image: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<string>> UploadEventBannerAsync(IFormFile file, int eventId, string webRootPath)
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

                if (eventId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid event ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                // Upload the file
                var fileUrl = await _fileUploadHelper.UploadEventBannerImage(file, eventId, webRootPath);

                response.Status = "Success";
                response.Message = "Event banner uploaded successfully";
                response.ErrorCode = "0";
                response.Data = fileUrl;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error uploading event banner: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        //public async Task<CommonResponseModel<EventCompleteResponseModel>> CreateEventWithArtistsAndGalleriesAsync(
        //    EventCreateRequestModel eventRequest, string webRootPath, string createdBy)
        //{
        //    var response = new CommonResponseModel<EventCompleteResponseModel>();
        //    try
        //    {
        //        if (eventRequest?.EventDetails == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Event details are required";
        //            response.ErrorCode = "400";
        //            return response;
        //        }

        //        // Get current user ID
        //        var userId = GetCurrentUserId();
        //        if (userId == Guid.Empty)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "User not authenticated";
        //            response.ErrorCode = "401";
        //            return response;
        //        }

        //        // Get organizer ID for the current user
        //        var organizerMapping = await _eventDetailsRepository.GetOrganizerByUserIdAsync(userId);
        //        if (organizerMapping == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "User is not registered as an organizer";
        //            response.ErrorCode = "403";
        //            return response;
        //        }

        //        // Set organizer ID from the mapping
        //        eventRequest.EventDetails.organizer_id = organizerMapping.organizer_id;

        //        // Set created by user
        //        eventRequest.EventDetails.created_by = createdBy;
        //        eventRequest.EventDetails.updated_by = createdBy;

        //        // Calculate total duration
        //        eventRequest.EventDetails.total_duration_minutes = CalculateDuration(
        //            eventRequest.EventDetails.start_time,
        //            eventRequest.EventDetails.end_time
        //        );

        //        // Set default status if not provided
        //        if (string.IsNullOrEmpty(eventRequest.EventDetails.status))
        //        {
        //            eventRequest.EventDetails.status = "draft";
        //        }

        //        // Create event in database
        //        var eventId = await _eventDetailsRepository.CreateEventAsync(eventRequest.EventDetails);

        //        if (eventId > 0)
        //        {
        //            eventRequest.EventDetails.event_id = eventId;

        //            var result = new EventCompleteResponseModel
        //            {
        //                EventDetails = eventRequest.EventDetails
        //            };

        //            // Upload and save banner if provided
        //            if (eventRequest.BannerImageFile != null && eventRequest.BannerImageFile.Length > 0)
        //            {
        //                var bannerUrl = await UploadEventBannerAsync(
        //                    eventRequest.BannerImageFile,
        //                    eventId,
        //                    webRootPath);

        //                if (bannerUrl.Status == "Success")
        //                {
        //                    eventRequest.EventDetails.banner_image = bannerUrl.Data;
        //                    // Update event with banner URL
        //                    await _eventDetailsRepository.UpdateEventAsync(eventRequest.EventDetails);
        //                }
        //            }

        //            // Add artists if any
        //            if (eventRequest.EventArtists != null && eventRequest.EventArtists.Count > 0)
        //            {
        //                foreach (var artist in eventRequest.EventArtists)
        //                {
        //                    artist.event_id = eventId;
        //                    artist.created_by = createdBy;
        //                    artist.updated_by = createdBy;

        //                    var artistId = await _eventDetailsRepository.AddEventArtistAsync(artist);
        //                    if (artistId > 0)
        //                    {
        //                        artist.event_artist_id = artistId;
        //                        result.EventArtists.Add(artist);
        //                    }
        //                }
        //            }

        //            // Add galleries if any
        //            if (eventRequest.EventGalleries != null && eventRequest.EventGalleries.Count > 0)
        //            {
        //                foreach (var gallery in eventRequest.EventGalleries)
        //                {
        //                    gallery.event_id = eventId;
        //                    gallery.created_by = createdBy;
        //                    gallery.updated_by = createdBy;

        //                    var galleryId = await _eventDetailsRepository.AddEventGalleryAsync(gallery);
        //                    if (galleryId > 0)
        //                    {
        //                        gallery.event_gallary_id = galleryId;
        //                        result.EventGalleries.Add(gallery);
        //                    }
        //                }
        //            }

        //            response.Status = "Success";
        //            response.Message = "Event created successfully with artists and galleries";
        //            response.ErrorCode = "0";
        //            response.Data = result;
        //        }
        //        else
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Failed to create event";
        //            response.ErrorCode = "1";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Status = "Failure";
        //        response.Message = $"Error creating event: {ex.Message}";
        //        response.ErrorCode = "1";
        //    }

        //    return response;
        //}

        public async Task<CommonResponseModel<EventCompleteResponseModel>> CreateEventWithArtistsAndGalleriesAsync(
            EventCreateRequestModel eventRequest, string webRootPath, string createdBy)
        {
            var response = new CommonResponseModel<EventCompleteResponseModel>();
            try
            {
                if (eventRequest?.EventDetails == null)
                {
                    response.Status = "Failure";
                    response.Message = "Event details are required";
                    response.ErrorCode = "400";
                    return response;
                }

                // Parse createdBy as Guid (user_id)
                if (!Guid.TryParse(createdBy, out Guid userId))
                {
                    // If createdBy is not a valid Guid, try to get user ID from email
                    userId = _eventArtistGalleryRepository.LookupUserIdByEmail(createdBy);
                    if (userId == Guid.Empty)
                    {
                        response.Status = "Failure";
                        response.Message = "Invalid user ID or user not found";
                        response.ErrorCode = "400";
                        return response;
                    }
                }

                // Get organizer ID for the current user
                var organizerMapping = await _eventDetailsRepository.GetOrganizerByUserIdAsync(userId);
                Guid organizerId;

                if (organizerMapping == null)
                {
                    // If user is not an organizer, use user_id as organizer_id
                    organizerId = userId;
                    // Optionally, you can create an organizer record here
                }
                else
                {
                    organizerId = organizerMapping.organizer_id;
                }

                // Set organizer ID
                eventRequest.EventDetails.organizer_id = organizerId;

                // Set created by and updated by user
                eventRequest.EventDetails.created_by = userId.ToString();
                eventRequest.EventDetails.updated_by = userId.ToString();

                // Calculate total duration
                eventRequest.EventDetails.total_duration_minutes = CalculateDuration(
                    eventRequest.EventDetails.start_time,
                    eventRequest.EventDetails.end_time
                );

                // Set default status if not provided
                if (string.IsNullOrEmpty(eventRequest.EventDetails.status))
                {
                    eventRequest.EventDetails.status = "active";
                }

                // **Handle banner image as Base64 - CRITICAL FIX**
                if (eventRequest.BannerImageFile != null && eventRequest.BannerImageFile.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await eventRequest.BannerImageFile.CopyToAsync(memoryStream);
                    var bannerBytes = memoryStream.ToArray();
                    var bannerBase64 = Convert.ToBase64String(bannerBytes);

                    // Store as Base64 string in the banner_image field
                    eventRequest.EventDetails.banner_image = $"data:image/png;base64,{bannerBase64}";
                }
                else
                {
                    // If no banner image, set to empty string
                    eventRequest.EventDetails.banner_image = "";
                }

                // Create event in database
                var eventId = await _eventDetailsRepository.CreateEventAsync(eventRequest.EventDetails);

                if (eventId > 0)
                {
                    eventRequest.EventDetails.event_id = eventId;

                    var result = new EventCompleteResponseModel
                    {
                        EventDetails = eventRequest.EventDetails
                    };

                    // Upload and save banner if provided
                    if (eventRequest.BannerImageFile != null && eventRequest.BannerImageFile.Length > 0)
                    {
                        var bannerUrl = await UploadEventBannerAsync(
                            eventRequest.BannerImageFile,
                            eventId,
                            webRootPath);

                        if (bannerUrl.Status == "Success")
                        {
                            eventRequest.EventDetails.banner_image = bannerUrl.Data;
                            // Update event with banner URL
                            await _eventDetailsRepository.UpdateEventAsync(eventRequest.EventDetails);
                        }
                    }

                    // Add artists if any
                    if (eventRequest.EventArtists != null && eventRequest.EventArtists.Count > 0)
                    {
                        foreach (var artist in eventRequest.EventArtists)
                        {
                            artist.event_id = eventId;
                            artist.created_by = userId.ToString();
                            artist.updated_by = userId.ToString();

                            var artistId = await _eventDetailsRepository.AddEventArtistAsync(artist);
                            if (artistId > 0)
                            {
                                artist.event_artist_id = artistId;
                                result.EventArtists.Add(artist);
                            }
                        }
                    }

                    // Add galleries if any
                    if (eventRequest.EventGalleries != null && eventRequest.EventGalleries.Count > 0)
                    {
                        foreach (var gallery in eventRequest.EventGalleries)
                        {
                            gallery.event_id = eventId;
                            gallery.created_by = userId.ToString();
                            gallery.updated_by = userId.ToString();

                            var galleryId = await _eventDetailsRepository.AddEventGalleryAsync(gallery);
                            if (galleryId > 0)
                            {
                                gallery.event_gallary_id = galleryId;
                                result.EventGalleries.Add(gallery);
                            }
                        }
                    }

                    // Add seat types if any
                    if (eventRequest.SeatTypes != null && eventRequest.SeatTypes.Count > 0)
                    {
                        foreach (var seatType in eventRequest.SeatTypes)
                        {
                            seatType.event_id = eventId;
                            seatType.created_by = userId.ToString();
                            seatType.updated_by = userId.ToString();

                            var seatTypeId = await _eventDetailsRepository.AddEventSeatTypeAsync(seatType);
                            if (seatTypeId > 0)
                            {
                                seatType.event_seat_type_inventory_id = seatTypeId;
                                result.SeatTypes.Add(seatType);
                            }
                        }
                    }

                    response.Status = "Success";
                    response.Message = "Event created successfully with artists and galleries";
                    response.ErrorCode = "0";
                    response.Data = result;
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

        //public async Task<CommonResponseModel<EventCompleteResponseModel>> UpdateEventWithArtistsAndGalleriesAsync(
        //    EventCreateRequestModel eventRequest, string webRootPath, string updatedBy)
        //{
        //    var response = new CommonResponseModel<EventCompleteResponseModel>();
        //    try
        //    {
        //        if (eventRequest?.EventDetails == null || eventRequest.EventDetails.event_id <= 0)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Valid event details are required";
        //            response.ErrorCode = "400";
        //            return response;
        //        }

        //        // Check if event exists
        //        var existingEvent = await _eventDetailsRepository.GetEventByIdAsync(eventRequest.EventDetails.event_id);
        //        if (existingEvent == null)
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Event not found";
        //            response.ErrorCode = "404";
        //            return response;
        //        }

        //        // Set updated by user
        //        eventRequest.EventDetails.updated_by = updatedBy;

        //        // Calculate total duration
        //        eventRequest.EventDetails.total_duration_minutes = CalculateDuration(
        //            eventRequest.EventDetails.start_time,
        //            eventRequest.EventDetails.end_time
        //        );

        //        // Upload and update banner if provided
        //        if (eventRequest.BannerImageFile != null && eventRequest.BannerImageFile.Length > 0)
        //        {
        //            var bannerUrl = await UploadEventBannerAsync(
        //                eventRequest.BannerImageFile,
        //                eventRequest.EventDetails.event_id,
        //                webRootPath);

        //            if (bannerUrl.Status == "Success")
        //            {
        //                eventRequest.EventDetails.banner_image = bannerUrl.Data;
        //            }
        //        }
        //        else
        //        {
        //            // Keep existing banner
        //            eventRequest.EventDetails.banner_image = existingEvent.banner_image;
        //        }

        //        // Update event details
        //        var affectedRows = await _eventDetailsRepository.UpdateEventAsync(eventRequest.EventDetails);

        //        if (affectedRows > 0)
        //        {
        //            // Delete existing artists and galleries
        //            await _eventDetailsRepository.DeleteAllEventArtistsAsync(eventRequest.EventDetails.event_id, updatedBy);
        //            await _eventDetailsRepository.DeleteAllEventGalleriesAsync(eventRequest.EventDetails.event_id, updatedBy);

        //            var result = new EventCompleteResponseModel
        //            {
        //                EventDetails = eventRequest.EventDetails
        //            };

        //            // Add new artists
        //            if (eventRequest.EventArtists != null && eventRequest.EventArtists.Count > 0)
        //            {
        //                foreach (var artist in eventRequest.EventArtists)
        //                {
        //                    artist.event_id = eventRequest.EventDetails.event_id;
        //                    artist.created_by = updatedBy;
        //                    artist.updated_by = updatedBy;

        //                    var artistId = await _eventDetailsRepository.AddEventArtistAsync(artist);
        //                    if (artistId > 0)
        //                    {
        //                        artist.event_artist_id = artistId;
        //                        result.EventArtists.Add(artist);
        //                    }
        //                }
        //            }

        //            // Add new galleries
        //            if (eventRequest.EventGalleries != null && eventRequest.EventGalleries.Count > 0)
        //            {
        //                foreach (var gallery in eventRequest.EventGalleries)
        //                {
        //                    gallery.event_id = eventRequest.EventDetails.event_id;
        //                    gallery.created_by = updatedBy;
        //                    gallery.updated_by = updatedBy;

        //                    var galleryId = await _eventDetailsRepository.AddEventGalleryAsync(gallery);
        //                    if (galleryId > 0)
        //                    {
        //                        gallery.event_gallary_id = galleryId;
        //                        result.EventGalleries.Add(gallery);
        //                    }
        //                }
        //            }

        //            response.Status = "Success";
        //            response.Message = "Event updated successfully with artists and galleries";
        //            response.ErrorCode = "0";
        //            response.Data = result;
        //        }
        //        else
        //        {
        //            response.Status = "Failure";
        //            response.Message = "Failed to update event";
        //            response.ErrorCode = "1";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Status = "Failure";
        //        response.Message = $"Error updating event: {ex.Message}";
        //        response.ErrorCode = "1";
        //    }

        //    return response;
        //}

        // **UPDATED UpdateEventWithArtistsAndGalleriesAsync method**
        public async Task<CommonResponseModel<EventCompleteResponseModel>> UpdateEventWithArtistsAndGalleriesAsync(
            EventCreateRequestModel eventRequest, string webRootPath, string updatedBy)
        {
            var response = new CommonResponseModel<EventCompleteResponseModel>();
            try
            {
                if (eventRequest?.EventDetails == null || eventRequest.EventDetails.event_id <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid event details are required";
                    response.ErrorCode = "400";
                    return response;
                }

                // Parse updatedBy as Guid
                if (!Guid.TryParse(updatedBy, out Guid userId))
                {
                    // Try to get user ID from email
                    userId = _eventArtistGalleryRepository.LookupUserIdByEmail(updatedBy);
                    if (userId == Guid.Empty)
                    {
                        response.Status = "Failure";
                        response.Message = "Invalid user ID or user not found";
                        response.ErrorCode = "400";
                        return response;
                    }
                }

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
                eventRequest.EventDetails.updated_by = userId.ToString();

                // Calculate total duration
                eventRequest.EventDetails.total_duration_minutes = CalculateDuration(
                    eventRequest.EventDetails.start_time,
                    eventRequest.EventDetails.end_time
                );

                // Upload and update banner if provided
                //if (eventRequest.BannerImageFile != null && eventRequest.BannerImageFile.Length > 0)
                //{
                //    var bannerUrl = await UploadEventBannerAsync(
                //        eventRequest.BannerImageFile,
                //        eventRequest.EventDetails.event_id,
                //        webRootPath);

                //    if (bannerUrl.Status == "Success")
                //    {
                //        eventRequest.EventDetails.banner_image = bannerUrl.Data;
                //    }
                //}
                //else
                //{
                //    // Keep existing banner
                //    eventRequest.EventDetails.banner_image = existingEvent.banner_image;
                //}

                // **Handle banner image as Base64**
                if (eventRequest.BannerImageFile != null && eventRequest.BannerImageFile.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await eventRequest.BannerImageFile.CopyToAsync(memoryStream);
                    var bannerBytes = memoryStream.ToArray();
                    var bannerBase64 = Convert.ToBase64String(bannerBytes);

                    // Store as Base64 string in the banner_image field
                    eventRequest.EventDetails.banner_image = $"data:image/png;base64,{bannerBase64}";
                }
                else
                {
                    // Keep existing banner if not updating
                    eventRequest.EventDetails.banner_image = existingEvent.banner_image;
                }

                // Update event details
                var affectedRows = await _eventDetailsRepository.UpdateEventAsync(eventRequest.EventDetails);

                if (affectedRows > 0)
                {
                    // Delete existing artists and galleries
                    await _eventDetailsRepository.DeleteAllEventArtistsAsync(eventRequest.EventDetails.event_id, userId.ToString());
                    await _eventDetailsRepository.DeleteAllEventGalleriesAsync(eventRequest.EventDetails.event_id, userId.ToString());

                    var result = new EventCompleteResponseModel
                    {
                        EventDetails = eventRequest.EventDetails
                    };

                    // Add new artists
                    if (eventRequest.EventArtists != null && eventRequest.EventArtists.Count > 0)
                    {
                        foreach (var artist in eventRequest.EventArtists)
                        {
                            artist.event_id = eventRequest.EventDetails.event_id;
                            artist.created_by = userId.ToString();
                            artist.updated_by = userId.ToString();

                            var artistId = await _eventDetailsRepository.AddEventArtistAsync(artist);
                            if (artistId > 0)
                            {
                                artist.event_artist_id = artistId;
                                result.EventArtists.Add(artist);
                            }
                        }
                    }

                    // Add new galleries
                    if (eventRequest.EventGalleries != null && eventRequest.EventGalleries.Count > 0)
                    {
                        foreach (var gallery in eventRequest.EventGalleries)
                        {
                            gallery.event_id = eventRequest.EventDetails.event_id;
                            gallery.created_by = userId.ToString();
                            gallery.updated_by = userId.ToString();

                            var galleryId = await _eventDetailsRepository.AddEventGalleryAsync(gallery);
                            if (galleryId > 0)
                            {
                                gallery.event_gallary_id = galleryId;
                                result.EventGalleries.Add(gallery);
                            }
                        }
                    }

                    // Add new seat types - ADD THIS SECTION
                    //if (eventRequest.SeatTypes != null && eventRequest.SeatTypes.Count > 0)
                    //{
                    //    foreach (var seatType in eventRequest.SeatTypes)
                    //    {
                    //        seatType.event_id = eventRequest.EventDetails.event_id;
                    //        seatType.created_by = userId.ToString();
                    //        seatType.updated_by = userId.ToString();

                    //        var seatTypeId = await _eventDetailsRepository.AddEventSeatTypeAsync(seatType);
                    //        if (seatTypeId > 0)
                    //        {
                    //            seatType.event_seat_type_inventory_id = seatTypeId;
                    //            result.SeatTypes.Add(seatType);
                    //        }
                    //    }
                    //}

                    if (eventRequest.SeatTypes != null && eventRequest.SeatTypes.Count > 0)
                    {
                        // Delete existing seat types first
                        await _eventDetailsRepository.DeleteAllEventSeatTypesAsync(
                            eventRequest.EventDetails.event_id,
                            userId.ToString()
                        );

                        foreach (var seatType in eventRequest.SeatTypes)
                        {
                            seatType.event_id = eventRequest.EventDetails.event_id;
                            seatType.created_by = userId.ToString();
                            seatType.updated_by = userId.ToString();
                            seatType.created_on = DateTime.UtcNow; // Set creation time
                            seatType.updated_on = DateTime.UtcNow;

                            var seatTypeId = await _eventDetailsRepository.AddEventSeatTypeAsync(seatType);
                            if (seatTypeId > 0)
                            {
                                seatType.event_seat_type_inventory_id = seatTypeId;
                                result.SeatTypes.Add(seatType);
                            }
                        }
                    }

                    response.Status = "Success";
                    response.Message = "Event updated successfully with artists and galleries";
                    response.ErrorCode = "0";
                    response.Data = result;
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

        public async Task<CommonResponseModel<EventCompleteResponseModel>> GetEventWithArtistsAndGalleriesAsync(int eventId)
        {
            var response = new CommonResponseModel<EventCompleteResponseModel>();
            try
            {
                if (eventId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid event ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                // Get event details
                var eventDetails = await _eventDetailsRepository.GetEventByIdAsync(eventId);

                if (eventDetails != null)
                {
                    var result = new EventCompleteResponseModel
                    {
                        EventDetails = eventDetails
                    };

                    // Get artists
                    var artists = await _eventDetailsRepository.GetEventArtistsByEventIdAsync(eventId);
                    result.EventArtists = artists.ToList();

                    // Get galleries
                    var galleries = await _eventDetailsRepository.GetEventGalleriesByEventIdAsync(eventId);
                    result.EventGalleries = galleries.ToList();

                    response.Status = "Success";
                    response.Message = "Event with artists and galleries fetched successfully";
                    response.ErrorCode = "0";
                    response.Data = result;
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

        public async Task<PagedResponse<List<EventCompleteResponseModel>>> GetPaginatedEventsByCreatedByAsync(
            EventPaginationRequest request)
        {
            var response = new PagedResponse<List<EventCompleteResponseModel>>();
            try
            {
                if (string.IsNullOrEmpty(request.created_by))
                {
                    response.Status = "Failure";
                    response.Message = "Created by user is required";
                    response.ErrorCode = "400";
                    return response;
                }

                // This needs to be implemented in repository
                // For now, get all events and filter manually
                var allEvents = await _eventDetailsRepository.GetAllEventsAsync();
                var filteredEvents = allEvents
                    .Where(e => e.created_by == request.created_by)
                    .ToList();

                var result = new List<EventCompleteResponseModel>();

                foreach (var eventDetail in filteredEvents)
                {
                    var eventWithDetails = await GetEventWithArtistsAndGalleriesAsync(eventDetail.event_id);
                    if (eventWithDetails.Data != null)
                    {
                        result.Add(eventWithDetails.Data);
                    }
                }

                response.Status = "Success";
                response.Message = "Events fetched successfully";
                response.ErrorCode = "0";
                response.Data = result;
                response.TotalCount = result.Count;
                response.TotalPages = 1;
                response.CurrentPage = request.PageNumber;
                response.PageSize = request.PageSize;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving events: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<bool>> DeleteEventWithArtistsAndGalleriesAsync(int eventId, string updatedBy)
        {
            var response = new CommonResponseModel<bool>();
            try
            {
                if (eventId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid event ID is required";
                    response.ErrorCode = "400";
                    response.Data = false;
                    return response;
                }

                // Check if event exists
                var existingEvent = await _eventDetailsRepository.GetEventByIdAsync(eventId);
                if (existingEvent == null)
                {
                    response.Status = "Failure";
                    response.Message = "Event not found";
                    response.ErrorCode = "404";
                    response.Data = false;
                    return response;
                }

                // Delete event (soft delete)
                var affectedRows = await _eventDetailsRepository.DeleteEventAsync(eventId, updatedBy);

                if (affectedRows > 0)
                {
                    // Delete associated artists and galleries
                    await _eventDetailsRepository.DeleteAllEventArtistsAsync(eventId, updatedBy);
                    await _eventDetailsRepository.DeleteAllEventGalleriesAsync(eventId, updatedBy);

                    response.Status = "Success";
                    response.Message = "Event and associated data deleted successfully";
                    response.ErrorCode = "0";
                    response.Data = true;
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to delete event";
                    response.ErrorCode = "1";
                    response.Data = false;
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error deleting event: {ex.Message}";
                response.ErrorCode = "1";
                response.Data = false;
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
