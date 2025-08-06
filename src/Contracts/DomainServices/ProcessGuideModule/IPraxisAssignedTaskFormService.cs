using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisAssignedTaskFormService
    {
        void CreateAssignedForm(string formId,string assignedEntityName, string assignedEntityId);
        AssignedTaskForm GetAssignedForm(string assignedEntityId, string assignedEntityName);
    }
}
