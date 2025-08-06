using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using System;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Models.Enum;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.Entities.PrimaryEntities.Caredoo;
using Selise.Ecap.Entities.PrimaryEntities.SKO;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.Builders
{
    public class EmailDataBuilder : IEmailDataBuilder
    {
        private readonly IConfiguration configuration;
        private readonly ICreateDynamicLink _createDynamicLinkService;
        private readonly IRepository _repository;
        private readonly string _firebaseUrl;
        private readonly string praxisWebUrl;
        private readonly ILogger<EmailDataBuilder> _logger;

        public EmailDataBuilder(
            IConfiguration configuration,
            ICreateDynamicLink createDynamicLinkService,
            IRepository repository,
            ILogger<EmailDataBuilder> logger)
        {
            this.configuration = configuration;
            _createDynamicLinkService = createDynamicLinkService;
            _repository = repository;
            _logger = logger;
            _firebaseUrl = configuration["FireBaseUrl"];
            praxisWebUrl = configuration["PraxisWebUrl"];
        }

        
        public Dictionary<string, string> BuildTaskSummaryEmailData(TaskSummary taskSummary, Person person, PraxisTask task, string clientName)
        {
            var taskScheduleDto = new Dictionary<string, string>
            {
                {"TaskTitle" , taskSummary.Title},
                {"Description" , taskSummary.Description},
                {"DisplayName", person.DisplayName },
                {"TaskUrl", GetPraxisTaskUrl(task) },
                {"TaskScheduleString",  GetTaskScheduleString(taskSummary)},
                {"TaskFulfillmentPercentage", task.TaskFulfillmentPercentage.ToString()},
                {"TaskItemId", task.ItemId },
                {"CategoryName",task.CategoryName},
                {"SubCategoryName",task.SubCategoryName},
                {"Language",task.Language},
                {"OrganizationName",clientName},
            };

            if (task.TaskSchedule.TaskMovedDates != null && task.TaskSchedule.TaskMovedDates.Any())
            {
                taskScheduleDto["TaskScheduleString"] = GetVerboseDateString(task.TaskSchedule.TaskMovedDates.Last());
            }

            return taskScheduleDto;
        }

        public Dictionary<string, string> BuildTaskSummaryEmailData(
            TaskSummary taskSummary, Person person, PraxisOpenItem openItem, string clientName = "", bool isForTrainning = false, string assignedBy = "")
        {
            var taskScheduleDto = new Dictionary<string, string>
            {
                {"TaskTitle", openItem.Title},
                {"OrganizationName", clientName },
                {"Topic", openItem.Topic?.Value},
                {"Description", taskSummary.Description},
                {"Remarks", openItem.Remarks},
                {"DisplayName", person.DisplayName},
                {"CategoryName", openItem.CategoryName},
                {"SubCategoryName", openItem.SubCategoryName},
                {"TaskUrl", GetOpenItemUrl(openItem)},
                {"TaskScheduleString", GetTaskScheduleString(taskSummary)},
                {"TaskReferenceTitle", openItem.TaskReferenceTitle},
                {"TaskReferenceId", openItem.TaskReferenceId},
                {"TaskItemId", openItem.ItemId},
                {"TrainingUrl", "#"},
                {"RelatedEntityId", openItem.ItemId },
                {"HostUrl", GetHostUrl() },
                {"Language",taskSummary.Language},
                {"TypeOfTask",openItem?.TaskReference?.Value ?? string.Empty},
                {"AssignedBy",assignedBy}
            };

            if (openItem.TaskSchedule != null && openItem.TaskSchedule.TaskMovedDates != null && openItem.TaskSchedule.TaskMovedDates.Any())
            {
                taskScheduleDto["TaskScheduleString"] = GetVerboseDateString(openItem.TaskSchedule.TaskMovedDates.Last());
            }

            if (isForTrainning && !string.IsNullOrEmpty(openItem.TaskReferenceId))
            {
                taskScheduleDto["TrainingUrl"] = configuration["PraxisWebUrl"] + "/trainings/" + openItem.TaskReferenceId + "/detail";
            }

            return taskScheduleDto;
        }

        public Dictionary<string, string> BuildTaskRescheduledEmailData(TaskSummary taskSummary, TaskSchedule taskSchedule, Person person, PraxisTask task, string clientName)
        {
            DateTime rescheduledDate = taskSchedule.TaskMovedDates.Max(date => date.Date);
            var taskScheduleDto = new Dictionary<string, string>
            {
                {"TaskTitle" , taskSummary.Title},
                {"Description" , taskSummary.Description},
                {"DisplayName", person.DisplayName },
                {"TaskUrl", GetPraxisTaskUrl(task) },
                {"TaskScheduleString",  GetVerboseDateString(rescheduledDate)},
                {"TaskItemId", task.ItemId },
                {"CategoryName",task.CategoryName},
                {"SubCategoryName",task.SubCategoryName},
                {"Language",task.Language},
                {"ClientName",clientName},
            };

            return taskScheduleDto;
        }
        
        public Dictionary<string, string> BuildTraingEmailData(PraxisTraining praxisTraining, Person person)
        {
            return new Dictionary<string, string>
            {
                {"TrainingTitle" , praxisTraining.Title},
                {"Description" , praxisTraining.Description},
                {"DisplayName", person.DisplayName },
                {"TrainingName", praxisTraining.Title },
                {"Topic", praxisTraining.TopicValue },
                {"TrainingUrl", GetPraxisTrainingUrl(praxisTraining) },
                {"TrainingScheduleString", praxisTraining.DueDate.ToString("dd MMMM yyyy")},
                {"Language",praxisTraining.Language},
                {"OrganizationName",praxisTraining.ClientName}
            };
        }
        
        public Dictionary<string, string> BuildEquipmentmaintenanceEmailData(
            PraxisEquipment praxisEquipment,
            PraxisEquipmentMaintenance praxisEquipmentMaintenance,
            Person person, string clientName, PraxisEquipmentMaintenanceByExternalUser externalUserInfo = null)
        {
            var endDate = praxisEquipmentMaintenance.MaintenanceEndDate.Year > 1000 ? 
                    praxisEquipmentMaintenance.MaintenanceEndDate : praxisEquipmentMaintenance.MaintenanceDate;
            return new Dictionary<string, string>
            {
                {"EquipmentName", praxisEquipment.Name},
                {"Topic", praxisEquipment.Topic?.Value},
                {"DisplayName", person.DisplayName},
                {"ScheduleType", praxisEquipmentMaintenance.ScheduleType ?? "MAINTENANCE"},
                {"EquipmentMaintenanceUrl", GetPraxisEquipmentMaintenanceUrl(praxisEquipmentMaintenance, externalUserInfo)},
                {"MaintenanceScheduleStartString",  GetVerboseDateString(praxisEquipmentMaintenance.MaintenanceDate)},
                {"MaintenanceScheduleString",  GetVerboseDateString(endDate)},
                {"Location", praxisEquipment.RoomName},
                {"ClientName",clientName},
                {"Language",praxisEquipmentMaintenance.Language}
            };
        }

        public Dictionary<string, string> BuildCirsReportEmailData(
            CirsGenericReport report,
            Person person, string clientName, MinimalSupplierInfo externalUserInfo = null)
        {
            var topic = report?.MetaData?.ContainsKey($"{IncidentMetaKey.Topic}") == true ? 
                            report.MetaData[$"{IncidentMetaKey.Topic}"] : null;

            return new Dictionary<string, string>
            {
                {"DisplayName", person.DisplayName},
                {"CirsReportUrl", GetCirsReportUrl(report, externalUserInfo)},
                {"Status", report.Status},
                {"ClientName", clientName},
                {"Title", report.Title},
                {"CardNumber", report.SequenceNumber},
                {"DashboardName", report.CirsDashboardName.ToString()},
                {"DateCreated", GetVerboseDateString(report.CreateDate)},
                {"KeyWords", string.Join(", ", report.KeyWords?.ToList() ?? new List<string>())},
                {"Description", report.Description},
                {"Topic", topic?.ToString() ?? string.Empty}
            };
        }

        public Dictionary<string, string> BuildOpenItemEmailData(PraxisOpenItem praxisOpenItem, Person person, Person CompletedByPerson, string clientName, string assignedBy)
        {
            DateTime rescheduledDate = praxisOpenItem.TaskSchedule.TaskDateTime.Date;
            return new Dictionary<string, string>
            {
                {"TaskTitle" , praxisOpenItem.TaskSchedule.Title},
                {"DisplayName", person.DisplayName },
                {"TaskScheduleString",  GetVerboseDateString(rescheduledDate)},
                {"TaskCompleteDateString",GetVerboseDateString( praxisOpenItem.TaskSchedule.SubmissionDate.Date) },
                {"CompletedBy", CompletedByPerson.DisplayName },
                {"RelatedEntityId", praxisOpenItem.ItemId },
                {"HostUrl", GetHostUrl() },
                {"CategoryName",praxisOpenItem.CategoryName},
                {"SubCategoryName",praxisOpenItem.SubCategoryName},
                {"ClientName",clientName},
                {"Language",praxisOpenItem.Language},
                {"TypeOfTask",praxisOpenItem?.TaskReference?.Value ?? string.Empty},
                {"Remarks",praxisOpenItem?.Remarks ?? string.Empty},
                {"AssignedBy",assignedBy}
            };
        }

        public Dictionary<string, string> BuildUserUpdateConfirmationEmailData(List<string> organizationNames, PraxisUser praxisUserData)
        {
            return new Dictionary<string, string>
            {
                {"DisplayName", praxisUserData.DisplayName },
                {"OrganizationNames", String.Join(", ", organizationNames) },
                {"PraxisWebUrl", praxisWebUrl }
            };
        }
        
        public Dictionary<string, string> BuildUserUserLimitReachedEmailData(string subscriptionPackage, string numberOfUser, string clientId, PraxisUser praxisUserData)
        {
            return new Dictionary<string, string>
            {
                {"DisplayName", praxisUserData.DisplayName },
                {"CurrentPlan", subscriptionPackage },
                {"CurrentUserLimit", numberOfUser },
                {"PraxisWebUrl", praxisWebUrl + "/purchase/update-subscription/" + clientId }
            };
        }
        
        public Dictionary<string, string> BuildUserUserSubscriptionUpdateEmailData(SubscriptionPlan newSubscriptionPlan, SubscriptionPlan previousSubscriptionPlan, PraxisUser praxisUserData)
        {
            return new Dictionary<string, string>
            {
                {"DisplayName", praxisUserData.DisplayName },
                {"NewNumberOfUser", newSubscriptionPlan.NumberOfUser.ToString() },
                {"NewSubscriptionPackage", newSubscriptionPlan.SubscriptionPackage },
                {"NewDurationOfSubscription", newSubscriptionPlan.DurationOfSubscription.ToString() },
                {"PreviousNumberOfUser", previousSubscriptionPlan.NumberOfUser.ToString() },
                {"PreviousSubscriptionPackage", previousSubscriptionPlan.SubscriptionPackage },
                {"PreviousDurationOfSubscription", previousSubscriptionPlan.DurationOfSubscription.ToString() },
                {"PraxisWebUrl", praxisWebUrl }
            };
        }
        
        private string GetTaskScheduleString(TaskSummary taskSummary)
        {
            string scheduleString = "";

            if (!taskSummary.IsRepeat)
            {
                var taskDate = taskSummary.SubmissionDates.First();
                scheduleString = GetVerboseDateString(taskDate);
            }
            else
            {
                var scheduleType = taskSummary.RepeatType;

                if (scheduleType == (int)RepeatTypeEnums.Daily)
                {
                    if (taskSummary.StartsOnDate.HasValue)
                    {
                        var startDate = GetVerboseDateString(taskSummary.StartsOnDate.Value);
                        scheduleString += "Repeat in every " + taskSummary.RepeatValue + " days of the month from " + startDate;
                    }
                }
                else if (scheduleType == (int)RepeatTypeEnums.Weekly)
                {
                    if (taskSummary.RepeatOnDayOfWeeks != null)
                    {
                        scheduleString += string.Join(", ",
                            taskSummary.RepeatOnDayOfWeeks.Select(day => System.Enum.GetName(typeof(DayOfWeek), day)));
                        scheduleString += " (Weekly)";
                    }
                }
                else if (scheduleType == (int)RepeatTypeEnums.Monthly)
                {
                    if (taskSummary.RepeatingDates != null)
                    {
                        scheduleString += string.Join(", ",
                            taskSummary.RepeatingDates.Select(date => date.Day + GetMonthOrdinalSuffix(date.Day)));
                        scheduleString += " day of the month (Monthly)";
                    }

                }
                else if (scheduleType == (int)RepeatTypeEnums.Yearly)
                {
                    if (taskSummary.RepeatingDates != null)
                    {
                        scheduleString += string.Join(", ",
                            taskSummary.RepeatingDates.Select(
                                date => date.Day + GetMonthOrdinalSuffix(date.Day) + " " + System.Enum.GetName(typeof(SortMonthEnums), date.Month))
                            );
                        scheduleString += ", " + DateTime.Now.Year.ToString();
                    }
                }
                else
                {
                    return scheduleString;
                }
            }

            return scheduleString;
        }
        
        private string GetVerboseDateString(DateTime date)
        {
            return date.ToString("dd MMMM yyyy") + " (" + date.DayOfWeek.ToString() + ")";
        }
        
        public string GetMonthOrdinalSuffix(int num)
        {
            if (num.ToString().EndsWith("11"))
            {
                return "ᵗʰ";
            }

            if (num.ToString().EndsWith("12"))
            {
                return "ᵗʰ";
            }

            if (num.ToString().EndsWith("13"))
            {
                return "ᵗʰ";
            }

            if (num.ToString().EndsWith('1'))
            {
                return "ˢᵗ";
            }

            if (num.ToString().EndsWith('2'))
            {
                return "ⁿᵈ";
            }

            if (num.ToString().EndsWith('3'))
            {
                return "ʳᵈ";
            }

            return "ᵗʰ";
        }
        
        private string GetPraxisTaskUrl(PraxisTask praxisTask)
        {
            var taskUrl = configuration["PraxisWebUrl"] + "/tasks/" + praxisTask.ItemId;
            return GetDynamicUrl(taskUrl);
        }
        
        private string GetOpenItemUrl(PraxisOpenItem openItem)
        {
            var todoUrl = configuration["PraxisWebUrl"] + "/open-items/details/" + openItem.ItemId;
            return GetDynamicUrl(todoUrl);
        }
        private string GetPraxisTrainingUrl(PraxisTraining praxisTraining)
        {
            var trainingUrl = configuration["PraxisWebUrl"] + "/trainings/" + praxisTraining.ItemId + "/detail";
            _logger.LogInformation("Training Url: {trainingUrl}", trainingUrl);
            _logger.LogInformation("GetDynamicUrl Url: {trainingUrl}", GetDynamicUrl(trainingUrl));
            return GetDynamicUrl(trainingUrl);
        }
        private string GetPraxisEquipmentMaintenanceUrl(PraxisEquipmentMaintenance praxisEquipmentMaintenance, PraxisEquipmentMaintenanceByExternalUser externalUserInfo)
        {
            if (externalUserInfo != null)
            {
                return configuration["PraxisWebUrl"] + "/riqs-service/equipment/" + praxisEquipmentMaintenance.PraxisEquipmentId + "/" + externalUserInfo.SupplierInfo?.SupplierId;
            }
            return configuration["PraxisWebUrl"] + "/equipment-management/details/" + praxisEquipmentMaintenance.PraxisEquipmentId + "/maintenance";
        }
        private string GetCirsReportUrl(CirsGenericReport report, MinimalSupplierInfo externalUserInfo)
        {
            if (externalUserInfo != null)
            {
                return configuration["PraxisWebUrl"] + "/external-login/" + externalUserInfo.ExternalUserId;
            }

            return configuration["PraxisWebUrl"] + $"/cirs-report?viewMode=active&dashboardName={report.CirsDashboardName}&cirsReportId={report.ItemId}";
        }
        private string GetHostUrl()
        {
            return configuration["PraxisWebUrl"];
        }

        private string GetDynamicUrl(string url)
        {
            try
            {
                var firebaseConfiguration = _repository.GetItem<PraxisFirebaseConfiguration>(f => true);
                if (firebaseConfiguration != null && firebaseConfiguration.PackageList != null)
                {
                    var mailTemplatePackage = firebaseConfiguration.PackageList.Find(p => p.PackageName == "MailTempletePackage");
                    if (mailTemplatePackage != null)
                    {
                        var domainUriPrefix = mailTemplatePackage.DomainUriPrefix;
                        var androidPackageName = mailTemplatePackage.AndroidPackageName;
                        var iosBundleId = mailTemplatePackage.IosInfo.IosBundleId;
                        var iosAppStoreId = mailTemplatePackage.IosInfo.IosAppStoreId;

                        var payload = new DynamicLinkGeneratePayload
                        {
                            DynamicLinkInfo = new DynamicLinkInfo
                            {
                                Link = url,
                                DomainUriPrefix = domainUriPrefix,
                                AndroidInfo = new AndroidInfo { AndroidPackageName = androidPackageName },
                                IosInfo = new IosInfo { IosBundleId = iosBundleId, IosAppStoreId = iosAppStoreId }
                            }
                        };

                        var fireBaseUrl = string.Format(_firebaseUrl, mailTemplatePackage.ApiKey);

                        return _createDynamicLinkService.CreateLink(fireBaseUrl, payload);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during generating dynamic link from Firebase with url: {Url}. Exception Message: {Message}. Exception details: {StackTrace}.",
                    url, ex.Message, ex.StackTrace);
                return url;
            }
            return url;
        }

        public Dictionary<string, string> BuildMaintenanceDeleteEmailData(Person person, string scheduleType, string equipmentName)
        {
            return new Dictionary<string, string>
            {
                { "DisplayName", person.DisplayName },
                { "ScheduleType", scheduleType },
                { "EquipmentName", equipmentName }
            };
        }

        public Dictionary<string, string> BuildProcessGuideOverDueEmailData(Person person, DateTime dueDate, PraxisProcessGuide praxisProcessGuide, string clientName)
        {
            var taskScheduleDto = new Dictionary<string, string>
            {
                {"ProcessGuideTitle" , praxisProcessGuide.Title},
                {"Description" , praxisProcessGuide.Description},
                {"DisplayName", person.DisplayName },
                {"ProcessGuideOverDueDateString",  GetProcessGuideOverDueDateString(dueDate)},
                {"ProcessGuideUrl", GetPraxisProcessGuideUrl(praxisProcessGuide) },
                {"ProcessGuideItemId", praxisProcessGuide.ItemId },
                {"Language",praxisProcessGuide.Language},
                {"ClientName",clientName},
            };

            return taskScheduleDto;
        }

        private string GetProcessGuideOverDueDateString(DateTime date)
        {
            return date.ToString("dd MMMM yyyy") + " (" + date.DayOfWeek.ToString() + ")";
        }

        private string GetPraxisProcessGuideUrl(PraxisProcessGuide praxisProcessGuide)
        {
            var taskUrl = configuration["PraxisWebUrl"] + "/process-guide/detail/" + praxisProcessGuide.ItemId;
            return GetDynamicUrl(taskUrl);
        }

        private string GetPraxisEquipmentMaintenanceSignatureUrl(PraxisEquipmentMaintenance praxisEquipmentMaintenance, string reportId, string externalUserId = null)
        {
            if (!string.IsNullOrEmpty(externalUserId))
            {
                return $"{configuration["PraxisWebUrl"]}riqs-service/equipment/{praxisEquipmentMaintenance.PraxisEquipmentId}/{reportId}/{externalUserId}";
            }
            return configuration["PraxisWebUrl"] + "/equipment-management/details/" + praxisEquipmentMaintenance.PraxisEquipmentId + "/maintenance";
        }

        public Dictionary<string, string> BuildExternalSignatureEmailData(PraxisEquipmentMaintenance maintenance, Person person, string reportId, string reportTitle,
            PraxisEquipmentMaintenanceByExternalUser externalUserInfo = null)
        {
            var externalSignatureUrl = GetPraxisEquipmentMaintenanceSignatureUrl(maintenance, reportId, externalUserInfo?.SupplierInfo?.SupplierId);
            var equipment = _repository.GetItem<PraxisEquipment>(e => e.ItemId == maintenance.PraxisEquipmentId);
            var clientId = equipment?.ClientId ?? string.Empty;
            var client = _repository.GetItem<PraxisClient>(c => c.ItemId == clientId && !c.IsMarkedToDelete);
            var organizationId = client?.ParentOrganizationId ?? string.Empty;
            var organization = _repository.GetItem<PraxisOrganization>(o => o.ItemId == organizationId);
            
            return new Dictionary<string, string>
            {
                { "ExternalUserName", person.DisplayName },
                { "ExternalSignatureUrl", externalSignatureUrl },
                { "ReportTitle", reportTitle },
                { "EquipmentName", equipment?.Name },
                { "OrganizationName", organization?.ClientName },
                { "DueDate", maintenance?.MaintenanceEndDate.ToString("dd/MM/yyyy") }
            };
        }
    }
}
