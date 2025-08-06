namespace Selise.Ecap.SC.WopiMonitor.Contracts.DomainServices.WopiModule
{
    public interface IWopiPermissionService
    {
        bool HasDepartmentPermission(string departmentId);
        bool HasWopiSessionPermission(string sessionId);
    }
} 