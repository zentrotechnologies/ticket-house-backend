using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Entities
{
    public class BannerManagementModel
    {
        public int banner_id { get; set; }
        public string? banner_img { get; set; }
        public string? action_link_url { get; set; }
        public string? created_by { get; set; }
        public DateTime created_on { get; set; }
        public string? updated_by { get; set; }
        public DateTime updated_on { get; set; }
        public int active { get; set; } = 1;
    }
}
