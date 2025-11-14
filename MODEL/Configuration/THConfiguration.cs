using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODEL.Configuration
{
    public class THConfiguration
    {
        public string? ConnectionString { get; set; }
        public string? EncryptionKey { get; set; }
        public string GoogleClientID { get; set; }
        public string GoogleClientIDAPK { get; set; }
        public string? JwtKey { get; set; }
        public string? JwtIssuer { get; set; }
        public string? JwtAudience { get; set; }
        public int JwtExpireMinutes { get; set; } = 30;
        public int RefreshTokenExpireDays { get; set; } = 7;

        // Email Configuration
        public string? SmtpServer { get; set; }
        public int SmtpPort { get; set; } = 587;
        public string? SmtpUsername { get; set; }
        public string? SmtpPassword { get; set; }
        public bool EnableSsl { get; set; } = true;
        public string? FromEmail { get; set; }
        public string? FromName { get; set; }
    }
    public class JwtConfiguration
    {
        public string? Key { get; set; }
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
        public int ExpireMinutes { get; set; } = 30;
    }
}
