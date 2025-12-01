using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Utilities
{
    public class FileUploadHelper
    {
        public static async Task<string> UploadTestimonialImage(IFormFile file, int testimonialId, string name, string assetsRootPath)
        {
            // FIX: Use assetsRootPath instead of webRootPath
            if (string.IsNullOrEmpty(assetsRootPath))
            {
                throw new ArgumentException("Assets root path is not configured properly.");
            }

            if (file == null || file.Length == 0)
                return null;

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Invalid file type. Only image files are allowed.");
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("File size too large. Maximum size is 5MB.");
            }

            // Create folder structure - relative to Assets folder
            var testimonialFolder = Path.Combine(assetsRootPath, "Testimonial_Profile");

            // Ensure directory exists
            try
            {
                if (!Directory.Exists(testimonialFolder))
                {
                    Directory.CreateDirectory(testimonialFolder);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not create directory: {testimonialFolder}. Error: {ex.Message}");
            }

            // Clean name for filename
            var cleanName = CleanFileName(name);
            var fileName = $"{testimonialId}_{cleanName}{fileExtension}";
            var filePath = Path.Combine(testimonialFolder, fileName);

            try
            {
                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative path for database storage
                // Note: This returns path relative to Assets folder
                return $"/Assets/Testimonial_Profile/{fileName}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving file: {ex.Message}");
            }
        }

        public static void DeleteTestimonialImage(string imagePath, string assetsRootPath)
        {
            // FIX: Use assetsRootPath
            if (string.IsNullOrEmpty(assetsRootPath) || string.IsNullOrEmpty(imagePath))
                return;

            if (imagePath.StartsWith("/Assets/Testimonial_Profile/"))
            {
                try
                {
                    // Remove the leading /Assets from the path
                    var relativePath = imagePath.Replace("/Assets/", "");
                    var fullPath = Path.Combine(assetsRootPath, relativePath);

                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't throw
                    Console.WriteLine($"Error deleting image: {ex.Message}");
                }
            }
        }

        private static string CleanFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "testimonial";

            var invalidChars = Path.GetInvalidFileNameChars();
            var cleanName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries))
                .Replace(" ", "_")
                .ToLowerInvariant();

            // Limit length
            return cleanName.Length > 50 ? cleanName.Substring(0, 50) : cleanName;
        }
    }
}
