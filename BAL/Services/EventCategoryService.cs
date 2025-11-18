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
    public interface IEventCategoryService
    {
        Task<CommonResponseModel<IEnumerable<EventCategoryModel>>> GetAllEventCategoriesAsync();
        Task<CommonResponseModel<EventCategoryModel>> GetEventCategoryByIdAsync(int eventCategoryId);
        Task<CommonResponseModel<EventCategoryModel>> AddEventCategoryAsync(EventCategoryModel eventCategory);
        Task<CommonResponseModel<EventCategoryModel>> UpdateEventCategoryAsync(EventCategoryModel eventCategory);
        Task<CommonResponseModel<EventCategoryModel>> DeleteEventCategoryAsync(int eventCategoryId, string updatedBy);
    }
    public class EventCategoryService: IEventCategoryService
    {
        private readonly IEventCategoryRepository _eventCategoryRepository;

        public EventCategoryService(IEventCategoryRepository eventCategoryRepository)
        {
            _eventCategoryRepository = eventCategoryRepository;
        }

        public async Task<CommonResponseModel<IEnumerable<EventCategoryModel>>> GetAllEventCategoriesAsync()
        {
            var response = new CommonResponseModel<IEnumerable<EventCategoryModel>>();
            try
            {
                var eventCategories = await _eventCategoryRepository.GetAllEventCategoriesAsync();
                response.Status = "Success";
                response.Message = "Event categories fetched successfully";
                response.ErrorCode = "0";
                response.Data = eventCategories;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving event categories: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<EventCategoryModel>> GetEventCategoryByIdAsync(int eventCategoryId)
        {
            var response = new CommonResponseModel<EventCategoryModel>();
            try
            {
                var eventCategory = await _eventCategoryRepository.GetEventCategoryByIdAsync(eventCategoryId);
                if (eventCategory != null)
                {
                    response.Status = "Success";
                    response.Message = "Event category fetched successfully";
                    response.ErrorCode = "0";
                    response.Data = eventCategory;
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Event category not found";
                    response.ErrorCode = "404";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving event category: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<EventCategoryModel>> AddEventCategoryAsync(EventCategoryModel eventCategory)
        {
            var response = new CommonResponseModel<EventCategoryModel>();
            try
            {
                var eventCategoryId = await _eventCategoryRepository.AddEventCategoryAsync(eventCategory);
                if (eventCategoryId > 0)
                {
                    eventCategory.event_category_id = eventCategoryId;
                    response.Status = "Success";
                    response.Message = "Event category added successfully";
                    response.ErrorCode = "0";
                    response.Data = eventCategory;
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to add event category";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("already exists"))
                {
                    response.Status = "Failure";
                    response.Message = "Event category with same name already exists";
                    response.ErrorCode = "DUPLICATE";
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = $"Error adding event category: {ex.Message}";
                    response.ErrorCode = "1";
                }
            }

            return response;
        }

        public async Task<CommonResponseModel<EventCategoryModel>> UpdateEventCategoryAsync(EventCategoryModel eventCategory)
        {
            var response = new CommonResponseModel<EventCategoryModel>();
            try
            {
                // Check if event category exists
                var existingEventCategory = await _eventCategoryRepository.GetEventCategoryByIdAsync(eventCategory.event_category_id);
                if (existingEventCategory == null)
                {
                    response.Status = "Failure";
                    response.Message = "Event category not found";
                    response.ErrorCode = "404";
                    return response;
                }

                var affectedRows = await _eventCategoryRepository.UpdateEventCategoryAsync(eventCategory);

                if (affectedRows > 0)
                {
                    response.Status = "Success";
                    response.Message = "Event category updated successfully";
                    response.ErrorCode = "0";
                    response.Data = eventCategory;
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to update event category";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("already exists"))
                {
                    response.Status = "Failure";
                    response.Message = "Event category with same name already exists";
                    response.ErrorCode = "DUPLICATE";
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = $"Error updating event category: {ex.Message}";
                    response.ErrorCode = "1";
                }
            }

            return response;
        }

        public async Task<CommonResponseModel<EventCategoryModel>> DeleteEventCategoryAsync(int eventCategoryId, string updatedBy)
        {
            var response = new CommonResponseModel<EventCategoryModel>();
            try
            {
                // Check if event category exists
                var existingEventCategory = await _eventCategoryRepository.GetEventCategoryByIdAsync(eventCategoryId);
                if (existingEventCategory == null)
                {
                    response.Status = "Failure";
                    response.Message = "Event category not found";
                    response.ErrorCode = "404";
                    return response;
                }

                var affectedRows = await _eventCategoryRepository.DeleteEventCategoryAsync(eventCategoryId, updatedBy);

                if (affectedRows > 0)
                {
                    response.Status = "Success";
                    response.Message = "Event category deleted successfully";
                    response.ErrorCode = "0";
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to delete event category";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error deleting event category: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }
    }
}
