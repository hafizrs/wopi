using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.TwoFactorAuthentication;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.TwoFactorAuthentication
{
    public class TwoFactorAuthenticationService : ITwoFactorAuthenticationService
    {
        private readonly ILogger<TwoFactorAuthenticationService> _logger;
        private readonly IServiceClient _serviceClient;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly string _twoFactorBaseUrl;
        private readonly string _twoFactorCommandUrl;
        private readonly string _twoFactorQueryUrl;

        public TwoFactorAuthenticationService(
            IServiceClient serviceClient,
            ILogger<TwoFactorAuthenticationService> logger,
            IConfiguration configuration,
            ISecurityContextProvider securityContextProvider
        )
        {
            _serviceClient = serviceClient;
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _twoFactorBaseUrl = configuration["TwoFactorBaseUrl"];
            _twoFactorCommandUrl = configuration["TwoFactorCommandUrl"];
            _twoFactorQueryUrl = configuration["TwoFactorQueryUrl"];
        }

        public async Task<string> GenerateCode(string twoFactorId)
        {
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();

                var payload = new
                {
                    securityContext.UserId,
                    securityContext.Language,
                    ContextId = Guid.NewGuid().ToString(),
                    ContextName = "Signature",
                    TwoFactorId = twoFactorId,
                    Medium = "Email"
                };
                var httpResponse = await _serviceClient.SendToHttpAsync<CommandResponse>(
                    HttpMethod.Post,
                    _twoFactorBaseUrl,
                    _twoFactorCommandUrl,
                    "Send2FactorAuthenticationCodeToUser",
                    payload,
                    securityContext.OauthBearerToken);
                if (httpResponse is { StatusCode: 0 })
                {
                    return twoFactorId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}",
                    GetType().Name, ex.Message, ex.StackTrace);
            }

            return string.Empty;
        }

        public Task<string> GenerateCode(string twoFactorId, string email, string name)
        {
            throw new NotImplementedException();
        }

        public async Task<TwoFAVerifyResponse> VerifyCode(TwoFactorCodeVerifyQuery query)
        {
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                return await _serviceClient.SendToHttpAsync<TwoFAVerifyResponse>(
                    HttpMethod.Post,
                    _twoFactorBaseUrl,
                    _twoFactorQueryUrl,
                    "VerifyTwoFactorAuthenticationCode",
                    query,
                    securityContext.OauthBearerToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured in {Name} in VerifyCode with error -> {ExMessage} trace -> {ExStackTrace}",
                    GetType().Name, ex.Message, ex.StackTrace);
            }

            return null;
        }
    }
}
