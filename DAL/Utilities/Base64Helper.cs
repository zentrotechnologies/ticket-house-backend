using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DAL.Utilities
{
    public static class Base64Helper
    {
        public static bool IsValidBase64(string base64String)
        {
            if (string.IsNullOrEmpty(base64String))
                return false;

            // Remove data URL prefix if present
            var base64Data = base64String;
            if (base64String.Contains(","))
            {
                base64Data = base64String.Substring(base64String.IndexOf(",") + 1);
            }

            // Check if it's a valid base64 string
            Span<byte> buffer = new Span<byte>(new byte[base64Data.Length]);
            return Convert.TryFromBase64String(base64Data, buffer, out _);
        }

        public static string GetBase64Data(string base64Url)
        {
            if (string.IsNullOrEmpty(base64Url))
                return null;

            if (base64Url.Contains(","))
            {
                return base64Url.Substring(base64Url.IndexOf(",") + 1);
            }

            return base64Url;
        }

        public static string GetMimeType(string base64Url)
        {
            if (string.IsNullOrEmpty(base64Url) || !base64Url.StartsWith("data:"))
                return null;

            var match = Regex.Match(base64Url, @"data:([^;]+);");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
