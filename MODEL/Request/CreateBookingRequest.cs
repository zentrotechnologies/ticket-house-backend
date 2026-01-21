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
}
