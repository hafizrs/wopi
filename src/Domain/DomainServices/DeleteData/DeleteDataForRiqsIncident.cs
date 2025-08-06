using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SLPC;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataForRiqsIncident : IDeleteDataByCollectionSpecific
    {
        private readonly ILogger<DeleteDataForRiqsIncident> _logger;
        private readonly IRepository _repository;

        public DeleteDataForRiqsIncident(
            ILogger<DeleteDataForRiqsIncident> logger,
            IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task<bool> DeleteData(
            string entityName,
            string itemId,
            string additionalInfosItemId = null,
            string additionalTitleId = null)
        {
            var response = false;
            if (!string.IsNullOrEmpty(itemId))
            {
                var organization = await GetOrganization(itemId);
                if (organization != null)
                {
                    _logger.LogInformation($"{nameof(RiqsIncident)} data delete started for OrganizationId: {itemId}.");
                    await DeleteRiqsIncidentData(itemId);
                    response = true;
                }
            }
            return response;
        }

        private async Task<PraxisOrganization> GetOrganization(string orgId)
        {
            return await _repository.GetItemAsync<PraxisOrganization>(o => o.ItemId == orgId);
        }

        private async Task DeleteRiqsIncidentData(string organizationId)
        {
            await _repository.DeleteAsync<RiqsIncident>(ri => ri.OrganizationId.Equals(organizationId));
        }
    }
}