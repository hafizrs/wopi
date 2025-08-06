using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteRiskDataForClientAdmin : RevokePermissionBase, IDeleteDataByRoleSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteRiskDataForClientAdmin> _logger;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly ISaveDataToArchivedRole _saveDataToArchivedRoleService;

        public DeleteRiskDataForClientAdmin(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteRiskDataForClientAdmin> logger,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            ISaveDataToArchivedRole saveDataToArchivedRoleService)
        {
            _securityContextProviderService = securityContextProviderService;
            _repository = repository;
            _logger = logger;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _saveDataToArchivedRoleService = saveDataToArchivedRoleService;
        }
        public bool DeleteData(string itemId)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation($"Going to delete {nameof(PraxisRisk)} data with ItemId: {itemId} for admin for tenantId:{securityContext.TenantId}.");
            try
            {
                var existingRisk = _repository.GetItem<PraxisRisk>(r => r.ItemId == itemId);
                if (existingRisk != null)
                {
                    RemovePermissionFromTastManagementData(existingRisk.ItemId);

                    var existingAssessments = _repository.GetItems<PraxisAssessment>(a => a.RiskId == existingRisk.ItemId).ToList();
                    foreach (var assessment in existingAssessments)
                    {
                        var existingAssessment = _repository.GetItem<PraxisAssessment>(a => a.ItemId == assessment.ItemId);
                        if (existingAssessment != null)
                        {
                            _saveDataToArchivedRoleService.InsertData(existingAssessment);
                            UpdatePermissionAndTag(existingAssessment);
                        }
                    }

                    _saveDataToArchivedRoleService.InsertData(existingRisk);
                    UpdatePermissionAndTag(existingRisk);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisRisk)} related all data with ItemId: {itemId} and tenantId: {securityContext.TenantId} for Client Admin role. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                return false;
            }
        }

        private void RemovePermissionFromTastManagementData(string taskReferenceId)
        {
            var existingPraxisOpenItemConfig =
                _repository.GetItem<PraxisOpenItemConfig>(oc => oc.TaskReferenceId == taskReferenceId);
            if (existingPraxisOpenItemConfig != null)
            {
                var distinctSummaryList = _repository
                    .GetItems<PraxisOpenItem>(ot => ot.OpenItemConfigId == existingPraxisOpenItemConfig.ItemId)
                    .GroupBy(g => g.TaskSchedule.TaskSummaryId)
                    .ToList();

                var existingPraxisOpenItems = _repository
                    .GetItems<PraxisOpenItem>(ot => ot.OpenItemConfigId == existingPraxisOpenItemConfig.ItemId)
                    .ToList();

                foreach (var praxisOpenItem in existingPraxisOpenItems)
                {
                    var existingTaskSchedule = _repository.GetItem<TaskSchedule>(s => s.ItemId == praxisOpenItem.TaskSchedule.ItemId);
                    if (existingTaskSchedule != null)
                    {
                        _saveDataToArchivedRoleService.InsertData(existingTaskSchedule);
                        UpdatePermissionAndTag(existingTaskSchedule);
                    }
                    _saveDataToArchivedRoleService.InsertData(praxisOpenItem);
                    UpdatePermissionAndTag(praxisOpenItem);
                }
                foreach (var summary in distinctSummaryList)
                {
                    var existingTaskSummary = _repository.GetItem<TaskSummary>(s => s.ItemId == summary.Key);
                    if (existingTaskSummary != null)
                    {
                        _saveDataToArchivedRoleService.InsertData(existingTaskSummary);
                        UpdatePermissionAndTag(existingTaskSummary);
                    }
                }
                var existingOpenItemConfig = _repository.GetItem<PraxisOpenItemConfig>(oc => oc.ItemId == existingPraxisOpenItemConfig.ItemId);
                if (existingOpenItemConfig != null)
                {
                    _saveDataToArchivedRoleService.InsertData(existingOpenItemConfig);
                    UpdatePermissionAndTag(existingOpenItemConfig);
                }
            }
        }

        private Dictionary<string, List<string>> PrepareNewPermission()
        {
            var rolesToAllow = new List<string> { "admin" };
            return new Dictionary<string, List<string>>
            {
                {nameof(EntityBase.IdsAllowedToRead), new List<string>()},
                {nameof(EntityBase.RolesAllowedToRead), rolesToAllow}
            };
        }

        private string[] AddNewTag(string[] tags, string newTag)
        {
            if (tags == null)
            {
                return new[] { newTag };
            }
            var newTags = tags.ToList();
            newTags.Add(newTag);
            return newTags.ToArray();
        }

        public override void UpdatePermissionAndTag(EntityBase entity)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            var db = _ecapMongoDbDataContextProvider.GetTenantDataContext(securityContext.TenantId.Trim());

            RevokePermissionBase.RevokePermissionFromEntity(entity, PrepareNewPermission());
            entity.Tags = AddNewTag(entity.Tags, "Is-Deleted-Record");

            var filter = Builders<BsonDocument>.Filter.Eq("_id", entity.ItemId);
            var update = Builders<BsonDocument>.Update
                .Set(nameof(EntityBase.IdsAllowedToRead), entity.IdsAllowedToRead)
                .Set(nameof(EntityBase.RolesAllowedToRead), entity.RolesAllowedToRead)
                .Set(nameof(EntityBase.Tags), entity.Tags);

            db.GetCollection<BsonDocument>($"{entity.GetType().Name}s").UpdateOne(filter, update);
        }
    }
}
