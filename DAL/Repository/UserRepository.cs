using DAL.Utilities;
using Dapper;
using MODEL.Configuration;
using MODEL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public interface IUserRepository
    {
        Task<UserModel> GetUserByEmail(string email);
        Task<UserModel> GetUserById(Guid userId);
        Task<Guid> AddUser(UserModel user);
        Task<bool> UpdateUser(UserModel user);
        Task<bool> CheckEmailExists(string email);
        Task<bool> CheckMobileExists(string mobile);
        Task<EventOrganizerModel> GetOrganizerByUserId(Guid userId);
        Task<Guid> AddEventOrganizer(EventOrganizerModel organizer);
    }
    public class UserRepository: IUserRepository
    {
        private readonly ITHDBConnection _dbConnection;
        private readonly THConfiguration _configuration;

        public UserRepository(ITHDBConnection dbConnection, THConfiguration configuration)
        {
            _dbConnection = dbConnection;
            _configuration = configuration;
        }

        public async Task<UserModel> GetUserByEmail(string email)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {DatabaseConfiguration.Users} 
                WHERE email = @Email AND active = 1 
                LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<UserModel>(query, new { Email = email });
        }

        public async Task<UserModel> GetUserById(Guid userId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {DatabaseConfiguration.Users} 
                WHERE user_id = @UserId AND active = 1 
                LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<UserModel>(query, new { UserId = userId });
        }

        public async Task<Guid> AddUser(UserModel user)
        {
            using var connection = _dbConnection.GetConnection();

            // Generate UUID in code to avoid casting issues
            user.user_id = Guid.NewGuid();

            var query = $@"
                INSERT INTO {DatabaseConfiguration.Users} 
                (user_id, first_name, last_name, email, country_code, mobile, profile_img, 
                 googleclient_id, role_id, password, created_by, updated_by)
                VALUES 
                (@user_id, @first_name, @last_name, @email, @country_code, @mobile, @profile_img,
                 @googleclient_id, @role_id, @password, @created_by, @updated_by)";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                user_id = user.user_id,
                first_name = user.first_name,
                last_name = user.last_name,
                email = user.email,
                country_code = user.country_code,
                mobile = user.mobile,
                profile_img = user.profile_img,
                googleclient_id = user.googleclient_id,
                role_id = user.role_id,
                password = user.password,
                created_by = user.created_by,
                updated_by = user.updated_by
            });

            return affectedRows > 0 ? user.user_id : Guid.Empty;
        }

        public async Task<bool> UpdateUser(UserModel user)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {DatabaseConfiguration.Users} 
                SET first_name = @first_name, last_name = @last_name, country_code = @country_code,
                    mobile = @mobile, profile_img = @profile_img, googleclient_id = @googleclient_id,
                    role_id = @role_id, password = @password, updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE user_id = @user_id";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                user_id = user.user_id,
                first_name = user.first_name,
                last_name = user.last_name,
                country_code = user.country_code,
                mobile = user.mobile,
                profile_img = user.profile_img,
                googleclient_id = user.googleclient_id,
                role_id = user.role_id,
                password = user.password,
                updated_by = user.updated_by
            });
            return affectedRows > 0;
        }

        public async Task<bool> CheckEmailExists(string email)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT COUNT(1) FROM {DatabaseConfiguration.Users} 
                WHERE email = @Email AND active = 1";

            var count = await connection.ExecuteScalarAsync<int>(query, new { Email = email });
            return count > 0;
        }

        public async Task<bool> CheckMobileExists(string mobile)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT COUNT(1) FROM {DatabaseConfiguration.Users} 
                WHERE mobile = @Mobile AND active = 1";

            var count = await connection.ExecuteScalarAsync<int>(query, new { Mobile = mobile });
            return count > 0;
        }

        public async Task<EventOrganizerModel> GetOrganizerByUserId(Guid userId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {DatabaseConfiguration.EventOrganizer} 
                WHERE user_id = @UserId AND active = 1 
                LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<EventOrganizerModel>(query, new { UserId = userId });
        }

        public async Task<Guid> AddEventOrganizer(EventOrganizerModel organizer)
        {
            using var connection = _dbConnection.GetConnection();

            // Generate UUID in code to avoid casting issues
            organizer.organizer_id = Guid.NewGuid();

            var query = $@"
                INSERT INTO {DatabaseConfiguration.EventOrganizer} 
                (organizer_id, user_id, org_name, org_start_date, bank_account_no, bank_ifsc, bank_name,
                 beneficiary_name, aadhar_number, pancard_number, owner_personal_email,
                 owner_mobile, state, city, country, gst_number, instagram_link,
                 youtube_link, facebook_link, twitter_link, created_by, updated_by)
                VALUES 
                (@organizer_id, @user_id, @org_name, @org_start_date, @bank_account_no, @bank_ifsc, @bank_name,
                 @beneficiary_name, @aadhar_number, @pancard_number, @owner_personal_email,
                 @owner_mobile, @state, @city, @country, @gst_number, @instagram_link,
                 @youtube_link, @facebook_link, @twitter_link, @created_by, @updated_by)";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                organizer_id = organizer.organizer_id,
                user_id = organizer.user_id,
                org_name = organizer.org_name,
                org_start_date = organizer.org_start_date,
                bank_account_no = organizer.bank_account_no,
                bank_ifsc = organizer.bank_ifsc,
                bank_name = organizer.bank_name,
                beneficiary_name = organizer.beneficiary_name,
                aadhar_number = organizer.aadhar_number,
                pancard_number = organizer.pancard_number,
                owner_personal_email = organizer.owner_personal_email,
                owner_mobile = organizer.owner_mobile,
                state = organizer.state,
                city = organizer.city,
                country = organizer.country,
                gst_number = organizer.gst_number,
                instagram_link = organizer.instagram_link,
                youtube_link = organizer.youtube_link,
                facebook_link = organizer.facebook_link,
                twitter_link = organizer.twitter_link,
                created_by = organizer.created_by,
                updated_by = organizer.updated_by
            });

            return affectedRows > 0 ? organizer.organizer_id : Guid.Empty;
        }
    }
}
