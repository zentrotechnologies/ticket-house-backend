using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Response
{
    public class CouponResponse
    {
        public int coupon_id { get; set; }
        public List<int> event_ids { get; set; }
        public string coupon_name { get; set; }
        public string coupon_code { get; set; }
        public string discount_type { get; set; }
        public decimal discount_amount { get; set; }
        public decimal min_order_amount { get; set; }
        public decimal max_order_amount { get; set; }
        public int min_seats { get; set; }
        public int max_seats { get; set; }
        public decimal min_ticket_price { get; set; }
        public decimal max_ticket_price { get; set; }
        public DateTime valid_from { get; set; }
        public DateTime valid_to { get; set; }
        public bool is_valid_now { get; set; }
    }
}
