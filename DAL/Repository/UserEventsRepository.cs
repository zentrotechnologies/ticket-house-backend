using DAL.Utilities;
using Dapper;
using MODEL.Configuration;
using MODEL.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public interface IUserEventsRepository
    {
        Task<IEnumerable<UpcomingEventResponse>> GetUpcomingEventsAsync(UpcomingEventsRequest request);
        Task<IEnumerable<ArtistResponse>> GetShowsByArtistsAsync(GetShowsByArtistsRequest request);
        Task<IEnumerable<TestimonialResponse>> GetTestimonialsByArtistsAsync();
    }
    public class UserEventsRepository: IUserEventsRepository
    {
        private readonly ITHDBConnection _dbConnection;
        private readonly THConfiguration _configuration;
        private readonly string events = DatabaseConfiguration.events;
        private readonly string event_artist = DatabaseConfiguration.event_artist;
        private readonly string testimonial = DatabaseConfiguration.testimonial;

        public UserEventsRepository(ITHDBConnection dbConnection, THConfiguration configuration)
        {
            _dbConnection = dbConnection;
            _configuration = configuration;
        }

        public async Task<IEnumerable<UpcomingEventResponse>> GetUpcomingEventsAsync(UpcomingEventsRequest request)
        {
            using var connection = _dbConnection.GetConnection();

            // Calculate date range for "coming this week"
            var today = DateTime.Today;
            var weekEnd = today.AddDays(7);

            var query = $@"
                WITH UpcomingEvents AS (
                    SELECT 
                        event_id,
                        event_name,
                        event_date,
                        TO_CHAR(start_time, 'HH12:MI AM') as start_time,
                        TO_CHAR(end_time, 'HH12:MI AM') as end_time,
                        location,
                        banner_image,
                        TO_CHAR(event_date, 'Dy, DD Mon YYYY') as formatted_date
                    FROM {events}
                    WHERE active = 1 
                    AND event_date >= @Today
                    AND event_date <= @WeekEnd
                    ORDER BY event_date ASC, start_time ASC
                    LIMIT @Count
                ),
                LaterEvents AS (
                    SELECT 
                        event_id,
                        event_name,
                        event_date,
                        TO_CHAR(start_time, 'HH12:MI AM') as start_time,
                        TO_CHAR(end_time, 'HH12:MI AM') as end_time,
                        location,
                        banner_image,
                        TO_CHAR(event_date, 'Dy, DD Mon YYYY') as formatted_date
                    FROM {events}
                    WHERE active = 1 
                    AND event_date > @WeekEnd
                    ORDER BY event_date ASC, start_time ASC
                    LIMIT CASE WHEN @IncludeLaterEvents = true 
                              THEN GREATEST(0, @Count - (SELECT COUNT(*) FROM UpcomingEvents)) 
                              ELSE 0 END
                )
                SELECT * FROM UpcomingEvents
                UNION ALL
                SELECT * FROM LaterEvents
                ORDER BY event_date ASC, start_time ASC
                LIMIT @Count";

            var parameters = new
            {
                Today = today,
                WeekEnd = weekEnd,
                Count = request.Count,
                IncludeLaterEvents = request.IncludeLaterEvents
            };

            return await connection.QueryAsync<UpcomingEventResponse>(query, parameters);
        }

        public async Task<IEnumerable<ArtistResponse>> GetShowsByArtistsAsync(GetShowsByArtistsRequest request)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                WITH DistinctArtists AS (
                    SELECT DISTINCT ON (LOWER(artist_name))
                        ea.event_artist_id,
                        ea.artist_name,
                        ea.artist_photo,
                        'Stand-up Comedian' as role, -- You can modify this based on your business logic
                        COUNT(e.event_id) as event_count
                    FROM {event_artist} ea
                    LEFT JOIN {events} e ON e.event_id = ea.event_id AND e.active = 1
                    WHERE ea.active = 1
                    GROUP BY ea.event_artist_id, ea.artist_name, ea.artist_photo
                    ORDER BY LOWER(artist_name), event_count DESC
                    LIMIT @Count
                )
                SELECT * FROM DistinctArtists
                ORDER BY event_count DESC";

            var parameters = new { Count = request.Count };

            return await connection.QueryAsync<ArtistResponse>(query, parameters);
        }

        public async Task<IEnumerable<TestimonialResponse>> GetTestimonialsByArtistsAsync()
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                SELECT 
                    testimonial_id,
                    name,
                    designation,
                    profile_img,
                    description,
                    COALESCE(designation, 'Stand-up Comedian & Storyteller') as role
                FROM {testimonial}
                WHERE active = 1
                ORDER BY created_on DESC";

            return await connection.QueryAsync<TestimonialResponse>(query);
        }
    }
}
