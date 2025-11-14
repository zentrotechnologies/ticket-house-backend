using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Request
{
    public class LoginRequest
    {
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
    }

    public class GenerateOTPRequest
    {
        public string contact_type { get; set; } = string.Empty; // "email" or "mobile"
        public string? email { get; set; }
        public string? mobile { get; set; }
        public string? country_code { get; set; }
        public bool newUser { get; set; }
        //public bool is2FA { get; set; }
    }

    public class VerifyOTPRequest
    {
        public int otp_id { get; set; }
        public string otp { get; set; } = string.Empty;
        public string? email { get; set; }
        public string? mobile { get; set; }
        public string contact_type { get; set; } = string.Empty;
    }

    public class ResendOTPRequest
    {
        public int otp_id { get; set; }
    }
}
