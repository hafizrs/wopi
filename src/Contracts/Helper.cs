using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.Entities.PrimaryEntities.StorageService;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;

public static class Helper
{
    public static List<Type> GetEntityTypes()
    {
        return new List<Type>
            {
                typeof(User),
                typeof(Person),
                typeof(Client),
                typeof(File),
                typeof(PlatformDictionary),
                typeof(Connection),
                typeof(DisconnectedConnection),
                typeof(GdprContent),
                typeof(PraxisClient),
                typeof(PraxisUser),
                typeof(PraxisClientCategory),
                typeof(PraxisForm),
                typeof(PraxisTaskConfig),
                typeof(PraxisTask),
                typeof(TaskSummary),
                typeof(TaskSchedule),
                typeof(PraxisRisk),
                typeof(PraxisAssessment),
                typeof(PraxisOpenItem),
                typeof(PraxisOpenItemConfig),
                typeof(PraxisEquipment),
                typeof(PraxisEquipmentMaintenance),
            };
    }
}
