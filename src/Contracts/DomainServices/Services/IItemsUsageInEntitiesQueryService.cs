using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.EquipmentModule;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

public interface IItemsUsageInEntitiesQueryService
{
    Task<List<GetItemsUsageInEntitiesQueryDto>> GetItemsUsageInEntities(GetItemsUsageInEntitiesQuery query);
}