using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AAG.Global.Data;
using System;
using System.Threading.Tasks;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Database.Accounts.Domain.configurations;
using GoogleAnalytics.Library.Helpers;
using System.Data.SqlClient;

namespace GoogleAnalytics.Library.Data
{
    public class AccountsDbContext : BaseDbContext<AccountsDbContext>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="bugSnag"></param>
        /// <param name="configuration"></param>
        public AccountsDbContext(
              ILogger<AccountsDbContext> logger
            , Bugsnag.IClient bugSnag
            , IConfiguration configuration) : base(configuration, logger, bugSnag, configuration.GetConnectionString(StaticNames.ConnectionAccounts)) 
        {
            _connectionString = configuration.GetConnectionString(StaticNames.ConnectionAccounts);
        }


        /// <summary>
        /// Read all google accounts.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Database.Accounts.Domain.accounts.Google>> ReadGoogleAccounts()
        {
            try
            {
                var connection = new SqlConnection(_connectionString);
                connection.Open();
                using SqlMapper.GridReader results = await connection.QueryMultipleAsync("[dbo].[GoogleRead]", new { @All = 1, @WithConfigurations = 1 }, commandType: System.Data.CommandType.StoredProcedure);
                List<Database.Accounts.Domain.accounts.Google> entities = results
                    ?.Read<Database.Accounts.Domain.accounts.Google>()
                    ?.ToList();

                if (entities != null)
                {
                    List<Database.Accounts.Domain.configurations.GoogleVdpUrlPattern> vdpUrls = results
                        ?.Read<Database.Accounts.Domain.configurations.GoogleVdpUrlPattern>()
                        ?.ToList();

                    if (vdpUrls != null)
                        entities.ForEach(e =>
                                e.VdpUrlPatterns = vdpUrls.Where(u => u.GoogleId == e.GoogleId).ToList()
                            );
                }
                connection.Close();
                return entities;
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }


        /// <summary>
        /// Read Srp Url Patterns.
        /// </summary>
        /// <returns></returns>
        public async Task<List<SrpPagePattern>> SrpUrlPatterns()
        {
            try
            {
                var connection = new SqlConnection(_connectionString);
                connection.Open();
                var results = await connection.QueryAsync<SrpPagePattern>("[configurations].[SrpUrlPatternsRead]", commandType: CommandType.StoredProcedure);
                connection.Close();
                return results.ToList();
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }
    }
}