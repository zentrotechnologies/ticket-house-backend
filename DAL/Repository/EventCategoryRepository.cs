using DAL.Utilities;
using Dapper;
using MODEL.Entities;
using MODEL.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public interface IEventCategoryRepository
    {
        Task<IEnumerable<EventCategoryModel>> GetAllEventCategoriesAsync();
        Task<EventCategoryModel> GetEventCategoryByIdAsync(int eventCategoryId);
        Task<int> AddEventCategoryAsync(EventCategoryModel eventCategory);
        Task<int> UpdateEventCategoryAsync(EventCategoryModel eventCategory);
        Task<int> DeleteEventCategoryAsync(int eventCategoryId, string updatedBy);
        Task<(int TotalPages, IEnumerable<EventCategoryModel> Data)> GetPaginatedEventCategoryByUserIdAsync(UserIdRequest request);
        Task<int> UpdateEventCategoryStatusAsync(int eventCategoryId, int active, string updatedBy);
    }
    public class EventCategoryRepository: IEventCategoryRepository
    {
        private readonly ITHDBConnection _dbConnection;
        private readonly string event_category = DatabaseConfiguration.event_category;

        public EventCategoryRepository(ITHDBConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<EventCategoryModel>> GetAllEventCategoriesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {event_category} 
                WHERE active IN (1,2) 
                ORDER BY event_category_id DESC";

            return await connection.QueryAsync<EventCategoryModel>(query);
        }

        public async Task<EventCategoryModel> GetEventCategoryByIdAsync(int eventCategoryId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {event_category} 
                WHERE event_category_id = @EventCategoryId AND active IN (1,2)";

            return await connection.QueryFirstOrDefaultAsync<EventCategoryModel>(query, new { EventCategoryId = eventCategoryId });
        }

        public async Task<int> AddEventCategoryAsync(EventCategoryModel eventCategoryModel)
        {
            using var connection = _dbConnection.GetConnection();

            // Check for duplicate category name
            var checkQuery = $@"
                SELECT COUNT(*) FROM {event_category} 
                WHERE LOWER(event_category_name) = LOWER(@event_category_name) 
                AND active = 1";

            var exists = await connection.ExecuteScalarAsync<int>(checkQuery, new
            {
                event_category_name = eventCategoryModel.event_category_name
            });

            if (exists > 0)
            {
                throw new Exception("Event category with same name already exists.");
            }

            var query = $@"
                INSERT INTO {event_category} 
                (event_category_name, event_category_desc, created_by, updated_by)
                VALUES 
                (@event_category_name, @event_category_desc, @created_by, @updated_by)
                RETURNING event_category_id";

            var eventCategoryId = await connection.ExecuteScalarAsync<int>(query, new
            {
                event_category_name = eventCategoryModel.event_category_name,
                event_category_desc = eventCategoryModel.event_category_desc,
                created_by = eventCategoryModel.created_by,
                updated_by = eventCategoryModel.updated_by
            });

            return eventCategoryId;
        }

        public async Task<int> UpdateEventCategoryAsync(EventCategoryModel eventCategoryModel)
        {
            using var connection = _dbConnection.GetConnection();

            // Check for duplicate category name excluding current record
            var checkQuery = $@"
                SELECT COUNT(*) FROM {event_category} 
                WHERE LOWER(event_category_name) = LOWER(@event_category_name) 
                AND event_category_id != @event_category_id 
                AND active IN (1,2)";

            var exists = await connection.ExecuteScalarAsync<int>(checkQuery, new
            {
                event_category_name = eventCategoryModel.event_category_name,
                event_category_id = eventCategoryModel.event_category_id
            });

            if (exists > 0)
            {
                throw new Exception("Event category with same name already exists.");
            }

            var query = $@"
                UPDATE {event_category} 
                SET event_category_name = @event_category_name, 
                    event_category_desc = @event_category_desc,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE event_category_id = @event_category_id AND active IN (1,2)";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                event_category_id = eventCategoryModel.event_category_id,
                event_category_name = eventCategoryModel.event_category_name,
                event_category_desc = eventCategoryModel.event_category_desc,
                updated_by = eventCategoryModel.updated_by
            });

            return affectedRows;
        }

        public async Task<int> DeleteEventCategoryAsync(int eventCategoryId, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {event_category} 
                SET active = 0,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE event_category_id = @event_category_id AND active IN (1,2)";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                event_category_id = eventCategoryId,
                updated_by = updatedBy
            });

            return affectedRows;
        }

        public async Task<(int TotalPages, IEnumerable<EventCategoryModel> Data)> GetPaginatedEventCategoryByUserIdAsync(UserIdRequest request)
        {
            using var connection = _dbConnection.GetConnection();

            var offset = (request.PageNumber - 1) * request.PageSize;

            // Build the base SQL queries
            var sql = new StringBuilder($@"
                SELECT * 
                FROM {event_category} 
                WHERE active IN (1,2) AND created_by = @UserId");

            var countSql = new StringBuilder($@"
                SELECT COUNT(*) 
                FROM {event_category} 
                WHERE active IN (1,2) AND created_by = @UserId");

            var parameters = new DynamicParameters();
            parameters.Add("UserId", request.user_id);
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", request.PageSize);

            // Apply filters if provided - simplified inline approach
            if (!string.IsNullOrWhiteSpace(request.FilterText))
            {
                string filterCondition;
                string filterPattern = $"%{request.FilterText.ToLower()}%";

                // Determine filter condition based on filter type
                if (string.IsNullOrWhiteSpace(request.FilterType))
                {
                    filterCondition = " AND (LOWER(event_category_name) LIKE @FilterPattern OR LOWER(event_category_desc) LIKE @FilterPattern)";
                }
                else
                {
                    filterCondition = request.FilterType.ToLower() switch
                    {
                        "name" => " AND LOWER(event_category_name) LIKE @FilterPattern",
                        "description" => " AND LOWER(event_category_desc) LIKE @FilterPattern",
                        _ => " AND (LOWER(event_category_name) LIKE @FilterPattern OR LOWER(event_category_desc) LIKE @FilterPattern)"
                    };
                }

                sql.Append(filterCondition);
                countSql.Append(filterCondition);
                parameters.Add("FilterPattern", filterPattern);
            }

            // Add ordering and pagination
            sql.Append(" ORDER BY event_category_id DESC ");
            sql.Append(" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");

            // Get total count
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql.ToString(), parameters);
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            // Get paginated data
            var data = await connection.QueryAsync<EventCategoryModel>(sql.ToString(), parameters);

            return (totalPages, data);
        }

        public async Task<int> UpdateEventCategoryStatusAsync(int eventCategoryId, int active, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();

            // Check if the status is already the same
            var checkQuery = $@"
            SELECT active 
            FROM {event_category} 
            WHERE event_category_id = @EventCategoryId AND active IN (1,2)";

            var currentStatus = await connection.ExecuteScalarAsync<int?>(checkQuery, new { EventCategoryId = eventCategoryId });

            if (currentStatus == active)
            {
                return -1; // Status is already the same
            }

            var updateQuery = $@"
            UPDATE {event_category} 
            SET active = @Active,
                updated_by = @UpdatedBy,
                updated_on = CURRENT_TIMESTAMP
            WHERE event_category_id = @EventCategoryId AND active IN (1,2)";

            var affectedRows = await connection.ExecuteAsync(updateQuery, new
            {
                EventCategoryId = eventCategoryId,
                Active = active,
                UpdatedBy = updatedBy
            });

            return affectedRows;
        }
    }
}
