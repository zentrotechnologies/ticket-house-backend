using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Response
{
    /// <summary>
    /// Comprehensive booking details response with event, user, seats and scan history
    /// </summary
    public class BookingDetailedResponse
    {
        // Booking Information
        public int booking_id { get; set; }
        public string booking_code { get; set; }
        public string status { get; set; }
        public DateTime created_on { get; set; }
        public DateTime? updated_on { get; set; }

        // Payment Information
        public decimal total_amount { get; set; }
        public decimal booking_amount { get; set; }
        public decimal convenience_fee { get; set; }
        public decimal gst_amount { get; set; }
        public decimal final_amount { get; set; }
        public string currency { get; set; }
        public string payment_status { get; set; }
        public string payment_method { get; set; }
        public DateTime? payment_date { get; set; }
        public string razorpay_order_id { get; set; }
        public string razorpay_payment_id { get; set; }

        // Event Information
        public EventInfo event_info { get; set; }

        // User Information
        public UserInfo user_info { get; set; }

        // Seat Details with Scan Information
        public List<BookingSeatDetailedInfo> booking_seats { get; set; }

        // Overall Scan Summary
        public ScanSummaryInfo scan_summary { get; set; }

        // Ticket Scan History
        public List<TicketScanHistoryInfo> scan_history { get; set; }
    }

    /// <summary>
    /// Event information subset
    /// </summary>
    public class EventInfo
    {
        public int event_id { get; set; }
        public string event_name { get; set; }
        public DateTime event_date { get; set; }
        public TimeSpan start_time { get; set; }
        public TimeSpan end_time { get; set; }
        public string location { get; set; }
        public string full_address { get; set; }
        public string banner_image { get; set; }
        public string status { get; set; }
    }

    /// <summary>
    /// User information subset
    /// </summary>
    public class UserInfo
    {
        public Guid user_id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string country_code { get; set; }
        public string mobile { get; set; }
    }

    /// <summary>
    /// Detailed booking seat information with seat names and scan counts
    /// </summary>
    public class BookingSeatDetailedInfo
    {
        public int booking_seat_id { get; set; }
        public int event_seat_type_inventory_id { get; set; }
        public string seat_name { get; set; }
        public int quantity { get; set; }
        public int scanned_quantity { get; set; }
        public int remaining_quantity { get; set; }
        public decimal price_per_seat { get; set; }
        public decimal subtotal { get; set; }
        public DateTime? last_scan_time { get; set; }
        public string scanned_by { get; set; }

        // Scan completion percentage for this seat type
        public decimal scan_percentage { get; set; }
        public bool is_fully_scanned { get; set; }
    }

    /// <summary>
    /// Overall scan summary for the booking
    /// </summary>
    public class ScanSummaryInfo
    {
        public int total_tickets { get; set; }
        public int total_scanned { get; set; }
        public int total_remaining { get; set; }
        public decimal overall_scan_percentage { get; set; }
        public bool is_fully_scanned { get; set; }
        public DateTime? first_scan_time { get; set; }
        public DateTime? last_scan_time { get; set; }
        public int total_scan_events { get; set; }
    }

    /// <summary>
    /// Individual ticket scan history entry
    /// </summary>
    public class TicketScanHistoryInfo
    {
        public int scan_id { get; set; }
        public int booking_seat_id { get; set; }
        public string seat_name { get; set; }
        public int scanned_quantity { get; set; }
        public DateTime scan_time { get; set; }
        public string scanned_by_name { get; set; }
        public string scan_type { get; set; }
        public string scan_status { get; set; }
        public string remarks { get; set; }
        public string device_info { get; set; }
    }
}
