using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    public class RiqsInterfaceCustomHttpClientService: IRiqsInterfaceCustomHttpClientService
    {
        private string _accessToken;
        private readonly IServiceClient _httpClient;
        private readonly IRiqsInterfaceTokenService _riqsInterfaceTokenService;
        private const int MaxRetryAttempts = 2; 

        public RiqsInterfaceCustomHttpClientService(IServiceClient httpClient, IRiqsInterfaceTokenService riqsInterfaceTokenService)
        {
            _httpClient = httpClient ;
            _riqsInterfaceTokenService = riqsInterfaceTokenService;
        }

        public async Task<HttpResponseMessage> SendRequestWithRetryAsync(HttpRequestMessage request)
        {
            HttpResponseMessage response = null;
            int attempt = 0;

            // Retry loop
            while (attempt < MaxRetryAttempts)
            {
                attempt++;
                if (!string.IsNullOrEmpty(_accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                }
                response = await _httpClient.SendToHttpAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                if (string.IsNullOrEmpty(_accessToken) || response.StatusCode == HttpStatusCode.Unauthorized && attempt < MaxRetryAttempts)
                {
                    var tokenInfo = await _riqsInterfaceTokenService.GetInterfaceTokenAsyncByUserId(); ;
                    _accessToken = tokenInfo.access_token;
                    if (string.IsNullOrEmpty(tokenInfo.access_token))
                    {
                        throw new UnauthorizedAccessException("Unable to refresh the token.");
                    }
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                }
                else
                {
                   
                    break;
                }
            }

            return response;
        }
    }
}
