using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Request
{
    public class GoogleSSORequest
    {
        public string token { get; set; } = string.Empty;
        public string source { get; set; } = string.Empty;
        public string? email { get; set; }
        public string? google_id { get; set; }
        public string? first_name { get; set; }
        public string? last_name { get; set; }
    }
}
