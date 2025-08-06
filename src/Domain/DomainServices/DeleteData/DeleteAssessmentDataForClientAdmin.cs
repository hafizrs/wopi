using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteAssessmentDataForClientAdmin : RevokePermissionBase, IDeleteDataByRoleSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteAssessmentDataForClientAdmin> _logger;
        private readonly ISaveDataToArchivedRole _saveDataToArchivedRoleService;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;

        public DeleteAssessmentDataForClientAdmin(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteAssessmentDataForClientAdmin> logger,
            ISaveDataToArchivedRole saveDataToArchivedRoleService,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider)
        {
            _securityContextProviderService = securityContextProviderService;
            _repository = repository;
            _logger = logger;
            _saveDataToArchivedRoleService = saveDataToArchivedRoleService;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
        }
        public bool DeleteData(string itemId)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation($"Going to delete {nameof(PraxisAssessment)} data with ItemId: {itemId} for client admin.");

            try
            {
                var existingAssessment = _repository.GetItem<PraxisAssessment>(a => a.ItemId == itemId);
                if (existingAssessment != null)
                {
                    var assessmentList = _repository.GetItems<PraxisAssessment>(a => a.RiskId == existingAssessment.RiskId)
                        .ToList();
                    if (assessmentList.Count > 1)
                    {
                        var existingRisk = _repository.GetItem<PraxisRisk>(r => r.ItemId == existingAssessment.RiskId);
                        if (existingRisk.RecentAssessment.ItemId == itemId)
                        {
                            var recentAssessment = assessmentList.Where(a => a.ItemId != existingAssessment.ItemId)
                                .OrderByDescending(o => o.CreateDate).FirstOrDefault();
                            if (recentAssessment != null)
                            {
                                RemoveAssessmentAndUpDateRiskData(existingRisk.ItemId, existingAssessment,
                                    recentAssessment);
                            }
                        }
                        else
                        {
                            _saveDataToArchivedRoleService.InsertData(existingAssessment);
                            UpdatePermissionAndTag(existingAssessment);
                        }
                    }
                }
                _logger.LogInformation($"Data has been successfully deleted from {nameof(PraxisAssessment)} entity with ItemId: {itemId} for tenantId: {securityContext.TenantId}.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisAssessment)} related all data with ItemId: {itemId} and tenantId: {securityContext.TenantId} for Admin role. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                return false;
            }
        }
        private void RemoveAssessmentAndUpDateRiskData(string riskItemId, PraxisAssessment deleteAssessment, PraxisAssessment assessment)
        {
            var existingRisk = _repository.GetItem<PraxisRisk>(r => r.ItemId == riskItemId);
            if (existingRisk != null)
            {
                existingRisk.RecentAssessment = assessment;
                _repository.Update<PraxisRisk>(r => r.ItemId == existingRisk.ItemId, existingRisk);
            }
            _saveDataToArchivedRoleService.InsertData(deleteAssessment);
            UpdatePermissionAndTag(deleteAssessment);
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
    }
}
