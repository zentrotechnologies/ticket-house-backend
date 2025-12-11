using DAL.Utilities;
using Dapper;
using MODEL.Entities;
using MODEL.Request;
using MODEL.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public interface IEventArtistGalleryRepository
    {
        // Event Artist methods
        Task<int> AddEventArtistAsync(EventArtistModel eventArtist);
        Task<IEnumerable<EventArtistModel>> GetEventArtistsByEventIdAsync(int eventId);
        Task<int> DeleteEventArtistAsync(int eventArtistId, string updatedBy);
        Task<int> DeleteAllEventArtistsAsync(int eventId, string updatedBy);
        Task<int> UpdateEventArtistAsync(EventArtistModel eventArtist);

        // Event Gallery methods
        Task<int> AddEventGalleryAsync(EventGalleryModel eventGallery);
        Task<IEnumerable<EventGalleryModel>> GetEventGalleriesByEventIdAsync(int eventId);
        Task<int> DeleteEventGalleryAsync(int eventGalleryId, string updatedBy);
        Task<int> DeleteAllEventGalleriesAsync(int eventId, string updatedBy);
        Task<int> UpdateEventGalleryAsync(EventGalleryModel eventGallery);

        // Combined methods
        Task<EventCompleteResponseModel> GetEventWithArtistsAndGalleriesAsync(int eventId);

        // Paginated events by created_by
        Task<PagedResponse<List<EventDetailsModel>>> GetPaginatedEventsByCreatedByAsync(
            string createdBy, int pageNumber, int pageSize, string searchText);

        // Get events with artists and galleries by created_by (for listing)
        Task<PagedResponse<List<EventCompleteResponseModel>>> GetPaginatedEventsWithDetailsByCreatedByAsync(
            EventPaginationRequest request);

        public Guid LookupUserIdByEmail(string email);
    }
    public class EventArtistGalleryRepository: IEventArtistGalleryRepository
    {
        private readonly ITHDBConnection _dbConnection;
        private readonly string event_artist = DatabaseConfiguration.event_artist;
        private readonly string event_gallary = DatabaseConfiguration.event_gallary;
        private readonly string events = DatabaseConfiguration.events;
        private readonly string Users = DatabaseConfiguration.Users;

        public EventArtistGalleryRepository(ITHDBConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        // Event Artist Methods
        public async Task<int> AddEventArtistAsync(EventArtistModel eventArtist)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                INSERT INTO {event_artist} 
                (event_id, artist_name, artist_photo, created_by, updated_by)
                VALUES 
                (@event_id, @artist_name, @artist_photo, @created_by, @updated_by)
                RETURNING event_artist_id";

            var artistId = await connection.ExecuteScalarAsync<int>(query, new
            {
                event_id = eventArtist.event_id,
                artist_name = eventArtist.artist_name,
                artist_photo = eventArtist.artist_photo,
                created_by = eventArtist.created_by,
                updated_by = eventArtist.updated_by
            });

            return artistId;
        }

        public async Task<IEnumerable<EventArtistModel>> GetEventArtistsByEventIdAsync(int eventId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {event_artist} 
                WHERE event_id = @EventId AND active = 1 
                ORDER BY event_artist_id";

            return await connection.QueryAsync<EventArtistModel>(query, new { EventId = eventId });
        }

        public async Task<int> DeleteEventArtistAsync(int eventArtistId, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {event_artist} 
                SET active = 0,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE event_artist_id = @event_artist_id AND active = 1";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                event_artist_id = eventArtistId,
                updated_by = updatedBy
            });

            return affectedRows;
        }

        public async Task<int> DeleteAllEventArtistsAsync(int eventId, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {event_artist} 
                SET active = 0,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE event_id = @event_id AND active = 1";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                event_id = eventId,
                updated_by = updatedBy
            });

            return affectedRows;
        }

        public async Task<int> UpdateEventArtistAsync(EventArtistModel eventArtist)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {event_artist} 
                SET artist_name = @artist_name,
                    artist_photo = @artist_photo,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE event_artist_id = @event_artist_id AND active = 1";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                event_artist_id = eventArtist.event_artist_id,
                artist_name = eventArtist.artist_name,
                artist_photo = eventArtist.artist_photo,
                updated_by = eventArtist.updated_by
            });

            return affectedRows;
        }

        // Event Gallery Methods
        public async Task<int> AddEventGalleryAsync(EventGalleryModel eventGallery)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                INSERT INTO {event_gallary} 
                (event_id, event_img, created_by, updated_by)
                VALUES 
                (@event_id, @event_img, @created_by, @updated_by)
                RETURNING event_gallary_id";

            var galleryId = await connection.ExecuteScalarAsync<int>(query, new
            {
                event_id = eventGallery.event_id,
                event_img = eventGallery.event_img,
                created_by = eventGallery.created_by,
                updated_by = eventGallery.updated_by
            });

            return galleryId;
        }

        public async Task<IEnumerable<EventGalleryModel>> GetEventGalleriesByEventIdAsync(int eventId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {event_gallary} 
                WHERE event_id = @EventId AND active = 1 
                ORDER BY event_gallary_id";

            return await connection.QueryAsync<EventGalleryModel>(query, new { EventId = eventId });
        }

        public async Task<int> DeleteEventGalleryAsync(int eventGalleryId, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {event_gallary} 
                SET active = 0,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE event_gallary_id = @event_gallary_id AND active = 1";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                event_gallary_id = eventGalleryId,
                updated_by = updatedBy
            });

            return affectedRows;
        }

        public async Task<int> DeleteAllEventGalleriesAsync(int eventId, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {event_gallary} 
                SET active = 0,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE event_id = @event_id AND active = 1";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                event_id = eventId,
                updated_by = updatedBy
            });

            return affectedRows;
        }

        public async Task<int> UpdateEventGalleryAsync(EventGalleryModel eventGallery)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {event_gallary} 
                SET event_img = @event_img,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE event_gallary_id = @event_gallary_id AND active = 1";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                event_gallary_id = eventGallery.event_gallary_id,
                event_img = eventGallery.event_img,
                updated_by = eventGallery.updated_by
            });

            return affectedRows;
        }

        // Combined Methods
        public async Task<EventCompleteResponseModel> GetEventWithArtistsAndGalleriesAsync(int eventId)
        {
            using var connection = _dbConnection.GetConnection();

            // Get event details
            var eventQuery = $@"
                SELECT * FROM {events} 
                WHERE event_id = @EventId AND active = 1";

            var eventDetails = await connection.QueryFirstOrDefaultAsync<EventDetailsModel>(eventQuery, new { EventId = eventId });

            if (eventDetails == null)
                return null;

            // Get artists
            var artists = await GetEventArtistsByEventIdAsync(eventId);

            // Get galleries
            var galleries = await GetEventGalleriesByEventIdAsync(eventId);

            return new EventCompleteResponseModel
            {
                EventDetails = eventDetails,
                EventArtists = artists.ToList(),
                EventGalleries = galleries.ToList()
            };
        }

        // Paginated events by created_by
        public async Task<PagedResponse<List<EventDetailsModel>>> GetPaginatedEventsByCreatedByAsync(
            string createdBy, int pageNumber, int pageSize, string searchText)
        {
            using var connection = _dbConnection.GetConnection();

            var offset = (pageNumber - 1) * pageSize;

            // Build search condition
            var searchCondition = string.Empty;
            var parameters = new DynamicParameters();
            parameters.Add("@created_by", createdBy);

            if (!string.IsNullOrEmpty(searchText))
            {
                searchCondition = " AND (event_name ILIKE @searchText OR location ILIKE @searchText OR event_description ILIKE @searchText)";
                parameters.Add("@searchText", $"%{searchText}%");
            }

            // Get total count
            var countQuery = $@"
                SELECT COUNT(*) FROM {events} 
                WHERE created_by = @created_by AND active = 1 {searchCondition}";

            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, parameters);

            // Get paginated data
            var query = $@"
                SELECT * FROM {events} 
                WHERE created_by = @created_by AND active = 1 {searchCondition}
                ORDER BY event_date DESC, created_at DESC
                LIMIT @pageSize OFFSET @offset";

            parameters.Add("@pageSize", pageSize);
            parameters.Add("@offset", offset);

            var eventsList = await connection.QueryAsync<EventDetailsModel>(query, parameters);

            return new PagedResponse<List<EventDetailsModel>>
            {
                Data = eventsList.ToList(),
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        // Get events with artists and galleries by created_by
        public async Task<PagedResponse<List<EventCompleteResponseModel>>> GetPaginatedEventsWithDetailsByCreatedByAsync(
            EventPaginationRequest request)
        {
            using var connection = _dbConnection.GetConnection();

            var offset = (request.PageNumber - 1) * request.PageSize;

            // Build conditions
            var conditions = new List<string> { "e.created_by = @created_by", "e.active = 1" };
            var parameters = new DynamicParameters();
            parameters.Add("@created_by", request.created_by);

            if (!string.IsNullOrEmpty(request.SearchText))
            {
                conditions.Add("(e.event_name ILIKE @searchText OR e.location ILIKE @searchText OR e.event_description ILIKE @searchText)");
                parameters.Add("@searchText", $"%{request.SearchText}%");
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                conditions.Add("e.status = @status");
                parameters.Add("@status", request.Status);
            }

            if (request.FromDate.HasValue)
            {
                conditions.Add("e.event_date >= @fromDate");
                parameters.Add("@fromDate", request.FromDate.Value.Date);
            }

            if (request.ToDate.HasValue)
            {
                conditions.Add("e.event_date <= @toDate");
                parameters.Add("@toDate", request.ToDate.Value.Date.AddDays(1).AddSeconds(-1));
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            // Get total count
            var countQuery = $@"
                SELECT COUNT(*) FROM {events} e
                {whereClause}";

            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, parameters);

            // Get paginated events
            var query = $@"
                SELECT e.* FROM {events} e
                {whereClause}
                ORDER BY e.event_date DESC, e.created_at DESC
                LIMIT @pageSize OFFSET @offset";

            parameters.Add("@pageSize", request.PageSize);
            parameters.Add("@offset", offset);

            var eventsList = await connection.QueryAsync<EventDetailsModel>(query, parameters);

            // Get artists and galleries for each event
            var result = new List<EventCompleteResponseModel>();

            foreach (var eventDetail in eventsList)
            {
                var artists = await GetEventArtistsByEventIdAsync(eventDetail.event_id);
                var galleries = await GetEventGalleriesByEventIdAsync(eventDetail.event_id);

                result.Add(new EventCompleteResponseModel
                {
                    EventDetails = eventDetail,
                    EventArtists = artists.ToList(),
                    EventGalleries = galleries.ToList()
                });
            }

            return new PagedResponse<List<EventCompleteResponseModel>>
            {
                Data = result,
                TotalCount = totalCount,
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }

        public Guid LookupUserIdByEmail(string email)
        {
            // Implement logic to get user ID by email from database
            // This is a simplified example
            using var connection = _dbConnection.GetConnection();
            var query = $@"SELECT user_id FROM {Users} WHERE email = @Email AND active = 1 LIMIT 1";
            return connection.QueryFirstOrDefault<Guid>(query, new { Email = email });
        }
    }
}
