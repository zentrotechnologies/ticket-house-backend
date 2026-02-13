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
}
