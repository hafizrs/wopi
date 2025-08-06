using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models.CirsReport;

public class UserCirsPermissionInfoResponse
{
    public string AssignmentLevel { get; set; }
    public Dictionary<string, bool> LoggedInUserPermission { get; set; }
}