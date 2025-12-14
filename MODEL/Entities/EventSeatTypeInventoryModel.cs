using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Entities
{
    public class EventSeatTypeInventoryModel
    {
        public int event_seat_type_inventory_id { get; set; }
        public int event_id { get; set; }
        public string seat_name { get; set; }
        public decimal price { get; set; }
        public int total_seats { get; set; }
        public int available_seats { get; set; }
        public string created_by { get; set; }
        public DateTime created_on { get; set; } = DateTime.UtcNow;
        public string updated_by { get; set; }
        public DateTime? updated_on { get; set; } = DateTime.UtcNow;
        public int active { get; set; } = 1;
    }

    // Extended Event Details Model with seat types
    public class EventDetailsWithSeatsModel : EventDetailsModel
    {
        public List<EventSeatTypeInventoryModel> SeatTypes { get; set; } = new List<EventSeatTypeInventoryModel>();
    }
}
