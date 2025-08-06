using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ObjectArtifactFileShareCommand
    {
        public string ObjectArtifactId { get; set; }
        public string ViewMode { get; set; }
        public string OrganizationId { get; set; }
        public bool IsSharedToWholeOrganization { get; set; }
        public List<string> SharedRolesWithOrganization { get; set; }
        public string Permission { get; set; }
        public List<DepartmentWiseObjectArtifactSharedDetail> SharedDepartmentList { get; set; }
        public bool NotifyToCockpit { get; set; }
        public bool? IsStandardFile { get; set; } = null;
        public bool? IsAdminViewEnabled { get; set; } = null;
    }

    public class DepartmentWiseObjectArtifactSharedDetail
    {
        public string DepartmentId { get; set; }
        public bool IsSharedToWholeDepartment { get; set; }
        public string Permission { get; set; }
        public List<AssigneePermissionModel> AssigneePermissions { get; set; }
    }

    public class AssigneePermissionModel
    {
        public string Permission { get; set; }
        public AssigneeChanges UserGroups { get; set; }
        public AssigneeChanges Members { get; set; }
    }

    public class AssigneeChanges
    {
        public string[] Added { get; set; }
        public string[] Removed { get; set; }
    }
}