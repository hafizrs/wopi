using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisShiftPermissionService
    {
        string[] GetRolesAllowedToRead(string departmentId);
        string[] GetRolesAllowedToUpdate(string departmentId);
        string[] GetRolesAllowedToDelete(string departmentId);
        bool HasDepartmentPermission(string departmentId);
        bool HasShiftPlanDepartmentPermission(string shiftId);
        bool HasDepartmentPermissionGetByShiftplanId(string shiftPlanId);
    }
}
