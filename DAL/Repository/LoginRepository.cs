using DAL.Utilities;
using Dapper;
using MODEL.Configuration;
using MODEL.Entities;
using MODEL.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repository
{
    public interface ILoginRepository
    {
        Task<UserModel> ValidateUser(LoginRequest loginRequest);
        Task<int> AddOtpVerification(OtpVerificationModel otp);
        Task<OtpVerificationModel> GetOtpVerification(int otpId);
        Task<bool> VerifyOtp(int otpId);
        Task<bool> UpdateOtpStatus(int otpId, string status);
        Task<OtpVerificationModel> GetLatestOtpByEmail(string email, string type = "email");
    }
    public class LoginRepository: ILoginRepository
    {
        private readonly ITHDBConnection _dbConnection;
        private readonly THConfiguration _configuration;

        public LoginRepository(ITHDBConnection dbConnection, THConfiguration configuration)
        {
            _dbConnection = dbConnection;
            _configuration = configuration;
        }

        public async Task<UserModel> ValidateUser(LoginRequest loginRequest)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT u.*, r.role_name 
                FROM {DatabaseConfiguration.Users} u
                LEFT JOIN {DatabaseConfiguration.RoleMaster} r ON u.role_id = r.role_id
                WHERE u.email = @Email AND u.active = 1 
                LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<UserModel>(query, new { Email = loginRequest.email });
        }

        //public async Task<int> AddOtpVerification(OtpVerificationModel otp)
        //{
        //    using var connection = _dbConnection.GetConnection();
        //    var query = $@"
        //        INSERT INTO {DatabaseConfiguration.OtpVerification} 
        //        (user_id, session_id, country_code, mobile, email, type, otp, status, created_by, updated_by)
        //        VALUES 
        //        (@user_id, @session_id, @country_code, @mobile, @email, @type, @otp, @status, @created_by, @updated_by)
        //        RETURNING otp_id";

        //    return await connection.ExecuteScalarAsync<int>(query, new
        //    {
        //        user_id = otp.user_id,
        //        session_id = otp.session_id,
        //        country_code = otp.country_code,
        //        mobile = otp.mobile,
        //        email = otp.email,
        //        type = otp.type,
        //        otp = otp.otp,
        //        status = otp.status,
        //        created_by = otp.created_by,
        //        updated_by = otp.updated_by
        //    });
        //}

        public async Task<int> AddOtpVerification(OtpVerificationModel otp)
        {
            using var connection = _dbConnection.GetConnection();

            // Use explicit casting to handle the SERIAL to int conversion properly
            var query = $@"
                        INSERT INTO {DatabaseConfiguration.OtpVerification} 
                        (user_id, session_id, country_code, mobile, email, type, otp, status, created_by, updated_by)
                        VALUES 
                        (@user_id, @session_id, @country_code, @mobile, @email, @type, @otp, @status, @created_by, @updated_by)
                        RETURNING otp_id";

            // Use QueryFirstAsync instead of ExecuteScalarAsync for better type handling
            var result = await connection.QueryFirstAsync<int>(query, new
            {
                user_id = otp.user_id,
                session_id = otp.session_id,
                country_code = otp.country_code,
                mobile = otp.mobile,
                email = otp.email,
                type = otp.type,
                otp = otp.otp,
                status = otp.status,
                created_by = otp.created_by,
                updated_by = otp.updated_by
            });

            return result;
        }

        public async Task<OtpVerificationModel> GetOtpVerification(int otpId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {DatabaseConfiguration.OtpVerification} 
                WHERE otp_id = @OtpId AND active = 1 
                LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<OtpVerificationModel>(query, new { OtpId = otpId });
        }

        public async Task<OtpVerificationModel> GetLatestOtpByEmail(string email, string type = "email")
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                SELECT * FROM {DatabaseConfiguration.OtpVerification} 
                WHERE email = @Email AND type = @Type AND active = 1 
                ORDER BY created_on DESC 
                LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<OtpVerificationModel>(query, new { Email = email, Type = type });
        }

        public async Task<bool> VerifyOtp(int otpId)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {DatabaseConfiguration.OtpVerification} 
                SET status = 'verified', updated_on = CURRENT_TIMESTAMP 
                WHERE otp_id = @OtpId";

            var affectedRows = await connection.ExecuteAsync(query, new { OtpId = otpId });
            return affectedRows > 0;
        }

        public async Task<bool> UpdateOtpStatus(int otpId, string status)
        {
            using var connection = _dbConnection.GetConnection();
            var query = $@"
                UPDATE {DatabaseConfiguration.OtpVerification} 
                SET status = @Status, updated_on = CURRENT_TIMESTAMP 
                WHERE otp_id = @OtpId";

            var affectedRows = await connection.ExecuteAsync(query, new { OtpId = otpId, Status = status });
            return affectedRows > 0;
        }
    }
}
