using DAL.Utilities;
using Dapper;
using Microsoft.Extensions.Logging;
using MODEL.Entities;
using MODEL.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public interface IEventDetailsRepository
    {
        Task<IEnumerable<EventDetailsModel>> GetAllEventsAsync();
        Task<EventDetailsModel> GetEventByIdAsync(int eventId);
        Task<int> CreateEventAsync(EventDetailsModel eventDetails);
        Task<int> UpdateEventAsync(EventDetailsModel eventDetails);
        Task<int> DeleteEventAsync(int eventId, string updatedBy);
        Task<int> AddEventMediaAsync(EventMediaModel eventMedia);
        Task<IEnumerable<EventMediaModel>> GetEventMediaByEventIdAsync(int eventId);
        Task<int> DeleteEventMediaAsync(int eventMediaId, string updatedBy);
        Task<int> DeleteAllEventMediaAsync(int eventId, string updatedBy);

        //to fetch events and media together
        Task<IEnumerable<EventWithMediaResponse>> GetAllEventsWithMediaAsync();
        Task<EventWithMediaResponse> GetEventWithMediaByIdAsync(int eventId);
        Task<IEnumerable<EventWithMediaResponse>> GetEventsWithMediaByCategoryAsync(int categoryId);
        Task<IEnumerable<EventWithMediaResponse>> GetUpcomingEventsWithMediaAsync(int days = 30);
        Task<IEnumerable<EventWithMediaResponse>> GetFeaturedEventsWithMediaAsync();

        Task<Guid?> GetOrganizerIdByUserIdAsync(Guid userId);
        Task<UserOrganizerMapping> GetOrganizerByUserIdAsync(Guid userId);

        // Artist methods
        Task<int> AddEventArtistAsync(EventArtistModel eventArtist);
        Task<IEnumerable<EventArtistModel>> GetEventArtistsByEventIdAsync(int eventId);
        Task<int> DeleteAllEventArtistsAsync(int eventId, string updatedBy);

        // Gallery methods
        Task<int> AddEventGalleryAsync(EventGalleryModel eventGallery);
        Task<IEnumerable<EventGalleryModel>> GetEventGalleriesByEventIdAsync(int eventId);
        Task<int> DeleteAllEventGalleriesAsync(int eventId, string updatedBy);
    }
    public class EventDetailsRepository: IEventDetailsRepository
    {
        private readonly ITHDBConnection _dbConnection;
        private readonly string events = DatabaseConfiguration.events;
        private readonly string event_media = DatabaseConfiguration.event_media;
        private readonly string EventOrganizer = DatabaseConfiguration.EventOrganizer;
        private readonly string Users = DatabaseConfiguration.Users;

        public EventDetailsRepository(ITHDBConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<EventDetailsModel>> GetAllEventsAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {events} 
                WHERE active = 1 
                ORDER BY event_id DESC";

            return await connection.QueryAsync<EventDetailsModel>(query);
        }

        public async Task<EventDetailsModel> GetEventByIdAsync(int eventId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {events} 
                WHERE event_id = @EventId AND active = 1";

            return await connection.QueryFirstOrDefaultAsync<EventDetailsModel>(query, new { EventId = eventId });
        }

        //public async Task<int> CreateEventAsync(EventDetailsModel eventDetails)
        //{
        //    using var connection = _dbConnection.GetConnection();
        //    var query = $@"
        //        INSERT INTO {events} 
        //        (organizer_id, event_name, event_description, event_date, start_time, end_time,
        //         total_duration_minutes, location, full_address, geo_map_url, latitude, longitude,
        //         language, event_category_id, banner_image, gallery_media, age_limit, artists,
        //         terms_and_conditions, min_price, max_price, is_featured, status, created_by, updated_by)
        //        VALUES 
        //        (@organizer_id, @event_name, @event_description, @event_date, @start_time, @end_time,
        //         @total_duration_minutes, @location, @full_address, @geo_map_url, @latitude, @longitude,
        //         @language, @event_category_id, @banner_image, @gallery_media, @age_limit, @artists,
        //         @terms_and_conditions, @min_price, @max_price, @is_featured, @status, @created_by, @updated_by)
        //        RETURNING event_id";

        //    var eventId = await connection.ExecuteScalarAsync<int>(query, new
        //    {
        //        organizer_id = eventDetails.organizer_id,
        //        event_name = eventDetails.event_name,
        //        event_description = eventDetails.event_description,
        //        event_date = eventDetails.event_date,
        //        start_time = eventDetails.start_time,
        //        end_time = eventDetails.end_time,
        //        total_duration_minutes = eventDetails.total_duration_minutes,
        //        location = eventDetails.location,
        //        full_address = eventDetails.full_address,
        //        geo_map_url = eventDetails.geo_map_url,
        //        latitude = eventDetails.latitude,
        //        longitude = eventDetails.longitude,
        //        language = eventDetails.language,
        //        event_category_id = eventDetails.event_category_id,
        //        banner_image = eventDetails.banner_image,
        //        gallery_media = eventDetails.gallery_media,
        //        age_limit = eventDetails.age_limit,
        //        artists = eventDetails.artists,
        //        terms_and_conditions = eventDetails.terms_and_conditions,
        //        min_price = eventDetails.min_price,
        //        max_price = eventDetails.max_price,
        //        is_featured = eventDetails.is_featured,
        //        status = eventDetails.status,
        //        created_by = eventDetails.created_by,
        //        updated_by = eventDetails.updated_by
        //    });

        //    return eventId;
        //}

        public async Task<int> CreateEventAsync(EventDetailsModel eventDetails)
        {
            using var connection = _dbConnection.GetConnection();

            // Parse gallery_media and artists to ensure they're proper JSON
            object galleryMedia = "[]";
            object artists = "[]";

            try
            {
                if (eventDetails.gallery_media != null)
                {
                    if (eventDetails.gallery_media is string galleryMediaStr)
                    {
                        // Try to parse as JSON to validate
                        JsonConvert.DeserializeObject(galleryMediaStr);
                        galleryMedia = galleryMediaStr;
                    }
                    else
                    {
                        galleryMedia = JsonConvert.SerializeObject(eventDetails.gallery_media);
                    }
                }

                if (eventDetails.artists != null)
                {
                    if (eventDetails.artists is string artistsStr)
                    {
                        // Try to parse as JSON to validate
                        JsonConvert.DeserializeObject(artistsStr);
                        artists = artistsStr;
                    }
                    else
                    {
                        artists = JsonConvert.SerializeObject(eventDetails.artists);
                    }
                }
            }
            catch
            {
                // If invalid JSON, use empty arrays
                galleryMedia = "[]";
                artists = "[]";
            }

            var query = $@"
                INSERT INTO {events} 
                (organizer_id, event_name, event_description, event_date, start_time, end_time,
                 total_duration_minutes, location, full_address, geo_map_url, latitude, longitude,
                 language, event_category_id, banner_image, gallery_media, age_limit, artists,
                 terms_and_conditions, min_price, max_price, is_featured, status, created_by, updated_by)
                VALUES 
                (@organizer_id, @event_name, @event_description, @event_date, @start_time, @end_time,
                 @total_duration_minutes, @location, @full_address, @geo_map_url, @latitude, @longitude,
                 @language, @event_category_id, @banner_image, @gallery_media::json, @age_limit, @artists::json,
                 @terms_and_conditions, @min_price, @max_price, @is_featured, @status, @created_by, @updated_by)
                RETURNING event_id";

            var eventId = await connection.ExecuteScalarAsync<int>(query, new
            {
                organizer_id = eventDetails.organizer_id,
                event_name = eventDetails.event_name,
                event_description = eventDetails.event_description,
                event_date = eventDetails.event_date,
                start_time = eventDetails.start_time,
                end_time = eventDetails.end_time,
                total_duration_minutes = eventDetails.total_duration_minutes,
                location = eventDetails.location,
                full_address = eventDetails.full_address,
                geo_map_url = eventDetails.geo_map_url,
                latitude = eventDetails.latitude,
                longitude = eventDetails.longitude,
                language = eventDetails.language,
                event_category_id = eventDetails.event_category_id,
                banner_image = eventDetails.banner_image,
                gallery_media = galleryMedia, // Use parsed JSON
                age_limit = eventDetails.age_limit,
                artists = artists, // Use parsed JSON
                terms_and_conditions = eventDetails.terms_and_conditions,
                min_price = eventDetails.min_price,
                max_price = eventDetails.max_price,
                is_featured = eventDetails.is_featured,
                status = eventDetails.status,
                created_by = eventDetails.created_by,
                updated_by = eventDetails.updated_by
            });

            return eventId;
        }

        //public async Task<int> UpdateEventAsync(EventDetailsModel eventDetails)
        //{
        //    using var connection = _dbConnection.GetConnection();
        //    var query = $@"
        //        UPDATE {events} 
        //        SET organizer_id = @organizer_id,
        //            event_name = @event_name,
        //            event_description = @event_description,
        //            event_date = @event_date,
        //            start_time = @start_time,
        //            end_time = @end_time,
        //            total_duration_minutes = @total_duration_minutes,
        //            location = @location,
        //            full_address = @full_address,
        //            geo_map_url = @geo_map_url,
        //            latitude = @latitude,
        //            longitude = @longitude,
        //            language = @language,
        //            event_category_id = @event_category_id,
        //            banner_image = @banner_image,
        //            gallery_media = @gallery_media,
        //            age_limit = @age_limit,
        //            artists = @artists,
        //            terms_and_conditions = @terms_and_conditions,
        //            min_price = @min_price,
        //            max_price = @max_price,
        //            is_featured = @is_featured,
        //            status = @status,
        //            updated_by = @updated_by,
        //            updated_at = CURRENT_TIMESTAMP
        //        WHERE event_id = @event_id AND active = 1";

        //    var affectedRows = await connection.ExecuteAsync(query, new
        //    {
        //        event_id = eventDetails.event_id,
        //        organizer_id = eventDetails.organizer_id,
        //        event_name = eventDetails.event_name,
        //        event_description = eventDetails.event_description,
        //        event_date = eventDetails.event_date,
        //        start_time = eventDetails.start_time,
        //        end_time = eventDetails.end_time,
        //        total_duration_minutes = eventDetails.total_duration_minutes,
        //        location = eventDetails.location,
        //        full_address = eventDetails.full_address,
        //        geo_map_url = eventDetails.geo_map_url,
        //        latitude = eventDetails.latitude,
        //        longitude = eventDetails.longitude,
        //        language = eventDetails.language,
        //        event_category_id = eventDetails.event_category_id,
        //        banner_image = eventDetails.banner_image,
        //        gallery_media = eventDetails.gallery_media,
        //        age_limit = eventDetails.age_limit,
        //        artists = eventDetails.artists,
        //        terms_and_conditions = eventDetails.terms_and_conditions,
        //        min_price = eventDetails.min_price,
        //        max_price = eventDetails.max_price,
        //        is_featured = eventDetails.is_featured,
        //        status = eventDetails.status,
        //        updated_by = eventDetails.updated_by
        //    });

        //    return affectedRows;
        //}

        public async Task<int> UpdateEventAsync(EventDetailsModel eventDetails)
        {
            using var connection = _dbConnection.GetConnection();

            // Parse gallery_media and artists to ensure they're proper JSON
            object galleryMedia = "[]";
            object artists = "[]";

            try
            {
                if (eventDetails.gallery_media != null)
                {
                    if (eventDetails.gallery_media is string galleryMediaStr)
                    {
                        // Try to parse as JSON to validate
                        JsonConvert.DeserializeObject(galleryMediaStr);
                        galleryMedia = galleryMediaStr;
                    }
                    else
                    {
                        galleryMedia = JsonConvert.SerializeObject(eventDetails.gallery_media);
                    }
                }

                if (eventDetails.artists != null)
                {
                    if (eventDetails.artists is string artistsStr)
                    {
                        // Try to parse as JSON to validate
                        JsonConvert.DeserializeObject(artistsStr);
                        artists = artistsStr;
                    }
                    else
                    {
                        artists = JsonConvert.SerializeObject(eventDetails.artists);
                    }
                }
            }
            catch
            {
                // If invalid JSON, use empty arrays
                galleryMedia = "[]";
                artists = "[]";
            }

            var query = $@"
                UPDATE {events} 
                SET organizer_id = @organizer_id,
                    event_name = @event_name,
                    event_description = @event_description,
                    event_date = @event_date,
                    start_time = @start_time,
                    end_time = @end_time,
                    total_duration_minutes = @total_duration_minutes,
                    location = @location,
                    full_address = @full_address,
                    geo_map_url = @geo_map_url,
                    latitude = @latitude,
                    longitude = @longitude,
                    language = @language,
                    event_category_id = @event_category_id,
                    banner_image = @banner_image,
                    gallery_media = @gallery_media::json,
                    age_limit = @age_limit,
                    artists = @artists::json,
                    terms_and_conditions = @terms_and_conditions,
                    min_price = @min_price,
                    max_price = @max_price,
                    is_featured = @is_featured,
                    status = @status,
                    updated_by = @updated_by,
                    updated_at = CURRENT_TIMESTAMP
                WHERE event_id = @event_id AND active = 1";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                event_id = eventDetails.event_id,
                organizer_id = eventDetails.organizer_id,
                event_name = eventDetails.event_name,
                event_description = eventDetails.event_description,
                event_date = eventDetails.event_date,
                start_time = eventDetails.start_time,
                end_time = eventDetails.end_time,
                total_duration_minutes = eventDetails.total_duration_minutes,
                location = eventDetails.location,
                full_address = eventDetails.full_address,
                geo_map_url = eventDetails.geo_map_url,
                latitude = eventDetails.latitude,
                longitude = eventDetails.longitude,
                language = eventDetails.language,
                event_category_id = eventDetails.event_category_id,
                banner_image = eventDetails.banner_image,
                gallery_media = galleryMedia, // Use parsed JSON
                age_limit = eventDetails.age_limit,
                artists = artists, // Use parsed JSON
                terms_and_conditions = eventDetails.terms_and_conditions,
                min_price = eventDetails.min_price,
                max_price = eventDetails.max_price,
                is_featured = eventDetails.is_featured,
                status = eventDetails.status,
                updated_by = eventDetails.updated_by
            });

            return affectedRows;
        }

        public async Task<int> DeleteEventAsync(int eventId, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {events} 
                SET active = 0,
                    updated_by = @updated_by,
                    updated_at = CURRENT_TIMESTAMP
                WHERE event_id = @event_id AND active = 1";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                event_id = eventId,
                updated_by = updatedBy
            });

            return affectedRows;
        }

        public async Task<int> AddEventMediaAsync(EventMediaModel eventMedia)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                INSERT INTO {event_media} 
                (event_id, media_type, media_url, created_by, updated_by)
                VALUES 
                (@event_id, @media_type, @media_url, @created_by, @updated_by)
                RETURNING event_media_id";

            var mediaId = await connection.ExecuteScalarAsync<int>(query, new
            {
                event_id = eventMedia.event_id,
                media_type = eventMedia.media_type,
                media_url = eventMedia.media_url,
                created_by = eventMedia.created_by,
                updated_by = eventMedia.updated_by
            });

            return mediaId;
        }

        public async Task<IEnumerable<EventMediaModel>> GetEventMediaByEventIdAsync(int eventId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {event_media} 
                WHERE event_id = @EventId AND active = 1 
                ORDER BY event_media_id";

            return await connection.QueryAsync<EventMediaModel>(query, new { EventId = eventId });
        }

        public async Task<int> DeleteEventMediaAsync(int eventMediaId, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {event_media} 
                SET active = 0,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE event_media_id = @event_media_id AND active = 1";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                event_media_id = eventMediaId,
                updated_by = updatedBy
            });

            return affectedRows;
        }

        public async Task<int> DeleteAllEventMediaAsync(int eventId, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {event_media} 
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

        //to fetch events and media together
        public async Task<IEnumerable<EventWithMediaResponse>> GetAllEventsWithMediaAsync()
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
            SELECT 
                e.*,
                em.event_media_id,
                em.media_type,
                em.media_url,
                em.created_by as media_created_by,
                em.created_on as media_created_on
            FROM {events} e
            LEFT JOIN {event_media} em ON e.event_id = em.event_id AND em.active = 1
            WHERE e.active = 1
            ORDER BY e.event_date DESC, e.event_id DESC, em.event_media_id";

            var eventsWithMedia = await connection.QueryAsync<EventDetailsWithMediaModel>(query);

            // Group events and their media
            return GroupEventsWithMedia(eventsWithMedia);
        }

        public async Task<EventWithMediaResponse> GetEventWithMediaByIdAsync(int eventId)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
            SELECT 
                e.*,
                em.event_media_id,
                em.media_type,
                em.media_url,
                em.created_by as media_created_by,
                em.created_on as media_created_on
            FROM {events} e
            LEFT JOIN {event_media} em ON e.event_id = em.event_id AND em.active = 1
            WHERE e.event_id = @EventId AND e.active = 1
            ORDER BY em.event_media_id";

            var eventsWithMedia = await connection.QueryAsync<EventDetailsWithMediaModel>(query, new { EventId = eventId });

            var groupedEvents = GroupEventsWithMedia(eventsWithMedia);
            return groupedEvents.FirstOrDefault();
        }

        public async Task<IEnumerable<EventWithMediaResponse>> GetEventsWithMediaByCategoryAsync(int categoryId)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
            SELECT 
                e.*,
                em.event_media_id,
                em.media_type,
                em.media_url,
                em.created_by as media_created_by,
                em.created_on as media_created_on
            FROM {events} e
            LEFT JOIN {event_media} em ON e.event_id = em.event_id AND em.active = 1
            WHERE e.active = 1 AND e.event_category_id = @CategoryId
            ORDER BY e.event_date DESC, e.event_id DESC, em.event_media_id";

            var eventsWithMedia = await connection.QueryAsync<EventDetailsWithMediaModel>(query, new { CategoryId = categoryId });

            return GroupEventsWithMedia(eventsWithMedia);
        }

        public async Task<IEnumerable<EventWithMediaResponse>> GetUpcomingEventsWithMediaAsync(int days = 30)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
            SELECT 
                e.*,
                em.event_media_id,
                em.media_type,
                em.media_url,
                em.created_by as media_created_by,
                em.created_on as media_created_on
            FROM {events} e
            LEFT JOIN {event_media} em ON e.event_id = em.event_id AND em.active = 1
            WHERE e.active = 1 
                AND e.event_date >= CURRENT_DATE 
                AND e.event_date <= CURRENT_DATE + INTERVAL '{days} days'
            ORDER BY e.event_date ASC, e.event_id DESC, em.event_media_id";

            var eventsWithMedia = await connection.QueryAsync<EventDetailsWithMediaModel>(query);

            return GroupEventsWithMedia(eventsWithMedia);
        }

        public async Task<IEnumerable<EventWithMediaResponse>> GetFeaturedEventsWithMediaAsync()
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
            SELECT 
                e.*,
                em.event_media_id,
                em.media_type,
                em.media_url,
                em.created_by as media_created_by,
                em.created_on as media_created_on
            FROM {events} e
            LEFT JOIN {event_media} em ON e.event_id = em.event_id AND em.active = 1
            WHERE e.active = 1 AND e.is_featured = true
            ORDER BY e.event_date ASC, e.event_id DESC, em.event_media_id";

            var eventsWithMedia = await connection.QueryAsync<EventDetailsWithMediaModel>(query);

            return GroupEventsWithMedia(eventsWithMedia);
        }

        private List<EventWithMediaResponse> GroupEventsWithMedia(IEnumerable<EventDetailsWithMediaModel> eventsWithMedia)
        {
            var groupedEvents = eventsWithMedia
                .GroupBy(e => new
                {
                    e.event_id,
                    e.organizer_id,
                    e.event_name,
                    e.event_description,
                    e.event_date,
                    e.start_time,
                    e.end_time,
                    e.total_duration_minutes,
                    e.location,
                    e.full_address,
                    e.geo_map_url,
                    e.latitude,
                    e.longitude,
                    e.language,
                    e.event_category_id,
                    e.banner_image,
                    e.gallery_media,
                    e.age_limit,
                    e.artists,
                    e.terms_and_conditions,
                    e.min_price,
                    e.max_price,
                    e.is_featured,
                    e.status,
                    e.created_by,
                    e.created_at,
                    e.updated_by,
                    e.updated_at,
                    e.active
                })
                .Select(g => new EventWithMediaResponse
                {
                    event_id = g.Key.event_id,
                    organizer_id = g.Key.organizer_id,
                    event_name = g.Key.event_name,
                    event_description = g.Key.event_description,
                    event_date = g.Key.event_date,
                    start_time = g.Key.start_time,
                    end_time = g.Key.end_time,
                    total_duration_minutes = g.Key.total_duration_minutes,
                    location = g.Key.location,
                    full_address = g.Key.full_address,
                    geo_map_url = g.Key.geo_map_url,
                    latitude = g.Key.latitude,
                    longitude = g.Key.longitude,
                    language = g.Key.language,
                    event_category_id = g.Key.event_category_id,
                    banner_image = g.Key.banner_image,
                    gallery_media = g.Key.gallery_media,
                    age_limit = g.Key.age_limit,
                    artists = g.Key.artists,
                    terms_and_conditions = g.Key.terms_and_conditions,
                    min_price = g.Key.min_price,
                    max_price = g.Key.max_price,
                    is_featured = g.Key.is_featured,
                    status = g.Key.status,
                    created_by = g.Key.created_by,
                    created_at = g.Key.created_at,
                    updated_by = g.Key.updated_by,
                    updated_at = g.Key.updated_at,
                    active = g.Key.active,
                    EventMedia = g.Where(x => x.event_media_id > 0)
                        .Select(m => new EventMediaModel
                        {
                            event_media_id = m.event_media_id,
                            event_id = m.event_id,
                            media_type = m.media_type,
                            media_url = m.media_url,
                            created_by = m.media_created_by,
                            created_on = m.media_created_on,
                            active = 1
                        })
                        .ToList()
                })
                .ToList();

            return groupedEvents;
        }

        public async Task<Guid?> GetOrganizerIdByUserIdAsync(Guid userId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
            SELECT organizer_id 
            FROM {EventOrganizer} 
            WHERE user_id = @UserId AND active = 1 
            LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<Guid?>(query, new { UserId = userId });
        }

        public async Task<UserOrganizerMapping> GetOrganizerByUserIdAsync(Guid userId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
            SELECT 
                eo.user_id,
                eo.organizer_id,
                u.email
            FROM {EventOrganizer} eo
            INNER JOIN {Users} u ON eo.user_id = u.user_id
            WHERE eo.user_id = @UserId 
                AND eo.active = 1 
                AND u.active = 1
            LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<UserOrganizerMapping>(query, new { UserId = userId });
        }

        // Event Artist Methods
        public async Task<int> AddEventArtistAsync(EventArtistModel eventArtist)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
            INSERT INTO event_artist 
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
            SELECT * FROM event_artist 
            WHERE event_id = @EventId AND active = 1 
            ORDER BY event_artist_id";

            return await connection.QueryAsync<EventArtistModel>(query, new { EventId = eventId });
        }

        public async Task<int> DeleteAllEventArtistsAsync(int eventId, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
            UPDATE event_artist 
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

        // Event Gallery Methods
        public async Task<int> AddEventGalleryAsync(EventGalleryModel eventGallery)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
            INSERT INTO event_gallary 
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
            SELECT * FROM event_gallary 
            WHERE event_id = @EventId AND active = 1 
            ORDER BY event_gallary_id";

            return await connection.QueryAsync<EventGalleryModel>(query, new { EventId = eventId });
        }

        public async Task<int> DeleteAllEventGalleriesAsync(int eventId, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
            UPDATE event_gallary 
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
    }
}
