using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Response
{
    public class SeatSelectionResponse
    {
        public int EventId { get; set; }
        public List<SeatDetail> SeatDetails { get; set; } = new List<SeatDetail>();
        public decimal TotalAmount { get; set; }
    }

    public class SeatDetail
    {
        public int SeatTypeId { get; set; }
        public string SeatName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public int AvailableSeats { get; set; }
    }

    public class BookingResponse
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class BookingDetailsResponse
    {
        public int booking_id { get; set; }
        public string booking_code { get; set; }
        public Guid user_id { get; set; }
        public int event_id { get; set; }
        public string event_name { get; set; }
        public DateTime event_date { get; set; }
        public TimeSpan start_time { get; set; }
        public TimeSpan end_time { get; set; }
        public string location { get; set; }
        public string banner_image { get; set; }
        public decimal total_amount { get; set; }
        public string status { get; set; }
        public DateTime created_on { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string mobile { get; set; }
        public List<BookingSeatResponse> BookingSeats { get; set; } = new List<BookingSeatResponse>();
    }

    public class BookingSeatResponse
    {
        public int booking_seat_id { get; set; }
        public int event_seat_type_inventory_id { get; set; }
        public string seat_name { get; set; }
        public int quantity { get; set; }
        public decimal price_per_seat { get; set; }
        public decimal subtotal { get; set; }
    }

    public class MyBookingsResponse
    {
        public int booking_id { get; set; }
        public string booking_code { get; set; }
        public Guid user_id { get; set; }
        public int event_id { get; set; }
        public decimal total_amount { get; set; }
        public string status { get; set; }
        public DateTime created_on { get; set; }

        // Event Details
        public string event_name { get; set; }
        public DateTime event_date { get; set; }
        public string start_time { get; set; }
        public string end_time { get; set; }
        public string location { get; set; }
        public string banner_image { get; set; }

        // Booking Seats
        public List<BookingSeatResponse> BookingSeats { get; set; } = new List<BookingSeatResponse>();
    }
}
