using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos
{
    public class EntityAccessPermission
    {
        public string ItemId { get; set; }
        public List<string> RolesAllowedToRead { get; set; }
        public List<string> IdsAllowedToRead { get; set; }
        public List<string> RolesAllowedToWrite { get; set; }
        public List<string> IdsAllowedToWrite { get; set; }
        public List<string> RolesAllowedToUpdate { get; set; }
        public List<string> IdsAllowedToUpdate { get; set; }
        public List<string> RolesAllowedToDelete { get; set; }
        public List<string> IdsAllowedToDelete { get; set; }
    }
}
