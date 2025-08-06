using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.StorageService;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UpdateLicensingSpecificationCommand = Selise.Ecap.SC.PraxisMonitor.Commands.UpdateLicensingSpecificationCommand;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
    public class ProcessClientDataService : IProcessClientData
    {
        private readonly ILogger<ProcessClientDataService> _logger;
        private readonly IRepository _repository;
        private readonly IPraxisClientService _praxisClientService;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly INavigationPreparationTypeStrategy _navigationPreparationTypeStrategy;
        private readonly IDocumentUploadAndConversion _documentUploadAndConversion;
        private readonly ILincensingService _licensingService;
        private readonly string _dmsBaseUrl;
        private readonly string _dmsVersion;
        private readonly string _dmsFolderCreateUrl;
        private readonly string _dmsFolderShareUrl;
        private readonly IServiceClient _serviceClient;
        private readonly AccessTokenProvider _accessTokenProvider;
        private readonly string _workSpaceId;

        public ProcessClientDataService(
            ILogger<ProcessClientDataService> logger,
            IRepository repository,
            IPraxisClientService praxisClientService,
            IMongoSecurityService mongoSecurityService,
            ISecurityContextProvider securityContextProvider,
            INavigationPreparationTypeStrategy navigationPreparationTypeStrategy,
            IDocumentUploadAndConversion documentUploadAndConversion,
            ILincensingService licensingService,
            IConfiguration configuration,
            IServiceClient serviceClient,
            AccessTokenProvider accessTokenProvider
        )
        {
            _logger = logger;
            _repository = repository;
            _praxisClientService = praxisClientService;
            _mongoSecurityService = mongoSecurityService;
            _securityContextProvider = securityContextProvider;
            _navigationPreparationTypeStrategy = navigationPreparationTypeStrategy;
            _documentUploadAndConversion = documentUploadAndConversion;
            _licensingService = licensingService;
            _dmsBaseUrl = configuration["DMSBaseUrl"];
            _dmsVersion = configuration["DMSVersion"];
            _dmsFolderCreateUrl = configuration["DMSFolderCreateUrl"];
            _dmsFolderShareUrl = configuration["DMSFolderShareUrl"];
            _workSpaceId = configuration["WorkSpaceId"];
            _serviceClient = serviceClient;
            _accessTokenProvider = accessTokenProvider;
        }

        public async Task<(bool, PraxisClient client)> SaveData(PaymentClientInformation clientInformation)
        {
            _logger.LogInformation("Going to save client information with name: {ClientName}.", clientInformation.ClientData.ClientName);

            PraxisClient client;
            try
            {
                clientInformation.ClientData.ItemId = Guid.NewGuid().ToString();
                clientInformation.ClientData.IsOrgTypeChangeable = true;
                clientInformation.ClientData.IsOpenOrganization = false;
                clientInformation.ClientData.AdditionalInfos = new List<ClientAdditionalInfo>();
                clientInformation.ClientData.IsCreateUserEnable = true;
                _praxisClientService.CreateDynamicRoles(clientInformation.ClientData.ItemId); //Create Dynamic Role

                if (!string.IsNullOrEmpty(clientInformation.FileName) &&
                    !string.IsNullOrEmpty(clientInformation.FileId))
                {
                    var fileId = clientInformation.FileId;
                    var fileName = clientInformation.FileName;
                    var clientResponse = await InsertClientData(clientInformation.ClientData, fileId, fileName);
                    if (clientResponse.Item1)
                    {
                        client = clientResponse.client;
                        await CreateConnection(
                                fileId,
                                nameof(File),
                                clientInformation.ClientData.ItemId,
                                nameof(PraxisClient),
                                new[] { "Logo-Of-PraxisClient" }
                              );
                        await _documentUploadAndConversion.FileConversion(fileId, TagName.LogoOfClient, clientInformation.ClientData.ItemId, nameof(PraxisClient));
                    }
                    else
                    {
                        return (false, null);
                    }
                }
                else
                {
                    var clientResponse = await InsertClientData(clientInformation.ClientData);
                    if (!clientResponse.Item1)
                    {
                        return (false, null);
                    }

                    client = clientResponse.client;
                }

                var navigationProcessService =
                    _navigationPreparationTypeStrategy.GetServiceType(clientInformation.NavigationProcessType);

                await navigationProcessService.ProcessNavigationData(
                    clientInformation.ClientData.ItemId,
                    clientInformation.NavigationList);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during saving client information: {ClientInformation}. Exception Message: {Message}. Exception Details: {StackTrace}.", JsonConvert.SerializeObject(clientInformation), ex.Message, ex.StackTrace);
                return (false, null);
            }

            return (true, client);
        }

        public async Task<bool> ProcessClientSubscription(
            PraxisClient client,
            double alreadyIncludedStorage,
            int additionalStorage
        )
        {
            try
            {
                return false;

                // await ProcessStorageLicensing(client.ItemId, alreadyIncludedStorage, additionalStorage);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in client subscription related process with error: {Message}", ex.Message);
                return false;
            }
        }

        private async Task<string> GetAdminToken()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var tokenInfo = new TokenInfo
            {
                UserId = "1bb370d7-7d42-4e9a-afde-9382fa96c417",
                TenantId = securityContext.TenantId,
                SiteId = securityContext.SiteId,
                SiteName = securityContext.SiteName,
                Origin = securityContext.RequestOrigin,
                DisplayName = "Kawsar Ahmed",
                UserName = "kawsar.ahmed@selise.ch",
                Language = securityContext.Language,
                PhoneNumber = securityContext.PhoneNumber,
                Roles = new List<string> { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.Tenantadmin }
            };
            var accessToken = await _accessTokenProvider.CreateForUserAsync(tokenInfo);
            return accessToken;
        }

        private async Task CreateConnection(
            string childEntityID,
            string childEntityName,
            string parentEntityID,
            string parentEntityName,
            string[] tags
        )
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            var clientAdminAccessRole = _mongoSecurityService.GetRoleName(
                DynamicRolePrefix.PraxisClientAdmin,
                parentEntityID
            );
            var clientReadAccessRole = _mongoSecurityService.GetRoleName(
                DynamicRolePrefix.PraxisClientRead,
                parentEntityID
            );
            var clientManagerAccessRole = _mongoSecurityService.GetRoleName(
                DynamicRolePrefix.PraxisClientManager,
                parentEntityID
            );

            var rolesAllowToRead = new[]
            {
                RoleNames.AppUser, RoleNames.Admin, RoleNames.TaskController, clientAdminAccessRole,
                clientManagerAccessRole, clientReadAccessRole
            };
            var rolesAllowToUpdate = new[] { RoleNames.Admin, RoleNames.TaskController, clientAdminAccessRole };
            var rolesAllowToDelete = new[] { RoleNames.Admin };

            var connection = new Connection
            {
                ItemId = Guid.NewGuid().ToString(),
                CreateDate = DateTime.UtcNow.ToLocalTime(),
                LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                Tags = tags,
                TenantId = securityContext.TenantId,
                IsMarkedToDelete = false,
                Language = securityContext.Language,
                RolesAllowedToRead = rolesAllowToRead,
                RolesAllowedToUpdate = rolesAllowToUpdate,
                RolesAllowedToDelete = rolesAllowToDelete,
                ChildEntityName = childEntityName,
                ChildEntityID = childEntityID,
                ParentEntityID = parentEntityID,
                ParentEntityName = parentEntityName
            };

            await _repository.SaveAsync<Connection>(connection);
            _logger.LogInformation(
                "Data has been successfully inserted for {EntityName} entity with ItemId: {ItemId}.",
                nameof(Connection),
                connection.ItemId
            );
        }

        private async Task<(bool, PraxisClient client)> InsertClientData(
            PraxisClient praxisClient,
            string fileId = null,
            string fileName = null)
        {
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();

                var clientAdminAccessRole = _mongoSecurityService.GetRoleName(
                    DynamicRolePrefix.PraxisClientAdmin,
                    praxisClient.ItemId
                );
                var clientReadAccessRole = _mongoSecurityService.GetRoleName(
                    DynamicRolePrefix.PraxisClientRead,
                    praxisClient.ItemId
                );
                var clientManagerAccessRole = _mongoSecurityService.GetRoleName(
                    DynamicRolePrefix.PraxisClientManager,
                    praxisClient.ItemId
                );

                var rolesAllowToRead = new[]
                {
                    RoleNames.Admin, RoleNames.TaskController, clientAdminAccessRole, clientManagerAccessRole,
                    clientReadAccessRole
                };
                var rolesAllowToUpdate = new[] { RoleNames.Admin, RoleNames.TaskController, clientAdminAccessRole };
                var rolesAllowToDelete = new[] { RoleNames.Admin };

                praxisClient.CreateDate = DateTime.UtcNow.ToLocalTime();
                praxisClient.Language = "en-US";
                praxisClient.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
                praxisClient.Tags = new[] { "Is-Valid-PraxisClient" };
                praxisClient.TenantId = securityContext.TenantId;
                praxisClient.IsMarkedToDelete = false;
                praxisClient.UserCount = 0;
                praxisClient.AuthorizedUserLimit = praxisClient.UserLimit * 2;
                praxisClient.RolesAllowedToRead = rolesAllowToRead;
                praxisClient.RolesAllowedToUpdate = rolesAllowToUpdate;
                praxisClient.RolesAllowedToDelete = rolesAllowToDelete;

                if (fileId != null)
                    praxisClient.Logo = new PraxisImage
                    {
                        FileId = fileId,
                        FileName = fileName
                    };

                await _repository.SaveAsync<PraxisClient>(praxisClient);
                _logger.LogInformation(
                    "Data has been successfully inserted to {EntityName} entity with ItemId: {ItemId}.",
                    nameof(PraxisClient),
                    praxisClient.ItemId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during saving data to {EntityName} entity with ItemId: {ItemId}. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(PraxisClient), praxisClient.ItemId, ex.Message, ex.StackTrace);
                return (false, null);
            }

            return (true, praxisClient);
        }

        public async Task<bool> ProcessPraxisClientSubscriptionNotification(
            PraxisClient clientData,
            PraxisClientSubscription existingClientSubscription
        )
        {
            try
            {
                var expiredDate = existingClientSubscription.SubscriptionExpirationDate;
                var newPraxisClientSubscriptionNotification = new PraxisClientSubscriptionNotification
                {
                    ItemId = Guid.NewGuid().ToString(),
                    ClientId = clientData.ItemId,
                    ClientEmail = existingClientSubscription.ClientEmail,
                    SubscriptionExpirationDate = existingClientSubscription.SubscriptionExpirationDate,
                    ExpirationRemainderDates = new List<DateTime>
                    {
                        expiredDate.AddDays(-90),
                        expiredDate.AddDays(-60),
                        expiredDate.AddDays(-30),
                        expiredDate.AddDays(-15)
                    },
                    DurationOfSubscription = Convert.ToString(existingClientSubscription.DurationOfSubscription)
                };
                await _repository.SaveAsync(newPraxisClientSubscriptionNotification);
                _logger.LogInformation(
                    "Data has been successfully inserted to {EntityName} with ItemId: {ItemId}.",
                    nameof(PraxisClientSubscriptionNotification),
                    newPraxisClientSubscriptionNotification.ItemId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured in process subscriprion notification with error -> {ex.Message}");
                return false;
            }

            return true;
        }

        public async Task<bool> ProcessStorageLicensing(
            string clientId,
            double alreadyIncludedStorage,
            int additionalStorage
        )
        {
            _logger.LogInformation("Licensing start for client id -> {ClientId}", clientId);
            GetLicensingSpecificationQuery query = new GetLicensingSpecificationQuery
            {
                FeatureId = "praxis-license",
                OrganizationId = clientId
            };
            var existingLicenseData = _licensingService.GetLicensingSpecification(query);
            bool success;
            if (existingLicenseData == null)
            {
                var licenseData = PrepareSetLicensingPayload(clientId, alreadyIncludedStorage, additionalStorage);
                success = await _licensingService.SetLicensingSpecification(licenseData);
            }
            else
            {
                var updateLicenseData = PrepareUpdateLicensingPayload(
                    clientId,
                    alreadyIncludedStorage,
                    additionalStorage
                );
                success = await _licensingService.UpdateLicensingSpecification(updateLicenseData);
            }

            _logger.LogInformation("Licensing for for client id -> {ClientId} is success -> {Success}", clientId, success);
            return success;
        }

        private SetLicensingSpecificationCommand PrepareSetLicensingPayload(
            string clientId,
            double alreadyIncludedStorage,
            int additionalStorage
        )
        {
            var licenseData = new SetLicensingSpecificationCommand
            {
                FeatureId = "praxis-license",
                OrganizationId = clientId,
                IsLicensed = true,
                IsLimitEnable = true,
                UsageLimit = (alreadyIncludedStorage + additionalStorage) * Math.Pow(1024, 3),
                Usage = 0,
                CanOverUse = false,
                OverUseLimit = 0,
                HasExpiryDate = false,
                RolePermissionRequired = false,
                UserPermissionRequired = false
            };
            return licenseData;
        }

        private UpdateLicensingSpecificationCommand PrepareUpdateLicensingPayload(
            string clientId,
            double alreadyIncludedStorage,
            int storageLimit
        )
        {
            var licenseData = new UpdateLicensingSpecificationCommand
            {
                FeatureId = "praxis-license",
                OrganizationId = clientId,
                IsLicensed = true,
                IsLimitEnable = true,
                UsageLimit = (storageLimit + alreadyIncludedStorage) * Math.Pow(1024, 3),
                CanOverUse = false,
                OverUseLimit = 0
            };
            return licenseData;
        }

        public SubscriptionPackage GetSubscriptionPackageInfo(PraxisClient client)
        {
            var subscriptionPackageId = string.Empty;
            var subscriptionPackageInfo = _repository.GetItem<PraxisPaymentModuleSeed>(
                x => x.ItemId == PraxisPriceSeed.PraxisPaymentModuleSeedId
            );
            if (subscriptionPackageInfo == null)
            {
                return null;
            }

            var packages = client.Navigations.Where(x => x.Name.Equals("MONITORING") || x.Name.Equals("PROCESS_GUIDE"))
                .ToList();
            if (!packages.Any())
            {
                var completePackageModuleList = subscriptionPackageInfo.SubscriptionPackages
                    .FirstOrDefault(x => x.ItemId == "1980879f-2fc9-4920-b1df-1dd1e2daf9b2");
                var moduleList = new List<PraxisKeyValue>();
                foreach (var nav in client.Navigations)
                {
                    moduleList.AddRange(completePackageModuleList!.ModuleList.Where(module => nav.Name == module.Value));
                }

                return new SubscriptionPackage { Title = "OTHER", ModuleList = moduleList };
            }

            if (packages.Count > 1)
            {
                subscriptionPackageId = "1980879f-2fc9-4920-b1df-1dd1e2daf9b2";
            }
            else if (packages[0].Name.Equals("PROCESS_GUIDE"))
            {
                subscriptionPackageId = "1980879f-2fc9-4920-b1df-1dd1e2daf9be";
            }
            else if (packages[0].Name.Equals("MONITORING"))
            {
                subscriptionPackageId = "b04bdc98-86d2-47ba-a3d0-5d6ee9e6eb43";
            }

            return subscriptionPackageInfo.SubscriptionPackages.FirstOrDefault(x => x.ItemId == subscriptionPackageId);
        }
    }
}