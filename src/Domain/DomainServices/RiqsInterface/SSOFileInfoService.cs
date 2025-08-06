using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using ZXing;

    public class SSOFileInfoService : ISSOFileInfoService
    {
        private readonly ILogger<SSOFileInfoService> _logger;
        private readonly IRepository _repository;
        private readonly HttpClient _httpClient;
        private readonly IRiqsInterfaceTokenService _tokenService;

        public SSOFileInfoService(
            ILogger<SSOFileInfoService> logger,
            IRepository repository,
            HttpClient httpClient,
            IRiqsInterfaceTokenService tokenService
        )
        {
            _logger = logger;
            _repository = repository;
            _httpClient = httpClient;
            _tokenService = tokenService;
        }

        public async Task<string> GetSSOFileInfo(string sharePointSite, string filePath)
        {
            var configuration = _repository.GetItem<RiqsInterfaceConfiguration>(x => x.Provider == "microsoft");
            if (configuration == null)
            {
                _logger.LogError("No configuration found for Microsoft provider.");
                return null;
            }

            ExternalUserTokenResponse tokenResponse;
            try
            {
                tokenResponse = await _tokenService.GetInterfaceTokenAsync(configuration.Code, "microsoft");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while retrieving access token: {ErrorMessage}", ex.Message);
                throw;
            }

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
            {
                _logger.LogError("Failed to retrieve access token.");
                return null;
            }

            string accessToken = tokenResponse.access_token;

            try
            {
                var graphApiUrl = $"https://graph.microsoft.com/v1.0/sites/{sharePointSite}/drive/root:/{filePath}";

                var graphRequest = new HttpRequestMessage(HttpMethod.Get, graphApiUrl);
                graphRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var graphResponse = await _httpClient.SendAsync(graphRequest);

                if (graphResponse.IsSuccessStatusCode)
                {
                    var graphContent = await graphResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("SharePoint file info retrieved successfully.");
                    return graphContent;
                }
                else
                {
                    _logger.LogError($"Failed to retrieve file info. Status code: {graphResponse.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while retrieving SharePoint file info: {ex.Message}");
                throw;
            }
        }
    }

}
