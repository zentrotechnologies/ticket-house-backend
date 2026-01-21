using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Entities
{
    public class BookingModel
    {
        public int booking_id { get; set; }
        public string booking_code { get; set; }
        public Guid user_id { get; set; }
        public int event_id { get; set; }
        public decimal total_amount { get; set; }
        public string status { get; set; } // pending/confirmed/cancelled/failed
        public DateTime created_on { get; set; } = DateTime.UtcNow;
        public string created_by { get; set; }
        public DateTime? updated_on { get; set; } = DateTime.UtcNow;
        public string updated_by { get; set; }
        public int active { get; set; } = 1;
    }

    public class BookingSeatModel
    {
        public int booking_seat_id { get; set; }
        public int booking_id { get; set; }
        public int event_seat_type_inventory_id { get; set; }
        public int quantity { get; set; }
        public decimal price_per_seat { get; set; }
        public decimal subtotal { get; set; }

        // Scanning related fields
        public int scanned_quantity { get; set; } = 0;
        public int remaining_quantity { get; set; }
        public DateTime? last_scan_time { get; set; }
        public string scanned_by { get; set; }

        public DateTime created_on { get; set; } = DateTime.UtcNow;
        public string created_by { get; set; }
        public DateTime? updated_on { get; set; } = DateTime.UtcNow;
        public string updated_by { get; set; }
        public int active { get; set; } = 1;
    }

    public class TicketScanHistoryModel
    {
        public int scan_id { get; set; }
        public int booking_id { get; set; }
        public int booking_seat_id { get; set; }
        public int scanned_quantity { get; set; }
        public DateTime scan_time { get; set; } = DateTime.UtcNow;
        public string scanned_by { get; set; }
        public string scan_type { get; set; } = "entry";
        public string scan_status { get; set; } = "success";
        public string remarks { get; set; }
        public string device_info { get; set; }
    }
}
