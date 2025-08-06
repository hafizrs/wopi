using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.Entities.PrimaryEntities.StorageService;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.Events;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
    public class OrganizationDataProcessService : IOrganizationDataProcessService
    {
        private readonly ILogger<IOrganizationDataProcessService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRepository _repository;
        private readonly IDocumentUploadAndConversion _documentUploadAndConversion;
        private readonly string _workSpaceId;
        private readonly string _systemOrganizationId;
        private readonly string _systemAdminUserId;
        private readonly IDmsService _dmsService;
        private readonly IChangeLogService _changeLogService;
        private readonly IServiceClient _serviceClient;
        private readonly ILincensingService _lincensingService;
        private readonly IPrepareNewRole _prepareNewRoleService;
        private readonly IOrganizationSubscriptionService _organizationSubscriptionService;
        private readonly IPraxisClientSubscriptionService _praxisClientSubscriptionService;

        public OrganizationDataProcessService(
            ILogger<IOrganizationDataProcessService> logger,
            ISecurityContextProvider securityContextProvider,
            IRepository repository,
            IDocumentUploadAndConversion documentUploadAndConversion,
            IDmsService dmsService,
            IConfiguration configuration,
            IServiceClient serviceClient,
            IChangeLogService changeLogService,
            ILincensingService lincensingService,
            IPrepareNewRole prepareNewRoleService,
            IOrganizationSubscriptionService organizationSubscriptionService,
            IPraxisClientSubscriptionService praxisClientSubscriptionService
        )
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _repository = repository;
            _documentUploadAndConversion = documentUploadAndConversion;
            _workSpaceId = configuration["WorkSpaceId"];
            _systemOrganizationId = configuration["SystemOrganizationId"];
            _systemAdminUserId = configuration["SystemAdminUserId"];
            _dmsService = dmsService;
            _changeLogService = changeLogService;
            _serviceClient = serviceClient;
            _lincensingService = lincensingService;
            _prepareNewRoleService = prepareNewRoleService;
            _organizationSubscriptionService = organizationSubscriptionService;
            _praxisClientSubscriptionService = praxisClientSubscriptionService;
        }

        // --------------------------------------------- Public methods --------------------------------------------- //

        public async Task<(bool isCreated, PraxisOrganization organization)> InitiateNewSubscriptionOrganizationCreateProcess(
            PraxisOrganization organizationData,
            string paymentDetailId)
        {
            _logger.LogInformation("Going to create organization with name: {ClientName}.", organizationData.ClientName);

            (bool isCreated, PraxisOrganization organization) response = (false, null);
            try
            {
                var subscriptionData = GetSubscriptionData(paymentDetailId);

                var organization = await CreateOrganizationViaPurchaseFlow(organizationData);
                if (organization != null)
                {
                    CreateOrgWideRoles(organization.ItemId);

                    await InitiateOrganizationLogoUploadPostProcess(organizationData);

                    await ProcessOrganizationStorageSpaceAllocation(organization);

                    var totalToken = (subscriptionData.TokenSubscription?.IncludedTokenInMillion ?? 0) + 
                                            (subscriptionData.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0);
                    var totalManualToken = (subscriptionData.ManualTokenSubscription?.IncludedTokenInMillion ?? 0) +
                                            (subscriptionData.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0);
                    var totalStorage = (subscriptionData.StorageSubscription?.IncludedStorageInGigaBites ?? 0) + 
                                            (subscriptionData.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0);

                    await _lincensingService.ProcessStorageLicensing(
                    organization.ItemId, totalStorage);

                    
                    var organizationSubsPayload = new OrganizationSubscription
                    {
                        OrganizationId = organization.ItemId,
                        TotalTokenSize = totalToken,
                        TotalStorageSize = totalStorage,
                        TokenOfOrganization = totalToken,
                        StorageOfOrganization = totalStorage,
                        TokenOfUnits = 0,
                        StorageOfUnits = 0,
                        SubscriptionDate = subscriptionData.SubscriptionDate,
                        SubscriptionExpirationDate = subscriptionData.SubscriptionExpirationDate,
                        IsTokenApplied = subscriptionData.IsTokenApplied,
                        TotalManualTokenSize = totalManualToken,
                        ManualTokenOfOrganization = totalManualToken,
                        ManualTokenOfUnits = 0,
                        IsManualTokenApplied = subscriptionData.IsManualTokenApplied
                    };

                    await _organizationSubscriptionService.SaveOrganizationSubscription(organizationSubsPayload);

                    response.isCreated = true;
                    response.organization = organization;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception occured during {nameof(PraxisOrganization)} create with payload: {JsonConvert.SerializeObject(organizationData)}.");
                _logger.LogError($"Exception Message: {ex.Message} Exception Details: {ex.StackTrace}.");
            }

            return response;
        }

        public async Task<bool> ProcessOrganizationStorageSpaceAllocation(PraxisOrganization organizationData)
        {
            var isCreated = await CreateOrganizationFolder(
                _systemAdminUserId,
                _workSpaceId,
                organizationData.ClientName,
                organizationData.ItemId);

            return true;
        }

        public async Task<bool> InitiateOrganizationLogoUploadPostProcess(PraxisOrganization organization)
        {
            if (!string.IsNullOrWhiteSpace(organization.Logo?.FileId))
                return await PerformOrganizationLogoUploadPostProcess(organization.ItemId, organization.Logo);

            return true;
        }

        public async Task<bool> InitiateOrganizationCreateUpdateProcess(ProcessOrganizationCreateUpdateCommand command)
        {
            var organization = GetOrganization(command.OrganizationData.ItemId).Result;

            var response = organization == null ? await CreateOrganization(command) : await UpdateOrganization(command.OrganizationData);

            if (response)
            {
                if (organization != null)
                {
                    command.OrganizationData = organization;
                }
                PublishOrganizationCreateUpdateEvent(command.OrganizationData, organization == null);
            }

            return response;
        }

        public async Task InitiateOrganizationExternalOfficeProcess(ProcessOrganizationExternalOfficeCommand command)
        {
            _logger.LogInformation("Going to InitiateOrganizationExternalOfficeProcess with OrgId: {OrgId}.", command.OrganizationId);

            try
            {
                if (string.IsNullOrEmpty(command?.OrganizationId))
                {
                    throw new Exception("Invalid Command");
                }

                var organization = await GetOrganization(command.OrganizationId);
                if (organization != null)
                {
                    var externalOffices = organization.ExternelReportingOffices ?? new List<ExternelReportingOffice>();

                    if (!string.IsNullOrEmpty(command.DeleteReportingId))
                    {
                        externalOffices = externalOffices.Where(e => e.ReportingOfficeId != command.DeleteReportingId).ToList();
                    }
                    else if (!string.IsNullOrEmpty(command?.ExternelReportingOffice.ReportingOfficeId))
                    {
                        var index = externalOffices.FindIndex(e => e.ReportingOfficeId == command.ExternelReportingOffice.ReportingOfficeId);
                        if (index != -1)
                        {
                            externalOffices[index] = command.ExternelReportingOffice;
                        }
                        else
                        {
                            externalOffices.Add(command.ExternelReportingOffice);
                        }
                    }
                    organization.ExternelReportingOffices = externalOffices;
                    await UpdateOrganizationExternalOffice(organization);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception occured on InitiateOrganizationExternalOfficeProcess with payload: {JsonConvert.SerializeObject(command)}.");
                _logger.LogError($"Exception Message: {ex.Message} Exception Details: {ex.StackTrace}.");
            }
        }

        private async Task UpdateOrganizationExternalOffice(PraxisOrganization org)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("_id", org.ItemId);

            var updates = new Dictionary<string, object>() 
            { 
                { nameof(PraxisOrganization.ExternelReportingOffices), org.ExternelReportingOffices }
            };

            await _changeLogService.UpdateChange(EntityName.PraxisOrganization, filter, updates);
        }

        private void PublishOrganizationCreateUpdateEvent(PraxisOrganization organizationData, bool isCreated)
        {
            var organizationCreateUpdateEvent = new GenericEvent
            {
                EventType = isCreated ? PraxisEventType.OrganizationCreatedEvent : PraxisEventType.OrganizationUpdatedEvent,
                JsonPayload = JsonConvert.SerializeObject(organizationData)
            };

            _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), organizationCreateUpdateEvent);
        }

        private void CreateOrgWideRoles(string orgId)
        {
            var roles = new List<(string, string)>
            {
                ($"{RoleNames.AdminB_Dynamic}_{orgId}", $"{RoleNames.AdminB}"),
                ($"{RoleNames.Organization_Read_Dynamic}_{orgId}", $"{RoleNames.Organization_Read_Dynamic}")
            };

            roles.ForEach(role => _prepareNewRoleService.SaveRole(
                role.Item1,
                orgId,
                role.Item2,
                true)
            );
        }

        // --------------------------------------------- Private methods --------------------------------------------- //

        // ---------------  Organization related methods --------------- //

        private async Task<PraxisOrganization> GetOrganization(string orgId)
        {
            return await _repository.GetItemAsync<PraxisOrganization>(o => o.ItemId == orgId);
        }

        private async Task<PraxisOrganization> CreateOrganizationViaPurchaseFlow(PraxisOrganization organizationData)
        {
            var currentTime = DateTime.UtcNow.ToLocalTime();
            var securityContext = _securityContextProvider.GetSecurityContext();

            var organization = new PraxisOrganization()
            {
                ItemId = organizationData.ItemId,

                TenantId = securityContext.TenantId,
                Tags = new[] { PraxisTag.IsValidPraxisOrganization },
                Language = securityContext.Language,
                CreateDate = currentTime,
                LastUpdateDate = currentTime,

                RolesAllowedToRead = GetRolesAllowedToReadPraxisOrganization(organizationData.ItemId),
                RolesAllowedToUpdate = GetRolesAllowedToUpdatePraxisOrganization(organizationData.ItemId),
                RolesAllowedToDelete = GetRolesAllowedToDeletePraxisOrganization(),

                ClientName = organizationData.ClientName,
                ClientNumber = organizationData.ClientNumber,
                UserCount = 0,
                UserLimit = organizationData.UserLimit,
                AuthorizedUserLimit = organizationData.UserLimit * 2,
                MemberNetwork = organizationData.MemberNetwork,
                WebPageUrl = organizationData.WebPageUrl,
                Software = organizationData.Software,
                ComputerSystem = organizationData.ComputerSystem,
                Logo = PrepareOrganizationLogo(organizationData.Logo, currentTime),
                Address = organizationData.Address,
                ContactEmail = organizationData.ContactEmail,
                ContactPhone = organizationData.ContactPhone,
                LibraryControlMechanism = organizationData.LibraryControlMechanism
            };

            await _repository.SaveAsync<PraxisOrganization>(organization);

            return organization;
        }

        private async Task<bool> CreateOrganization(ProcessOrganizationCreateUpdateCommand command)
        {
            var organizationData = command.OrganizationData;
            var currentTime = DateTime.UtcNow.ToLocalTime();
            var securityContext = _securityContextProvider.GetSecurityContext();

            var organization = new PraxisOrganization()
            {
                ItemId = organizationData.ItemId,

                TenantId = securityContext.TenantId,
                Tags = new[] { PraxisTag.IsValidPraxisOrganization },
                Language = securityContext.Language,
                CreateDate = currentTime,
                CreatedBy = securityContext.UserId,
                LastUpdateDate = currentTime,
                LastUpdatedBy = securityContext.UserId,

                RolesAllowedToRead = GetRolesAllowedToReadPraxisOrganization(organizationData.ItemId),
                RolesAllowedToUpdate = GetRolesAllowedToUpdatePraxisOrganization(organizationData.ItemId),
                RolesAllowedToDelete = GetRolesAllowedToDeletePraxisOrganization(),
                IdsAllowedToRead = securityContext.Roles.Contains(RoleNames.GroupAdmin) ? new string[] { securityContext.UserId } : new string[] {},

                ClientName = organizationData.ClientName,
                ClientNumber = organizationData.ClientNumber,
                UserCount = 0,
                UserLimit = organizationData.UserLimit,
                AuthorizedUserLimit = organizationData.UserLimit * 2,
                MemberNetwork = organizationData.MemberNetwork,
                WebPageUrl = organizationData.WebPageUrl,
                Software = organizationData.Software,
                ComputerSystem = organizationData.ComputerSystem,
                Logo = PrepareOrganizationLogo(organizationData.Logo, currentTime),
                Address = organizationData.Address,
                ContactEmail = organizationData.ContactEmail,
                ContactPhone = organizationData.ContactPhone,
                LibraryControlMechanism = organizationData.LibraryControlMechanism,
                ReportingConfigurations = organizationData.ReportingConfigurations,
                HaveAdditionalPurchasePermission = organizationData.HaveAdditionalPurchasePermission,
                HaveAdditionalAllocationPermission = organizationData.HaveAdditionalAllocationPermission
            };

            await _repository.SaveAsync(organization);

            await CreateRiqsLibraryControlMechanism(organization);

            await _praxisClientSubscriptionService.SaveClientSubscriptionOnOrgCreateUpdate(organization.ItemId, command.SubscriptionData, organization);

            return true;
        }

        private async Task<bool> UpdateOrganization(PraxisOrganization organizationData)
        {
            var updates = PrepareOrganizationUpdateData(organizationData);

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("_id", organizationData.ItemId);
            await CreateRiqsLibraryControlMechanism(organizationData);
            return await _changeLogService.UpdateChange("PraxisOrganization", filter, updates);
        }

        private Dictionary<string, object> PrepareOrganizationUpdateData(PraxisOrganization organizationData)
        {
            var currentTime = DateTime.UtcNow.ToLocalTime();
            var updateAbleProps =
                new List<string>() {
                    nameof(PraxisOrganization.ClientName),
                    nameof(PraxisOrganization.ClientNumber),
                    nameof(PraxisOrganization.UserLimit),
                    nameof(PraxisOrganization.AuthorizedUserLimit),
                    nameof(PraxisOrganization.MemberNetwork),
                    nameof(PraxisOrganization.WebPageUrl),
                    nameof(PraxisOrganization.Software),
                    nameof(PraxisOrganization.ComputerSystem),
                    nameof(PraxisOrganization.Logo),
                    nameof(PraxisOrganization.Address),
                    nameof(PraxisOrganization.ContactEmail),
                    nameof(PraxisOrganization.ContactPhone),
                    nameof(PraxisOrganization.AdminUserId),
                    nameof(PraxisOrganization.DeputyAdminUserId),
                    nameof(PraxisOrganization.LibraryControlMechanism),
                    nameof(PraxisOrganization.ReportingConfigurations),
                    nameof(PraxisOrganization.HaveAdditionalPurchasePermission),
                    nameof(PraxisOrganization.HaveAdditionalAllocationPermission)
                };

            var updates = new Dictionary<string, object>() { };
            organizationData.AuthorizedUserLimit = organizationData.UserLimit * 2;
            var orgDataProperties = organizationData.GetType().GetProperties().Where(prop => updateAbleProps.Contains(prop.Name));
            foreach (PropertyInfo prop in orgDataProperties)
            {
                var value = prop.GetValue(organizationData, null);
                if (value != null)
                {
                    var processedValue = 
                        prop.Name == nameof(PraxisOrganization.Logo)?
                        PrepareOrganizationLogo((PraxisImage)value, currentTime) :
                        value;

                    updates.Add(prop.Name, processedValue);
                }
            }

            updates.Add("LastUpdateDate", currentTime);
            updates.Add("LastUpdatedBy", _securityContextProvider.GetSecurityContext().UserId);

            return updates;
        }

        private PraxisImage PrepareOrganizationLogo(PraxisImage logoData, DateTime currentTime)
        {
            var organizationLogo =
                string.IsNullOrWhiteSpace(logoData?.FileId) ?
                null :
                new PraxisImage()
                {
                    FileId = logoData.FileId,
                    FileName = logoData.FileName,
                    FileSize = logoData.FileSize,
                    CreateDate = currentTime,
                    CreatedOn = currentTime,
                    IsUploadedFromWeb = true
                };

            return organizationLogo;
        }

        private string[] GetRolesAllowedToReadPraxisOrganization(string organizationId)
        {
            return new[] { RoleNames.Admin, RoleNames.TaskController, $"{RoleNames.AdminB_Dynamic}_{organizationId}" };
        }

        private string[] GetRolesAllowedToUpdatePraxisOrganization(string organizationId)
        {
            return new[] { RoleNames.Admin, RoleNames.TaskController, $"{RoleNames.AdminB_Dynamic}_{organizationId}" };
        }

        private string[] GetRolesAllowedToDeletePraxisOrganization()
        {
            return new[] { RoleNames.Admin };
        }

        private async Task RemoveLogoOfOrganizationConnection(string parentEntityId, string parentEntityName, string tagName)
        {
            try
            {
                var tags = new List<string>();
                var tagSuffixs = new List<string>() { "", "-40-40","-64-64", "-128-128", "-256-256", "-512-512", "-200-200" };
                foreach (var suffix in tagSuffixs)
                {
                    tags.Add(tagName + suffix);
                }

                var connections = _repository.GetItems<Connection>
                                        (c => c.ParentEntityID == parentEntityId && c.ParentEntityName == parentEntityName
                                        && c.Tags.Any(t => tags.Contains(t)))?.ToList();
                if (connections != null)
                {
                    foreach (var connection in connections)
                    {
                        await _repository.DeleteAsync<Connection>(c => c.ItemId == connection.ItemId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in RemoveLogoOfOrganizationConnection: {Exception}", ex);
            }
        }

        private async Task<bool> PerformOrganizationLogoUploadPostProcess(string organizationId, PraxisImage logo)
        {
            await RemoveLogoOfOrganizationConnection(organizationId, nameof(PraxisOrganization), TagName.LogoOfOrganization);
            await CreateConnection(
                    logo.FileId,
                    nameof(File),
                    organizationId,
                    nameof(PraxisOrganization),
                    new[] { TagName.LogoOfOrganization });
            var isConversionSuccess = await _documentUploadAndConversion.FileConversion(logo.FileId, TagName.LogoOfOrganization, organizationId, nameof(PraxisOrganization));
            return isConversionSuccess;
        }

        private async Task<bool> CreateConnection(
            string childEntityID,
            string childEntityName,
            string parentEntityID,
            string parentEntityName,
            string[] tags)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            var rolesAllowToRead = new[]
            {
                RoleNames.AppUser, RoleNames.Admin, RoleNames.TaskController, $"{RoleNames.AdminB_Dynamic}_{parentEntityID}"
            };

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
                RolesAllowedToUpdate = GetRolesAllowedToUpdatePraxisOrganization(parentEntityID),
                RolesAllowedToDelete = GetRolesAllowedToDeletePraxisOrganization(),
                ChildEntityName = childEntityName,
                ChildEntityID = childEntityID,
                ParentEntityID = parentEntityID,
                ParentEntityName = parentEntityName
            };

            await _repository.SaveAsync<Connection>(connection);

            return true;
        }

        // -------------- Subsctiption related methods ------------- //

        private PraxisClientSubscription GetSubscriptionData(string paymentDetailId)
        {
            return _repository.GetItem<PraxisClientSubscription>(x => x.PaymentHistoryId == paymentDetailId);
        }

        // ---------------  Storage related methods --------------- //

        private async Task<bool> CreateOrganizationFolder(
            string userId,
            string workSpaceId,
            string folderName,
            string organizationId,
            string description = "")
        {
            var payload = new ObjectArtifactFolderCreateCommand()
            {
                ObjectArtifactId = organizationId,
                ParentId = _systemOrganizationId,
                OrganizationId = organizationId,
                Description = description,
                Name = folderName,
                UserId = userId,
                WorkspaceId = workSpaceId,
                Secured = false,
                Tags = new string[] { "create_folder" },
                IsAOrganizationFolder = true
            };

            return await _dmsService.CreateFolder(payload);
        }


        private async Task CreateRiqsLibraryControlMechanism(PraxisOrganization organization)
        {
            if (!string.IsNullOrEmpty(organization.LibraryControlMechanism))
            {
                var controlMechanism = await _repository
                .GetItemAsync<RiqsLibraryControlMechanism>(
                    i =>
                        i.OrganizationId.Equals(organization.ItemId)
                        && !i.IsMarkedToDelete
                );

                if (controlMechanism == null)
                {
                    controlMechanism = new RiqsLibraryControlMechanism
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        OrganizationId = organization.ItemId,
                        ControlMechanismName = organization.LibraryControlMechanism,
                        ApprovalAdmins = new List<UserPraxisUserIdPair>(),
                        UploadAdmins = new List<UserPraxisUserIdPair>()
                    };

                    await _repository.SaveAsync(controlMechanism);
                    LibraryControlMechanismConstant.ResetLibraryControlMechanism(controlMechanism);
                }
                else if (controlMechanism.ControlMechanismName != organization.LibraryControlMechanism)
                {
                    controlMechanism.ControlMechanismName = organization.LibraryControlMechanism;

                    await _repository.UpdateAsync(o => o.ItemId.Equals(controlMechanism.ItemId), controlMechanism);
                    LibraryControlMechanismConstant.ResetLibraryControlMechanism(controlMechanism);
                    PublishLibraryAdminAssignedEvent(controlMechanism.ItemId);
                }
            }
        }

        private void PublishLibraryAdminAssignedEvent(string itemId)
        {
            var libraryAdminAssignedEvent = new GenericEvent
            {
                EventType = PraxisEventType.LibraryRightsUpdatedEvent,
                JsonPayload = JsonConvert.SerializeObject(itemId)
            };

            _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), libraryAdminAssignedEvent);
        }
    }
}