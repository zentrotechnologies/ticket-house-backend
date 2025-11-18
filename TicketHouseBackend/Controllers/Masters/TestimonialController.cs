using BAL.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MODEL.Entities;
using MODEL.Response;

namespace TicketHouseBackend.Controllers.Masters
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestimonialController : ControllerBase
    {
        private readonly ITestimonialService _testimonialService;

        public TestimonialController(ITestimonialService testimonialService)
        {
            _testimonialService = testimonialService;
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
        public async Task<CommonResponseModel<TestimonialModel>> AddTestimonial([FromBody] TestimonialModel testimonial)
        {
            try
            {
                if (testimonial == null)
                {
                    return new CommonResponseModel<TestimonialModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Testimonial data is required"
                    };
                }

                // Validate required fields
                if (string.IsNullOrEmpty(testimonial.name) || string.IsNullOrEmpty(testimonial.description))
                {
                    return new CommonResponseModel<TestimonialModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Name and Description are required fields"
                    };
                }

                var result = await _testimonialService.AddTestimonialAsync(testimonial);
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

        [HttpPost("UpdateTestimonial")]
        public async Task<CommonResponseModel<TestimonialModel>> UpdateTestimonial([FromBody] TestimonialModel testimonial)
        {
            try
            {
                if (testimonial == null || testimonial.testimonial_id <= 0)
                {
                    return new CommonResponseModel<TestimonialModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Valid testimonial data is required"
                    };
                }

                // Validate required fields
                if (string.IsNullOrEmpty(testimonial.name) || string.IsNullOrEmpty(testimonial.description))
                {
                    return new CommonResponseModel<TestimonialModel>
                    {
                        ErrorCode = "400",
                        Status = "Error",
                        Message = "Name and Description are required fields"
                    };
                }

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

                var result = await _testimonialService.DeleteTestimonialAsync(testimonialId, updatedBy);
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
    }
}
