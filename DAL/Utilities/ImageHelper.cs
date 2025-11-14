using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Utilities
{
    public class ImageHelper
    {
        private static readonly string BannerImagesFolder = "banner_images";

        // Method 1: Save as file in folder (Recommended)
        public static async Task<string> SaveImageAsFile(string base64Image, string fileName)
        {
            try
            {
                // Create directory if it doesn't exist
                var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                if (!Directory.Exists(wwwrootPath))
                {
                    Directory.CreateDirectory(wwwrootPath);
                }

                var directoryPath = Path.Combine(wwwrootPath, BannerImagesFolder);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Generate unique file name
                var fileExtension = GetImageExtension(base64Image);
                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}{fileExtension}";
                var filePath = Path.Combine(directoryPath, uniqueFileName);

                // Convert base64 to bytes and save
                var cleanBase64 = GetCleanBase64String(base64Image);
                var imageBytes = Convert.FromBase64String(cleanBase64);
                await File.WriteAllBytesAsync(filePath, imageBytes);

                return $"/{BannerImagesFolder}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                throw new Exception("Error saving image: " + ex.Message);
            }
        }

        // Extract clean base64 string from data URL
        private static string GetCleanBase64String(string base64Image)
        {
            if (string.IsNullOrEmpty(base64Image))
                return base64Image;

            // Remove data URL prefix if present
            if (base64Image.Contains("base64,"))
            {
                return base64Image.Split(new[] { "base64," }, StringSplitOptions.None)[1];
            }

            return base64Image;
        }

        // Method 2: Store as base64 string in database
        public static string FormatBase64Image(string base64Image)
        {
            if (string.IsNullOrEmpty(base64Image))
                return null;

            // Remove data URL prefix if present
            return GetCleanBase64String(base64Image);
        }

        public static string GetBase64FromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            var imageBytes = File.ReadAllBytes(filePath);
            return Convert.ToBase64String(imageBytes);
        }

        private static string GetImageExtension(string base64Image)
        {
            if (base64Image.StartsWith("data:image/jpeg") || base64Image.Contains("/jpeg"))
                return ".jpg";
            if (base64Image.StartsWith("data:image/png") || base64Image.Contains("/png"))
                return ".png";
            if (base64Image.StartsWith("data:image/gif") || base64Image.Contains("/gif"))
                return ".gif";
            if (base64Image.StartsWith("data:image/webp") || base64Image.Contains("/webp"))
                return ".webp";
            if (base64Image.StartsWith("data:image/svg+xml") || base64Image.Contains("/svg+xml"))
                return ".svg";

            // Default to jpg if cannot determine
            return ".jpg";
        }

        public static bool IsValidBase64Image(string base64Image)
        {
            if (string.IsNullOrEmpty(base64Image))
                return false;

            try
            {
                // Remove data URL prefix if present
                var cleanBase64 = GetCleanBase64String(base64Image);

                // Try to convert to check if it's valid base64
                Convert.FromBase64String(cleanBase64);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
