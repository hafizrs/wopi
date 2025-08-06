using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.ESignature.Service.Events;
using SeliseBlocks.Genesis.Framework.Events;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.Signature;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Signature;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class LibraryFormAdoptFactoryService : ILibraryFormAdoptFactoryService
    {
        private readonly IPraxisProcessGuideAnswerService _praxisProcessGuideAnswerService;
        private readonly IPraxisOpenItemService _praxisOpenItemServiceService;
        private readonly IPraxisEquipmentMaintenanceService _praxisEquipmentMaintenanceService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly ICirsReportUpdateService _cirsReportUpdateService;
        private readonly IPraxisShiftService _praxisShiftService;
        private readonly IQuickTaskService _quickTaskService;

        public LibraryFormAdoptFactoryService(
            IPraxisProcessGuideAnswerService praxisProcessGuideAnswerService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IPraxisOpenItemService praxisOpenItemServiceService,
            IPraxisEquipmentMaintenanceService praxisEquipmentMaintenanceService,
            ICirsReportUpdateService cirsReportUpdateService,
            IPraxisShiftService praxisShiftService,
            IQuickTaskService quickTaskService
        )
        {
            _praxisProcessGuideAnswerService = praxisProcessGuideAnswerService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _praxisOpenItemServiceService = praxisOpenItemServiceService;
            _praxisEquipmentMaintenanceService = praxisEquipmentMaintenanceService;
            _cirsReportUpdateService = cirsReportUpdateService;
            _praxisShiftService = praxisShiftService;
            _quickTaskService = quickTaskService;
        }

        public async Task AdoptLibraryFormResponse(ObjectArtifact objectArtifactData)
        {
            if (objectArtifactData == null || objectArtifactData.MetaData == null) return;

            var entityName =
                _objectArtifactUtilityService.GetMetaDataValueByKey(objectArtifactData.MetaData, "EntityName");
            if (entityName == EntityName.PraxisProcessGuide)
            {
                await _praxisProcessGuideAnswerService.UpdateProcessGuideLibraryFormResponse(objectArtifactData);
            }
            else if (entityName == EntityName.PraxisOpenItem)
            {
                await _praxisOpenItemServiceService.UpdateOpenItemLibraryFormResponse(objectArtifactData);
            }
            else if (entityName == EntityName.PraxisEquipmentMaintenance)
            {
                await _praxisEquipmentMaintenanceService.UpdateEquipmentMaintenanceLibraryFormResponse(objectArtifactData);
            }
            else if (entityName == EntityName.CirsGenericReport)
            {
                await _cirsReportUpdateService.UpdateLibraryFormResponse(objectArtifactData);
            }
            else if (entityName == EntityName.RiqsShiftPlan)
            {
                await _praxisShiftService.UpdateLibraryFormResponse(objectArtifactData);
            }
            else if (entityName == nameof(RiqsQuickTaskPlan))
            {
                await _quickTaskService.UpdateLibraryFormResponse(objectArtifactData);
            }
        }
    }
}
