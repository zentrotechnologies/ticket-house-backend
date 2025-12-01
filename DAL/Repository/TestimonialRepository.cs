using DAL.Utilities;
using Dapper;
using MODEL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public interface ITestimonialRepository
    {
        Task<IEnumerable<TestimonialModel>> GetAllTestimonialsAsync();
        Task<TestimonialModel> GetTestimonialByIdAsync(int testimonialId);
        Task<int> AddTestimonialAsync(TestimonialModel testimonial);
        Task<int> UpdateTestimonialAsync(TestimonialModel testimonial);
        Task<int> DeleteTestimonialAsync(int testimonialId, string updatedBy);
        Task<int> UpdateTestimonialStatusAsync(int testimonialId, int status, string updatedBy);
    }
    public class TestimonialRepository: ITestimonialRepository
    {
        private readonly ITHDBConnection _dbConnection;
        private readonly string testimonial = DatabaseConfiguration.testimonial;

        public TestimonialRepository(ITHDBConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<TestimonialModel>> GetAllTestimonialsAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {testimonial} 
                WHERE active = 1 
                ORDER BY testimonial_id DESC";

            return await connection.QueryAsync<TestimonialModel>(query);
        }

        public async Task<TestimonialModel> GetTestimonialByIdAsync(int testimonialId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {testimonial} 
                WHERE testimonial_id = @TestimonialId AND active = 1";

            return await connection.QueryFirstOrDefaultAsync<TestimonialModel>(query, new { TestimonialId = testimonialId });
        }

        public async Task<int> AddTestimonialAsync(TestimonialModel testimonialModel)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                INSERT INTO {testimonial} 
                (name, designation, profile_img, description, created_by, updated_by)
                VALUES 
                (@name, @designation, @profile_img, @description, @created_by, @updated_by)
                RETURNING testimonial_id";

            var testimonialId = await connection.ExecuteScalarAsync<int>(query, new
            {
                name = testimonialModel.name,
                designation = testimonialModel.designation,
                profile_img = testimonialModel.profile_img,
                description = testimonialModel.description,
                created_by = testimonialModel.created_by,
                updated_by = testimonialModel.updated_by
            });

            return testimonialId;
        }

        public async Task<int> UpdateTestimonialAsync(TestimonialModel testimonialModel)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {testimonial} 
                SET name = @name, 
                    designation = @designation, 
                    profile_img = @profile_img, 
                    description = @description,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE testimonial_id = @testimonial_id AND active = 1";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                testimonial_id = testimonialModel.testimonial_id,
                name = testimonialModel.name,
                designation = testimonialModel.designation,
                profile_img = testimonialModel.profile_img,
                description = testimonialModel.description,
                updated_by = testimonialModel.updated_by
            });

            return affectedRows;
        }

        public async Task<int> DeleteTestimonialAsync(int testimonialId, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {testimonial} 
                SET active = 0,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE testimonial_id = @testimonial_id AND active = 1";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                testimonial_id = testimonialId,
                updated_by = updatedBy
            });

            return affectedRows;
        }

        public async Task<int> UpdateTestimonialStatusAsync(int testimonialId, int status, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {testimonial} 
                SET active = @status,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE testimonial_id = @testimonial_id";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                testimonial_id = testimonialId,
                status = status,
                updated_by = updatedBy
            });

            return affectedRows;
        }
    }
}
