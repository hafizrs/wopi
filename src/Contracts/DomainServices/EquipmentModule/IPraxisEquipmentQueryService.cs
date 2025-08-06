using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.EquipmentModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.EquipmentModule
{
    public interface IPraxisEquipmentQueryService
    {
        Task<EntityQueryResponse<ProjectedEquipmentResponse>> GetPraxisEquipments(GetEquipementQuery query);
        Task<EntityQueryResponse<ProjectedEquipmentResponse>> GetPraxisEquipmentDetail(GetEquipementQuery query);
        Task<EntityQueryResponse<ProjectedEquipmentMaintenanceResponse>> GetPraxisEquipmentMaintenances(GetPraxisEquipmentMaintenancesQuery query);
        Task<EntityQueryResponse<ProjectedClientResponse>> GetPraxisClientsForEquipement(GetEquipementClientQuery query);
        Task<EntityQueryResponse<ProjectedUserResponse>> GetPraxisUsersForEquipement(GetEquipementUserQuery query);
        Task<EntityQueryResponse<ProjectedPraxisClientCategoryResponse>> GetPraxisClientCategoryForEquipement(GetEquipementClientCategoryQuery query);
        Task<EntityQueryResponse<ProjectedPraxisRoomResponse>> GetPraxisRoomForEquipement(GetEquipementRoomQuery query);
    }
}

