using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteUserDataForClientAdmin : RevokePermissionBase, IDeleteDataByRoleSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;

        public DeleteUserDataForClientAdmin(
            ISecurityContextProvider securityContextProviderService,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider
            )
        {
            _securityContextProviderService = securityContextProviderService;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
        }
        public bool DeleteData(string itemId)
        {
            return true;
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
