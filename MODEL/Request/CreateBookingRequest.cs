using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Request
{
    public class SeatSelectionRequest
    {
        public int EventId { get; set; }
        public List<SeatSelection> SeatSelections { get; set; } = new List<SeatSelection>();
    }

    public class SeatSelection
    {
        public int SeatTypeId { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateBookingRequest
    {
        public int EventId { get; set; }
        public List<SeatSelection> SeatSelections { get; set; } = new List<SeatSelection>();
    }

    public class SeatAvailabilityRequest
    {
        public List<SeatSelection> SeatSelections { get; set; } = new List<SeatSelection>();
    }

    public class SeatUpdateRequest
    {
        public int SeatTypeId { get; set; }
        public int Quantity { get; set; }
    }

    public class QRCodeDecodeRequest
    {
        public string QRCodeBase64 { get; set; }
    }

    public class ScanTicketRequest
    {
        public string BookingCode { get; set; }
        public int? SeatTypeId { get; set; } // Optional for partial scanning
        public int QuantityToScan { get; set; } = 1;
        public string ScannedBy { get; set; }
        public string ScanType { get; set; } = "entry";
        public string DeviceInfo { get; set; }
        public bool ForceScan { get; set; } = false;
    }

    public class PartialScanRequest
    {
        public int BookingId { get; set; }
        public List<SeatScanDetail> SeatScanDetails { get; set; }
        public string ScannedBy { get; set; }
        public string DeviceInfo { get; set; }
    }

    public class SeatScanDetail
    {
        public int SeatTypeId { get; set; }
        public int QuantityToScan { get; set; }
    }

    public class CreatePaymentOrderRequest
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public Dictionary<string, string>? Notes { get; set; }
    }

    public class VerifyPaymentRequest
    {
        public int BookingId { get; set; }
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpaySignature { get; set; } = string.Empty;
    }

    public class PaymentStatusRequest
    {
        public int BookingId { get; set; }
        public string PaymentId { get; set; } = string.Empty;
    }

    public class PaymentRefundRequest
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class CreateBookingWithPaymentRequest
    {
        public int EventId { get; set; }
        public List<SeatSelection> SeatSelections { get; set; } = new List<SeatSelection>();
    }
}
