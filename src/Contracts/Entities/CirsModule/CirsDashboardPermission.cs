using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;

public class CirsDashboardPermission : EntityBase
{
    public string OrganizationId { get; set; }

    public string PraxisClientId { get; set; }

    public CirsDashboardName CirsDashboardName { get; set; }

    public AssignmentLevel AssignmentLevel { get; set; }

    public IEnumerable<PraxisIdDto> AdminIds { get; set; }
}