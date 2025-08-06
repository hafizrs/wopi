using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsInterfaces
{
    public class RiqsUserInterfaceMigrationSummary : EntityBase
    {
        public string ClientId { get; set; }
        public string OrganizationId { get; set; }
        public bool IsDraft { get; set; } = false;
        public List<TempPraxisUserInterfacePastData> PraxisUsers { get; set; }
        public long TotalRecord { get; set; } = 0;
        public bool IsUpdate { get; set; } = false;
    }

    public class TempPraxisUserInterfacePastData : PraxisUser
    {
        public string MigrationSummeryId { get; set; }
        public bool IsExist { get; set; } = false;
    }
}
