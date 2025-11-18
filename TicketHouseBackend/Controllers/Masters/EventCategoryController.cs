using BAL.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MODEL.Entities;
using MODEL.Response;

namespace TicketHouseBackend.Controllers.Masters
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventCategoryController : ControllerBase
    {
        private readonly IEventCategoryService _eventCategoryService;

        public EventCategoryController(IEventCategoryService eventCategoryService)
        {
            _eventCategoryService = eventCategoryService;
        }

        [HttpGet("GetAllEventCategories")]
        public async Task<CommonResponseModel<IEnumerable<EventCategoryModel>>> GetAllEventCategories()
        {
            try
            {
                var eventCategories = await _eventCategoryService.GetAllEventCategoriesAsync();
                return eventCategories;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<IEnumerable<EventCategoryModel>>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetEventCategoryById/{eventCategoryId}")]
        public async Task<CommonResponseModel<EventCategoryModel>> GetEventCategoryById(int eventCategoryId)
        {
            try
            {
                var eventCategory = await _eventCategoryService.GetEventCategoryByIdAsync(eventCategoryId);
                return eventCategory;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<EventCategoryModel>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("AddEventCategory")]
        public async Task<CommonResponseModel<EventCategoryModel>> AddEventCategory([FromBody] EventCategoryModel eventCategory)
        {
            try
            {
                if (eventCategory == null)
                {
                    return new CommonResponseModel<EventCategoryModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Event category data is required"
                    };
                }

                // Validate required fields
                if (string.IsNullOrEmpty(eventCategory.event_category_name))
                {
                    return new CommonResponseModel<EventCategoryModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Event category name is required"
                    };
                }

                var result = await _eventCategoryService.AddEventCategoryAsync(eventCategory);
                return result;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<EventCategoryModel>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("UpdateEventCategory")]
        public async Task<CommonResponseModel<EventCategoryModel>> UpdateEventCategory([FromBody] EventCategoryModel eventCategory)
        {
            try
            {
                if (eventCategory == null || eventCategory.event_category_id <= 0)
                {
                    return new CommonResponseModel<EventCategoryModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event category data is required"
                    };
                }

                // Validate required fields
                if (string.IsNullOrEmpty(eventCategory.event_category_name))
                {
                    return new CommonResponseModel<EventCategoryModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Event category name is required"
                    };
                }

                var result = await _eventCategoryService.UpdateEventCategoryAsync(eventCategory);
                return result;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<EventCategoryModel>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("DeleteEventCategory/{eventCategoryId}")]
        public async Task<CommonResponseModel<EventCategoryModel>> DeleteEventCategory(int eventCategoryId, [FromBody] string updatedBy = null)
        {
            try
            {
                if (eventCategoryId <= 0)
                {
                    return new CommonResponseModel<EventCategoryModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event category ID is required"
                    };
                }

                var result = await _eventCategoryService.DeleteEventCategoryAsync(eventCategoryId, updatedBy);
                return result;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<EventCategoryModel>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }
    }
}
