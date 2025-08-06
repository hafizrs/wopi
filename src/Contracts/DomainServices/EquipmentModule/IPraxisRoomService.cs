using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisRoomService
    {
        PraxisRoom GetPraxisRoom(string itemId);
        void UpdatePraxisRoom(string itemId);
        List<PraxisRoom> GetAllPraxisRoom();
        void AddRowLevelSecurity(string itemId, string clientId);
        void RemoveRowLevelSecurity(string clientId);
        List<PraxisRoom> GetPraxisRoomsByIds(List<string> roomsIds);
    }
}
