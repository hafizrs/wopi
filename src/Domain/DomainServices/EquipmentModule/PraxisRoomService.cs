using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisRoomService : IPraxisRoomService, IDeleteDataForClientInCollections
    {
        private readonly IRepository _repository;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly ILogger<PraxisRoomService> _logger;

        public PraxisRoomService(
            IRepository repository,
            IMongoSecurityService mongoSecurityService,
            ILogger<PraxisRoomService> logger
        )
        {
            _repository = repository;
            _mongoSecurityService = mongoSecurityService;
            _logger = logger;
        }

        public void AddRowLevelSecurity(string itemId, string clientId)
        {
            var clientAdminAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, clientId);
            var clientReadAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, clientId);
            var clientManagerAccessRole =
                _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, clientId);

            var permission = new EntityReadWritePermission
            {
                Id = Guid.Parse(itemId)
            };

            permission.RolesAllowedToRead.Add(clientAdminAccessRole);
            permission.RolesAllowedToRead.Add(clientManagerAccessRole);
            permission.RolesAllowedToRead.Add(clientReadAccessRole);

            permission.RolesAllowedToUpdate.Add(clientAdminAccessRole);
            permission.RolesAllowedToUpdate.Add(clientReadAccessRole);

            _mongoSecurityService.UpdateEntityReadWritePermission<PraxisRoom>(permission);
        }

        public List<PraxisRoom> GetAllPraxisRoom()
        {
            throw new NotImplementedException();
        }

        public PraxisRoom GetPraxisRoom(string itemId)
        {
            throw new NotImplementedException();
        }

        public void RemoveRowLevelSecurity(string clientId)
        {
            throw new NotImplementedException();
        }

        public void UpdatePraxisRoom(string itemId)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteDataForClient(string clientId, string orgId = null)
        {
            _logger.LogInformation("Going to delete {PraxisRoom} for client {ClientId}", nameof(PraxisRoom), clientId);

            try
            {
                await _repository.DeleteAsync<PraxisRoom>(room => room.ClientId.Equals(clientId));
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while trying to delete {PraxisRoom} for client {ClientId}. Error: {Message}. Stacktrace: {StackTrace}", nameof(PraxisRoom), clientId, e.Message, e.StackTrace);
            }
        }

        public List<PraxisRoom> GetPraxisRoomsByIds(List<string> roomsIds)
        {
            var praxisRooms = _repository.GetItems<PraxisRoom>(p => !p.IsMarkedToDelete && roomsIds.Contains(p.ItemId))?.ToList() ?? new List<PraxisRoom>();
            return praxisRooms;
        }
    }
}