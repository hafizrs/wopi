using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices
{
    public class UrlShortnerService : IUrlShortnerService
    {
        private readonly ILogger<UrlShortnerService> _logger;
        private readonly IServiceClient _serviceClient;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly string _urlShortnerBaseUrl;
        private readonly string _urlShortnerCommandUrl;


        public UrlShortnerService(
            IServiceClient serviceClient,
            ILogger<UrlShortnerService> logger,
            IConfiguration configuration,
            ISecurityContextProvider securityContextProvider
        )
        {
            _serviceClient = serviceClient;
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _urlShortnerBaseUrl = configuration["UrlShortnerBaseUrl"];
            _urlShortnerCommandUrl = configuration["UrlShortnerCommandUrl"];

        }

        public async Task<CommandResponse> ShortenUriAsync(string shortUriId, string shortUri)
        {
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();

                var payload = new
                {
                    ItemId = shortUriId ?? Guid.NewGuid().ToString(),
                    Uri = shortUri,
                    UriOnForbidden = string.Empty
                };

                var httpResponse = await _serviceClient.SendToHttpAsync<CommandResponse>(
                     HttpMethod.Post,
                     _urlShortnerBaseUrl,
                     _urlShortnerCommandUrl,
                     "Send2FactorAuthenticationCodeToUser",
                     payload,
                     securityContext.OauthBearerToken);
                if (httpResponse is { StatusCode: 0 })
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occurred in {ClassName} in ShortenUriAsync with error -> {ExceptionMessage}, trace -> {ExceptionStackTrace}",
                    GetType().Name, ex.Message, ex.StackTrace);
            }

            return null;
        }

    }
}
