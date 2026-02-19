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
                // Validate base64 image if needed
                if (!string.IsNullOrEmpty(request.banner_img) && !IsValidBase64Url(request.banner_img))
                {
                    return new CommonResponseModel<int>
                    {
                        Status = "Failure",
                        Success = false,
                        Message = "Invalid base64 image format",
                        ErrorCode = "INVALID_BASE64_IMAGE"
                    };
                }

                var banner = new BannerManagementModel
                {
                    banner_img = request.banner_img, // Store base64 directly
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

                // Validate base64 image if provided
                if (!string.IsNullOrEmpty(request.banner_img) && !IsValidBase64Url(request.banner_img))
                {
                    return new CommonResponseModel<bool>
                    {
                        Status = "Failure",
                        Success = false,
                        Message = "Invalid base64 image format",
                        ErrorCode = "INVALID_BASE64_IMAGE"
                    };
                }

                var banner = new BannerManagementModel
                {
                    banner_id = bannerId,
                    banner_img = request.banner_img ?? existingBanner.banner_img, // Keep existing if no new image
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

        // Helper method to validate base64 image URL
        private bool IsValidBase64Url(string base64String)
        {
            if (string.IsNullOrEmpty(base64String))
                return false;

            // Check if it's a valid data URL format
            if (base64String.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                // Check if it contains base64 data
                var base64Data = base64String.Substring(base64String.IndexOf(",") + 1);
                try
                {
                    Convert.FromBase64String(base64Data);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            // Check if it's plain base64
            else
            {
                try
                {
                    Convert.FromBase64String(base64String);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
