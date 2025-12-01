using BAL.Services;
using DAL.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MODEL.Entities;
using MODEL.Request;
using MODEL.Response;

namespace TicketHouseBackend.Controllers.Masters
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestimonialController : ControllerBase
    {
        private readonly ITestimonialService _testimonialService;
        private readonly IWebHostEnvironment _environment;

        public TestimonialController(ITestimonialService testimonialService, IWebHostEnvironment environment)
        {
            _testimonialService = testimonialService;
            _environment = environment;
        }

        [HttpGet("GetAllTestimonials")]
        public async Task<CommonResponseModel<IEnumerable<TestimonialModel>>> GetAllTestimonials()
        {
            try
            {
                var testimonials = await _testimonialService.GetAllTestimonialsAsync();
                return testimonials;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<IEnumerable<TestimonialModel>>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpGet("GetTestimonialById/{testimonialId}")]
        public async Task<CommonResponseModel<TestimonialModel>> GetTestimonialById(int testimonialId)
        {
            try
            {
                var testimonial = await _testimonialService.GetTestimonialByIdAsync(testimonialId);
                return testimonial;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<TestimonialModel>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("AddTestimonial")]
        public async Task<CommonResponseModel<TestimonialModel>> AddTestimonial([FromForm] TestimonialRequest request)
        {
            try
            {
                if (request == null)
                {
                    return new CommonResponseModel<TestimonialModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Testimonial data is required"
                    };
                }

                // Validate required fields
                if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Description))
                {
                    return new CommonResponseModel<TestimonialModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Name and Description are required fields"
                    };
                }

                var testimonial = new TestimonialModel
                {
                    name = request.Name,
                    designation = request.Designation ?? string.Empty,
                    description = request.Description,
                    active = request.Active,
                    created_by = request.CreatedBy ?? string.Empty,
                    updated_by = request.UpdatedBy ?? string.Empty,
                    profile_img = string.Empty // Empty string for no image
                };

                // Add testimonial to database FIRST (to get the testimonial_id)
                var addResult = await _testimonialService.AddTestimonialAsync(testimonial);

                if (addResult.Status != "Success" || addResult.Data == null)
                {
                    return addResult;
                }

                // Now handle file upload if provided (AFTER we have the testimonial_id)
                if (request.ProfileImage != null && request.ProfileImage.Length > 0)
                {
                    try
                    {
                        // Use ContentRootPath
                        var assetsRootPath = Path.Combine(_environment.ContentRootPath, "Assets");

                        // Create Assets folder if it doesn't exist
                        if (!Directory.Exists(assetsRootPath))
                        {
                            Directory.CreateDirectory(assetsRootPath);
                        }

                        // Upload image with ACTUAL testimonial_id
                        var imagePath = await FileUploadHelper.UploadTestimonialImage(
                            request.ProfileImage,
                            addResult.Data.testimonial_id, // Use actual ID
                            request.Name,
                            assetsRootPath
                        );

                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            // Update testimonial with image path
                            addResult.Data.profile_img = imagePath;

                            // Update database with image path
                            await _testimonialService.UpdateTestimonialAsync(addResult.Data);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Don't fail the operation if image upload fails
                        // Just log it and continue
                        Console.WriteLine($"Image upload failed (testimonial saved): {ex.Message}");
                    }
                }

                return addResult;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<TestimonialModel>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("UpdateTestimonial")]
        public async Task<CommonResponseModel<TestimonialModel>> UpdateTestimonial([FromForm] TestimonialRequest request)
        {
            try
            {
                if (request == null || request.TestimonialId <= 0)
                {
                    return new CommonResponseModel<TestimonialModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid testimonial data is required"
                    };
                }

                // Validate required fields
                if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Description))
                {
                    return new CommonResponseModel<TestimonialModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Name and Description are required fields"
                    };
                }

                // Get existing testimonial to check current image
                var existingTestimonial = await _testimonialService.GetTestimonialByIdAsync(request.TestimonialId);
                if (existingTestimonial.Data == null)
                {
                    return new CommonResponseModel<TestimonialModel>
                    {
                        ErrorCode = "404",
                        Status = "Error",
                        Message = "Testimonial not found"
                    };
                }

                string imagePath = existingTestimonial.Data.profile_img;

                // Handle file upload if new image provided
                if (request.ProfileImage != null && request.ProfileImage.Length > 0)
                {
                    try
                    {
                        // Use ContentRootPath
                        var assetsRootPath = Path.Combine(_environment.ContentRootPath, "Assets");

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingTestimonial.Data.profile_img))
                        {
                            FileUploadHelper.DeleteTestimonialImage(existingTestimonial.Data.profile_img, assetsRootPath);
                        }

                        // Upload new image with actual testimonial_id
                        imagePath = await FileUploadHelper.UploadTestimonialImage(
                            request.ProfileImage,
                            request.TestimonialId, // Use actual ID
                            request.Name,
                            assetsRootPath
                        );
                    }
                    catch (Exception ex)
                    {
                        return new CommonResponseModel<TestimonialModel>
                        {
                            ErrorCode = "1",
                            Status = "Error",
                            Message = $"Error uploading image: {ex.Message}"
                        };
                    }
                }

                var testimonial = new TestimonialModel
                {
                    testimonial_id = request.TestimonialId,
                    name = request.Name,
                    designation = request.Designation ?? string.Empty,
                    profile_img = imagePath ?? string.Empty,
                    description = request.Description,
                    active = request.Active,
                    updated_by = request.UpdatedBy ?? string.Empty
                };

                var result = await _testimonialService.UpdateTestimonialAsync(testimonial);
                return result;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<TestimonialModel>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("DeleteTestimonial/{testimonialId}")]
        public async Task<CommonResponseModel<TestimonialModel>> DeleteTestimonial(int testimonialId, [FromBody] string updatedBy = null)
        {
            try
            {
                if (testimonialId <= 0)
                {
                    return new CommonResponseModel<TestimonialModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid testimonial ID is required"
                    };
                }

                // Get testimonial to delete image
                var existingTestimonial = await _testimonialService.GetTestimonialByIdAsync(testimonialId);
                if (existingTestimonial.Data != null && !string.IsNullOrEmpty(existingTestimonial.Data.profile_img))
                {
                    var assetsRootPath = Path.Combine(_environment.ContentRootPath, "Assets");
                    FileUploadHelper.DeleteTestimonialImage(existingTestimonial.Data.profile_img, assetsRootPath);
                }

                var result = await _testimonialService.DeleteTestimonialAsync(testimonialId, updatedBy ?? string.Empty);
                return result;
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<TestimonialModel>
                {
                    ErrorCode = "1",
                    Status = "Error",
                    Message = ex.Message
                };
            }
        }

        [HttpPost("UpdateTestimonialStatus")]
        public async Task<CommonResponseModel<bool>> UpdateTestimonialStatus([FromBody] UpdateTestimonialStatusRequest request)
        {
            try
            {
                if (request == null || request.TestimonialId <= 0)
                {
                    return new CommonResponseModel<bool>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid testimonial status data is required",
                        Data = false
                    };
                }

                var result = await _testimonialService.UpdateTestimonialStatusAsync(
                    request.TestimonialId,
                    request.Status,
                    request.UpdatedBy
                );

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
