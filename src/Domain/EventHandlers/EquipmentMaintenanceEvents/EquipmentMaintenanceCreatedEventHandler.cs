using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.EquipmentMaintenanceEvents
{
    public class EquipmentMaintenanceCreatedEventHandler : IBaseEventHandler<GqlEvent<PraxisEquipmentMaintenance>>
    {
        private readonly IRepository repository;
        private readonly ILogger<EquipmentMaintenanceUpdatedEventHandler> _logger;
        private readonly IEmailNotifierService emailNotifierService;
        private readonly IEmailDataBuilder emailDataBuilder;
        private readonly IPraxisEquipmentMaintenanceService praxisEquipmentMaintenanceService;

        public EquipmentMaintenanceCreatedEventHandler(
            IRepository repository,
            ILogger<EquipmentMaintenanceUpdatedEventHandler> logger,
            IEmailNotifierService emailNotifierService,
            IEmailDataBuilder emailDataBuilder,
            IPraxisEquipmentMaintenanceService praxisEquipmentMaintenanceService
            
        )
        {
            this.repository = repository;
            _logger = logger;
            this.emailNotifierService = emailNotifierService;
            this.emailDataBuilder = emailDataBuilder;
            this.praxisEquipmentMaintenanceService = praxisEquipmentMaintenanceService;
        }
        public bool Handle(GqlEvent<PraxisEquipmentMaintenance> eventPayload)
        {
            try
            {
                praxisEquipmentMaintenanceService.AddRowLevelSecurity(
                    eventPayload.EntityData.ItemId,
                    eventPayload.EntityData.ClientId
                );

                UpdatePraxisEquipmentMaintenanceDates(eventPayload.EntityData);

                ProcessEmailForResponsibleUsers(eventPayload.EntityData).GetAwaiter().GetResult();

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("EquipmentMaintenanceCreatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return false;
        }

        private async Task<bool> ProcessEmailForResponsibleUsers(PraxisEquipmentMaintenance equipmentMaintenance)
        {
            var mailSendDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
            if (!(mailSendDate >= equipmentMaintenance.MaintenanceDate.Date && mailSendDate <= equipmentMaintenance.MaintenanceEndDate)) return false;

            PraxisEquipment praxisEquipment = await
                repository.GetItemAsync<PraxisEquipment>(pe => pe.ItemId.Equals(equipmentMaintenance.PraxisEquipmentId) && !pe.IsMarkedToDelete);

            if (!string.IsNullOrEmpty(equipmentMaintenance?.ItemId) && !string.IsNullOrEmpty(praxisEquipment?.ItemId))
            {
                var personIds = new List<string>();
                if (equipmentMaintenance.ExecutivePersonIds?.Count() > 0)
                {
                    personIds.AddRange(equipmentMaintenance.ExecutivePersonIds.ToList());
                }
                if (equipmentMaintenance.ApprovedPersonIds?.Count() > 0)
                {
                    personIds.AddRange(equipmentMaintenance.ApprovedPersonIds.ToList());
                }
                personIds = personIds.Distinct().ToList();

                var emailTasks = new List<Task<bool>>();

                foreach (var personId in personIds)
                {
                    var person = repository.GetItem<Person>(p => p.ItemId.Equals(personId) && !p.IsMarkedToDelete);

                    if (!string.IsNullOrWhiteSpace(person?.Email))
                    {
                        var emailData = emailDataBuilder.BuildEquipmentmaintenanceEmailData(praxisEquipment, equipmentMaintenance, person, praxisEquipment.ClientName);
                        var emailStatus = emailNotifierService.SendMaintenanceScheduleEmail(person, emailData);
                        emailTasks.Add(emailStatus);
                    }
                }

                if (equipmentMaintenance.ExternalUserInfos?.Count > 0)
                {
                    foreach (var externalInfo in equipmentMaintenance.ExternalUserInfos)
                    {
                        if (!string.IsNullOrEmpty(externalInfo?.SupplierInfo?.SupplierEmail))
                        {
                            var person = new Person()
                            {
                                DisplayName = externalInfo.SupplierInfo.SupplierName,
                                Email = externalInfo.SupplierInfo.SupplierEmail
                            };
                            var emailData = emailDataBuilder.BuildEquipmentmaintenanceEmailData(praxisEquipment, equipmentMaintenance, person, praxisEquipment.ClientName, externalInfo);
                            var emailStatus = emailNotifierService.SendMaintenanceScheduleEmail(person, emailData);
                            emailTasks.Add(emailStatus);
                        }
                    }
                }

                await Task.WhenAll(emailTasks);

                return true;
            }

            return false;
        }

        private void UpdatePraxisEquipmentMaintenanceDates(PraxisEquipmentMaintenance equipmentMaintenance)
        {
            try
            {
                _logger.LogInformation("Enter UpdatePraxisEquipmentMaintenanceDates ");
                PraxisEquipment praxisEquipment =
                    repository.GetItem<PraxisEquipment>(pe => pe.ItemId.Equals(equipmentMaintenance.PraxisEquipmentId) && !pe.IsMarkedToDelete);

                if (praxisEquipment != null)
                {
                    var toUpdateMaintenanceDate = new MaintenanceDateProp
                    {
                        ItemId = equipmentMaintenance.ItemId,
                        Date = equipmentMaintenance.MaintenanceEndDate,
                        CompletionStatus = equipmentMaintenance.CompletionStatus
                    };
                    if (praxisEquipment.MaintenanceDates == null)
                    {
                        List<MaintenanceDateProp> maintenanceDates = new List<MaintenanceDateProp>
                        {
                           toUpdateMaintenanceDate
                        };

                        praxisEquipment.MaintenanceDates = maintenanceDates;
                    }
                    else
                    {
                        List<MaintenanceDateProp> maintenanceDates = praxisEquipment.MaintenanceDates.ToList();
                        maintenanceDates.Add(toUpdateMaintenanceDate);
                        praxisEquipment.MaintenanceDates = maintenanceDates.OrderBy(md => md.Date).ToList();
                    }

                    MaintenanceDatePropWithType toUpdateMaintenanceWithType = new MaintenanceDatePropWithType()
                    {
                        Date = toUpdateMaintenanceDate.Date,
                        CompletionStatus = toUpdateMaintenanceDate.CompletionStatus,
                        ScheduleType = equipmentMaintenance.ScheduleType,
                        ItemId = toUpdateMaintenanceDate.ItemId,
                    };
                    praxisEquipmentMaintenanceService.UpdatePraxisEquipmentMaintenanceDatesMetaData(praxisEquipment, toUpdateMaintenanceWithType);
                    repository.Update(pe => pe.ItemId.Equals(praxisEquipment.ItemId),
                        praxisEquipment);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Got Error while updating praxisEquipment in EquipmentMaintenanceCreatedEventHandler");
            }
        }
    }
}
