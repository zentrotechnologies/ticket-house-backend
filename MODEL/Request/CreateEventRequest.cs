using Microsoft.AspNetCore.Http;
using MODEL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Request
{
    public class CreateEventRequest
    {
        public EventDetailsModel EventDetails { get; set; }
        public List<EventMediaModel> EventMedia { get; set; }
    }

    //event category
    public class UpdateEventCategoryStatusRequest
    {
        public int event_category_id { get; set; }
        public int active { get; set; }
        public string updated_by { get; set; }
    }

    // Request model for creating/updating events with artists and galleries
    //public class EventCreateRequestModel
    //{
    //    public EventDetailsModel EventDetails { get; set; }
    //    public List<EventArtistModel> EventArtists { get; set; } = new List<EventArtistModel>();
    //    public List<EventGalleryModel> EventGalleries { get; set; } = new List<EventGalleryModel>();
    //    public List<EventMediaModel> EventMedia { get; set; } = new List<EventMediaModel>();
    //}

    // Add this class to handle the request wrapper
    public class EventCreateWrapperRequest
    {
        //[Required(ErrorMessage = "eventRequest is required")]
        public EventCreateRequestModel eventRequest { get; set; }
    }

    // For paginated events by created_by
    public class EventPaginationRequest
    {
        public string created_by { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SearchText { get; set; }
        public string Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    // Artist upload request
    //public class ArtistUploadRequest
    //{
    //    public string ArtistName { get; set; }
    //    public IFormFile ArtistPhoto { get; set; }
    //}

    //// Gallery upload request
    //public class GalleryUploadRequest
    //{
    //    public IFormFile GalleryImage { get; set; }
    //}

    // Request model for creating/updating events with artists and galleries
    public class EventCreateRequestModel
    {
        public EventDetailsModel EventDetails { get; set; }
        public List<EventArtistModel> EventArtists { get; set; } = new List<EventArtistModel>();
        public List<EventGalleryModel> EventGalleries { get; set; } = new List<EventGalleryModel>();

        [DataType(DataType.Upload)]
        public IFormFile BannerImageFile { get; set; }
    }

    // For single artist upload
    public class ArtistUploadRequest
    {
        //[Required]
        public int EventId { get; set; }

        //[Required]
        public string ArtistName { get; set; }

        //[Required]
        public IFormFile ArtistPhoto { get; set; }
    }

    // For single gallery upload
    public class GalleryUploadRequest
    {
        //[Required]
        public int EventId { get; set; }

        //[Required]
        public IFormFile GalleryImage { get; set; }
    }

    // For banner upload
    public class BannerUploadRequest
    {
        //[Required]
        public int EventId { get; set; }

        //[Required]
        public IFormFile BannerImage { get; set; }
    }
}
