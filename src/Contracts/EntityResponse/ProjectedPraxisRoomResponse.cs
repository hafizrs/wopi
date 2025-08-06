using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public record ProjectedPraxisRoomResponse(
        string ClientId,
        string Name,
        string Description,
        string RoomKey,
        string RoomLevel,
        EquipmentAddress Address,
        string CreatedBy,
        DateTime CreateDate,
        string ItemId,
        DateTime LastUpdateDate,
        string Remarks,
        string ServiceProviderId,
        string ServiceProviderName
    );

}
