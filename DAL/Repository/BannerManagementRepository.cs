using DAL.Utilities;
using Dapper;
using MODEL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public interface IBannerManagementRepository
    {
        Task<IEnumerable<BannerManagementModel>> GetAllBanners();
        Task<BannerManagementModel> GetBannerById(int bannerId);
        Task<int> CreateBanner(BannerManagementModel banner);
        Task<bool> UpdateBanner(BannerManagementModel banner);
        Task<bool> DeleteBanner(int bannerId, string updatedBy);
        Task<bool> SoftDeleteBanner(int bannerId, string updatedBy);
    }
    public class BannerManagementRepository: IBannerManagementRepository
    {
        private readonly ITHDBConnection _dbConnection;

        public BannerManagementRepository(ITHDBConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<BannerManagementModel>> GetAllBanners()
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {DatabaseConfiguration.BannerManagement} 
                WHERE active = 1 
                ORDER BY created_on DESC";

            return await connection.QueryAsync<BannerManagementModel>(query);
        }

        public async Task<BannerManagementModel> GetBannerById(int bannerId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {DatabaseConfiguration.BannerManagement} 
                WHERE banner_id = @BannerId AND active = 1 
                LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<BannerManagementModel>(query, new { BannerId = bannerId });
        }

        public async Task<int> CreateBanner(BannerManagementModel banner)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                INSERT INTO {DatabaseConfiguration.BannerManagement} 
                (banner_img, action_link_url, created_by, updated_by)
                VALUES 
                (@banner_img, @action_link_url, @created_by, @updated_by)
                RETURNING banner_id";

            return await connection.ExecuteScalarAsync<int>(query, new
            {
                banner_img = banner.banner_img,
                action_link_url = banner.action_link_url,
                created_by = banner.created_by,
                updated_by = banner.updated_by
            });
        }

        public async Task<bool> UpdateBanner(BannerManagementModel banner)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {DatabaseConfiguration.BannerManagement} 
                SET banner_img = COALESCE(@banner_img, banner_img),
                    action_link_url = COALESCE(@action_link_url, action_link_url),
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE banner_id = @banner_id AND active = 1";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                banner_id = banner.banner_id,
                banner_img = banner.banner_img,
                action_link_url = banner.action_link_url,
                updated_by = banner.updated_by
            });
            return affectedRows > 0;
        }

        public async Task<bool> DeleteBanner(int bannerId, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {DatabaseConfiguration.BannerManagement} 
                SET active = 0, updated_by = @updated_by, updated_on = CURRENT_TIMESTAMP
                WHERE banner_id = @banner_id";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                banner_id = bannerId,
                updated_by = updatedBy
            });
            return affectedRows > 0;
        }

        public async Task<bool> SoftDeleteBanner(int bannerId, string updatedBy)
        {
            return await DeleteBanner(bannerId, updatedBy);
        }
    }
}
