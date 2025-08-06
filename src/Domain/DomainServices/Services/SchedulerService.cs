using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class SchedulerService : ISchedulerService
    {
        private readonly ILogger<SchedulerService> _logger;
        private readonly IPraxisTaskService praxisTaskService;
        private readonly IPraxisEquipmentMaintenanceService praxisEquipmentMaintenanceService;
        private readonly IEmailNotifierService emailNotifierService;
        private readonly IRepository repository;
        private readonly IEmailDataBuilder emailDataBuilder;
        public SchedulerService(
            ILogger<SchedulerService> logger,
            IPraxisTaskService praxisTaskService,
            IPraxisEquipmentMaintenanceService praxisEquipmentMaintenanceService,
            IEmailNotifierService emailNotifierService,
            IRepository repository,
            IEmailDataBuilder emailDataBuilder)
        {
            _logger = logger;
            this.praxisTaskService = praxisTaskService;
            this.praxisEquipmentMaintenanceService = praxisEquipmentMaintenanceService;
            this.emailNotifierService = emailNotifierService;
            this.repository = repository;
            this.emailDataBuilder = emailDataBuilder;
        }
        public async Task<bool> ProcesNotFullfilledTask()
        {
            int pageNumber = 0;
            int totalCount = 0;

            while (true)
            {

                var previouseDay = GetPreviousDate();
                var startdate = new DateTime(previouseDay.Year, previouseDay.Month, previouseDay.Day, 0, 0, 0, DateTimeKind.Utc);
                var endDate = new DateTime(previouseDay.Year, previouseDay.Month, previouseDay.Day, 23, 59, 59, DateTimeKind.Utc);
                var builder = Builders<BsonDocument>.Filter;
                var taskDateTimeFilter = builder.Lte("TaskSchedule.TaskDateTime", BsonValue.Create(endDate)) &
                                         builder.Gte("TaskSchedule.TaskDateTime", BsonValue.Create(startdate));

                var taskmovedDatesFilter = builder.ElemMatch("TaskSchedule.TaskMovedDates",
                    builder.Lte("TaskSchedule.TaskMovedDates", BsonValue.Create(endDate).ToUniversalTime()) &
                    builder.Gte("TaskSchedule.TaskMovedDates", BsonValue.Create(startdate).ToUniversalTime())
                );
                var filter = builder.Or(taskDateTimeFilter, taskmovedDatesFilter) &
                             builder.Eq("IsActive", true) &
                             builder.Eq("TaskSchedule.HasToMoveNextDay", true) &
                             builder.Eq("TaskSchedule.SubmissionDate", DateTime.MinValue) &
                             builder.Eq("IsMarkedToDelete", false) &
                             builder.Eq("TaskSchedule.IsCompleted", false);

                var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();
                var renderFilter = filter.Render(documentSerializer, BsonSerializer.SerializerRegistry);

                EntityQueryResponse<PraxisTask> dataset = await praxisTaskService.GetPraxisTasks(renderFilter.ToString(), "{CreateDate: 1}", pageNumber, 100);

                if (dataset.Results == null || dataset.TotalRecordCount < 1)
                {
                    break;
                }

                totalCount += dataset.Results.Count();

                var success = await StartProcess(dataset);

                if (!success) break;

                if (dataset.TotalRecordCount > totalCount)
                {
                    pageNumber++;
                }

                else break;
            }

            return false;
        }

        public async Task<bool> SendMaintenanceMailToResposibleUser()
        {
            int pageNumber = 0;
            int totalCount = 0;

            while (true)
            {
                var maintenanceDay = DateTime.UtcNow.AddDays(10);
                var startdate = new DateTime(maintenanceDay.Year, maintenanceDay.Month, maintenanceDay.Day, 0, 0, 0, DateTimeKind.Utc);
                var endDate = new DateTime(maintenanceDay.Year, maintenanceDay.Month, maintenanceDay.Day, 23, 59, 59, DateTimeKind.Utc);
                var builder = Builders<BsonDocument>.Filter;
                var maintenanceDateTimeFilter = builder.Lte("MaintenanceEndDate", BsonValue.Create(endDate)) &
                                       builder.Gte("MaintenanceEndDate", BsonValue.Create(startdate));

                var filter = builder.Or(maintenanceDateTimeFilter) &
                             builder.Eq("IsMarkedToDelete", false);

                var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();
                var renderFilter = filter.Render(documentSerializer, BsonSerializer.SerializerRegistry);

                EntityQueryResponse<PraxisEquipmentMaintenance> dataset = await praxisEquipmentMaintenanceService.GetPraxisMaintenances(renderFilter.ToString(), "{CreateDate: 1}", pageNumber, 100);

                if (dataset.Results == null || dataset.TotalRecordCount < 1)
                {
                    break;
                }

                totalCount += dataset.Results.Count();

                var success = await StartSendingMail(dataset);

                if (!success) break;

                if (dataset.TotalRecordCount > totalCount)
                {
                    pageNumber++;
                }

                else break;
            }



            return true;
        }


        private DateTime GetPreviousDate()
        {
            if (DateTime.UtcNow.ToString("ddd") == "Mon")
            {
                var PreviousDate = DateTime.UtcNow.AddDays(-3);
                return PreviousDate;
            }
            else
            {
                var PreviousDate = DateTime.UtcNow.AddDays(-1);
                return PreviousDate;
            }
        }

        private async Task<bool> StartProcess(EntityQueryResponse<PraxisTask> datasets)
        {
            var taskStatusList = new List<Task<bool>>();
            foreach (PraxisTask task in datasets.Results)
            {
                if (!string.IsNullOrEmpty(task.ItemId))
                {
                    var clientName = string.Empty;
                    var client = repository.GetItem<PraxisClient>(c => c.ItemId == task.ClientId);
                    if (client != null)
                    {
                        clientName = client.ClientName;
                    }
                    PraxisTaskConfig taskConfig = repository.GetItem<PraxisTaskConfig>(tc => tc.ItemId.Equals(task.TaskConfigId) && !tc.IsMarkedToDelete);

                    if (taskConfig == null) return false;

                    if (task.TaskFulfillmentPercentage > task.TaskSchedule.TaskPercentage &&
                        taskConfig.TaskNotification.TaskNotFullFilled.IsEnable &&
                        taskConfig.TaskNotification.TaskNotFullFilled.Members.Any())
                    {
                        TaskSummary taskSummary =
                            repository.GetItem<TaskSummary>(ts => ts.ItemId.Equals(task.TaskSchedule.TaskSummaryId) && !ts.IsMarkedToDelete);

                        if (taskSummary != null)
                        {
                            foreach (string member in taskConfig.TaskNotification.TaskNotFullFilled.Members)
                            {
                                var fulfillmentMailTask = SendTaskNotFulfilledEmail(member, taskSummary, task, clientName);
                                taskStatusList.Add(fulfillmentMailTask);
                            }
                        }

                        await UpdatePraxisTaskAndSchedule(task);
                    }
                }
            }

            await Task.WhenAll(taskStatusList);

            return true;
        }



        private async Task<bool> UpdatePraxisTaskAndSchedule(PraxisTask task)
        {
            TaskSchedule taskSchedule =
                repository.GetItem<TaskSchedule>(ts => ts.ItemId.Equals(task.TaskSchedule.ItemId) && !ts.IsMarkedToDelete);

            if (taskSchedule != null && taskSchedule.HasToMoveNextDay && !taskSchedule.IsCompleted)
            {

                if (taskSchedule.AvoidWeekend && DateTime.Now.ToString("ddd") != "Sat" || DateTime.Now.ToString("ddd") != "Sun")
                {
                    return false;
                }

                if (taskSchedule.TaskMovedDates == null)
                {
                    List<DateTime> dates = new List<DateTime> { DateTime.Today };
                    taskSchedule.TaskMovedDates = dates;
                }
                else
                {
                    List<DateTime> taskMovedDates = taskSchedule.TaskMovedDates.ToList();
                    taskMovedDates.Add(DateTime.Today);
                    taskSchedule.TaskMovedDates = taskMovedDates.AsEnumerable().Distinct();
                }

                taskSchedule.IsOverdue = false;
                await repository.UpdateAsync(ts => ts.ItemId.Equals(taskSchedule.ItemId), taskSchedule);

                task.TaskSchedule = taskSchedule;
                await repository.UpdateAsync(pt => pt.ItemId.Equals(task.ItemId), task);

                return true;
            }

            return false;
        }

        private async Task<bool> SendTaskNotFulfilledEmail(string personId, TaskSummary taskSummary, PraxisTask task, string clientName)
        {
            var person = repository.GetItem<Person>(p => p.ItemId.Equals(personId) && !p.IsMarkedToDelete);

            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                var emailData = emailDataBuilder.BuildTaskSummaryEmailData(taskSummary, person, task, clientName);
                return await emailNotifierService.SendTaskNotFulfilledEmail(person, emailData);
            }

            return false;
        }


        private async Task<bool> StartSendingMail(EntityQueryResponse<PraxisEquipmentMaintenance> datasets)
        {
            if (datasets?.Results == null || !datasets.Results.Any())
            {
                _logger.LogInformation("No equipment maintenance records found to process.");
                return false;
            }
            var taskStatusList = new List<Task<bool>>();
            foreach (PraxisEquipmentMaintenance equipmentMaintenance in datasets.Results)
            {
                try
                {
                    if (string.IsNullOrEmpty(equipmentMaintenance?.PraxisEquipmentId) || string.IsNullOrEmpty(equipmentMaintenance.ItemId))
                    {
                        continue;
                    }
                    
                    var responsiblePersonIds = equipmentMaintenance.ResponsiblePersonIds?.ToList() ?? new List<string>();
                    if (responsiblePersonIds.Count == 0)
                        continue;
                    
                    PraxisEquipment praxisEquipment = await repository.GetItemAsync<PraxisEquipment>(pe => pe.ItemId.Equals(equipmentMaintenance.PraxisEquipmentId) && !pe.IsMarkedToDelete);
                    
                    if (string.IsNullOrEmpty(praxisEquipment?.ItemId))
                        continue;
                    
                    foreach (var personId in responsiblePersonIds)
                    {
                        if (string.IsNullOrEmpty(personId))
                            continue;

                        var person = await repository.GetItemAsync<Person>(p => p.ItemId.Equals(personId) && !p.IsMarkedToDelete);

                        if (!string.IsNullOrWhiteSpace(person?.Email))
                        {
                            var emailData = emailDataBuilder.BuildEquipmentmaintenanceEmailData(praxisEquipment, equipmentMaintenance, person, praxisEquipment.ClientName);
                            var emailStatus = emailNotifierService.SendMaintenanceScheduleEmail(person, emailData);
                            taskStatusList.Add(emailStatus);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error processing equipment maintenance email for {EquipmentId}: {Message}", equipmentMaintenance.ItemId, ex.Message);
                    continue;
                }

            }

            if (taskStatusList.Any())
            {
                try
                {
                    var results = await Task.WhenAll(taskStatusList);
                    var successCount = results.Count(r => r);
                    var failureCount = results.Length - successCount;

                    _logger.LogInformation("Email sending completed: {successCount} successful, {failureCount} failed", successCount, failureCount);
                    return failureCount == 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error sending maintenance emails: {ex.Message}", ex.Message);
                    return false;
                }
            }

            return true;
        }
    }
}
