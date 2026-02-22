using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Request
{
    public class CreateCouponRequest
    {
        public List<int> event_ids { get; set; }
        public string coupon_name { get; set; }
        public string discount_type { get; set; } // 'fixed' or 'percentage'
        public decimal discount_amount { get; set; }
        public decimal min_order_amount { get; set; } = 0;
        public decimal max_order_amount { get; set; } = 999999;
        public int min_seats { get; set; } = 0;
        public int max_seats { get; set; } = 999;
        public decimal min_ticket_price { get; set; } = 0;
        public decimal max_ticket_price { get; set; } = 999999;
        public DateTime valid_from { get; set; }
        public DateTime valid_to { get; set; }
    }

    public class UpdateCouponRequest : CreateCouponRequest
    {
        public int coupon_id { get; set; }
        public int active { get; set; }
    }

    // Request Model for checking coupon applicability
    public class CheckCouponRequest
    {
        public int event_id { get; set; }
        public List<SeatSelection> seat_selections { get; set; }
        public decimal ticket_base_price { get; set; }
        public int total_seats { get; set; }
        public decimal subtotal { get; set; }
    }

    public class CouponResult
    {
        public int? coupon_id { get; set; }
        public string coupon_code { get; set; }
        public string coupon_name { get; set; }
        public string discount_type { get; set; }
        public decimal discount_amount { get; set; }
        public decimal original_amount { get; set; }
        public decimal discount_value { get; set; }
        public decimal final_amount { get; set; }
        public bool is_applied { get; set; }
        public string message { get; set; }
    }

    public class ApplyCouponRequest
    {
        public int booking_id { get; set; }
        public string coupon_code { get; set; }
    }
}
