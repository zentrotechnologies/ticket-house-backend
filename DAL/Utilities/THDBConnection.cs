using MODEL.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Utilities
{
    public interface ITHDBConnection
    {
        public IDbConnection GetConnection();
    }
    public class THDBConnection: ITHDBConnection
    {
        private readonly THConfiguration _configuration;
        public THDBConnection(THConfiguration Configuration)
        {
            _configuration = Configuration;
        }
        public IDbConnection GetConnection()
        {
            IDbConnection connection = new NpgsqlConnection(_configuration.ConnectionString);
            return connection;
        }
    }
}
