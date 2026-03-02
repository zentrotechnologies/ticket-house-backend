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
        public string qr_code { get; set; }
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

    /// <summary>
    /// Comprehensive response for event booking history
    /// Contains all bookings for an event with detailed information
    /// </summary>
    public class EventBookingHistoryResponse
    {
        public int event_id { get; set; }
        public string event_name { get; set; }
        public DateTime event_date { get; set; }
        public TimeSpan start_time { get; set; }
        public TimeSpan end_time { get; set; }
        public string location { get; set; }

        // Summary statistics
        public EventBookingSummary summary { get; set; }

        // List of all bookings for this event
        public List<EventBookingDetail> bookings { get; set; }
    }

    public class EventBookingSummary
    {
        public int total_bookings { get; set; }
        public int total_tickets_booked { get; set; }
        public int total_tickets_scanned { get; set; }
        public int total_tickets_remaining { get; set; }
        public decimal total_revenue { get; set; }
        public decimal total_discount_given { get; set; }
        public int total_coupons_used { get; set; }
        public int fully_scanned_bookings { get; set; }
        public int partially_scanned_bookings { get; set; }
        public int unscanned_bookings { get; set; }
        public Dictionary<string, int> bookings_by_status { get; set; }
        public Dictionary<string, int> bookings_by_payment_status { get; set; }
    }

    public class EventBookingDetail
    {
        // Booking Information
        public int booking_id { get; set; }
        public string booking_code { get; set; }
        public DateTime booking_date { get; set; }
        public string booking_status { get; set; }

        // Customer Information
        public Guid user_id { get; set; }
        public string customer_name { get; set; }
        public string customer_email { get; set; }
        public string customer_mobile { get; set; }

        // Payment Information
        public decimal total_amount { get; set; }
        public decimal booking_amount { get; set; }
        public decimal convenience_fee { get; set; }
        public decimal gst_amount { get; set; }
        public decimal final_amount { get; set; }
        public string currency { get; set; }
        public string payment_method { get; set; }
        public string payment_status { get; set; }
        public DateTime? payment_date { get; set; }
        public string transaction_id { get; set; }
        public string razorpay_order_id { get; set; }
        public string razorpay_payment_id { get; set; }

        // Coupon Information
        public int? coupon_id { get; set; }
        public string coupon_code { get; set; }
        public decimal discount_amount { get; set; }

        // Seat Summary for this booking
        public BookingSeatSummary seat_summary { get; set; }

        // Detailed Seat Information
        public List<BookingSeatDetail> seat_details { get; set; }

        // Scan Information
        public BookingScanInfo scan_info { get; set; }

        // Scan History
        public List<TicketScanHistoryInfo> scan_history { get; set; }
    }

    public class BookingSeatSummary
    {
        public int total_seats_booked { get; set; }
        public int total_seats_scanned { get; set; }
        public int total_seats_remaining { get; set; }
        public decimal total_seat_amount { get; set; }
        public bool is_fully_scanned { get; set; }
        public bool is_partially_scanned { get; set; }
        public decimal scan_percentage { get; set; }
    }

    public class BookingSeatDetail
    {
        public int booking_seat_id { get; set; }
        public int seat_type_id { get; set; }
        public string seat_name { get; set; }
        public int quantity_booked { get; set; }
        public int quantity_scanned { get; set; }
        public int quantity_remaining { get; set; }
        public decimal price_per_seat { get; set; }
        public decimal subtotal { get; set; }
        public DateTime? last_scan_time { get; set; }
        public string last_scanned_by { get; set; }
        public bool is_fully_scanned { get; set; }
    }

    public class BookingScanInfo
    {
        public bool has_any_scan { get; set; }
        public bool is_fully_scanned { get; set; }
        public bool is_partially_scanned { get; set; }
        public DateTime? first_scan_time { get; set; }
        public DateTime? last_scan_time { get; set; }
        public int total_scan_events { get; set; }
        public List<string> scanned_by_users { get; set; }
    }

    public class TicketScanHistoryByEventResponse
    {
        public EventDetails EventDetails { get; set; }
        public PaginatedScanHistory ScanHistory { get; set; }
    }

    public class EventDetails
    {
        public int event_id { get; set; }
        public string event_name { get; set; }
        public DateTime event_date { get; set; }
        public TimeSpan start_time { get; set; }
        public TimeSpan end_time { get; set; }
        public string location { get; set; }
    }

    public class PaginatedScanHistory
    {
        public int current_page { get; set; }
        public int page_size { get; set; }
        public int total_pages { get; set; }
        public int total_records { get; set; }
        public List<TicketScanHistoryDetail> data { get; set; }
    }

    public class TicketScanHistoryDetail
    {
        public int scan_id { get; set; }
        public int booking_id { get; set; }
        public string customer_name { get; set; }
        public string customer_email { get; set; }
        public string customer_mobile { get; set; }
        public int previous_seat_remaining_count { get; set; }
        public int scanned_count { get; set; }
        public int remaining_count { get; set; }
        public DateTime? last_scan_time { get; set; }
        public string last_scanned_by { get; set; }
        public bool is_fully_scanned { get; set; }
    }
}
