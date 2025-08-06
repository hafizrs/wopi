using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ChangeEvents;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;
namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.ClientEvents
{
    public class PraxisClientUpdatedEventHandler : IBaseEventHandlerAsync<GqlEvent<PraxisClient>>
    {
        private readonly ILogger<PraxisClientUpdatedEventHandler> _logger;
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
        private readonly IChangeLogService _changeLogService;
        private readonly IUserCountMaintainService _userCountMaintainService;
        private readonly IRepository _repository;
        private readonly ICirsPermissionService _cirsPermissionsService;
        private readonly IServiceClient _serviceClient;
        private readonly IPraxisClientSubscriptionService _praxisClientSubscriptionService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

        public PraxisClientUpdatedEventHandler(
            ILogger<PraxisClientUpdatedEventHandler> logger,
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider,
            IChangeLogService changeLogService,
            IUserCountMaintainService userCountMaintainService,
            IRepository repository,
            ICirsPermissionService cirsPermissionsService,
            IServiceClient serviceClient,
            IPraxisClientSubscriptionService praxisClientSubscriptionService,
            ICockpitSummaryCommandService cockpitSummaryCommandService
        )
        {
            _logger = logger;
            _mongoDbDataContextProvider = mongoDbDataContextProvider;
            _changeLogService = changeLogService;
            _userCountMaintainService = userCountMaintainService;
            _repository = repository;
            _cirsPermissionsService = cirsPermissionsService;
            _serviceClient = serviceClient;
            _praxisClientSubscriptionService = praxisClientSubscriptionService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
        }

        public async Task<bool> HandleAsync(GqlEvent<PraxisClient> eventPayload)
        {
            try
            {
                if (eventPayload.EventData != null)
                {
                    PraxisClientChangeEvent clientChanges = JsonConvert.DeserializeObject<PraxisClientChangeEvent>(eventPayload.EventData);

                    if (clientChanges != null)
                    {
                        await _userCountMaintainService.UpdateOrganizationLevelUserCount(clientChanges.ItemId);
                        await _praxisClientSubscriptionService.SaveClientSubscriptionOnClientCreateUpdate(clientChanges.ItemId);
                        await UpdateDependencies(eventPayload?.Filter);
                        _ = ProcessClientChanges(clientChanges);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during in {EventHandlerName}", nameof(PraxisClientUpdatedEventHandler));
                _logger.LogError("Exception Message: {ExceptionMessage} Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }

            return false;
        }

        private bool ProcessClientChanges(PraxisClientChangeEvent changes)
        {
            try
            {
                var clientNameUpdateStatuses = new List<Task<bool>>();

                if (!string.IsNullOrEmpty(changes.Name) && !string.IsNullOrEmpty(changes.ItemId))
                {
                    var updates = new Dictionary<string, object>
                    {
                        { "ClientName", changes.Name }
                    };

                    var builders = Builders<BsonDocument>.Filter;
                    var dataFilters = builders.Eq("ClientId", changes.ItemId);

                    #region ClientName update in Training & Answer, Equipment

                    var praxisTrainingUpdateStatus = _changeLogService.UpdateChange(
                        EntityName.PraxisTraining,
                        dataFilters,
                        updates
                    );
                    clientNameUpdateStatuses.Add(praxisTrainingUpdateStatus);

                    var praxisEquipmentUpdateStatus =
                        _changeLogService.UpdateChange(EntityName.PraxisEquipment, dataFilters, updates);
                    clientNameUpdateStatuses.Add(praxisEquipmentUpdateStatus);

                    var praxisTrainingAnswerStatus =
                        _changeLogService.UpdateChange(EntityName.PraxisTrainingAnswer, dataFilters, updates);
                    clientNameUpdateStatuses.Add(praxisTrainingAnswerStatus);

                    var praxisUserStatus =
                        _changeLogService.UpdateChange(EntityName.PraxisUser, dataFilters, updates);
                    clientNameUpdateStatuses.Add(praxisUserStatus);

                    #endregion

                    #region ClientName update in PraxisUser, Person,

                    var filter = Builders<PraxisUser>.Filter.Eq("ClientList.ClientId", changes.ItemId);

                    var praxisUsers = _mongoDbDataContextProvider.GetTenantDataContext()
                        .GetCollection<PraxisUser>("PraxisUsers")
                        .Find(filter)
                        .ToList();
                    foreach (var praxisUser in praxisUsers)
                    {
                        praxisUser.ClientList.FirstOrDefault(c => c.ClientId == changes.ItemId)!.ClientName = changes.Name;

                        var update = Builders<PraxisUser>.Update
                            .Set("ClientList", praxisUser.ClientList);

                        var updateFilter = Builders<PraxisUser>.Filter.Eq("_id", praxisUser.ItemId);
                        _mongoDbDataContextProvider.GetTenantDataContext()
                            .GetCollection<PraxisUser>("PraxisUsers")
                            .UpdateOne(updateFilter, update);

                        if (!praxisUser.ClientList.Any())
                        {
                            var personFilter = Builders<Person>.Filter.Eq("_id", praxisUser.ItemId);
                            var person = _mongoDbDataContextProvider.GetTenantDataContext()
                                .GetCollection<Person>("Persons")
                                .Find(personFilter)
                                .FirstOrDefault();
                            if (person != null)
                            {
                                person.Organization = changes.Name;

                                var personUpdate = Builders<Person>.Update
                                    .Set("Organization", person.Organization);
                                var personUpdateFilter = Builders<Person>.Filter.Eq("_id", person.ItemId);
                                _mongoDbDataContextProvider.GetTenantDataContext()
                                    .GetCollection<Person>("Persons")
                                    .UpdateOne(personUpdateFilter, personUpdate);
                            }
                        }
                    }

                    #endregion

                    #region ClientName update in ProcessGuide & PraxisForm

                    var pgFilter = Builders<PraxisProcessGuide>.Filter.Eq("Clients.ClientId", changes.ItemId);
                    var processGuides = _mongoDbDataContextProvider.GetTenantDataContext()
                        .GetCollection<PraxisProcessGuide>("PraxisProcessGuides")
                        .Find(pgFilter)
                        .ToList();

                    foreach (var processGuide in processGuides)
                    {
                        if (processGuide.Clients != null)
                        {
                            processGuide.Clients.FirstOrDefault(cl => cl.ClientId == changes.ItemId)!.ClientName =
                            changes.Name;
                        }
                        if (processGuide.ClientCompletionInfo != null)
                        {
                            processGuide.ClientCompletionInfo.FirstOrDefault(cl => cl.ClientId == changes.ItemId)!
                            .ClientName = changes.Name;
                        }

                        var pgUpdate = Builders<PraxisProcessGuide>.Update
                            .Set("Clients", processGuide.Clients)
                            .Set("ClientCompletionInfo", processGuide.ClientCompletionInfo);

                        var pgUpdateFilter = Builders<PraxisProcessGuide>.Filter.Eq("_id", processGuide.ItemId);
                        _mongoDbDataContextProvider.GetTenantDataContext()
                            .GetCollection<PraxisProcessGuide>("PraxisProcessGuides")
                            .UpdateOne(pgUpdateFilter, pgUpdate);
                    }

                    var formFilter = Builders<PraxisForm>.Filter.Eq("ProcessGuideCheckList.ClientId", changes.ItemId);
                    var praxisForms = _mongoDbDataContextProvider.GetTenantDataContext()
                        .GetCollection<PraxisForm>("PraxisForms")
                        .Find(formFilter)
                        .ToList();

                    foreach (var praxisForm in praxisForms)
                    {
                        praxisForm.ProcessGuideCheckList.FirstOrDefault(cl => cl.ClientId == (changes.ItemId))!
                            .ClientName = changes.Name;

                        var pfUpdate = Builders<PraxisForm>.Update
                            .Set("ProcessGuideCheckList", praxisForm.ProcessGuideCheckList);

                        var pgUpdateFilter = Builders<PraxisForm>.Filter.Eq("_id", praxisForm.ItemId);
                        _mongoDbDataContextProvider.GetTenantDataContext()
                            .GetCollection<PraxisForm>("PraxisUsers")
                            .UpdateOne(pgUpdateFilter, pfUpdate);
                    }

                    #endregion
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task UpdateDependencies(string clientId)
        {
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                var filter = Builders<PraxisClient>.Filter.Eq("_id", clientId);
                var clients = _mongoDbDataContextProvider.GetTenantDataContext()
                    .GetCollection<PraxisClient>("PraxisClients")
                    .Find(filter)
                    .ToList();
                var client = clients?.Count > 0 ? clients[0] : null;
                if (client != null)
                {
                    await UpdateCirsReportDependencies(client);
                    await UpdateMaintenanceDependencies(client);
                    await UpdateCirsReportCockpitDependencies(client.ItemId);
                }
            }
        }

        private async Task UpdateMaintenanceDependencies(PraxisClient client)
        {
            var supplierIds = client?.AdditionalInfos?.Select(c => c.ItemId)?.ToList() ?? new List<string>();
            if (supplierIds?.Count > 0)
            {
                var filter = Builders<PraxisEquipmentMaintenance>.Filter.Eq("ClientId", client.ItemId) &
                             Builders<PraxisEquipmentMaintenance>.Filter.In("ExternalUserInfos.SupplierInfo.SupplierId", supplierIds.ToArray());

                var maintenances = _mongoDbDataContextProvider.GetTenantDataContext()
                    .GetCollection<PraxisEquipmentMaintenance>("PraxisEquipmentMaintenances")
                    .Find(filter)
                    .ToList();

                foreach (var maintenance in maintenances)
                {
                    foreach (var supplier in (maintenance.ExternalUserInfos ?? new List<PraxisEquipmentMaintenanceByExternalUser>()))
                    {
                        if (supplier?.SupplierInfo != null)
                        {
                            var clientSupplier = client?.AdditionalInfos?.FirstOrDefault(c => c.ItemId == supplier.SupplierInfo.SupplierId);
                            if (clientSupplier != null)
                            {
                                supplier.SupplierInfo.SupplierName = clientSupplier.Name;
                            }
                        }
                    }
                    var updates = new Dictionary<string, object>
                    {
                        { "ExternalUserInfos", maintenance.ExternalUserInfos }
                    };
                    var builders = Builders<BsonDocument>.Filter;
                    var dataFilters = builders.Eq("_id", maintenance.ItemId);

                    await _changeLogService.UpdateChange(EntityName.PraxisEquipmentMaintenance, dataFilters, updates);
                }
            }
        }

        private async Task UpdateCirsReportDependencies(PraxisClient client)
        {
            var permissions = _repository.GetItems<CirsDashboardPermission>(c => c.PraxisClientId == client.ItemId).ToList();
            var dashboardPermissionIds = new List<string>();
            foreach (var permission in permissions)
            {
                var assignmentLevel = _cirsPermissionsService
                    .GetAssignmentLevelByDashboardName(permission.CirsDashboardName, client.CirsReportConfig) ?? AssignmentLevel.None;
                if (permission.AssignmentLevel != assignmentLevel || permission.AssignmentLevel == AssignmentLevel.None)
                {
                    await _repository.DeleteAsync<CirsDashboardPermission>(c => c.ItemId == permission.ItemId);
                    dashboardPermissionIds.Add(permission.ItemId);
                }
            }

            await UpdateCirsGenericReport(client);

            PublishUpdateCirsAssignedAdminForCockpitEvent(dashboardPermissionIds);
        }

        private Task UpdateCirsGenericReport(PraxisClient client)
        {
            try
            {
                var visibilityInfo = client?.MetaDataList?.FirstOrDefault(m => m.Key == "CirsReportVisibilityMode")?.MetaData?.Value;
                string visibility = null;
                if (!string.IsNullOrEmpty(visibilityInfo))
                {
                    var visibilityDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(visibilityInfo);
                    if (visibilityDict.TryGetValue("value", out var visibilityValue))
                    {
                        if (visibilityValue is long longValue)
                        {
                            var intValue = (int)longValue;
                            if (System.Enum.IsDefined(typeof(ReportingVisibility), intValue))
                            {
                                visibility = ((ReportingVisibility)intValue).ToString();
                            }
                        }
                        else if (visibilityValue is int intValue)
                        {
                            if (System.Enum.IsDefined(typeof(ReportingVisibility), intValue))
                            {
                                visibility = ((ReportingVisibility)intValue).ToString();
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(visibility))
                {
                    var cirsReports = _repository.GetItems<CirsGenericReport>(c => 
                        !c.IsMarkedToDelete && c.CirsDashboardName == CirsDashboardName.Incident &&
                        (c.MetaData == null || c.MetaData.ContainsKey($"{CommonCirsMetaKey.ReportingVisibility}") == false 
                        || c.MetaData[$"{CommonCirsMetaKey.ReportingVisibility}"] == null || c.MetaData[$"{CommonCirsMetaKey.ReportingVisibility}"] as string != visibility)
                    )?.ToList() ?? new List<CirsGenericReport>();

                    cirsReports.ForEach(async report =>
                    {
                        report.MetaData ??= new Dictionary<string, object>();
                        report.MetaData[$"{CommonCirsMetaKey.ReportingVisibility}"] = visibility;
                        _cirsPermissionsService.SetCirsReportPermission(report);

                        var updates = new Dictionary<string, object>
                        {
                            { nameof(CirsGenericReport.MetaData), report.MetaData },
                            { nameof(CirsGenericReport.RolesAllowedToRead), report.RolesAllowedToRead },
                            { nameof(CirsGenericReport.IdsAllowedToRead), report.IdsAllowedToRead },
                        };

                        var builders = Builders<BsonDocument>.Filter;
                        var dataFilters = builders.Eq("_id", report.ItemId);

                        await _changeLogService.UpdateChange(EntityName.CirsGenericReport, dataFilters, updates);
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in UpdateCirsGenericReport {message}", ex.Message);
            }

            return Task.CompletedTask;
        }

        private async Task UpdateCirsReportCockpitDependencies(string clientId)
        {
            var cockpitReports = _repository
                .GetItems<RiqsTaskCockpitSummary>(c =>
                    c.IsMarkedToDelete == false &&
                    c.IsSummaryHidden == false &&
                    c.RelatedEntityName == CockpitTypeNameEnum.CirsGenericReport &&
                    c.DepartmentDetails != null && c.DepartmentDetails.Any(p => p.DepartmentId == clientId))?
                .Select(c => c.RelatedEntityId)?
                .ToList() ?? new List<string>();

            foreach (var id in cockpitReports)
            {
                await _cockpitSummaryCommandService.CreateSummary(id, EntityName.CirsGenericReport, true);
            }
        }

        private void PublishUpdateCirsAssignedAdminForCockpitEvent(List<string> dashboardPermissionIds)
        {
            var updateCirsAssignedAdminForCockpitEvent = new GenericEvent
            {
                EventType = PraxisEventType.UpdateCirsAssignedAdminForCockpitEvent,
                JsonPayload = JsonConvert.SerializeObject(new UpdateCirsAssignedAdminForCockpitEventModel
                {
                    DashboardPermissionIds = dashboardPermissionIds
                })
            };

            _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), updateCirsAssignedAdminForCockpitEvent);
        }
    }
}