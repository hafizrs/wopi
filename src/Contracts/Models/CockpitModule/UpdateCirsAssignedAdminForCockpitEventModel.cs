using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

public class UpdateCirsAssignedAdminForCockpitEventModel
{
    public List<string> DashboardPermissionIds { get; set; }
}