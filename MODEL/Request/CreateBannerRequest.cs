using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Request
{
    public class CreateBannerRequest
    {
        public string? banner_img { get; set; } // Base64 string or file path
        public string? action_link_url { get; set; }
        public string? created_by { get; set; }
    }

    public class UpdateBannerRequest
    {
        public string? banner_img { get; set; }
        public string? action_link_url { get; set; }
        public string? updated_by { get; set; }
    }
}
