using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.MailService;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.Signature;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Utils;
using static ZXing.QrCode.Internal.Version;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices
{
    public class TokenService : ITokenService
    {
        private readonly ILogger<TokenService> _logger;
        private readonly IServiceClient _serviceClient;
        private readonly string _identityBaseUrl;
        private readonly string _identityVersion;
        private readonly AccessTokenProvider _accessTokenProvider;
        private readonly string _origin;
        private readonly ImpersonationContextProvider _impersonationContextProvider;
        private readonly IConfiguration _configuration;
        public TokenService(
            ILogger<TokenService> logger,
            IServiceClient serviceClient,
            IConfiguration configuration,
            AccessTokenProvider accessTokenProvider,
            ImpersonationContextProvider impersonationContextProvider)
        {
            _logger = logger;
            _serviceClient = serviceClient;
            _accessTokenProvider = accessTokenProvider;
            _identityBaseUrl = configuration["IdentityBaseUrl"];
            _identityVersion = configuration["IdentityVersion"];
            _origin = configuration["PraxisWebUrl"];
            _impersonationContextProvider = impersonationContextProvider;
            _configuration = configuration;
        }

        public async Task<string> GetExternalToken(string clientId, string clientSecret, string origin)
        {
            return await GetExternalTokenInternal(clientId, clientSecret, origin, true);
        }

        public async Task<string> GetAdminToken()
        {
            var tokenInfo = new TokenInfo
            {
                UserId = Guid.NewGuid().ToString(),
                TenantId = PraxisConstants.PraxisTenant,
                SiteId = "151996CD-412B-4F48-8413-3F5DE1B9617B",
                SiteName = "RQ-Monitor Team",
                Origin = _origin,
                DisplayName = string.Empty,
                UserName = string.Empty,
                Language = string.Empty,
                PhoneNumber = string.Empty,
                Roles = new List<string> { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.Tenantadmin }
            };
            var accessToken = await _accessTokenProvider.CreateForUserAsync(tokenInfo);
            return accessToken;
        }

        public void CreateAdminImpersonateContext(SecurityContext securityContext)
        {


            var roles = new List<string> { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.Tenantadmin };
            var securityContextUpdated = new SecurityContext
             (

                 email: "kawsar.ahmed@selise.ch",
                 language: string.Empty,
                 requestOrigin: "",
                 phoneNumber: "no-phone",
                  roles: roles ?? new List<string> { RoleNames.AppUser },
                 sessionId: $"ecap-{Guid.NewGuid().ToString()}",
                 siteId: "151996CD-412B-4F48-8413-3F5DE1B9617B",
                 siteName: "RQ-Monitor Team",
                 tenantId: PraxisConstants.PraxisTenant,
                 displayName: string.Empty,
                 userId: Guid.NewGuid().ToString(),
                 isUserAuthenticated: true,
                 userName: string.Empty,
                 hasDynamicRoles: false,
                 userAutoExpire: false,
                 userExpireOn: DateTime.MinValue,
                 userPrefferedLanguage: string.Empty,
                 isAuthenticated: true,
                 oauthBearerToken: string.Empty,
                 requestUri: new Uri(_origin),
                 serviceVersion: string.Empty,
                 tokenHijackingProtectionHash: string.Empty,
                 postLogOutHandlerDataKey: string.Empty,
                 organizationId: string.Empty
             );


            _ = _impersonationContextProvider.ImpersonateBy(
                securityContext: securityContextUpdated,
                tenantId: securityContextUpdated.TenantId,
                siteId: securityContextUpdated.SiteId,
                userId: securityContextUpdated.UserId,
                email: securityContextUpdated.Email,
                roles: securityContextUpdated.Roles);
        }



        public void CreateImpersonateContext(
            SecurityContext securityContext,
            string email,
            string userId,
            List<string> roles

            )
        {

            var securityContextUpdated = new SecurityContext
             (

                 email: email,
                 language: string.Empty,
                 requestOrigin: "",
                 phoneNumber: "no-phone",
                  roles: roles ?? new List<string> { RoleNames.AppUser },
                 sessionId: $"ecap-{Guid.NewGuid().ToString()}",
                 siteId: "151996CD-412B-4F48-8413-3F5DE1B9617B",
                 siteName: "RQ-Monitor Team",
                 tenantId: PraxisConstants.PraxisTenant,
                 displayName: string.Empty,
                 userId: userId,
                 isUserAuthenticated: true,
                 userName: string.Empty,
                 hasDynamicRoles: false,
                 userAutoExpire: false,
                 userExpireOn: DateTime.MinValue,
                 userPrefferedLanguage: string.Empty,
                 isAuthenticated: true,
                 oauthBearerToken: string.Empty,
                 requestUri: new Uri(_origin),
                 serviceVersion: string.Empty,
                 tokenHijackingProtectionHash: string.Empty,
                 postLogOutHandlerDataKey: string.Empty,
                 organizationId: string.Empty
             );

            _ = _impersonationContextProvider.ImpersonateBy(
                securityContext: securityContextUpdated,
                tenantId: securityContextUpdated.TenantId,
                siteId: securityContextUpdated.SiteId,
                userId: securityContextUpdated.UserId,
                email: securityContextUpdated.Email,
                roles: securityContextUpdated.Roles);
        }

        public async Task<SignatureTokenResponseCommand> GetExternalToken(string clientId, string clientSecret)
        {
            try
            {
                var tokenRequestParameters = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", clientId },
                    { "client_secret", clientSecret }
                };

                var tokenUri = _identityBaseUrl + _identityVersion + "/identity/token";
                var url = new UrlFactoryProvider(_configuration).GetUrl(false, tokenUri);

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new FormUrlEncodedContent(tokenRequestParameters)
                };

                request.Headers.Referrer = new Uri(_origin);

                _logger.LogInformation("Request {Message}", JsonConvert.SerializeObject(request));

                using var response = await _serviceClient.SendToHttpAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();

                    _logger.LogError("Error invoking token endpoint: {Uri} with parameters: {Parameters}. Response: {ResponseJson}",
                        request.RequestUri.AbsoluteUri, JsonConvert.SerializeObject(tokenRequestParameters),
                        responseJson);

                    return null;
                }

                var responseJsonString = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<SignatureTokenResponseCommand>(responseJsonString);
                return tokenResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occurred in GetExternalTokenInternal with error -> {ExMessage} trace -> {ExStackTrace}",
                    ex.Message, ex.StackTrace);
            }

            return null;
        }


        private async Task<string> GetExternalTokenInternal(string clientId, string clientSecret, string origin, bool isBlocks)
        {
            try
            {
                var tokenRequestParameters = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", clientId },
                    { "client_secret", clientSecret }
                };

                var tokenUri = _identityBaseUrl + _identityVersion + "/identity/token";
                var url = new UrlFactoryProvider(_configuration).GetUrl(isBlocks, tokenUri);

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new FormUrlEncodedContent(tokenRequestParameters)
                };

                if (origin != null)
                {
                    request.Headers.Referrer = new Uri(origin);
                }
                else
                {
                    request.Headers.Referrer = new Uri(_origin);
                }

                _logger.LogInformation("Request {Message}", JsonConvert.SerializeObject(request));

                using var response = await _serviceClient.SendToHttpAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();

                    _logger.LogError("Error invoking token endpoint: {Uri} with parameters: {Parameters}. Response: {ResponseJson}",
                        request.RequestUri.AbsoluteUri, JsonConvert.SerializeObject(tokenRequestParameters),
                        responseJson);

                    return null;
                }

                var responseJsonString = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<SignatureTokenResponseCommand>(responseJsonString);

                return tokenResponse.access_token;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occurred in GetExternalTokenInternal with error -> {ExMessage} trace -> {ExStackTrace}",
                    ex.Message, ex.StackTrace);
            }

            return null;
        }

        public async Task<string> CreateExternalToken(SecurityContext securityContext, string email, string userId, List<string> roles)
        {
            var tokenInfo = new TokenInfo
            {
                UserId = Guid.NewGuid().ToString(),
                TenantId = PraxisConstants.PraxisTenant,
                SiteId = "151996CD-412B-4F48-8413-3F5DE1B9617B",
                SiteName = "RQ-Monitor Team",
                Origin = _origin,
                DisplayName = string.Empty,
                UserName = string.Empty,
                Language = string.Empty,
                PhoneNumber = string.Empty,
                Roles = roles
            };
            var accessToken = await _accessTokenProvider.CreateForUserAsync(tokenInfo);
            return accessToken;
        }
    }
}