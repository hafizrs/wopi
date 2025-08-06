using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.DataFixServices;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Navigation;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Navigation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DataFixServices
{
    public class NewApplicationAccessManagementService : IResolveProdDataIssuesService
    {
        private readonly ILogger<NewApplicationAccessManagementService> _logger;
        private readonly IRepository _repository;
        private readonly UpdateDynamicNavigationService _updateDynamicNavigationService;

        public NewApplicationAccessManagementService(
            ILogger<NewApplicationAccessManagementService> logger,
            IRepository repository,
            UpdateDynamicNavigationService updateDynamicNavigationService)
        {
            _logger = logger;
            _repository = repository;
            _updateDynamicNavigationService = updateDynamicNavigationService;
        }

        public async Task<bool> InitiateFix(ResolveProdDataIssuesCommand command)
        {
            await UpdateNewApplicationAccessManegmentForDepartment();
            return true;
        }

        private async Task<bool> UpdateNewApplicationAccessManegmentForDepartment()
        {
            try
            {
                _logger.LogInformation("Entered service:{ServiceName}.", nameof(NewApplicationAccessManagementService));

                var departmentList = _repository.GetItems<PraxisClient>(c => !c.IsMarkedToDelete)?.ToList();
                var navigationList = _repository.GetItems<PraxisNavigation>(n => !n.IsMarkedToDelete)?.ToList();
                var cirsNav = navigationList?.Find(n => n.Name.Equals("INCIDENT_REPORTS"));
                if (cirsNav == null)
                {
                    _logger.LogError("Cirs navigation not found");
                    return false;
                }
                if (departmentList != null)
                {
                    foreach (var dept in departmentList)
                    {
                        var navs = dept.Navigations?.ToList();
                        if (navs != null && navs.Find(n => n.ItemId == cirsNav.ItemId) == null)
                        {
                            navs.Add
                            (
                                new NavigationDto()
                                {
                                    ItemId = cirsNav.ItemId,
                                    Name = cirsNav.Name
                                }
                            );
                            dept.Navigations = navs;
                            _repository.Update<PraxisClient>(c => c.ItemId == dept.ItemId, dept);

                            _logger.LogInformation("Updated DepartmentId: {ItemId} --> DepartmentName: {ClientName}", dept.ItemId, dept.ClientName);

                            await PrepareAndUpdateDynamicNavigationsForDepartment(dept, navs, navigationList);
                        }

                    }
                }

                _logger.LogInformation("Exiting service:{ServiceName}.", nameof(NewApplicationAccessManagementService));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {ServiceName}. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(UpdateNewApplicationAccessManegmentForDepartment), ex.Message, ex.StackTrace);
                return false;
            }
        }

        private async Task<bool> PrepareAndUpdateDynamicNavigationsForDepartment(
            PraxisClient dept,
            List<NavigationDto> navs,
            List<PraxisNavigation>navigationList)
        {
            try
            {
                var navInfoList = new List<NavInfo>();
                foreach (var nav in navs)
                {
                    var navInfo = navigationList.Find(n => n.ItemId == nav.ItemId);
                    if (navInfo != null)
                    {
                        navInfoList.Add
                        (
                            new NavInfo()
                            {
                                FeatureId = navInfo.FeatureId,
                                FeatureName = navInfo.FeatureName,
                                AppType = navInfo.AppType,
                                AppName = navInfo.AppName
                            }
                        );
                    }
                }
                var isUpdated = await _updateDynamicNavigationService.ProcessNavigationData(dept.ItemId, navInfoList);
                if (isUpdated) _logger.LogInformation("Updated Dynamic Navigation's DepartmentId: {ItemId} --> DepartmentName: {ClientName}", dept.ItemId, dept.ClientName);
                else
                {
                    _logger.LogError("Failed to update Dynamic Navigation's DepartmentId: {ItemId} --> DepartmentName: {ClientName}", dept.ItemId, dept.ClientName);
                }
                
                return isUpdated;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {ServiceName}. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(PrepareAndUpdateDynamicNavigationsForDepartment), ex.Message, ex.StackTrace);
                return false;
            }
        }

    }
}