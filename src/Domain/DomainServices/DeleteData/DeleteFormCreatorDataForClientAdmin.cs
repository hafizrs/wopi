using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteFormCreatorDataForClientAdmin : RevokePermissionBase, IDeleteDataByRoleSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteFormCreatorDataForClientAdmin> _logger;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly ISaveDataToArchivedRole _saveDataToArchivedRoleService;

        public DeleteFormCreatorDataForClientAdmin(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteFormCreatorDataForClientAdmin> logger,
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
            _logger.LogInformation($"Going to delete {nameof(PraxisForm)} data with ItemId: {itemId} for client admin.");

            try
            {
                var existingForm = _repository.GetItem<PraxisForm>(f => f.ItemId == itemId);
                if (existingForm != null)
                {
                    _saveDataToArchivedRoleService.InsertData(existingForm);
                    UpdatePermissionAndTag(existingForm);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisForm)} entity data for FormId:{itemId}. Exception Message: {ex.Message}. Exception Details: {ex.StackTrace}.");
                return false;
            }
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
            var rolesToAllow = new List<string> { "admin", "task_controller" };
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
