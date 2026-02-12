using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Entities
{
    public class BookingModel
    {
        public int booking_id { get; set; }
        public string booking_code { get; set; }
        public Guid user_id { get; set; }
        public int event_id { get; set; }
        public decimal total_amount { get; set; }
        public string status { get; set; } // pending/confirmed/cancelled/failed
        public DateTime created_on { get; set; } = DateTime.UtcNow;
        public string created_by { get; set; }
        public DateTime? updated_on { get; set; } = DateTime.UtcNow;
        public string updated_by { get; set; }
        public int active { get; set; } = 1;

        // Razorpay fields
        public string razorpay_order_id { get; set; }
        public string razorpay_payment_id { get; set; }
        public string razorpay_signature { get; set; }
        public string transaction_id { get; set; }
        public string payment_method { get; set; }
        public string payment_status { get; set; } = "pending";
        public DateTime? payment_date { get; set; }
        public string currency { get; set; } = "INR";
        public string razorpay_notes { get; set; }

        // Add these new properties
        public decimal booking_amount { get; set; } // Base ticket amount
        public decimal convenience_fee { get; set; } // 6.5%
        public decimal gst_amount { get; set; } // 18% on convenience fee
        public decimal final_amount { get; set; } // Total for Razorpay
        //public string currency { get; set; } = "INR";
    }

    public class BookingSeatModel
    {
        public int booking_seat_id { get; set; }
        public int booking_id { get; set; }
        public int event_seat_type_inventory_id { get; set; }
        public int quantity { get; set; }
        public decimal price_per_seat { get; set; }
        public decimal subtotal { get; set; }

        // Scanning related fields
        public int scanned_quantity { get; set; } = 0;
        public int remaining_quantity { get; set; }
        public DateTime? last_scan_time { get; set; }
        public string scanned_by { get; set; }

        public DateTime created_on { get; set; } = DateTime.UtcNow;
        public string created_by { get; set; }
        public DateTime? updated_on { get; set; } = DateTime.UtcNow;
        public string updated_by { get; set; }
        public int active { get; set; } = 1;
    }

    public class TicketScanHistoryModel
    {
        public int scan_id { get; set; }
        public int booking_id { get; set; }
        public int booking_seat_id { get; set; }
        public int scanned_quantity { get; set; }
        public DateTime scan_time { get; set; } = DateTime.UtcNow;
        public string scanned_by { get; set; }
        public string scan_type { get; set; } = "entry";
        public string scan_status { get; set; } = "success";
        public string remarks { get; set; }
        public string device_info { get; set; }
    }

    // Add PaymentHistory entity
    public class PaymentHistoryModel
    {
        public int payment_id { get; set; }
        public int booking_id { get; set; }
        public string razorpay_order_id { get; set; }
        public string razorpay_payment_id { get; set; }
        public string razorpay_signature { get; set; }
        public string transaction_id { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; } = "INR";
        public string payment_method { get; set; }
        public string payment_status { get; set; } = "pending";
        public DateTime? payment_date { get; set; }
        public string card_last4 { get; set; }
        public string card_network { get; set; }
        public string bank_name { get; set; }
        public string wallet { get; set; }
        public string vpa { get; set; }
        public string razorpay_notes { get; set; }
        public DateTime created_on { get; set; } = DateTime.UtcNow;
        public string created_by { get; set; }
        public DateTime? updated_on { get; set; }
        public string updated_by { get; set; }
        public int active { get; set; } = 1;
    }
}
