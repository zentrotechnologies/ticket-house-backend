using DAL.Repository;
using MODEL.Entities;
using MODEL.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BAL.Services
{
    public interface IUserEventsService
    {
        Task<CommonResponseModel<IEnumerable<UpcomingEventResponse>>> GetUpcomingEventsAsync(UpcomingEventsRequest request);
        Task<CommonResponseModel<IEnumerable<ArtistResponse>>> GetShowsByArtistsAsync(GetShowsByArtistsRequest request);
        Task<CommonResponseModel<IEnumerable<TestimonialResponse>>> GetTestimonialsByArtistsAsync();
        Task<CommonResponseModel<EventDetailsModel>> GetEventDetailsByIdAsync(int eventId);
        Task<CommonResponseModel<IEnumerable<UpcomingEventResponse>>> GetSimilarEventsByCategoryAsync(SimilarEventsRequest request);
    }
    public class UserEventsService: IUserEventsService
    {
        public readonly IUserEventsRepository _userEventsRepository;

        public UserEventsService(IUserEventsRepository userEventsRepository)
        {
            _userEventsRepository = userEventsRepository;
        }

        public async Task<CommonResponseModel<IEnumerable<UpcomingEventResponse>>> GetUpcomingEventsAsync(UpcomingEventsRequest request)
        {
            var response = new CommonResponseModel<IEnumerable<UpcomingEventResponse>>();

            try
            {
                // Validate request
                if (request == null)
                {
                    response.Status = "Failure";
                    response.Message = "Request cannot be null";
                    response.ErrorCode = "400";
                    return response;
                }

                if (request.Count <= 0)
                {
                    request.Count = 8; // Default to 8
                }

                var events = await _userEventsRepository.GetUpcomingEventsAsync(request);

                response.Status = "Success";
                response.Message = "Upcoming events fetched successfully";
                response.ErrorCode = "0";
                response.Data = events;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error fetching upcoming events: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<IEnumerable<ArtistResponse>>> GetShowsByArtistsAsync(GetShowsByArtistsRequest request)
        {
            var response = new CommonResponseModel<IEnumerable<ArtistResponse>>();

            try
            {
                // Validate request
                if (request == null)
                {
                    response.Status = "Failure";
                    response.Message = "Request cannot be null";
                    response.ErrorCode = "400";
                    return response;
                }

                if (request.Count <= 0)
                {
                    request.Count = 5; // Default to 5
                }

                var artists = await _userEventsRepository.GetShowsByArtistsAsync(request);

                response.Status = "Success";
                response.Message = "Artists fetched successfully";
                response.ErrorCode = "0";
                response.Data = artists;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error fetching artists: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<IEnumerable<TestimonialResponse>>> GetTestimonialsByArtistsAsync()
        {
            var response = new CommonResponseModel<IEnumerable<TestimonialResponse>>();

            try
            {
                var testimonials = await _userEventsRepository.GetTestimonialsByArtistsAsync();

                response.Status = "Success";
                response.Message = "Testimonials fetched successfully";
                response.ErrorCode = "0";
                response.Data = testimonials;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error fetching testimonials: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<EventDetailsModel>> GetEventDetailsByIdAsync(int eventId)
        {
            var response = new CommonResponseModel<EventDetailsModel>();

            try
            {
                if (eventId <= 0)
                {
                    response.Status = "Failure";
                    response.Message = "Valid event ID is required";
                    response.ErrorCode = "400";
                    return response;
                }

                var eventDetails = await _userEventsRepository.GetEventDetailsByIdAsync(eventId);

                if (eventDetails == null)
                {
                    response.Status = "Failure";
                    response.Message = "Event not found";
                    response.ErrorCode = "404";
                    return response;
                }

                response.Status = "Success";
                response.Message = "Event details fetched successfully";
                response.ErrorCode = "0";
                response.Data = eventDetails;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error fetching event details: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<IEnumerable<UpcomingEventResponse>>> GetSimilarEventsByCategoryAsync(SimilarEventsRequest request)
        {
            var response = new CommonResponseModel<IEnumerable<UpcomingEventResponse>>();

            try
            {
                if (request == null)
                {
                    response.Status = "Failure";
                    response.Message = "Request cannot be null";
                    response.ErrorCode = "400";
                    return response;
                }

                if (request.Count <= 0)
                {
                    request.Count = 4; // Default to 4 similar events
                }

                var similarEvents = await _userEventsRepository.GetSimilarEventsByCategoryAsync(
                    request.CategoryId,
                    request.ExcludeEventId,
                    request.Count
                );

                response.Status = "Success";
                response.Message = "Similar events fetched successfully";
                response.ErrorCode = "0";
                response.Data = similarEvents;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error fetching similar events: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }
    }
}
