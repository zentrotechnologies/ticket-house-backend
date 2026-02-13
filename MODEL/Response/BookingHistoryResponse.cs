using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Response
{
    public class BookingHistoryResponse
    {
        public int booking_id { get; set; }
        public string booking_code { get; set; }
        public string event_name { get; set; }
        public DateTime? event_date { get; set; }
        public TimeSpan? start_time { get; set; }
        public TimeSpan? end_time { get; set; }
        public string location { get; set; }
        public string full_address { get; set; }
        public decimal final_amount { get; set; }
        public decimal total_amount { get; set; }
        public decimal convenience_fee { get; set; }
        public decimal gst_amount { get; set; }
        public string currency { get; set; }
        public string payment_method { get; set; }
        public string payment_status { get; set; }
        public DateTime? payment_date { get; set; }
        public string razorpay_payment_id { get; set; }
        public string razorpay_order_id { get; set; }
        public string booking_status { get; set; }
        public DateTime created_on { get; set; }
        public List<BookingSeatHistoryResponse> seats { get; set; }
    }

    public class BookingSeatHistoryResponse
    {
        public string seat_name { get; set; }
        public int quantity { get; set; }
        public int scanned_quantity { get; set; }
        public int remaining_quantity { get; set; }
        public decimal price_per_seat { get; set; }
        public decimal subtotal { get; set; }
        public DateTime? last_scan_time { get; set; } // Store as timestamp or null
        public string scanned_by { get; set; }
    }

    public class PagedBookingHistoryResponse : CommonResponseModel<List<BookingHistoryResponse>>
    {
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }
}
