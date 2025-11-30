using MODEL.Entities;
using System;
using System.Collections.Generic;
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
}
