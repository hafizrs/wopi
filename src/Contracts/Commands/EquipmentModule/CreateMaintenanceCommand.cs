using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class CreateMaintenanceCommand
    {
        [Required] public List<DateTime> MaintenanceDates { get; set; }
        [Required] public int MaintenancePeriodDays { get; set; } = 1;
        [Required] public PraxisEquipmentMaintenance Maintenance { get; set; }
    }
}
