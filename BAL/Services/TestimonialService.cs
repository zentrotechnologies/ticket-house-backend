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
    public interface ITestimonialService
    {
        Task<CommonResponseModel<IEnumerable<TestimonialModel>>> GetAllTestimonialsAsync();
        Task<CommonResponseModel<TestimonialModel>> GetTestimonialByIdAsync(int testimonialId);
        Task<CommonResponseModel<TestimonialModel>> AddTestimonialAsync(TestimonialModel testimonial);
        Task<CommonResponseModel<TestimonialModel>> UpdateTestimonialAsync(TestimonialModel testimonial);
        Task<CommonResponseModel<TestimonialModel>> DeleteTestimonialAsync(int testimonialId, string updatedBy);
    }
    public class TestimonialService: ITestimonialService
    {
        private readonly ITestimonialRepository _testimonialRepository;

        public TestimonialService(ITestimonialRepository testimonialRepository)
        {
            _testimonialRepository = testimonialRepository;
        }

        public async Task<CommonResponseModel<IEnumerable<TestimonialModel>>> GetAllTestimonialsAsync()
        {
            var response = new CommonResponseModel<IEnumerable<TestimonialModel>>();
            try
            {
                var testimonials = await _testimonialRepository.GetAllTestimonialsAsync();
                response.Status = "Success";
                response.Message = "Testimonials fetched successfully";
                response.ErrorCode = "0";
                response.Data = testimonials;
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving testimonials: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<TestimonialModel>> GetTestimonialByIdAsync(int testimonialId)
        {
            var response = new CommonResponseModel<TestimonialModel>();
            try
            {
                var testimonial = await _testimonialRepository.GetTestimonialByIdAsync(testimonialId);
                if (testimonial != null)
                {
                    response.Status = "Success";
                    response.Message = "Testimonial fetched successfully";
                    response.ErrorCode = "0";
                    response.Data = testimonial;
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Testimonial not found";
                    response.ErrorCode = "404";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error retrieving testimonial: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<TestimonialModel>> AddTestimonialAsync(TestimonialModel testimonial)
        {
            var response = new CommonResponseModel<TestimonialModel>();
            try
            {
                var testimonialId = await _testimonialRepository.AddTestimonialAsync(testimonial);
                if (testimonialId > 0)
                {
                    testimonial.testimonial_id = testimonialId;
                    response.Status = "Success";
                    response.Message = "Testimonial added successfully";
                    response.ErrorCode = "0";
                    response.Data = testimonial;
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to add testimonial";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error adding testimonial: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<TestimonialModel>> UpdateTestimonialAsync(TestimonialModel testimonial)
        {
            var response = new CommonResponseModel<TestimonialModel>();
            try
            {
                // Check if testimonial exists
                var existingTestimonial = await _testimonialRepository.GetTestimonialByIdAsync(testimonial.testimonial_id);
                if (existingTestimonial == null)
                {
                    response.Status = "Failure";
                    response.Message = "Testimonial not found";
                    response.ErrorCode = "404";
                    return response;
                }

                var affectedRows = await _testimonialRepository.UpdateTestimonialAsync(testimonial);

                if (affectedRows > 0)
                {
                    response.Status = "Success";
                    response.Message = "Testimonial updated successfully";
                    response.ErrorCode = "0";
                    response.Data = testimonial;
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to update testimonial";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error updating testimonial: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }

        public async Task<CommonResponseModel<TestimonialModel>> DeleteTestimonialAsync(int testimonialId, string updatedBy)
        {
            var response = new CommonResponseModel<TestimonialModel>();
            try
            {
                // Check if testimonial exists
                var existingTestimonial = await _testimonialRepository.GetTestimonialByIdAsync(testimonialId);
                if (existingTestimonial == null)
                {
                    response.Status = "Failure";
                    response.Message = "Testimonial not found";
                    response.ErrorCode = "404";
                    return response;
                }

                var affectedRows = await _testimonialRepository.DeleteTestimonialAsync(testimonialId, updatedBy);

                if (affectedRows > 0)
                {
                    response.Status = "Success";
                    response.Message = "Testimonial deleted successfully";
                    response.ErrorCode = "0";
                }
                else
                {
                    response.Status = "Failure";
                    response.Message = "Failed to delete testimonial";
                    response.ErrorCode = "1";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Message = $"Error deleting testimonial: {ex.Message}";
                response.ErrorCode = "1";
            }

            return response;
        }
    }
}
