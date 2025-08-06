using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.DataFixServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DataFixServices
{
    public class NewPropertyAdoptionService : IResolveProdDataIssuesService
    {
        private readonly ILogger<NewPropertyAdoptionService> _logger;
        private readonly IRepository _repository;

        public NewPropertyAdoptionService(
            ILogger<NewPropertyAdoptionService> logger,
            IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task<bool> InitiateFix(ResolveProdDataIssuesCommand command)
        {
            _logger.LogInformation("Entered service: {ServiceName}", nameof(NewPropertyAdoptionService));
            try
            {
                var isFixed = await FixDepartmentOrganizationName();
                return isFixed;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {ServiceName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(NewPropertyAdoptionService), ex.Message, ex.StackTrace);
                return false;
            }
        }

        private async Task<bool> FixDepartmentOrganizationName()
        {
            try
            {
                var orgList = GetOrganizationList();
                var departments = _repository.GetItems<PraxisClient>(c => !c.IsMarkedToDelete && !string.IsNullOrEmpty(c.ParentOrganizationId))?.ToList();
                if (departments != null)
                {
                    foreach (var department in departments)
                    {
                        var orgInfo = orgList.FirstOrDefault(o => o.ItemId == department.ParentOrganizationId);
                        if (orgInfo != null && orgInfo.ClientName != department.ParentOrganizationName)
                        {
                            department.ParentOrganizationName = orgInfo.ClientName;
                            await _repository.UpdateAsync<PraxisClient>(c => c.ItemId == department.ItemId, department);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {MethodName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(FixDepartmentOrganizationName), ex.Message, ex.StackTrace);
                return false;
            }
        }

        private List<PraxisOrganization> GetOrganizationList()
        {
            var orgList = _repository.GetItems<PraxisOrganization>(o => !o.IsMarkedToDelete)?.ToList();
            return orgList;
        }
    }
}