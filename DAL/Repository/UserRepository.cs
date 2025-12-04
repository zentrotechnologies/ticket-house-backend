using DAL.Utilities;
using Dapper;
using MODEL.Configuration;
using MODEL.Entities;
using MODEL.Request;
using MODEL.Response;
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

        Task<OrganizerPagedResponse> GetPaginatedOrganizers(PaginationRequest request);
        Task<OrganizerResponse> GetOrganizerById(Guid organizerId);
        Task<bool> UpdateEventOrganizer(EventOrganizerModel organizer);
        Task<bool> DeleteEventOrganizer(Guid organizerId, string updatedBy);
        Task<bool> UpdateOrganizerStatus(Guid organizerId, int status, string updatedBy);
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

        public async Task<OrganizerPagedResponse> GetPaginatedOrganizers(PaginationRequest request)
        {
            using var connection = _dbConnection.GetConnection();

            // Base query
            var baseQuery = $@"
                SELECT 
                    u.user_id,
                    u.first_name,
                    u.last_name,
                    u.email,
                    u.mobile,
                    u.role_id,
                    u.created_on,
                    u.active as user_active,
                    eo.organizer_id,
                    eo.org_name,
                    eo.org_start_date,
                    eo.bank_account_no,
                    eo.bank_ifsc,
                    eo.bank_name,
                    eo.beneficiary_name,
                    eo.owner_personal_email,
                    eo.owner_mobile,
                    eo.state,
                    eo.city,
                    eo.country,
                    eo.aadhar_number,
                    eo.pancard_number,
                    eo.gst_number,
                    eo.instagram_link,
                    eo.youtube_link,
                    eo.facebook_link,
                    eo.twitter_link,
                    eo.verification_status,
                    eo.active as organizer_active,
                    rm.role_name
                FROM {DatabaseConfiguration.Users} u
                INNER JOIN {DatabaseConfiguration.EventOrganizer} eo ON u.user_id = eo.user_id
                LEFT JOIN {DatabaseConfiguration.RoleMaster} rm ON u.role_id = rm.role_id
                WHERE u.role_id = 2 AND u.active = 1 AND eo.active = 1";

            // Apply filter if provided
            if (request.FilterFlag && !string.IsNullOrEmpty(request.FilterText))
            {
                baseQuery += $@"
                    AND (
                        LOWER(u.first_name) LIKE LOWER(@FilterText) OR
                        LOWER(u.last_name) LIKE LOWER(@FilterText) OR
                        LOWER(u.email) LIKE LOWER(@FilterText) OR
                        LOWER(eo.org_name) LIKE LOWER(@FilterText) OR
                        LOWER(eo.verification_status) LIKE LOWER(@FilterText)
                    )";
            }

            // Count query
            var countQuery = $"SELECT COUNT(*) FROM ({baseQuery}) as count_query";
            var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, new
            {
                FilterText = $"%{request.FilterText}%"
            });

            // Add pagination and ordering
            var paginatedQuery = baseQuery + $@"
                ORDER BY u.created_on DESC
                LIMIT @PageSize OFFSET @Offset";

            var offset = (request.PageNumber - 1) * request.PageSize;

            var organizers = await connection.QueryAsync<OrganizerResponse>(paginatedQuery, new
            {
                FilterText = $"%{request.FilterText}%",
                PageSize = request.PageSize,
                Offset = offset
            });

            return new OrganizerPagedResponse
            {
                Status = "Success",
                Success = true,
                Message = "Organizers fetched successfully",
                Data = organizers.ToList(),
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<OrganizerResponse> GetOrganizerById(Guid organizerId)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                SELECT 
                    u.user_id,
                    u.first_name,
                    u.last_name,
                    u.email,
                    u.mobile,
                    u.role_id,
                    u.created_on,
                    u.active as user_active,
                    eo.organizer_id,
                    eo.org_name,
                    eo.org_start_date,
                    eo.bank_account_no,
                    eo.bank_ifsc,
                    eo.bank_name,
                    eo.beneficiary_name,
                    eo.owner_personal_email,
                    eo.owner_mobile,
                    eo.state,
                    eo.city,
                    eo.country,
                    eo.aadhar_number,
                    eo.pancard_number,
                    eo.gst_number,
                    eo.instagram_link,
                    eo.youtube_link,
                    eo.facebook_link,
                    eo.twitter_link,
                    eo.verification_status,
                    eo.active as organizer_active,
                    rm.role_name
                FROM {DatabaseConfiguration.Users} u
                INNER JOIN {DatabaseConfiguration.EventOrganizer} eo ON u.user_id = eo.user_id
                LEFT JOIN {DatabaseConfiguration.RoleMaster} rm ON u.role_id = rm.role_id
                WHERE eo.organizer_id = @OrganizerId AND eo.active = 1
                LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<OrganizerResponse>(query, new { OrganizerId = organizerId });
        }

        public async Task<bool> UpdateEventOrganizer(EventOrganizerModel organizer)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                UPDATE {DatabaseConfiguration.EventOrganizer} 
                SET 
                    org_name = @org_name,
                    org_start_date = @org_start_date,
                    bank_account_no = @bank_account_no,
                    bank_ifsc = @bank_ifsc,
                    bank_name = @bank_name,
                    beneficiary_name = @beneficiary_name,
                    aadhar_number = @aadhar_number,
                    pancard_number = @pancard_number,
                    owner_personal_email = @owner_personal_email,
                    owner_mobile = @owner_mobile,
                    state = @state,
                    city = @city,
                    country = @country,
                    gst_number = @gst_number,
                    instagram_link = @instagram_link,
                    youtube_link = @youtube_link,
                    facebook_link = @facebook_link,
                    twitter_link = @twitter_link,
                    verification_status = @verification_status,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE organizer_id = @organizer_id";

            var affectedRows = await connection.ExecuteAsync(query, organizer);
            return affectedRows > 0;
        }

        //public async Task<bool> DeleteEventOrganizer(Guid organizerId, string updatedBy)
        //{
        //    using var connection = _dbConnection.GetConnection();

        //    // We'll update the active status instead of hard delete
        //    var query = $@"
        //        UPDATE {DatabaseConfiguration.EventOrganizer} 
        //        SET 
        //            active = 0,
        //            updated_by = @updated_by,
        //            updated_on = CURRENT_TIMESTAMP
        //        WHERE organizer_id = @organizer_id";

        //    var affectedRows = await connection.ExecuteAsync(query, new
        //    {
        //        organizer_id = organizerId,
        //        updated_by = updatedBy
        //    });
        //    return affectedRows > 0;
        //}

        public async Task<bool> DeleteEventOrganizer(Guid organizerId, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
        -- Update organizer table
        UPDATE {DatabaseConfiguration.EventOrganizer} 
        SET active = 0, updated_by = @updated_by, updated_on = CURRENT_TIMESTAMP
        WHERE organizer_id = @organizer_id;
        
        -- Update user table using the organizer's user_id
        UPDATE {DatabaseConfiguration.Users} 
        SET active = 0, updated_by = @updated_by, updated_on = CURRENT_TIMESTAMP
        WHERE user_id = (
            SELECT user_id 
            FROM {DatabaseConfiguration.EventOrganizer} 
            WHERE organizer_id = @organizer_id
        );";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                organizer_id = organizerId,
                updated_by = updatedBy
            });

            return affectedRows > 0;
        }

        public async Task<bool> UpdateOrganizerStatus(Guid organizerId, int status, string updatedBy)
        {
            using var connection = _dbConnection.GetConnection();

            var query = $@"
                UPDATE {DatabaseConfiguration.EventOrganizer} 
                SET 
                    verification_status = @status,
                    updated_by = @updated_by,
                    updated_on = CURRENT_TIMESTAMP
                WHERE organizer_id = @organizer_id";

            var affectedRows = await connection.ExecuteAsync(query, new
            {
                organizer_id = organizerId,
                status = status == 1 ? "approved" : "rejected",
                updated_by = updatedBy
            });
            return affectedRows > 0;
        }
    }
}
