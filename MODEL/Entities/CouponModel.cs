using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Entities
{
    public class CouponModel
    {
        public int coupon_id { get; set; }
        public string event_ids { get; set; } // Comma separated event IDs
        public string coupon_name { get; set; }
        public string coupon_code { get; set; }
        public string discount_type { get; set; } // 'fixed' or 'percentage'
        public decimal discount_amount { get; set; }
        public decimal min_order_amount { get; set; }
        public decimal max_order_amount { get; set; }
        public int min_seats { get; set; }
        public int max_seats { get; set; }
        public decimal min_ticket_price { get; set; }
        public decimal max_ticket_price { get; set; }
        public DateTime valid_from { get; set; }
        public DateTime valid_to { get; set; }
        public string created_by { get; set; }
        public DateTime created_on { get; set; }
        public string updated_by { get; set; }
        public DateTime updated_on { get; set; }
        public int active { get; set; }

        // Helper property to parse event_ids
        public List<int> GetEventIdsList()
        {
            if (string.IsNullOrEmpty(event_ids))
                return new List<int>();

            return event_ids.Split(',')
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => int.Parse(id.Trim()))
                .ToList();
        }
    }
}
