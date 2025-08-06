using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumboard.CirsPermissions;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard.CirsPermissions
{
    public class PermissionsFactoryService : IPermissionsFactoryService
    {
        private readonly IncidentPermissionsService _incidentPermissionsService;
        private readonly IdeaPermissionsService _ideaPermissionsService;
        private readonly HintPermissionsService _hintPermissionsService;
        private readonly ComplainPermissionsService _complainPermissionsService;
        private readonly AnotherPermissionsService _anotherPermissionsService;
        private readonly FaultPermissionService _faultPermissionService;

        public PermissionsFactoryService(
            IncidentPermissionsService incidentPermissionsService,
            IdeaPermissionsService ideaPermissionsService,
            HintPermissionsService hintPermissionsService,
            ComplainPermissionsService complainPermissionsService,
            AnotherPermissionsService anotherPermissionsService,
            FaultPermissionService faultPermissionService)
        {
            _ideaPermissionsService = ideaPermissionsService;
            _incidentPermissionsService = incidentPermissionsService;
            _complainPermissionsService = complainPermissionsService;
            _hintPermissionsService = hintPermissionsService;
            _anotherPermissionsService = anotherPermissionsService;
            _faultPermissionService = faultPermissionService;
        }

        public IPermissionsService GetPermissionsService(CirsDashboardName dashboardName)
        {
            return dashboardName switch
            {
                CirsDashboardName.Incident => _incidentPermissionsService,
                CirsDashboardName.Complain => _complainPermissionsService,
                CirsDashboardName.Idea => _ideaPermissionsService,
                CirsDashboardName.Hint => _hintPermissionsService,
                CirsDashboardName.Another => _anotherPermissionsService,
                CirsDashboardName.Fault => _faultPermissionService,
                _ => null,
            };
        }
    }
}
