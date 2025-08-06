using System;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.ClientEvents
{
    public class PraxisOrganizationUpdatedEventHandler : IBaseEventHandlerAsync<GqlEvent<PraxisOrganization>>
    {
        private readonly ILogger<PraxisOrganizationUpdatedEventHandler> _logger;
        private readonly IRepository _repository;
        private readonly IChangeLogService _changeLogService;

        public PraxisOrganizationUpdatedEventHandler(
            ILogger<PraxisOrganizationUpdatedEventHandler> logger,
            IRepository repository,
            IChangeLogService changeLogService)
        {
            _logger = logger;
            _repository = repository;
            _changeLogService = changeLogService;
        }

        public async Task<bool> HandleAsync(GqlEvent<PraxisOrganization> eventPayload)
        {
            _logger.LogInformation("Entered into the event handler {HandlerName}.", nameof(PraxisOrganizationUpdatedEventHandler));
            bool response = false;
            try
            {
                string orgId = eventPayload.Filter;
                await ProcessChangeEffects(orgId, eventPayload.EntityData);
                response = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception in the event handler {nameof(PraxisOrganizationUpdatedEventHandler)}." +
                    $"Exception Message: {ex.Message}." +
                    $" Exception detaiils: {ex.StackTrace}.");
            }
            _logger.LogInformation("Handled by the event handler {HandlerName}.", nameof(PraxisOrganizationUpdatedEventHandler));
            return response;
        }

        private async Task ProcessChangeEffects(string orgId, PraxisOrganization orgData)
        {
            var departments = GetDepartments(orgId);
            if (orgData.Address != null)
            {
                await UpdateDepartmentAddress(departments, orgData.Address);
            }
            await UpdateDepartmentOrganizationName(departments, orgData.ClientName);
            await UpdatePraxisUserOrganizationName(orgId, orgData.ClientName);
            await UpdateClientSubscriptionOrganizationName(orgId, orgData.ClientName);
        }

        private async Task UpdateDepartmentAddress(List<PraxisClient> departments, PraxisAddress orgAddress)
        {
            var targetedDepartmentIds = 
                departments.Where(d => d.IsSameAddressAsParentOrganization).Select(d => d.ItemId).ToList();
            var filterBuilder = Builders<BsonDocument>.Filter;
            var updateFilters = filterBuilder.In("_id", targetedDepartmentIds);

            var updates = new Dictionary<string, object>
            {
                {"Address",  orgAddress}
            };

            await _changeLogService.UpdateChange(EntityName.PraxisClient, updateFilters, updates);
        }

        private async Task UpdateDepartmentOrganizationName(List<PraxisClient> departments, string orgName)
        {
            var targetedDepartmentIds = departments.Select(d => d.ItemId).ToList();

            var filterBuilder = Builders<BsonDocument>.Filter;
            var updateFilters = filterBuilder.In("_id", targetedDepartmentIds);

            var updates = new Dictionary<string, object>
            {
                {"ParentOrganizationName",  orgName}
            };

            await _changeLogService.UpdateChange(EntityName.PraxisClient, updateFilters, updates);
        }

        private async Task UpdatePraxisUserOrganizationName(string orgId, string orgName)
        {
            var targetedPraxisUsers = _repository.GetItems<PraxisUser>
                        (x => !x.IsMarkedToDelete && x.ClientList != null
                        && x.ClientList.Any(c => c.ParentOrganizationId == orgId)).ToList();

            foreach (var praxisUser in targetedPraxisUsers)
            {
                foreach (var client in praxisUser.ClientList)
                {
                    if (client.ParentOrganizationId == orgId)
                    {
                        client.ParentOrganizationName = orgName;
                    }
                }
                var filterBuilder = Builders<BsonDocument>.Filter;
                var updateFilters = filterBuilder.Eq("_id", praxisUser.ItemId);

                var updates = new Dictionary<string, object>
                {
                    {"ClientList",  praxisUser.ClientList}
                };

                await _changeLogService.UpdateChange(EntityName.PraxisUser, updateFilters, updates);
            }
        }

        private async Task UpdateClientSubscriptionOrganizationName(string orgId, string orgName)
        {
            var updateData = new Dictionary<string, object>
            {
                {"OrganizationName",  orgName}
            };

            await _repository.UpdateManyAsync<PraxisClientSubscription>(c => !c.IsMarkedToDelete && c.OrganizationId == orgId, updateData);
        }

        private List<PraxisClient> GetDepartments(string orgId)
        {
            return _repository.GetItems<PraxisClient>(p => p.ParentOrganizationId == orgId && !p.IsMarkedToDelete).ToList();
        }
    }
}