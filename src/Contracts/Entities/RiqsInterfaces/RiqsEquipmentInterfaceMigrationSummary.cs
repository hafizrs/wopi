using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsInterfaces
{
    public class RiqsEquipmentInterfaceMigrationSummary : EntityBase
    {
        public string ClientId { get; set; }
        public string OrganizationId { get; set; }
        public bool IsDraft { get; set; } = false;
        public List<TempEquipmentInterfacePaseData> PraxisEquipments { get; set; }
        public List<TempEquipmentMaintenancesInterfacePastData> PraxisEquipmentMaintenances { get; set; }
        public long TotalRecord { get; set; } = 0;
        public bool IsUpdate { get; set; } = false;
    }


    public class TempEquipmentInterfacePaseData : PraxisEquipment
    {
        public string MigrationSummeryId { get; set; }
    }

    public class TempEquipmentMaintenancesInterfacePastData : PraxisEquipmentMaintenance
    {
        public string MigrationSummeryId { get; set; }
    }

    public class MaintenanceValidationDateProp
    {
        public string ItemId { get; set; }
        public string ScheduleType { get; set; }
        public DateTime? Date { get; set; }
        public PraxisKeyValue CompletionStatus { get; set; }
    }
}
