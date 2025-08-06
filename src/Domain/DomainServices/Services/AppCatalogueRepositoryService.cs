
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SLPC;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Navigation;

using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
//using Selise.Ecap.SC.PraxisMonitor.Domain.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class AppCatalogueRepositoryService : IAppCatalogueRepositoryService
    {
        private readonly ILogger<AppCatalogueRepositoryService> _logger;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;

        public AppCatalogueRepositoryService(
            ILogger<AppCatalogueRepositoryService> logger,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService
        )
        {
            _logger = logger;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
               
        }

        public IEnumerable<FeatureRoleMap> GetFeatureRoleMapsByRoles(IEnumerable<string> roles)
        {
            var featureRoleMapsCollection = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<FeatureRoleMap>("FeatureRoleMaps");

            var byRolesFilter = Builders<FeatureRoleMap>.Filter.In("RoleName", roles);

            return featureRoleMapsCollection.Find(byRolesFilter).ToEnumerable();
        }

        public IEnumerable<AppResponse> GetFeatureRoles()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var featureRoleMaps = GetFeatureRoleMapsByRoles(securityContext.Roles).ToArray();

            var appResponses = new List<AppResponse>();

            if (_securityHelperService.IsAAuthorizedUser())
            {

                appResponses.Add(new AppResponse
                {
                    Type = NavigationFeatureRoleMaps.PraxisClientNavigationFeatureId,
                    Name = NavigationFeatureRoleMaps.PraxisClientNavigationFeatureId,
                    Features = new List<string>
                    {
                        NavigationFeatureRoleMaps.PraxisClientNavigationFeatureId
                    }
                });

                if (!_securityHelperService.IsADepartmentLevelUser())
                {
                    var navigationCollection = _ecapMongoDbDataContextProvider
                        .GetTenantDataContext()
                        .GetCollection<PraxisNavigation>("PraxisNavigations");
                    var navigationList = navigationCollection.Find(_ => true).ToList();

                    var newNavigationList = navigationList.Select(navigation => new
                    {
                        navigation.AppType,
                        navigation.AppName,
                        navigation.FeatureId
                    }).ToList();

                    featureRoleMaps = featureRoleMaps.Concat(newNavigationList.Select(nav => new FeatureRoleMap(new Dictionary<string, string>
                    {
                        { "AppType", nav.AppType },
                        { "AppName", nav.AppName },
                        { "FeatureId", nav.FeatureId }
                    }
                    ))).ToArray();
                }
                else if (_securityHelperService.IsADepartmentLevelUser())
                {
                    featureRoleMaps = ProcessDepartmentLevelUserNavigation(_securityContextProvider, featureRoleMaps, appResponses);
                }
            }

            var groupedFeatureRoleMaps = featureRoleMaps
                .GroupBy(frm => new { frm.AppName, frm.AppType })
                .Select(g => new AppResponse
                {
                    Name = g.Key.AppName,
                    Type = g.Key.AppType,
                    Features = g.Select(frm => frm.FeatureId).Distinct().ToList()
                });

            appResponses.AddRange(groupedFeatureRoleMaps);

            return appResponses;
        }

        private FeatureRoleMap[] ProcessDepartmentLevelUserNavigation(ISecurityContextProvider securityContext, FeatureRoleMap[] featureRoleMaps, List<AppResponse> appResponses)
        {
            var departmentId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
            var clientCollection = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<PraxisClient>("PraxisClients");

            var byIdFilter = Builders<PraxisClient>.Filter.Eq("_id", departmentId);
            var client = clientCollection.Find(byIdFilter).FirstOrDefault();

            var navigationNames = client?.Navigations?.Select(navigation => navigation.Name)?.ToList() ?? new List<string>();

            if (_securityHelperService.IsAMpaUser())
            {
                if (navigationNames.Contains("SHIFT_PLAN", StringComparer.InvariantCultureIgnoreCase))
                {
                    appResponses.Add(new AppResponse
                    {
                        Type = NavigationFeatureRoleMaps.PraxisNavigationEmployeeType,
                        Name = NavigationFeatureRoleMaps.PraxisNavigationEmployeeFeatureIdShiftPlan,
                        Features = new List<string>
                        {
                            NavigationFeatureRoleMaps.PraxisNavigationEmployeeFeatureIdShiftPlan
                        }
                    });
                }
                if (navigationNames.Contains("PROCESS_GUIDE", StringComparer.InvariantCultureIgnoreCase))
                {
                    appResponses.Add(new AppResponse
                    {
                        Type = NavigationFeatureRoleMaps.PraxisNavigationEmployeeType,
                        Name = NavigationFeatureRoleMaps.PraxisNavigationEmployeeFeatureIdTemplates,
                        Features = new List<string>
                        {
                            NavigationFeatureRoleMaps.PraxisNavigationEmployeeFeatureIdTemplates
                        }
                    });
                }
            }

            var navigationCollection = _ecapMongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<PraxisNavigation>("PraxisNavigations");

            var navigationList = navigationCollection.Find(_ => true).ToList();
            var filteredNavigationList = navigationList
                .Where(navigation => navigationNames.Contains(navigation.Name, StringComparer.InvariantCultureIgnoreCase))
                .ToList();

            var navRoleList = securityContext.GetSecurityContext().Roles
                .Where(role => role.Contains("Nav", StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            var eeGroupOneNavList = filteredNavigationList
                .Where(n => !NavigationFeatureRoleMaps.InaccessibleNavigationsForMpaGroup1.Contains(n.AppName))
                .ToList();

            var eeGroupTwoNavList = filteredNavigationList
                .Where(n => !NavigationFeatureRoleMaps.InaccessibleNavigationsForMpaGroup2.Contains(n.AppName))
                .ToList();

            var newNavRoleList = new List<PraxisNavigation>();

            foreach (var role in navRoleList)
            {
                var roleParts = role.Split('_');
                if (roleParts.Length == 0 || string.IsNullOrEmpty(roleParts[0]))
                {
                    continue;
                }

                switch (roleParts[0])
                {
                    case RoleNames.PowerUser_Nav:
                        newNavRoleList.AddRange(filteredNavigationList);
                        break;

                    case RoleNames.Leitung_Nav:
                        newNavRoleList.AddRange(filteredNavigationList);
                        break;

                    case RoleNames.MpaGroup1_Nav:
                        newNavRoleList.AddRange(eeGroupOneNavList);
                        break;

                    case RoleNames.MpaGroup2_Nav:
                        newNavRoleList.AddRange(eeGroupTwoNavList);
                        break;
                    default:
                        break;
                }
            }

            newNavRoleList = newNavRoleList.DistinctBy(a => a.FeatureId).ToList();
            var newNavigationList = newNavRoleList.Select(navigation => new
            {
                navigation.AppType,
                navigation.AppName,
                navigation.FeatureId
            }).ToList();

            return featureRoleMaps.Concat(newNavigationList.Select(nav => new FeatureRoleMap(new Dictionary<string, string>
            {
                { "AppType", nav.AppType },
                { "AppName", nav.AppName },
                { "FeatureId", nav.FeatureId }
            }))).ToArray();
        }





    }
}
