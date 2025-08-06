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
    public class DeleteCategoryDataForClientAdmin : RevokePermissionBase, IDeleteDataByRoleSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IRepository _repository;
        private readonly ILogger<DeleteCategoryDataForClientAdmin> _logger;
        private readonly ISaveDataToArchivedRole _saveDataToArchivedRoleService;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;

        public DeleteCategoryDataForClientAdmin(
            ISecurityContextProvider securityContextProviderService,
            IRepository repository,
            ILogger<DeleteCategoryDataForClientAdmin> logger,
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
            _logger.LogInformation($"Going to delete {nameof(PraxisClientCategory)} entity SubCategory data with ItemId: {itemId} for Client Admin.");
            try
            {
                var existingCategory = _repository.GetItem<PraxisClientCategory>(c => c.ItemId == itemId);
                if (existingCategory != null)
                {
                    _saveDataToArchivedRoleService.InsertData(existingCategory);
                    UpdatePermissionAndTag(existingCategory);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisClientCategory)} data with ItemId: {itemId} and tenantId: {securityContext.TenantId} for Admin role. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
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
