using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Licensing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using UpdateLicensingSpecificationCommand = Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.UpdateLicensingSpecificationCommand;
using SetLicensingSpecificationCommand = Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.SetLicensingSpecificationCommand;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Licensing
{
    public class LincensingService : ILincensingService
    {
        private readonly ILogger<LincensingService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IServiceClient _serviceClient;
        private readonly AccessTokenProvider _accessTokenProvider;
        private readonly IRepository _repository;
        private readonly string _licensingUrl;
        private readonly string _licensingVersion;
        private readonly string _setLicensingPath;
        private readonly string _updateLicensingPath;
        private readonly string _getLicensingPath;

        public LincensingService(ILogger<LincensingService> logger,
            IConfiguration configuration,
             ISecurityContextProvider securityContextProvider,
             IServiceClient serviceClient,
             AccessTokenProvider accessTokenProvider,
             IRepository repository)
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _serviceClient = serviceClient;
            _accessTokenProvider = accessTokenProvider;
            _repository = repository;
            _licensingUrl = configuration["LicensingBaseUrl"];
            _licensingVersion = configuration["LicensingVersion"];
            _setLicensingPath = configuration["SetLicensingPath"];
            _updateLicensingPath = configuration["UpdateLicensingPath"];
            _getLicensingPath = configuration["GetLicensingPath"];
        }

        public async Task<bool> ProcessStorageLicensing(string organizationId, double totalStorage)
        {
            _logger.LogInformation("Licensing start for client id -> {OrganizationId}", organizationId);
            GetLicensingSpecificationQuery query = new GetLicensingSpecificationQuery
            {
                FeatureId = "praxis-license",
                OrganizationId = organizationId
            };
            var existingLicenseData = GetLicensingSpecification(query);
            bool success;
            if (existingLicenseData == null)
            {
                var licenseData = PrepareSetLicensingPayload(organizationId, totalStorage);
                success = await SetLicensingSpecification(licenseData);
            }
            else
            {
                var updateLicenseData = PrepareUpdateLicensingPayload(organizationId, totalStorage);
                success = await UpdateLicensingSpecification(updateLicenseData);
            }

            _logger.LogInformation("Licensing for client id -> {OrganizationId} is success -> {Success}", organizationId, success);
            return success;
        }

        private SetLicensingSpecificationCommand PrepareSetLicensingPayload(string organizationId, double totalStorage)
        {
            var licenseData = new SetLicensingSpecificationCommand
            {
                FeatureId = "praxis-license",
                OrganizationId = organizationId,
                IsLicensed = true,
                IsLimitEnable = true,
                UsageLimit = totalStorage * Math.Pow(1024, 3),
                Usage = 0,
                CanOverUse = false,
                OverUseLimit = 0,
                HasExpiryDate = false,
                RolePermissionRequired = false,
                UserPermissionRequired = false
            };

            return licenseData;
        }

        private UpdateLicensingSpecificationCommand PrepareUpdateLicensingPayload(string organizationId, double totalStorage)
        {
            var licenseData = new UpdateLicensingSpecificationCommand
            {
                FeatureId = "praxis-license",
                OrganizationId = organizationId,
                IsLicensed = true,
                IsLimitEnable = true,
                UsageLimit = totalStorage * Math.Pow(1024, 3),
                CanOverUse = false,
                OverUseLimit = 0
            };

            return licenseData;
        }

        public ArcFeatureLicensing GetLicensingSpecification(GetLicensingSpecificationQuery query)
        {
            try
            {
                var licensinngSpecification = _repository.GetItems<ArcFeatureLicensing>(lps =>
                lps.FeatureId == query.FeatureId && lps.OrganizationId == query.OrganizationId
                ).FirstOrDefault();
                return licensinngSpecification;
            }
            catch(Exception ex)
            {
                _logger.LogError("Exception in get license data with error -> {ErrorMessage}", ex.Message);
                return null;
            }
        }

        public async Task<GetLicensingSpecificationResponse> GetLicensingSpecificationResponse(GetLicensingSpecificationQuery query)
        {
            try
            {
                var respnose = await _serviceClient.SendToHttpAsync<GetLicensingSpecificationResponse>(HttpMethod.Post, _licensingUrl, _licensingVersion, _getLicensingPath, query, _securityContextProvider.GetSecurityContext().OauthBearerToken);
                return respnose;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in get license data with error -> {ErrorMessage}", ex.Message);
                return null;
            }
        }

        public async Task<bool> SetLicensingSpecification(SetLicensingSpecificationCommand command)
        {
            _logger.LogInformation("Entry into the service {ServiceName} with command -> {Command}", nameof(LincensingService), JsonConvert.SerializeObject(command));
            try
            {
                var token = await GetAdminToken();
                var payload = PrepareSetLicensingSpecificationPayload(command);
                var respnose = await _serviceClient.SendToHttpAsync<SetLicensingSpecification>(HttpMethod.Post, _licensingUrl, _licensingVersion, _setLicensingPath, payload, token);

                if (!respnose.ExecutionStatus && respnose.Errors.Count > 0)
                {
                    _logger.LogError("Error occured during storage set licensing Error: {Response}", JsonConvert.SerializeObject(respnose));
                    return false;
                }

                _logger.LogInformation("Storage set licensing done successfully with command {Command}", JsonConvert.SerializeObject(command));
                return true;
            }
            catch(Exception ex)
            {
                _logger.LogError("Exception occurd in licensing service -> {ErrorMessage}", ex.Message);
            }
            return true;
        }

        public async Task<bool> UpdateLicensingSpecification(UpdateLicensingSpecificationCommand command)
        {
            _logger.LogInformation("Entry into the service {ServiceName} with command -> {Command}.", nameof(LincensingService), JsonConvert.SerializeObject(command));
            try
            {
                var token = await GetAdminToken();
                var payload = PrepareUpdateLicensingSpecificationPayload(command);
                var respnose = await _serviceClient.SendToHttpAsync<UpdateLicensingSpecification>(HttpMethod.Post, _licensingUrl, _licensingVersion, _updateLicensingPath, payload, token);

                if (respnose == null)
                {
                    _logger.LogError("Error occured during storage licensing Error: {Response}", JsonConvert.SerializeObject(respnose));
                    return false;
                }

                if (!respnose.ExecutionStatus && respnose.Errors.Count > 0)
                {
                    _logger.LogError("Error occured during storage licensing Error: {Response}", JsonConvert.SerializeObject(respnose));
                    return false;
                }

                _logger.LogInformation("Storage licensing update successfully for command: {Command}", JsonConvert.SerializeObject(command));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurd in licensing service -> {ErrorMessage}", ex.Message);
            }
            return true;
        }

        private SetLicensingSpecificationPayload PrepareSetLicensingSpecificationPayload(SetLicensingSpecificationCommand command)
        {
            var payload = new SetLicensingSpecificationPayload
            {
                ArcFeatureLicensings = new List<SetLicensingSpecificationCommand>()
            };
            payload.ArcFeatureLicensings.Add(command);
            return payload;
        }

        private UpdateLicensingSpecificationPayload PrepareUpdateLicensingSpecificationPayload(UpdateLicensingSpecificationCommand command)
        {
            var payload = new UpdateLicensingSpecificationPayload
            {
                ArcFeatureLicensing = command
            };
            return payload;
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
                Roles = new List<string> { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.Tenantadmin }
            };


            var accessToken = await _accessTokenProvider.CreateForUserAsync(tokenInfo);
            return accessToken;
        }
    }
}
