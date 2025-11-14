using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Entities
{
    public class EventOrganizerModel
    {
        public Guid organizer_id { get; set; }
        public Guid user_id { get; set; }
        public string? org_name { get; set; }
        public DateTime? org_start_date { get; set; }
        public string? bank_account_no { get; set; }
        public string? bank_ifsc { get; set; }
        public string? bank_name { get; set; }
        public string? beneficiary_name { get; set; }
        public string? aadhar_number { get; set; }
        public string? pancard_number { get; set; }
        public string? owner_personal_email { get; set; }
        public string? owner_mobile { get; set; }
        public string? state { get; set; }
        public string? city { get; set; }
        public string? country { get; set; }
        public string? gst_number { get; set; }
        public string? instagram_link { get; set; }
        public string? youtube_link { get; set; }
        public string? facebook_link { get; set; }
        public string? twitter_link { get; set; }
        public string verification_status { get; set; } = "pending";
        public string? created_by { get; set; }
        public DateTime created_on { get; set; }
        public string? updated_by { get; set; }
        public DateTime updated_on { get; set; }
        public int active { get; set; } = 1;
    }
}
