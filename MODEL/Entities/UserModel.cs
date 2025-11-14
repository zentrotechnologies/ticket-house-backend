using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Entities
{
    public class UserModel
    {
        public Guid user_id { get; set; }
        public string? first_name { get; set; }
        public string? last_name { get; set; }
        public string email { get; set; } = string.Empty;
        public string? country_code { get; set; }
        public string? mobile { get; set; }
        public string? profile_img { get; set; }
        public string? googleclient_id { get; set; }
        public int role_id { get; set; }
        public string? password { get; set; }
        public string? created_by { get; set; }
        public DateTime created_on { get; set; }
        public string? updated_by { get; set; }
        public DateTime updated_on { get; set; }
        public int active { get; set; } = 1;
        public string? role_name { get; set; }
    }
}
