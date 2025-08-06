using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class HtmlFromTemplateGeneratorService : IHtmlFromTemplateGeneratorService
    {
        private readonly ILogger<HtmlFromTemplateGeneratorService> _logger;
        private readonly IServiceClient _serviceClient;
        private readonly AccessTokenProvider _accessTokenProvider;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly string _templateServiceBaseUrl;
        private readonly string _templateServiceVersion;
        private readonly string _templateServicePath;

        public HtmlFromTemplateGeneratorService(
            ILogger<HtmlFromTemplateGeneratorService> logger,
            IConfiguration configuration,
            IServiceClient serviceClient,
            AccessTokenProvider accessTokenProvider,
            ISecurityContextProvider securityContextProvider
        )
        {
            _logger = logger;
            _serviceClient = serviceClient;
            _securityContextProvider = securityContextProvider;
            _accessTokenProvider = accessTokenProvider;
            _templateServiceBaseUrl = configuration["TemplateEngineBaseUrl"];
            _templateServiceVersion = configuration["TemplateEngineVersion"];
            _templateServicePath = configuration["TemplateEnginePath"];
        }

        public async Task<bool> GenerateHtml(TemplateEnginePayload templateEnginePayload)
        {
            _logger.LogInformation("Generating HTML from template with payload: {Payload}", JsonConvert.SerializeObject(templateEnginePayload));
            var token = await GetAdminToken();
            var response = await _serviceClient.SendToHttpAsync<CommandResponse>(
                HttpMethod.Post,
                _templateServiceBaseUrl,
                _templateServiceVersion,
                _templateServicePath,
                templateEnginePayload,
                token
            );

            return response.StatusCode.Equals(0);
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
                Roles = new List<string> { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.AppUser }
            };
            var accessToken = await _accessTokenProvider.CreateForUserAsync(tokenInfo);
            return accessToken;
        }
    }
}