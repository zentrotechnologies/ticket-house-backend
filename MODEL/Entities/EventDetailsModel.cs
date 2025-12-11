using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MODEL.Entities
{
    public class EventDetailsModel
    {
        public int event_id { get; set; }
        public Guid organizer_id { get; set; }
        public string event_name { get; set; }
        public string event_description { get; set; }
        public DateTime event_date { get; set; }
        public TimeSpan start_time { get; set; }
        public TimeSpan end_time { get; set; }
        public int total_duration_minutes { get; set; }
        public string location { get; set; }
        public string full_address { get; set; }
        public string geo_map_url { get; set; }
        public decimal? latitude { get; set; }
        public decimal? longitude { get; set; }
        public string language { get; set; }
        public int event_category_id { get; set; }
        public string banner_image { get; set; }
        //public string gallery_media { get; set; } // JSON string
        [JsonProperty(TypeNameHandling = TypeNameHandling.None)]
        public object gallery_media { get; set; } // Changed from string to object
        public int? age_limit { get; set; }
        //public string artists { get; set; } // JSON string
        [JsonProperty(TypeNameHandling = TypeNameHandling.None)]
        public object artists { get; set; } // Changed from string to object
        public string terms_and_conditions { get; set; }
        public decimal? min_price { get; set; }
        public decimal? max_price { get; set; }
        public bool is_featured { get; set; } = false;
        public string status { get; set; }
        public int? no_of_seats { get; set; }
        public string created_by { get; set; }
        public DateTime created_at { get; set; } = DateTime.UtcNow;
        public string updated_by { get; set; }
        public DateTime? updated_at { get; set; } = DateTime.UtcNow;
        public int active { get; set; } = 1;
    }

    public class EventMediaModel
    {
        public int event_media_id { get; set; }
        public int event_id { get; set; }
        public string media_type { get; set; }
        public string media_url { get; set; }
        public string created_by { get; set; }
        public DateTime created_on { get; set; } = DateTime.UtcNow;
        public string updated_by { get; set; }
        public DateTime? updated_on { get; set; } = DateTime.UtcNow;
        public int active { get; set; } = 1;
    }

    //to fetch events and media of that event if any exists
    public class EventDetailsWithMediaModel
    {
        public int event_id { get; set; }
        public Guid organizer_id { get; set; }
        public string event_name { get; set; }
        public string event_description { get; set; }
        public DateTime event_date { get; set; }
        public TimeSpan start_time { get; set; }
        public TimeSpan end_time { get; set; }
        public int total_duration_minutes { get; set; }
        public string location { get; set; }
        public string full_address { get; set; }
        public string geo_map_url { get; set; }
        public decimal? latitude { get; set; }
        public decimal? longitude { get; set; }
        public string language { get; set; }
        public int event_category_id { get; set; }
        public string banner_image { get; set; }
        public string gallery_media { get; set; }
        public int? age_limit { get; set; }
        public string artists { get; set; }
        public string terms_and_conditions { get; set; }
        public decimal? min_price { get; set; }
        public decimal? max_price { get; set; }
        public bool is_featured { get; set; }
        public string status { get; set; }
        public string created_by { get; set; }
        public DateTime created_at { get; set; }
        public string updated_by { get; set; }
        public DateTime? updated_at { get; set; }
        public int active { get; set; }

        // Media properties for direct mapping
        public int event_media_id { get; set; }
        public string media_type { get; set; }
        public string media_url { get; set; }
        public string media_created_by { get; set; }
        public DateTime media_created_on { get; set; }
    }

    // New models for artist and gallery
    public class EventArtistModel
    {
        public int event_artist_id { get; set; }
        public int event_id { get; set; }
        public string artist_name { get; set; }
        public string artist_photo { get; set; }
        public string created_by { get; set; }
        public DateTime created_on { get; set; } = DateTime.UtcNow;
        public string updated_by { get; set; }
        public DateTime? updated_on { get; set; } = DateTime.UtcNow;
        public int active { get; set; } = 1;
    }

    public class EventGalleryModel
    {
        public int event_gallary_id { get; set; }
        public int event_id { get; set; }
        public string event_img { get; set; }
        public string created_by { get; set; }
        public DateTime created_on { get; set; } = DateTime.UtcNow;
        public string updated_by { get; set; }
        public DateTime? updated_on { get; set; } = DateTime.UtcNow;
        public int active { get; set; } = 1;
    }

    // Add this model for organizer lookup
    public class UserOrganizerMapping
    {
        public Guid user_id { get; set; }
        public Guid organizer_id { get; set; }
        public string email { get; set; }
    }
}
