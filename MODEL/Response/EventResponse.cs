using MODEL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Response
{
    public class EventResponse
    {
        public EventDetailsModel EventDetails { get; set; }
        public List<EventMediaModel> EventMedia { get; set; }
    }

    public class EventWithMediaResponse
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

        public List<EventMediaModel> EventMedia { get; set; } = new List<EventMediaModel>();
    }

    // Combined response model
    public class EventCompleteResponseModel
    {
        public EventDetailsModel EventDetails { get; set; }
        public List<EventArtistModel> EventArtists { get; set; } = new List<EventArtistModel>();
        public List<EventGalleryModel> EventGalleries { get; set; } = new List<EventGalleryModel>();
        public List<EventMediaModel> EventMedia { get; set; } = new List<EventMediaModel>();
    }
}
