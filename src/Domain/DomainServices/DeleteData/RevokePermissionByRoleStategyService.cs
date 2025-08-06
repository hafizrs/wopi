using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class RevokePermissionByRoleStrategyService : IRevokePermissionByRoleStrategy
    {
        private readonly RevokePermissionForPowerUser _revokePermissionForPowerUser;
        private readonly RevokePermissionForLeitung _revokePermissionForLeitung;
        private readonly RevokePermissionForEEGroupOne _revokePermissionForEEGroupOne;
        private readonly RevokePermissionForEEGroupTwo _revokePermissionForEEGroupTwo;

        public RevokePermissionByRoleStrategyService(
            RevokePermissionForPowerUser revokePermissionForPowerUser,
            RevokePermissionForLeitung revokePermissionForLeitung,
            RevokePermissionForEEGroupOne revokePermissionForEEGroupOne,
            RevokePermissionForEEGroupTwo revokePermissionForEEGroupTwo)
        {
            _revokePermissionForPowerUser = revokePermissionForPowerUser;
            _revokePermissionForLeitung = revokePermissionForLeitung;
            _revokePermissionForEEGroupOne = revokePermissionForEEGroupOne;
            _revokePermissionForEEGroupTwo = revokePermissionForEEGroupTwo;
        }
        public IRevokePermissionForRoleSpecific GetService(string role)
        {
            return role switch
            {
                RoleNames.PowerUser => _revokePermissionForPowerUser,
                RoleNames.Leitung => _revokePermissionForLeitung,
                RoleNames.MpaGroup1 => _revokePermissionForEEGroupOne,
                RoleNames.MpaGroup2 => _revokePermissionForEEGroupTwo,
                _ => null
            };
        }
    }
}
