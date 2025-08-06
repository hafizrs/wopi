using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;


namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    public class RiqsInterfaceTokenService : IRiqsInterfaceTokenService
    {
       
        private const string MicrosoftProvider = "microsoft";
        private readonly IRiqsInterfaceSessionService _riqsInterfaceSessionService;
        private readonly ILogger<RiqsInterfaceTokenService> _logger;
        private readonly IRepository _repository;
        private readonly IServiceClient _httpClient;
        private readonly ISecurityContextProvider _securityContextProvider;
        public RiqsInterfaceTokenService(
            ILogger<RiqsInterfaceTokenService> logger,
            IRiqsInterfaceSessionService riqsInterfaceSessionService,
            IRepository repository,
            IServiceClient httpClient,
            ISecurityContextProvider securityContextProvider)
        {
            _logger = logger;
            _repository = repository;
            _httpClient = httpClient;
            _riqsInterfaceSessionService = riqsInterfaceSessionService;
            _securityContextProvider = securityContextProvider;
        }

        public async Task<ExternalUserTokenResponse> GetInterfaceTokenAsync(string code, string stateData)
        {

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(stateData)) return null;
            var stateInfo = RiqsInterfaceStateInfo.InitializeFromBase64EncodedString(stateData);
            return await GetTokenInternalAsync(
                new InterfaceTokenRequestParameters
                {
                    GrantType = "authorization_code",
                    Code = code,
                    Provider= stateInfo.Provider
                });
        }

        public async Task<ExternalUserTokenResponse> GetInterfaceTokenAsync(string refreshTokenId)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenId))
            {
                _logger.LogWarning("Refresh tokenId is null or empty.");
                return null;
            }
            var refreshtokenInfo = await _riqsInterfaceSessionService.GetRefreshTokenSessionByRefTokenIdAsync(refreshTokenId);
            if (refreshtokenInfo==null)
            {
                _logger.LogWarning("Refresh token info null or empty.");
                return null;
            }
            return await GetTokenInternalAsync(
                new InterfaceTokenRequestParameters
                {
                    GrantType = "refresh_token",
                    RefreshToken = refreshtokenInfo.refresh_token,
                    Provider=refreshtokenInfo.provider
                });
        }

        private async Task<ExternalUserTokenResponse> GetTokenInternalAsync(InterfaceTokenRequestParameters parameters)
        {
            var configuration = _repository.GetItem<RiqsInterfaceConfiguration>(x => x.Provider == parameters.Provider);
            if (configuration == null)
            {
                _logger.LogWarning($"No configuration found for provider: {MicrosoftProvider}");
                return null;
            }

            var postData = CreateTokenRequestData(configuration, parameters);
            var tokenRequest = CreateTokenRequest(postData, configuration.TokenUrl);

            try
            {
                return await SendTokenRequestAsync(tokenRequest, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving access token");
                
            }
            return null;
        }

        private List<KeyValuePair<string, string>> CreateTokenRequestData(
            RiqsInterfaceConfiguration configuration,
            InterfaceTokenRequestParameters parameters)
        {
            var postData = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("client_id", configuration.ClientId),
            new KeyValuePair<string, string>("client_secret", configuration.ClientSecret),
            new KeyValuePair<string, string>("redirect_uri", configuration.RedirectUri),
            new KeyValuePair<string, string>("grant_type", parameters.GrantType)
        };

            if (!string.IsNullOrWhiteSpace(parameters.Code))
            {
                postData.Add(new KeyValuePair<string, string>("code", parameters.Code));
            }

            if (!string.IsNullOrWhiteSpace(parameters.RefreshToken))
            {
                postData.Add(new KeyValuePair<string, string>("refresh_token", parameters.RefreshToken));
            }

            return postData;
        }

        private HttpRequestMessage CreateTokenRequest(List<KeyValuePair<string, string>> postData,string tokenUrl)
        {
            return new HttpRequestMessage(HttpMethod.Post, tokenUrl)
            {
                Content = new FormUrlEncodedContent(postData)
            };
        }

        private async Task<ExternalUserTokenResponse> SendTokenRequestAsync(
            HttpRequestMessage tokenRequest,
            InterfaceTokenRequestParameters parameters
            )
        {
            try
            {
                var tokenResponse = await  _httpClient.SendToHttpAsync(tokenRequest);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to retrieve access token. Status code: {StatusCode}.", tokenResponse.StatusCode);
                    return null;
                }

                var responseContent = await tokenResponse.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(responseContent);

                var accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();
                var refressToken = jsonDoc.RootElement.TryGetProperty("refresh_token", out var tokenElement)
                                   && tokenElement.ValueKind == JsonValueKind.String
                                   ? tokenElement.GetString()
                                   : parameters.RefreshToken;
                var expires_in = jsonDoc.RootElement.GetProperty("expires_in").GetInt32();
                _logger.LogInformation("Access token retrieved successfully.");
                var externalTokenResponse = new ExternalUserTokenResponse
                {
                    expires_in = expires_in.ToString(),
                    access_token = accessToken,
                    refresh_token = refressToken,
                    refresh_token_id= Guid.NewGuid().ToString(),
                    provider = parameters.Provider
                };
                await _riqsInterfaceSessionService.AddRefreshTokenSessionAsync(externalTokenResponse, _securityContextProvider.GetSecurityContext().UserId);

                return externalTokenResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(SendTokenRequestAsync), ex.Message, ex.StackTrace);
                return null;
            }


        }

        public async Task<ExternalUserTokenResponse> GetInterfaceTokenAsyncByUserId()
        {

            try
            {
                if (_securityContextProvider is null || _securityContextProvider?.GetSecurityContext() is null || _securityContextProvider?.GetSecurityContext().Email is null)
                {
                    _logger.LogError("Error while retrieving access token userId missing");
                }

                var userId = _securityContextProvider?.GetSecurityContext().UserId;
                var refreshTokenId = await _riqsInterfaceSessionService.GetRefreshTokenIdAsync(userId);

                return await GetInterfaceTokenAsync(refreshTokenId);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving access token");
                
            }
            return null;
        }
    }

}
