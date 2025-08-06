using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataForPraxisOrganization : IDeleteDataByCollectionSpecific
    {
        private readonly ILogger<DeleteDataForPraxisOrganization> _logger;
        private readonly IRepository _repository;
        private readonly DeleteDataForPraxisClient _departmentDeleteService;
        private readonly DeleteDataForRiqsIncident _riqsIncidentDeleteService;
        private readonly IDmsService _dmsService;

        public DeleteDataForPraxisOrganization(
            IRepository repository,
            ILogger<DeleteDataForPraxisOrganization> logger,
            DeleteDataForPraxisClient departmentDeleteService,
            DeleteDataForRiqsIncident riqsIncidentDeleteService,
            IDmsService dmsService)
        {
            _logger = logger;
            _repository = repository;
            _departmentDeleteService = departmentDeleteService;
            _riqsIncidentDeleteService = riqsIncidentDeleteService;
            _dmsService = dmsService;
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
                    _logger.LogInformation($"Going to delete {nameof(PraxisOrganization)} data with ItemId: {itemId}.");
                    var departmnetIds = GetDepartmentIds(itemId);
                    await TriggerDeleteForAllTheDepartments(departmnetIds, itemId);   
                    DeleteOrganizationFolder(itemId);
                    await _riqsIncidentDeleteService.DeleteData(nameof(RiqsIncident), itemId);
                    await DeleteOrganizationData(itemId);
                }
                response = true;
            }
            return response;
        }

        private async Task<PraxisOrganization> GetOrganization(string orgId)
        {
            return await _repository.GetItemAsync<PraxisOrganization>(o => o.ItemId == orgId);
        }

        private List<string> GetDepartmentIds(string orgId)
        {
            return _repository.GetItems<PraxisClient>(d => d.ParentOrganizationId == orgId)
                .Select(d => d.ItemId)
                .ToList();
        }

        private async Task TriggerDeleteForAllTheDepartments(List<string> departmentIds, string orgId)
        {
            var deleteDataTasks = departmentIds.Select(id =>
                _departmentDeleteService.DeleteData(nameof(PraxisClient), id, orgId)
            );

            await Task.WhenAll(deleteDataTasks);
        }

        private void DeleteOrganizationFolder(string organizationId)
        {
            try
            {
                _logger.LogInformation($"Delete folder strated for OrganizationId: {organizationId}");
                var artifacts = _repository.GetItems<ObjectArtifact>(c => c.OrganizationId == organizationId)?.ToList() ?? new List<ObjectArtifact>();

                foreach (var artifact in artifacts)
                {
                    _dmsService.DeleteObjectArtifact(artifact.ItemId, artifact.OrganizationId).GetAwaiter().GetResult();
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"Exception occured in delete folder on client delete with error -> {ex.Message} trace -> {ex.StackTrace}");
            }
        }

        private async Task DeleteOrganizationData(string organizationId)
        {
            await _repository.DeleteAsync<PraxisOrganization>(o => o.ItemId.Equals(organizationId));
        }
    }
}