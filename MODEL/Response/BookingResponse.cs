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
        public decimal ConvenienceFee { get; set; }
        public decimal GstAmount { get; set; }
        public decimal FinalAmount { get; set; } // For Razorpay
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
        public decimal booking_amount { get; set; }  // Add this
        public decimal convenience_fee { get; set; }  // Add this
        public decimal gst_amount { get; set; }       // Add this
        public decimal final_amount { get; set; }
        public string status { get; set; }
        public DateTime created_on { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string mobile { get; set; }
        public string? qr_code { get; set; }
        public List<BookingSeatResponse> BookingSeats { get; set; } = new List<BookingSeatResponse>();
        // Add to your existing BookingModel class
        public int? coupon_id { get; set; }
        public decimal discount_amount { get; set; } = 0;
        public string coupon_code_applied { get; set; }
    }

    //public class BookingSeatResponse
    //{
    //    public int booking_seat_id { get; set; }
    //    public int event_seat_type_inventory_id { get; set; }
    //    public string seat_name { get; set; }
    //    public int quantity { get; set; }
    //    public decimal price_per_seat { get; set; }
    //    public decimal subtotal { get; set; }
    //}

    public class BookingSeatResponse
    {
        public int booking_seat_id { get; set; }
        public int booking_id { get; set; }
        public int event_seat_type_inventory_id { get; set; }
        public string seat_name { get; set; }
        public int quantity { get; set; }
        public decimal price_per_seat { get; set; }
        public decimal subtotal { get; set; }

        // Scanning properties
        public int scanned_quantity { get; set; }
        public int remaining_quantity { get; set; }
        public DateTime? last_scan_time { get; set; }
        public string scanned_by { get; set; }

        public DateTime created_on { get; set; }
        public string created_by { get; set; }
        public DateTime? updated_on { get; set; }
        public string updated_by { get; set; }
        public int active { get; set; }
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

    //bookings with QR

    public class BookingQRResponse : BookingResponse
    {
        public string QRCodeBase64 { get; set; }
        public string ThankYouMessage { get; set; }
        public BookingDetailsResponse BookingDetails { get; set; }
    }

    public class ConfirmBookingWithQRRequest
    {
        public int BookingId { get; set; }
        public string UserEmail { get; set; }
    }

    public class QRCodeDataResponse
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string EventTime { get; set; }
        public string Location { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime BookingDate { get; set; }
        public List<QRSeatDetail> Seats { get; set; } = new List<QRSeatDetail>();
        public string Message { get; set; }
    }

    public class QRSeatDetail
    {
        public string SeatType { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class TicketScanResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string Status { get; set; } // success/partial/error
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string EventName { get; set; }
        public string CustomerName { get; set; }
        public DateTime ScanTime { get; set; }
        public List<SeatScanResult> ScanResults { get; set; } = new List<SeatScanResult>();
        public ScanSummary Summary { get; set; }
    }

    public class SeatScanResult
    {
        public int SeatTypeId { get; set; }
        public string SeatName { get; set; }
        public int RequestedQuantity { get; set; }
        public int ScannedQuantity { get; set; }
        public int RemainingQuantity { get; set; }
        public bool IsFullyScanned { get; set; }
        public string Status { get; set; }
    }

    public class ScanSummary
    {
        public int TotalTickets { get; set; }
        public int ScannedTickets { get; set; }
        public int RemainingTickets { get; set; }
        public bool IsFullyScanned { get; set; }
        public decimal PercentageScanned { get; set; }
    }

    public class BookingScanSummaryResponse
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string CustomerName { get; set; }
        public List<SeatScanInfo> SeatScanInfo { get; set; } = new List<SeatScanInfo>();
        public ScanSummary Summary { get; set; }
        public DateTime? FirstScanTime { get; set; }
        public DateTime? LastScanTime { get; set; }
    }

    public class SeatScanInfo
    {
        public int SeatTypeId { get; set; }
        public string SeatName { get; set; }
        public int TotalQuantity { get; set; }
        public int ScannedQuantity { get; set; }
        public int RemainingQuantity { get; set; }
        public bool IsFullyScanned { get; set; }
        public DateTime? LastScanTime { get; set; }
        public string LastScannedBy { get; set; }
    }

    //public class PaymentOrderResponse
    //{
    //    public string OrderId { get; set; } = string.Empty;
    //    public string KeyId { get; set; } = string.Empty;
    //    public decimal Amount { get; set; }
    //    public string Currency { get; set; } = "INR";
    //    public string CompanyName { get; set; } = string.Empty;
    //    public string CustomerName { get; set; } = string.Empty;
    //    public string CustomerEmail { get; set; } = string.Empty;
    //    public Dictionary<string, string> Notes { get; set; } = new Dictionary<string, string>();
    //}

    public class PaymentOrderResponse
    {
        public string OrderId { get; set; } = string.Empty;
        public string KeyId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string CompanyName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public Dictionary<string, string> Notes { get; set; } = new Dictionary<string, string>();

        // Add booking info
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal ConvenienceFee { get; set; }
        public decimal GstAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string EventName { get; set; }
    }

    public class PaymentVerificationResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string CardLast4 { get; set; } = string.Empty;
        public string CardNetwork { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string Wallet { get; set; } = string.Empty;
        public string VPA { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public Dictionary<string, string>? PaymentDetails { get; set; }
    }

    public class PaymentStatusResponse
    {
        public string PaymentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Method { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, string>? Notes { get; set; }
    }

    public class RefundResponse
    {
        public string RefundId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
