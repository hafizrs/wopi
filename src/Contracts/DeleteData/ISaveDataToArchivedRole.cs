using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData
{
    public interface ISaveDataToArchivedRole
    {
        void InsertData(EntityBase entity);
        void UpdateArchiveRole(EntityBase entity, List<string> RelatedProperties, string EntityItemId);
    }
}
