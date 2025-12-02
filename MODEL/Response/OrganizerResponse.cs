using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Response
{
    public class OrganizerResponse
    {
        public Guid? user_id { get; set; }
        public Guid? organizer_id { get; set; }
        public string? first_name { get; set; }
        public string? last_name { get; set; }
        public string? email { get; set; }
        public string? mobile { get; set; }
        public string? org_name { get; set; }
        public DateTime? org_start_date { get; set; }
        public string? verification_status { get; set; }
        public int? active { get; set; }
        public string? created_on { get; set; }

        // User role info
        public int? role_id { get; set; }
        public string? role_name { get; set; }

        // Bank details
        public string? bank_account_no { get; set; }
        public string? bank_name { get; set; }
        public string? beneficiary_name { get; set; }

        // Contact info
        public string? owner_personal_email { get; set; }
        public string? owner_mobile { get; set; }
        public string? state { get; set; }
        public string? city { get; set; }
        public string? country { get; set; }

        // Document numbers
        public string? aadhar_number { get; set; }
        public string? pancard_number { get; set; }
        public string? gst_number { get; set; }

        // Social links
        public string? instagram_link { get; set; }
        public string? youtube_link { get; set; }
        public string? facebook_link { get; set; }
        public string? twitter_link { get; set; }
    }

    public class OrganizerListResponse : CommonResponseModel<List<OrganizerResponse>>
    {
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
