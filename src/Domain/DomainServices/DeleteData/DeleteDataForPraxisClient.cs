using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataForPraxisClient : IDeleteDataByCollectionSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteDataForPraxisClient> _logger;
        private readonly PraxisEquipmentService _equipmentService;
        private readonly PraxisFormService _formService;
        private readonly PraxisOpenItemService _openItemService;
        private readonly PraxisRiskService _riskService;
        private readonly PraxisRoomService _roomService;
        private readonly PraxisTaskService _taskService;
        private readonly PraxisTrainingAnswerService _trainingAnswerService;
        private readonly PraxisTrainingService _trainingService;
        private readonly PraxisProcessGuideService _processGuideService;
        private readonly PraxisUserService _praxisUserService;
        private readonly PraxisClientCategoryService _categoryService;
        private readonly IDmsService _dmsService;
        private readonly IPraxisClientService _praxisClientService;
        private readonly IUserCountMaintainService _userCountMaintainService;
        private readonly IPraxisShiftService _praxisShiftService;
        private readonly IDeleteCirsReportsService _deleteCirsReportsService;
        private readonly IDeleteDmsArtifactUsageReferenceService _deleteDmsArtifactUsageReferenceService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly IQuickTaskService _quickTaskService;


        public DeleteDataForPraxisClient(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteDataForPraxisClient> logger,
            PraxisEquipmentService equipmentService,
            PraxisFormService formService,
            PraxisOpenItemService openItemService,
            PraxisRiskService riskService,
            PraxisRoomService roomService,
            PraxisTaskService taskService,
            PraxisTrainingAnswerService trainingAnswerService,
            PraxisTrainingService trainingService,
            PraxisProcessGuideService processGuideService,
            PraxisUserService praxisUserService,
            PraxisClientCategoryService categoryService,
            IDmsService dmsService,
            IPraxisClientService praxisClientService,
            IUserCountMaintainService userCountMaintainService,
            IPraxisShiftService praxisShiftService,
            IDeleteCirsReportsService deleteCirsReportsService,
            IDeleteDmsArtifactUsageReferenceService deleteDmsArtifactUsageReferenceService,
            ICockpitSummaryCommandService cockpitSummaryCommandService,
            IQuickTaskService quickTaskService
        )
        {
            _securityContextProviderService = securityContextProviderService;
            _repository = repository;
            _logger = logger;
            _riskService = riskService;
            _roomService = roomService;
            _openItemService = openItemService;
            _equipmentService = equipmentService;
            _trainingService = trainingService;
            _formService = formService;
            _taskService = taskService;
            _trainingAnswerService = trainingAnswerService;
            _processGuideService = processGuideService;
            _praxisUserService = praxisUserService;
            _categoryService = categoryService;
            _dmsService = dmsService;
            _praxisClientService = praxisClientService;
            _userCountMaintainService = userCountMaintainService;
            _praxisShiftService = praxisShiftService;
            _deleteCirsReportsService = deleteCirsReportsService;
            _deleteDmsArtifactUsageReferenceService = deleteDmsArtifactUsageReferenceService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _quickTaskService = quickTaskService;
        }

        public async Task<bool> DeleteData(string entityName, string itemId, string additionalInfosItemId = null, string orgId = null)
        {
            if (entityName.Equals(nameof(PraxisUserAdditionalInfo)))
            {
                return _praxisClientService.DeletePraxisUserAdditionalInfo(additionalInfosItemId, itemId);
            }
            else if (string.IsNullOrEmpty(additionalInfosItemId))
            {
                var securityContext = _securityContextProviderService.GetSecurityContext();
                _logger.LogInformation(
                    $"Going to delete {nameof(PraxisClient)} data with ItemId: {itemId}" +
                    $" and tenantId: {securityContext.TenantId}."
                );

                var praxisClient = await _repository.GetItemAsync<PraxisClient>(c => c.ItemId.Equals(itemId));

                await DeleteClientFolder(itemId);

                var deleteTasks = new List<Task>
                {
                    _praxisUserService.DeleteDataForClient(itemId, orgId),
                    _equipmentService.DeleteDataForClient(itemId, orgId),
                    _formService.DeleteDataForClient(itemId, orgId),
                    _openItemService.DeleteDataForClient(itemId, orgId),
                    _processGuideService.DeleteDataForClient(itemId),
                    _riskService.DeleteDataForClient(itemId, orgId),
                    _roomService.DeleteDataForClient(itemId, orgId),
                    _taskService.DeleteDataForClient(itemId, orgId),
                    _trainingAnswerService.DeleteDataForClient(itemId, orgId),
                    _trainingService.DeleteDataForClient(itemId, orgId),
                    _categoryService.DeleteDataForClient(itemId, orgId),
                    _praxisShiftService.DeleteDataForClient(itemId, orgId),
                    _quickTaskService.DeleteDataForClient(itemId, orgId),
                    _deleteCirsReportsService.DeleteDataForClient(itemId, orgId),
                    _deleteDmsArtifactUsageReferenceService.DeleteDataForClient(itemId, orgId),
                    _cockpitSummaryCommandService.DeleteDataForClient(itemId, orgId),
                    _repository.DeleteAsync<PraxisClient>(client => client.ItemId.Equals(itemId)),
                    _repository.DeleteAsync<FeatureRoleMap>(featureRoleMap => featureRoleMap.RoleName.Contains(itemId)),
                };

                await Task.WhenAll(deleteTasks);

                await _userCountMaintainService.UpdateOrganizationLevelUserCount(itemId, praxisClient.ParentOrganizationId);

                return true;
            }
            else
            {
                return DeleteDataForSupplier(itemId, additionalInfosItemId);
            }
        }

        private bool DeleteDataForSupplier(string clientId, string additionalInfosItemId)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation(
                $"Going to delete {nameof(PraxisClient)} supplier data with ItemId: {clientId} and " +
                $"AdditionalInfosItemId: {additionalInfosItemId} and tenantId: {securityContext.TenantId}."
            );
            try
            {
                var existingClient = _repository.GetItem<PraxisClient>(c => c.ItemId == clientId);
                if (existingClient != null)
                {
                    var additionalInfos = existingClient.AdditionalInfos.Where(a => a.ItemId != additionalInfosItemId)
                        .ToList();
                    existingClient.AdditionalInfos = additionalInfos;

                    _repository.Update(c => c.ItemId == existingClient.ItemId, existingClient);
                    _logger.LogInformation(
                        $"Supplier information has been successfully remove from {nameof(PraxisClient)} entity " +
                        $"with ItemId: {existingClient.ItemId}."
                    );
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception occured during remove supplier information from {nameof(PraxisClient)} entity" +
                    $"with ItemId: {clientId} and tenantId: {securityContext.TenantId}." +
                    $"Exception Message: {ex.Message}. Exception details: {ex.StackTrace}."
                );
                return false;
            }
        }
        private async Task DeleteClientFolder(string clientId)
        {
            try
            {
                _logger.LogInformation("Start to delete folder for client delete for clientId -> {ClientId}", clientId);
                var artifacts = _repository.GetItems<ObjectArtifact>
                    (c => c.MetaData != null && c.MetaData.ContainsKey("DepartmentId") && c.MetaData["DepartmentId"].Value == clientId)?.ToList() ?? new List<ObjectArtifact>();

                var deleteTasks = artifacts.Select(artifact =>
                    _dmsService.DeleteObjectArtifact(artifact.ItemId, artifact.OrganizationId)
                );

                await Task.WhenAll(deleteTasks);
            }
            catch(Exception ex)
            {
                _logger.LogError($"Exception occured in delete folder on client delete with error -> {ex.Message} trace -> {ex.StackTrace}");
            }
        }
    }
}