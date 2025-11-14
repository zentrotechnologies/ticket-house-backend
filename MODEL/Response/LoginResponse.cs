using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Response
{
    public class LoginResponse: CommonStatusResponse
    {
        public Guid user_id { get; set; }
        public string? token { get; set; }
        public string? refresh_token { get; set; }
        public DateTime? token_expiry { get; set; }
        public DateTime? refresh_token_expiry { get; set; }
        public string? user_role { get; set; }
        public string? first_name { get; set; }
        public string? last_name { get; set; }
        public string? email { get; set; }
        public string? mobile { get; set; }
        public string? country_code { get; set; }
        public string? profile_img { get; set; }
        public int role_id { get; set; }
        //public int is_2fa_enabled { get; set; }
        public bool is_otp_required { get; set; }
        public int? validationotp_id { get; set; }
        public string? tempToken { get; set; }
    }

    public class OTPResponse : CommonStatusResponse
    {
        public int validationotp_id { get; set; }
    }

    public class ResendOTPResponse : CommonStatusResponse
    {
        public int new_otp_id { get; set; }
    }
}
