using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.ClientEvents;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.OrganizationEvents
{
    public class PraxisOrganizationCreatedEventHandler : IBaseEventHandlerAsync<GqlEvent<PraxisOrganization>>
    {
        private readonly ILogger<PraxisOrganizationCreatedEventHandler> _logger;
        private readonly IRepository _repository;
        private readonly IPrepareNewRole _prepareNewRoleService;

        public PraxisOrganizationCreatedEventHandler(
            ILogger<PraxisOrganizationCreatedEventHandler> logger,
            IRepository repository,
            IPrepareNewRole prepareNewRoleService)
        {
            _logger = logger;
            _repository = repository;
            _prepareNewRoleService = prepareNewRoleService;
        }

        public async Task<bool> HandleAsync(GqlEvent<PraxisOrganization> eventPayload)
        {
            try
            {
                await ProcessChangeEffects(eventPayload.EntityData);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"PraxisOrganizationCreatedEventHandler -> {e.Message}");
            }

            return false;
        }

        private async Task ProcessChangeEffects(PraxisOrganization orgData)
        {
            var orgDynamicRole = CreateOrgDynamicRole(orgData.ItemId);
            await UpdateOrgDataPermissions(orgData.ItemId, orgDynamicRole);
        }

        private string CreateOrgDynamicRole(string orgId)
        {
            var role = _prepareNewRoleService.SaveRole(
                $"{RoleNames.AdminB_Dynamic}_{orgId}",
                orgId,
                RoleNames.AdminB,
                true);
            return role;
        }

        private async Task UpdateOrgDataPermissions(string orgId, string orgDynamicRole)
        {
            var orgData = GetOrganization(orgId);

            var rolesAllowedToRead = new List<string>();
            rolesAllowedToRead.AddRange(orgData.RolesAllowedToRead);
            rolesAllowedToRead.Add(orgDynamicRole);
            orgData.RolesAllowedToRead = rolesAllowedToRead.Distinct().ToArray();

            await _repository.UpdateAsync(o => o.ItemId.Equals(orgId), orgData);
        }

        private PraxisOrganization GetOrganization(string id)
        {
            return _repository.GetItem<PraxisOrganization>(p => p.ItemId == id);
        }
    }
}
