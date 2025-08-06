using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts
{
    public interface IEmailDataBuilder
    {
        Dictionary<string, string> BuildTaskSummaryEmailData(TaskSummary taskSummary, Person person, PraxisTask task, string clientName);
        Dictionary<string, string> BuildTaskSummaryEmailData(TaskSummary taskSummary,
            Person person, PraxisOpenItem openItem, string clientName = "", bool isForTrainning = false, string assignedBy = "");
        Dictionary<string, string> BuildTaskRescheduledEmailData(TaskSummary taskSummary, TaskSchedule taskSchedule, Person person, PraxisTask task, string clientName);
        Dictionary<string, string> BuildTraingEmailData(PraxisTraining praxisTraining, Person person);
        Dictionary<string, string> BuildEquipmentmaintenanceEmailData(PraxisEquipment praxisEquipment, PraxisEquipmentMaintenance praxisEquipmentMaintenance, Person person, string clientName, PraxisEquipmentMaintenanceByExternalUser externalUserInfo = null);
        Dictionary<string, string> BuildOpenItemEmailData(PraxisOpenItem praxisOpenItem, Person person, Person CompletedByPerson, string clientName, string assignedBy);
        Dictionary<string, string> BuildUserUpdateConfirmationEmailData(List<string> organizationNames, PraxisUser praxisUserData);
        Dictionary<string, string> BuildUserUserLimitReachedEmailData(string subscriptionPackage, string numberOfUser, string clientId, PraxisUser praxisUserData);
        Dictionary<string, string> BuildUserUserSubscriptionUpdateEmailData(SubscriptionPlan newSubscriptionPlan, SubscriptionPlan previousSubscriptionPlan, PraxisUser praxisUserData);
        Dictionary<string, string> BuildMaintenanceDeleteEmailData(Person person, string scheduleType, string equipmentName);
        Dictionary<string, string> BuildCirsReportEmailData(
            CirsGenericReport report,
            Person person, string clientName, MinimalSupplierInfo externalUserInfo = null);
        Dictionary<string, string> BuildProcessGuideOverDueEmailData(Person person, DateTime dueDate, PraxisProcessGuide praxisProcessGuide, string clientName);
        Dictionary<string, string> BuildExternalSignatureEmailData(PraxisEquipmentMaintenance maintenance, Person person, string reportId, string reportTitle,
            PraxisEquipmentMaintenanceByExternalUser externalUserInfo = null);
    }
}
