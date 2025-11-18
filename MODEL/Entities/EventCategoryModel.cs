using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Entities
{
    public class EventCategoryModel
    {
        public int event_category_id { get; set; }
        public string event_category_name { get; set; }
        public string event_category_desc { get; set; }
        public string created_by { get; set; }
        public DateTime created_on { get; set; } = DateTime.UtcNow;
        public string updated_by { get; set; }
        public DateTime updated_on { get; set; } = DateTime.UtcNow;
        public int active { get; set; } = 1;
    }
}
