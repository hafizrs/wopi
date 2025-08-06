using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models.CirsReport
{
    public class GetPermissionFilterModel
    {
        public Dictionary<string, bool> LoggedInUserPermission { get; set; }
        public CirsDashboardName DashboardName { get; set; }
        public string ClientId { get; set; }
        public string ClientOrganizationId { get; set; }
        public bool IsActive { get; set; }
        public string CirsReportId { get; set; }
        public bool HaveOfficerPermission { get; set; }
        public PraxisClient PraxisClient { get; set; }
        public bool IsACirsAdmin { get; set; }
        public CirsDashboardPermission DashboardPermission { get; set; }
    }
}
