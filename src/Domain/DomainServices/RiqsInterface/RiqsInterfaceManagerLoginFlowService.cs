using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    public class RiqsInterfaceManagerLoginFlowService : IRiqsInterfaceManagerLoginFlowService
    {
        private readonly ILogger<RiqsInterfaceManagerLoginFlowService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRiqsInterfaceTokenService _riqsInterfaceTokenService;
        private readonly IServiceClient _httpClient;
        public RiqsInterfaceManagerLoginFlowService(
            ILogger<RiqsInterfaceManagerLoginFlowService> logger,
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IRiqsInterfaceTokenService riqsInterfaceTokenService,
            IServiceClient httpClient)
        {
            _logger = logger;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _riqsInterfaceTokenService = riqsInterfaceTokenService;
            _httpClient = httpClient;
        }

        public async Task<string> GetInterfaceManagerLoginFlow(string provider)
        {
            var config = await _repository.GetItemAsync<RiqsInterfaceConfiguration>(c => c.Provider == provider);

            if (config != null)
            {
                _logger.LogInformation("Configuration found for provider: {Provider}", provider);
                var userId = _securityContextProvider?.GetSecurityContext().UserId;
                var stateInfo = RiqsInterfaceStateInfo.CreateNew(userId, config?.Provider);
                var state = stateInfo.Serialize();

                var queryParams = new Dictionary<string, string>
                {
                    { "client_id", config?.ClientId },
                    { "response_type", "code" },
                    { "redirect_uri", config?.RedirectUri },
                    { "scope", config?.Scopes != null ? string.Join(" ", config.Scopes) : string.Empty },
                    { "state", state }
                };
                if (provider == RiqsInterfaceConstants.GoogleProvider)
                {
                    queryParams["access_type"] = "offline";
                    queryParams["prompt"] = "consent";
                }
                if (provider == RiqsInterfaceConstants.MicrosoftProvider)
                {
                    queryParams["prompt"] = "login";
                }

                string authUrl = $"{config?.AuthorizationUrl}?{string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"))}";
                _logger.LogInformation("Generated authorization URL for provider : {provider}", provider);

                return authUrl;
            }

            _logger.LogWarning("No configuration found for provider: {Provider}", provider);
            return string.Empty;
        }

        public async Task LogOutInterfaceManager(string provider)
        {
            var userId = _securityContextProvider?.GetSecurityContext().UserId;

            var userLoginSession = _repository.GetItems<RiqsInterfaceSession>(x => x.UserId == userId && x.Provider == provider);

            if (userLoginSession != null && userLoginSession.Count() > 0)
            {
                 var interfaceSessionIds = userLoginSession.Select(s => s.ItemId).ToList();

                 await _repository.DeleteAsync<RiqsInterfaceSession>(d => interfaceSessionIds.Contains(d.ItemId));

                _logger.LogInformation("Successfully Interface Manager Logout for userId : {userId}", userId);
            }
        }


        public async Task<InterfaceManagerLoginDetail> GetInterfaceManagerLoginInfo(string provider)
        {
            var tokenInfo = await _riqsInterfaceTokenService.GetInterfaceTokenAsyncByUserId();

            if (tokenInfo == null || string.IsNullOrEmpty(tokenInfo.access_token))
                return null;

            string baseUrl;
            string url;

            var loginDetails = new InterfaceManagerLoginDetail();
            loginDetails.provider = tokenInfo.provider;
            loginDetails.accessToken = tokenInfo.access_token;

            if (tokenInfo.provider == RiqsInterfaceConstants.GoogleProvider)
            {
                baseUrl = "https://www.googleapis.com/drive/v3";
                url = $"{baseUrl}/about?fields=user";
            }
            else if (tokenInfo.provider == RiqsInterfaceConstants.MicrosoftProvider)
            {
                return loginDetails;
            }
            else
            {
                return loginDetails;
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenInfo.access_token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendToHttpAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    loginDetails = JsonSerializer.Deserialize<InterfaceManagerLoginDetail>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    loginDetails.provider = tokenInfo.provider;
                    loginDetails.accessToken = tokenInfo.access_token;
                    return loginDetails;
                }
                else
                {
                    _logger.LogError("Request failed with status: {StatusCode}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during login info fetch");
                return null;
            }
        }

    }
}
