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
}
