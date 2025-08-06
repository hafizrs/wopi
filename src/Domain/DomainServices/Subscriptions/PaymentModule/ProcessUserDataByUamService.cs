using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
    public class ProcessUserDataByUamService : IProcessUserDataByUam
    {
        private readonly ILogger<ProcessUserDataByUamService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly AccessTokenProvider _accessTokenProvider;
        private readonly IServiceClient _serviceClient;
        

        private readonly string _uamServiceBaseUrl;
        private readonly string _uamVersion;
        private readonly string _uamCreateUserPath;
        private readonly string _uamUpdateUserPath;

        public ProcessUserDataByUamService(
            ILogger<ProcessUserDataByUamService> logger,
            ISecurityContextProvider securityContextProvider,
            AccessTokenProvider accessTokenProvider,
            IConfiguration configuration,
            IServiceClient serviceClient
            )
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _accessTokenProvider = accessTokenProvider;
            _serviceClient = serviceClient;
            _uamServiceBaseUrl = configuration["UamServiceBaseUrl"];
            _uamVersion = configuration["UamServiceVersion"];
            _uamCreateUserPath = configuration["UamCreateUserPath"];
            _uamUpdateUserPath = configuration["UamUpdateUserPath"];
        }

        public async Task<(bool, string userId)> SaveData(PersonInformation userInformation)
        {
            try
            {
                _logger.LogInformation("Going to save user information: {UserInformation}", JsonConvert.SerializeObject(userInformation));

                var token = await GetAdminToken();

                var response = await _serviceClient.SendToHttpAsync<CommandResponse>(
                    HttpMethod.Post,
                    _uamServiceBaseUrl,
                    _uamVersion,
                    _uamCreateUserPath,
                    userInformation,
                    token
                );

                if (response.HttpStatusCode != HttpStatusCode.OK && response.StatusCode != 0) 
                {
                    _logger.LogError("Error occurred during create user by UAM service. Error: {Response}", JsonConvert.SerializeObject(response));
                    return (false, string.Empty);
                }

                _logger.LogInformation("Data has been successfully posted to UAM service for create user with user information: {UserInformation}", JsonConvert.SerializeObject(userInformation));

                return (true, userInformation.ItemId);

            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during create user: {UserInformation}. Exception Message: {Message}. Exception Details: {StackTrace}.", JsonConvert.SerializeObject(userInformation), ex.Message, ex.StackTrace);

                return (false, string.Empty);
            }
        }

        public async Task<bool> UpdateData(PersonInformation userInformation)
        {
            try
            {
                _logger.LogInformation("Going to update user information: {UserInformation}", JsonConvert.SerializeObject(userInformation));

                var token = await GetAdminToken();

                var respnose = await _serviceClient.SendToHttpAsync<CommandResponse>(HttpMethod.Post, _uamServiceBaseUrl, _uamVersion, _uamUpdateUserPath, userInformation, token);

                if (respnose.HttpStatusCode != HttpStatusCode.OK && respnose.StatusCode != 0)
                {
                    _logger.LogError("Error occurred during update user by UAM service. Error: {Response}", JsonConvert.SerializeObject(respnose));
                    return false;
                }

                _logger.LogInformation("Data has been successfully posted to UAM service for update user with user information: {UserInformation}", JsonConvert.SerializeObject(userInformation));
                return true;
            }
            catch(Exception ex)
            {
                _logger.LogError("Exception occurred during update user: {UserInformation}. Exception Message: {Message}. Exception Details: {StackTrace}.", JsonConvert.SerializeObject(userInformation), ex.Message, ex.StackTrace);
                return false;
            }
        }

        private async Task<string> GetAdminToken()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var tokenInfo = new TokenInfo
            {
                UserId = Guid.NewGuid().ToString(),
                TenantId = securityContext.TenantId,
                SiteId = securityContext.SiteId,
                SiteName = securityContext.SiteName,
                Origin = securityContext.RequestOrigin,
                DisplayName = "lalu vulu",
                UserName = "laluvulu@yopmail.com",
                Language = securityContext.Language,
                PhoneNumber = securityContext.PhoneNumber,
                Roles = new List<string> { RoleNames.Admin, RoleNames.SystemAdmin }
            };
            var accessToken = await _accessTokenProvider.CreateForUserAsync(tokenInfo);
            return accessToken;
        }
    }
}
