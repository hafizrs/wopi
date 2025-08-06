using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class MaintenanceDatePropWithType: MaintenanceDateProp
    {
        public string ScheduleType { get; set; }
    }
}
