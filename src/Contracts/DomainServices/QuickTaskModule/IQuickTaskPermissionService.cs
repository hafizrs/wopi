namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule
{
    public interface IQuickTaskPermissionService
    {
        string[] GetRolesAllowedToRead(string departmentId);
        string[] GetRolesAllowedToUpdate(string departmentId);
        string[] GetRolesAllowedToDelete(string departmentId);
        bool HasDepartmentPermission(string departmentId);
        bool HasQuickTaskPlanDepartmentPermission(string quickTaskId);
        bool HasDepartmentPermissionGetByQuickTaskPlanId(string quickTaskPlanId);
    }
} 