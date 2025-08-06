using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.EquipmentModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.EquipmentModule
{
    public interface IAssignEquipmentAdminsService
    {
        Task<bool> AssignEquipmentAdmins(AssignEquipmentAdminsCommand command);
        Task<PraxisEquipmentRight> GetEquipmentRights(GetEquipmentRightsQuery query);
    }
}
