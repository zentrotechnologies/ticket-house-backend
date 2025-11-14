using DAL.Repository;
using DAL.Utilities;
using MODEL.Entities;
using MODEL.Request;
using MODEL.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BAL.Services
{
    public interface IBannerManagementService
    {
        Task<CommonResponseModel<IEnumerable<BannerManagementModel>>> GetAllBanners();
        Task<CommonResponseModel<BannerManagementModel>> GetBannerById(int bannerId);
        Task<CommonResponseModel<int>> CreateBanner(CreateBannerRequest request);
        Task<CommonResponseModel<bool>> UpdateBanner(int bannerId, UpdateBannerRequest request);
        Task<CommonResponseModel<bool>> DeleteBanner(int bannerId, string updatedBy);
    }
    public class BannerManagementService: IBannerManagementService
    {
        private readonly IBannerManagementRepository _bannerRepository;

        public BannerManagementService(IBannerManagementRepository bannerRepository)
        {
            _bannerRepository = bannerRepository;
        }

        public async Task<CommonResponseModel<IEnumerable<BannerManagementModel>>> GetAllBanners()
        {
            try
            {
                var banners = await _bannerRepository.GetAllBanners();
                return new CommonResponseModel<IEnumerable<BannerManagementModel>>
                {
                    Status = "Success",
                    Success = true,
                    Message = "Banners retrieved successfully",
                    Data = banners
                };
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<IEnumerable<BannerManagementModel>>
                {
                    Status = "Failure",
                    Success = false,
                    Message = "Error retrieving banners",
                    ErrorCode = "GET_BANNERS_ERROR"
                };
            }
        }

        public async Task<CommonResponseModel<BannerManagementModel>> GetBannerById(int bannerId)
        {
            try
            {
                var banner = await _bannerRepository.GetBannerById(bannerId);
                if (banner == null)
                {
                    return new CommonResponseModel<BannerManagementModel>
                    {
                        Status = "Failure",
                        Success = false,
                        Message = "Banner not found",
                        ErrorCode = "BANNER_NOT_FOUND"
                    };
                }

                return new CommonResponseModel<BannerManagementModel>
                {
                    Status = "Success",
                    Success = true,
                    Message = "Banner retrieved successfully",
                    Data = banner
                };
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<BannerManagementModel>
                {
                    Status = "Failure",
                    Success = false,
                    Message = "Error retrieving banner",
                    ErrorCode = "GET_BANNER_ERROR"
                };
            }
        }

        public async Task<CommonResponseModel<int>> CreateBanner(CreateBannerRequest request)
        {
            try
            {
                // Process banner image if provided
                string processedImagePath = null;
                if (!string.IsNullOrEmpty(request.banner_img))
                {
                    processedImagePath = await ProcessBannerImage(request.banner_img, "create");
                }

                var banner = new BannerManagementModel
                {
                    banner_img = processedImagePath, // Use processed image path
                    action_link_url = request.action_link_url,
                    created_by = request.created_by,
                    updated_by = request.created_by
                };

                var bannerId = await _bannerRepository.CreateBanner(banner);
                return new CommonResponseModel<int>
                {
                    Status = "Success",
                    Success = true,
                    Message = "Banner created successfully",
                    Data = bannerId
                };
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<int>
                {
                    Status = "Failure",
                    Success = false,
                    Message = "Error creating banner: " + ex.Message,
                    ErrorCode = "CREATE_BANNER_ERROR"
                };
            }
        }

        public async Task<CommonResponseModel<bool>> UpdateBanner(int bannerId, UpdateBannerRequest request)
        {
            try
            {
                var existingBanner = await _bannerRepository.GetBannerById(bannerId);
                if (existingBanner == null)
                {
                    return new CommonResponseModel<bool>
                    {
                        Status = "Failure",
                        Success = false,
                        Message = "Banner not found",
                        ErrorCode = "BANNER_NOT_FOUND"
                    };
                }

                // Process banner image if provided
                string processedImagePath = null;
                if (!string.IsNullOrEmpty(request.banner_img))
                {
                    processedImagePath = await ProcessBannerImage(request.banner_img, "update");

                    // Delete old image file if it exists and we're uploading a new one
                    if (!string.IsNullOrEmpty(existingBanner.banner_img) &&
                        existingBanner.banner_img.StartsWith("/banner_images/"))
                    {
                        DeleteOldImageFile(existingBanner.banner_img);
                    }
                }

                var banner = new BannerManagementModel
                {
                    banner_id = bannerId,
                    banner_img = processedImagePath ?? existingBanner.banner_img, // Keep existing if no new image
                    action_link_url = request.action_link_url ?? existingBanner.action_link_url,
                    updated_by = request.updated_by
                };

                var result = await _bannerRepository.UpdateBanner(banner);
                return new CommonResponseModel<bool>
                {
                    Status = "Success",
                    Success = true,
                    Message = "Banner updated successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<bool>
                {
                    Status = "Failure",
                    Success = false,
                    Message = "Error updating banner: " + ex.Message,
                    ErrorCode = "UPDATE_BANNER_ERROR"
                };
            }
        }

        public async Task<CommonResponseModel<bool>> DeleteBanner(int bannerId, string updatedBy)
        {
            try
            {
                var existingBanner = await _bannerRepository.GetBannerById(bannerId);
                if (existingBanner == null)
                {
                    return new CommonResponseModel<bool>
                    {
                        Status = "Failure",
                        Success = false,
                        Message = "Banner not found",
                        ErrorCode = "BANNER_NOT_FOUND"
                    };
                }

                // Delete associated image file if it exists
                if (!string.IsNullOrEmpty(existingBanner.banner_img) &&
                    existingBanner.banner_img.StartsWith("/banner_images/"))
                {
                    DeleteOldImageFile(existingBanner.banner_img);
                }

                var result = await _bannerRepository.SoftDeleteBanner(bannerId, updatedBy);
                return new CommonResponseModel<bool>
                {
                    Status = "Success",
                    Success = true,
                    Message = "Banner deleted successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new CommonResponseModel<bool>
                {
                    Status = "Failure",
                    Success = false,
                    Message = "Error deleting banner: " + ex.Message,
                    ErrorCode = "DELETE_BANNER_ERROR"
                };
            }
        }

        // Method to handle image processing - NOW BEING USED
        private async Task<string> ProcessBannerImage(string bannerImg, string operationType = "create")
        {
            if (string.IsNullOrEmpty(bannerImg))
                return null;

            try
            {
                // Check if it's a base64 string or already a file path
                if (bannerImg.StartsWith("data:image") || bannerImg.StartsWith("/9j/") || ImageHelper.IsValidBase64Image(bannerImg))
                {
                    // It's a base64 image, save as file
                    var fileName = $"banner_{DateTime.Now:yyyyMMddHHmmss}";
                    return await ImageHelper.SaveImageAsFile(bannerImg, fileName);
                }
                else if (bannerImg.StartsWith("/banner_images/"))
                {
                    // It's already a file path, return as is
                    return bannerImg;
                }
                else
                {
                    // It might be a URL or other format, store as is
                    return bannerImg;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing banner image: {ex.Message}");
            }
        }

        // Method to delete old image files
        private void DeleteOldImageFile(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !imagePath.StartsWith("/banner_images/"))
                    return;

                var fileName = imagePath.Substring("/banner_images/".Length);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "banner_images", fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - we don't want image deletion failure to stop the main operation
                Console.WriteLine($"Warning: Could not delete old image file: {ex.Message}");
            }
        }
    }
}
