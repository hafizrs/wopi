using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.Wopi.Contracts.DomainServices;

namespace Selise.Ecap.SC.Wopi.Domain.DomainServices.Services
{
    public class ConnectionService : IConnectionService
    {
        private readonly IBlocksMongoDbDataContextProvider ecapRepository;

        public ConnectionService(IBlocksMongoDbDataContextProvider ecapRepository)
        {
            this.ecapRepository = ecapRepository;
        }

        public async Task<List<Connection>> GetConnections(string ParentEntityId, string[] tags)
        {
            var filter = Builders<Connection>.Filter.In("Tags", tags) &
                         Builders<Connection>.Filter.Eq("ParentEntityID", ParentEntityId);

            return await Task.Run(() =>
            {
                return ecapRepository.GetTenantDataContext().GetCollection<Connection>("Connections").Find(filter)
                    .ToList();
            });
        }

        public async Task<Connection> GetParentEntity(string ChildEntityId, string[] tags)
        {
            var filter = Builders<Connection>.Filter.Eq("ChildEntityID", ChildEntityId)
                         & Builders<Connection>.Filter.In("Tags", tags);

            return await Task.Run(() =>
            {
                return ecapRepository.GetTenantDataContext().GetCollection<Connection>("Connections").Find(filter)
                    .FirstOrDefault();
            });
        }
    }
}
