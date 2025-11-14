using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Entities
{
    public class OtpVerificationModel
    {
        public int otp_id { get; set; }
        public DateTime otp_sent_timestamp { get; set; } = DateTime.UtcNow;
        public Guid? user_id { get; set; }
        public string? session_id { get; set; }
        public string? country_code { get; set; } = "+91";
        public string? mobile { get; set; }
        public string? email { get; set; }
        public string? type { get; set; } // "email" or "mobile"
        public string? otp { get; set; }
        public string status { get; set; } = "pending"; // "pending", "verified", "expired"
        public string? created_by { get; set; }
        public DateTime created_on { get; set; } = DateTime.UtcNow;
        public string? updated_by { get; set; }
        public DateTime updated_on { get; set; } = DateTime.UtcNow;
        public int active { get; set; } = 1;
    }
}
