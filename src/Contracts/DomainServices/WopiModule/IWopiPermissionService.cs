namespace Selise.Ecap.SC.Wopi.Contracts.DomainServices.WopiModule
{
    public interface IWopiPermissionService
    {
        bool HasDepartmentPermission(string departmentId);
        bool HasWopiSessionPermission(string sessionId);
    }
} 