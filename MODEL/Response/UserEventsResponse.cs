using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Response
{
    public class UserEventsResponse
    {
    }
    public class EventModel
    {
        public int event_id { get; set; }
        public string? organizer_id { get; set; }
        public string? event_name { get; set; }
        public string? event_description { get; set; }
        public DateTime? event_date { get; set; }
        public TimeSpan? start_time { get; set; }
        public TimeSpan? end_time { get; set; }
        public int? total_duration_minutes { get; set; }
        public string? location { get; set; }
        public string? full_address { get; set; }
        public string? geo_map_url { get; set; }
        public decimal? latitude { get; set; }
        public decimal? longitude { get; set; }
        public string? language { get; set; }
        public int? event_category_id { get; set; }
        public string? banner_image { get; set; }
        public string? gallery_media { get; set; }
        public int? age_limit { get; set; }
        public string? artists { get; set; }
        public string? terms_and_conditions { get; set; }
        public decimal? min_price { get; set; }
        public decimal? max_price { get; set; }
        public bool? is_featured { get; set; }
        public string? status { get; set; }
        public int? no_of_seats { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public string? created_by { get; set; }
        public string? updated_by { get; set; }
        public int? active { get; set; }
    }

    public class UpcomingEventResponse
    {
        public int event_id { get; set; }
        public string? event_name { get; set; }
        public DateTime? event_date { get; set; }
        public string? start_time { get; set; }
        public string? end_time { get; set; }
        public string? location { get; set; }
        public string? banner_image { get; set; }
        public string? formatted_date { get; set; }
    }

    public class ArtistResponse
    {
        public int event_artist_id { get; set; }
        public string? artist_name { get; set; }
        public string? artist_photo { get; set; }
        public string? role { get; set; }
        public int? event_count { get; set; }
    }

    public class TestimonialResponse
    {
        public int testimonial_id { get; set; }
        public string? name { get; set; }
        public string? designation { get; set; }
        public string? profile_img { get; set; }
        public string? description { get; set; }
        public string? role { get; set; }
    }

    public class UpcomingEventsRequest
    {
        public int Count { get; set; } = 8;
        public bool IncludeLaterEvents { get; set; } = true;
    }

    public class GetShowsByArtistsRequest
    {
        public int Count { get; set; } = 5;
    }
}
