using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Entities
{
    public class TestimonialModel
    {
        public int testimonial_id { get; set; }
        public string name { get; set; }
        public string designation { get; set; }
        public string profile_img { get; set; }
        public string description { get; set; }
        public string created_by { get; set; }
        public DateTime created_on { get; set; } = DateTime.UtcNow;
        public string updated_by { get; set; }
        public DateTime updated_on { get; set; } = DateTime.UtcNow;
        public int active { get; set; } = 1;
    }
}
