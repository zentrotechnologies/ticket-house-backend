using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Utilities
{
    public interface IFileUploadHelper
    {
        Task<string> UploadEventBannerImage(IFormFile file, int eventId, string webRootPath);
        Task<string> UploadArtistPhoto(IFormFile file, int eventId, string artistName, string webRootPath);
        Task<string> UploadGalleryImage(IFormFile file, int eventId, string webRootPath);
        void DeleteEventFile(string filePath, string webRootPath);
    }
    public class EventFileUploadHelper: IFileUploadHelper
    {
        public async Task<string> UploadEventBannerImage(IFormFile file, int eventId, string webRootPath)
        {
            if (file == null || file.Length == 0)
                return null;

            // Validate file size (max 5MB for banner)
            if (file.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("File size exceeds 5MB limit");
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Invalid file type. Allowed types: jpg, jpeg, png, gif, webp");
            }

            var bannersPath = Path.Combine(webRootPath, "Assets", "event_media", "banners");

            if (!Directory.Exists(bannersPath))
            {
                Directory.CreateDirectory(bannersPath);
            }

            // Generate filename with event_id prefix
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = $"{eventId}_banner_{timestamp}{fileExtension}";
            var filePath = Path.Combine(bannersPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/Assets/event_media/banners/{fileName}";
        }

        public async Task<string> UploadArtistPhoto(IFormFile file, int eventId, string artistName, string webRootPath)
        {
            if (file == null || file.Length == 0)
                return null;

            // Validate file size (max 2MB)
            if (file.Length > 2 * 1024 * 1024)
            {
                throw new ArgumentException("File size exceeds 2MB limit");
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Invalid file type. Allowed types: jpg, jpeg, png, gif, webp");
            }

            var artistsPath = Path.Combine(webRootPath, "Assets", "event_media", "artists");

            if (!Directory.Exists(artistsPath))
            {
                Directory.CreateDirectory(artistsPath);
            }

            // Clean artist name for filename
            var cleanArtistName = CleanFileName(artistName);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = $"{eventId}_{cleanArtistName}_{timestamp}{fileExtension}";
            var filePath = Path.Combine(artistsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/Assets/event_media/artists/{fileName}";
        }

        public async Task<string> UploadGalleryImage(IFormFile file, int eventId, string webRootPath)
        {
            if (file == null || file.Length == 0)
                return null;

            // Validate file size (max 2MB)
            if (file.Length > 2 * 1024 * 1024)
            {
                throw new ArgumentException("File size exceeds 2MB limit");
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException("Invalid file type. Allowed types: jpg, jpeg, png, gif, webp");
            }

            var galleryPath = Path.Combine(webRootPath, "Assets", "event_media", "gallery");

            if (!Directory.Exists(galleryPath))
            {
                Directory.CreateDirectory(galleryPath);
            }

            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = $"{eventId}_{timestamp}{fileExtension}";
            var filePath = Path.Combine(galleryPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/Assets/event_media/gallery/{fileName}";
        }

        public void DeleteEventFile(string filePath, string webRootPath)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(webRootPath))
                return;

            try
            {
                // Remove leading slash if present
                var relativePath = filePath.TrimStart('/');
                var fullPath = Path.Combine(webRootPath, relativePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
            }
        }

        private string CleanFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "artist";

            var invalidChars = Path.GetInvalidFileNameChars();
            var cleanName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries))
                .Replace(" ", "_")
                .ToLowerInvariant();

            // Limit length
            return cleanName.Length > 50 ? cleanName.Substring(0, 50) : cleanName;
        }
    }
}
