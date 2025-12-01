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
        public async Task<CommonResponseModel<EventCategoryModel>> DeleteEventCategory(int eventCategoryId, [FromQuery] string updatedBy = null)
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

        [HttpPost("GetPaginatedEventCategoryByUserId")]
        public async Task<PagedResponse<IEnumerable<EventCategoryModel>>> GetPaginatedEventCategoryByUserId([FromBody] UserIdRequest request)
        {
            try
            {
                if (request == null)
                {
                    return new PagedResponse<IEnumerable<EventCategoryModel>>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Request data is required",
                        Data = null,
                        TotalPages = 0,
                        CurrentPage = 0,
                        PageSize = 0
                    };
                }

                // Validate and set default pagination parameters inline
                if (request.PageNumber <= 0) request.PageNumber = 1;
                if (request.PageSize <= 0) request.PageSize = 10;

                return await _eventCategoryService.GetPaginatedEventCategoryByUserIdAsync(request);
            }
            catch (Exception ex)
            {
                return new PagedResponse<IEnumerable<EventCategoryModel>>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message,
                    Data = null,
                    TotalPages = 0,
                    CurrentPage = request?.PageNumber ?? 0,
                    PageSize = request?.PageSize ?? 0
                };
            }
        }

        [HttpPost("UpdateEventCategoryStatus")]
        public async Task<CommonResponseModel<bool>> UpdateEventCategoryStatus([FromBody] UpdateEventCategoryStatusRequest request)
        {
            try
            {
                if (request == null)
                {
                    return new CommonResponseModel<bool>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Request data is required",
                        Data = false
                    };
                }

                if (request.event_category_id <= 0)
                {
                    return new CommonResponseModel<bool>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid event category ID is required",
                        Data = false
                    };
                }

                if (request.active != 1 && request.active != 2)
                {
                    return new CommonResponseModel<bool>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Active status must be 1 (Active) or 2 (Inactive)",
                        Data = false
                    };
                }

                if (string.IsNullOrEmpty(request.updated_by))
                {
                    return new CommonResponseModel<bool>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Updated by user is required",
                        Data = false
                    };
                }

                return await _eventCategoryService.UpdateEventCategoryStatusAsync(
                    request.event_category_id,
                    request.active,
                    request.updated_by
                );
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
