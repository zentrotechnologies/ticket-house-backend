using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Request
{
    public class OrganizerRequest
    {
        // User fields
        public string? first_name { get; set; }
        public string? last_name { get; set; }
        public string email { get; set; } = string.Empty;
        public string? country_code { get; set; }
        public string? mobile { get; set; }
        public string password { get; set; } = string.Empty;
        public int role_id { get; set; } = 2; // Organizer role

        // Organizer fields
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

        public string created_by { get; set; } = string.Empty;
        public string updated_by { get; set; } = string.Empty;
    }

    // Helper class for status update
    public class UpdateStatusRequest
    {
        public int Status { get; set; } // 1 for approved, 0 for rejected
    }
}
