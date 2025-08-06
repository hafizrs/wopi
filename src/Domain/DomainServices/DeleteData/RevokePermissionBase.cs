using System.Collections.Generic;
using System.Linq;
using SeliseBlocks.Genesis.Framework.PDS.Entity;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public abstract class RevokePermissionBase
    {
        public abstract void UpdatePermissionAndTag(EntityBase entity);
        public static void RevokePermissionFromEntity(EntityBase entity, Dictionary<string, List<string>> permissionsToBeRemoved)
        {
            //IdsAllowedToRead
            if (permissionsToBeRemoved.ContainsKey(nameof(entity.IdsAllowedToRead)))
            {
                entity.IdsAllowedToRead = permissionsToBeRemoved[nameof(entity.IdsAllowedToRead)].ToArray();
            }
            //RolesAllowedToRead
            if (permissionsToBeRemoved.ContainsKey(nameof(entity.RolesAllowedToRead)))
            {
                var rolesToRemaining = permissionsToBeRemoved[nameof(entity.RolesAllowedToRead)];
                if (rolesToRemaining.Any())
                {
                    var existingsRoles = (entity.RolesAllowedToRead ?? new string[] { }).ToList();
                    var newRoles = existingsRoles.Where(r=>rolesToRemaining.Contains(r));
                    entity.RolesAllowedToRead = newRoles.ToArray();
                }
            }
        }


    }
}
