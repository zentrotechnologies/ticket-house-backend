using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Response
{
    public class EventSummaryResponse
    {
        public int EventId { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Location { get; set; }

        // Seat Summary
        public int TotalSeats { get; set; }
        public int BookedSeats { get; set; }
        public int AvailableSeats { get; set; }
        public decimal OccupancyPercentage { get; set; }

        // Financial Summary
        public decimal TotalProfit { get; set; }
        public string Currency { get; set; } = "INR";

        // Detailed Seat Type Breakdown
        public List<SeatTypeSummary> SeatTypeDetails { get; set; } = new List<SeatTypeSummary>();

        // Payment Summary
        public PaymentSummary PaymentDetails { get; set; } = new PaymentSummary();
    }

    public class SeatTypeSummary
    {
        public int SeatTypeId { get; set; }
        public string SeatName { get; set; }
        public decimal Price { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public int BookedSeats { get; set; }
        public decimal Revenue { get; set; }
        public decimal OccupancyPercentage { get; set; }
    }

    public class PaymentSummary
    {
        public int TotalSuccessfulBookings { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalConvenienceFee { get; set; }
        public decimal TotalGST { get; set; }
        public DateTime? FirstBookingDate { get; set; }
        public DateTime? LastBookingDate { get; set; }
    }

    public class ActiveEventResponse
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
        public decimal? min_price { get; set; }
        public decimal? max_price { get; set; }
        public int? no_of_seats { get; set; }
        public List<EventSeatTypeInfo> seat_types { get; set; }
    }

    public class EventSeatTypeInfo
    {
        public int seat_type_id { get; set; }
        public string seat_name { get; set; }
        public decimal price { get; set; }
        public int total_seats { get; set; }
        public int available_seats { get; set; }
    }

    public class ActiveEventsListResponse
    {
        public int total_events { get; set; }
        public List<ActiveEventResponse> events { get; set; }
    }
}
