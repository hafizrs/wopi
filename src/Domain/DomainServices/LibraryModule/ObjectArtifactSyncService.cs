using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactSyncService : IObjectArtifactSyncService
    {
        private readonly IRepository _repository;
        private readonly IGenericEventPublishService _genericEventPublishService;

        public ObjectArtifactSyncService(
            IRepository repository,
            IGenericEventPublishService genericEventPublishService
        )
        {
            _repository = repository;
            _genericEventPublishService = genericEventPublishService;
        }

        public async Task UpdateEntityDependencyAsync(List<string> artifactIds, ObjectArtifact updatedArtifact)
        {
            await UpdateEquipmentArtifacts(artifactIds, updatedArtifact);
            await UpdateEquipmentMaintenanceArtifacts(artifactIds, updatedArtifact);
            await UpdateDeveloperArtifacts(artifactIds, updatedArtifact);
            await UpdatePraxisOpenItems(artifactIds, updatedArtifact);
            await UpdateShiftPlans(artifactIds, updatedArtifact);
            await UpdateShifts(artifactIds, updatedArtifact);
            await UpdateGenericReports(artifactIds, updatedArtifact);
        }

        private async Task UpdateGenericReports(List<string> artifactIds, ObjectArtifact artifact)
        {
            if (!string.IsNullOrEmpty(artifact.ItemId) && !artifactIds.Contains(artifact.ItemId))
            {
                var reports = _repository.GetItems<CirsGenericReport>
                        (e => !e.IsMarkedToDelete && ((e.AttachedDocuments != null && e.AttachedDocuments.Any(f => artifactIds.Contains(f.ItemId))) ||
                            (e.AttachedForm != null && artifactIds.Contains(e.AttachedForm.ItemId))))?
                        .ToList() ?? new List<CirsGenericReport>();
                foreach (var report in reports)
                {
                    var files = report.AttachedDocuments?.Where(f => artifactIds.Contains(f.ItemId))?.ToList() ?? new List<ReportingAttachmentFile>();
                    if (!string.IsNullOrEmpty(report?.AttachedForm?.ItemId) && artifactIds.Contains(report?.AttachedForm?.ItemId))
                    {
                        files.Add(report.AttachedForm);
                    }
                    foreach (var file in files)
                    {
                        if (report.AttachmentIds != null && report.AttachmentIds.Any(a => a == file.FileStorageId))
                        {
                            report.AttachmentIds = report.AttachmentIds
                                                .Where(a => a != file.FileStorageId)
                                                .Append(file.FileStorageId)
                                                .ToList();
                        }
                        file.ItemId = artifact.ItemId;
                        file.FileStorageId = artifact.FileStorageId;
                    }
                    await _repository.UpdateAsync(e => e.ItemId == report.ItemId, report);

                    _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(report);
                }
            }
        }

        private async Task UpdatePraxisOpenItems(List<string> artifactIds, ObjectArtifact artifact)
        {
            if (!string.IsNullOrEmpty(artifact.ItemId))
            {
                var praxisOpenItems = _repository.GetItems<PraxisOpenItem>
                        (
                            e=> !e.IsMarkedToDelete && !(e.OverAllCompletionStatus != null && e.OverAllCompletionStatus.Key == "done") &&
                                ((e.DocumentInfo != null && e.DocumentInfo.Any(f => artifactIds.Contains(f.DocumentId))) ||
                                 (artifactIds.Contains(e.TaskReferenceId) && e.TaskReference != null && e.TaskReference.Key == "form"))
                        )?.ToList() ?? new List<PraxisOpenItem>();
                foreach (var praxisOpenItem in praxisOpenItems)
                {
                    var files = praxisOpenItem.DocumentInfo?.Where(f => artifactIds.Contains(f.DocumentId))?.ToList() ?? new List<PraxisOpenItemDocument>();
                    foreach (var file in files)
                    {
                        file.DocumentId = artifact.ItemId;
                        file.DocumnentName = artifact.Name;
                    }
                    if (artifactIds.Contains(praxisOpenItem.TaskReferenceId) && praxisOpenItem.TaskReference?.Key == "form")
                    {
                        praxisOpenItem.TaskReferenceId = artifact.ItemId;
                        praxisOpenItem.TaskReferenceTitle = artifact.Name;
                    }
                    await _repository.UpdateAsync(e => e.ItemId == praxisOpenItem.ItemId, praxisOpenItem);

                    _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(praxisOpenItem);
                }
            }
        }

        private async Task UpdateShifts(List<string> artifactIds, ObjectArtifact artifact)
        {
            if (!string.IsNullOrEmpty(artifact.ItemId))
            {
                var shifts = _repository.GetItems<RiqsShift>
                        (e => !e.IsMarkedToDelete && ((e.Files != null && e.Files.Any(f => artifactIds.Contains(f.DocumentId))) || 
                            (e.LibraryForms != null && e.LibraryForms.Any(f => artifactIds.Contains(f.LibraryFormId)))) )?
                        .ToList()
                        ?? new List<RiqsShift>();
                foreach (var shift in shifts)
                {
                    var files = shift?.Files?.Where(f => artifactIds.Contains(f.DocumentId))?.ToList() ?? new List<PraxisDocument>();
                    foreach (var file in files)
                    {
                        file.DocumentId = artifact.ItemId;
                        file.DocumentName = artifact.Name;
                    }
                    var libraryForms = shift?.LibraryForms?.Where(f => artifactIds.Contains(f.LibraryFormId))?.ToList() ?? new List<PraxisLibraryEntityDetail>();
                    foreach (var libraryForm in libraryForms)
                    {
                        libraryForm.LibraryFormId = artifact.ItemId;
                        libraryForm.LibraryFormName = artifact.Name;
                    }

                    await _repository.UpdateAsync(e => e.ItemId == shift.ItemId, shift);

                    _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(shift);
                }
            }
        }

        private async Task UpdateShiftPlans(List<string> artifactIds, ObjectArtifact artifact)
        {
            var currentDate = DateTime.UtcNow.Date;
            if (!string.IsNullOrEmpty(artifact.ItemId))
            {
                var shiftPlans = _repository.GetItems<RiqsShiftPlan>(e => 
                                     !e.IsMarkedToDelete &&
                                     e.ShiftDate >= currentDate &&
                                     e.Shift != null &&
                                     ((e.Shift.Files != null &&
                                     e.Shift.Files.Any(s => artifactIds.Contains(s.DocumentId))) || 
                                     (e.Shift.LibraryForms != null &&
                                      e.Shift.LibraryForms.Any(s => artifactIds.Contains(s.LibraryFormId)))))?
                        .ToList()
                        ?? new List<RiqsShiftPlan>();
                foreach (var shiftPlan in shiftPlans)
                {
                    var shift = shiftPlan.Shift;
                    if (shift == null) continue;
                    var files = shift.Files?
                                    .Where(f => artifactIds.Contains(f.DocumentId))?
                                    .ToList() ?? new List<PraxisDocument>();
                    foreach (var file in files)
                    {
                        file.DocumentId = artifact.ItemId;
                        file.DocumentName = artifact.Name;
                    }

                    var libraryForms = shift.LibraryForms?
                        .Where(f => artifactIds.Contains(f.LibraryFormId))
                        .ToList() ?? new List<PraxisLibraryEntityDetail>();
                    foreach (var libraryForm in libraryForms)
                    {
                        libraryForm.LibraryFormId = artifact.ItemId;
                        libraryForm.LibraryFormName = artifact.Name;
                    }
                    await _repository.UpdateAsync(e => e.ItemId == shiftPlan.ItemId, shiftPlan);
                }
            }
        }

        private async Task UpdateEquipmentArtifacts(List<string> artifactIds, ObjectArtifact artifact)
        {
            if (!string.IsNullOrEmpty(artifact.ItemId))
            {
                var equipments = _repository.GetItems<PraxisEquipment>
                        (e => !e.IsMarkedToDelete && e.Files != null && e.Files.Any(f => artifactIds.Contains(f.DocumentId)))?.ToList()
                        ?? new List<PraxisEquipment>();
                foreach (var equipment in equipments)
                {
                    var files = equipment.Files?.Where(f => artifactIds.Contains(f.DocumentId))?.ToList() ?? new List<PraxisDocument>();
                    foreach (var file in files)
                    {
                        file.DocumentId = artifact.ItemId;
                        file.DocumentName = artifact.Name;
                    }
                    await _repository.UpdateAsync(e => e.ItemId == equipment.ItemId, equipment);

                    _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(equipment);
                }
            }
        }

        private async Task UpdateEquipmentMaintenanceArtifacts(List<string> artifactIds, ObjectArtifact artifact)
        {
            if (!string.IsNullOrEmpty(artifact.ItemId))
            {
                var maintenances = _repository.GetItems<PraxisEquipmentMaintenance>
                        (m => !m.IsMarkedToDelete && !(m.CompletionStatus != null && m.CompletionStatus.Key == "done") && m.LibraryForms != null && m.LibraryForms.Any(f => artifactIds.Contains(f.LibraryFormId)))?.ToList()
                        ?? new List<PraxisEquipmentMaintenance>();
                foreach (var maintenance in maintenances)
                {
                    var files = maintenance.LibraryForms?.Where(f => artifactIds.Contains(f.LibraryFormId))?.ToList() ?? new List<PraxisLibraryEntityDetail>();
                    foreach (var file in files)
                    {
                        file.LibraryFormId = artifact.ItemId;
                        file.LibraryFormName = artifact.Name;
                    }
                    await _repository.UpdateAsync(m => m.ItemId == maintenance.ItemId, maintenance);
                    
                    _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(maintenance);
                }
            }
        }

        private async Task UpdateDeveloperArtifacts(List<string> artifactIds, ObjectArtifact artifact)
        {
            if (!string.IsNullOrEmpty(artifact.ItemId))
            {
                var developers = _repository.GetItems<PraxisForm>(d =>
                        !d.IsMarkedToDelete && (d.ProcessGuideCheckList != null &&
                        d.ProcessGuideCheckList.Any(c =>
                            c.ProcessGuideTask != null && c.ProcessGuideTask.Any(p =>
                                (p.Files != null && p.Files.Any(f => artifactIds.Contains(f.DocumentId))) ||
                                (p.LibraryForms != null && p.LibraryForms.Any(f => artifactIds.Contains(f.LibraryFormId)))
                            )
                        )) ||
                        (d.Files != null && d.Files.Any(f => artifactIds.Contains(f.DocumentId))) ||
                        (
                            d.QuestionsList != null && d.QuestionsList.Any(q =>
                                (q.Files != null && q.Files.Any(f => artifactIds.Contains(f.DocumentId))) ||
                                (
                                    q.QuestionOptions != null && q.QuestionOptions.Any(o =>
                                        o.Files != null && o.Files.Any(f => artifactIds.Contains(f.DocumentId))
                                    )
                                )
                            )
                        )
                    )?.ToList() ?? new List<PraxisForm>();

                foreach (var developer in developers)
                {
                    var files = developer?.ProcessGuideCheckList?
                        .SelectMany(c => c?.ProcessGuideTask?
                            .SelectMany(p => p?.Files?.Where(f => artifactIds.Contains(f.DocumentId)))
                        )?.ToList() ?? new List<PraxisDocument>();

                    files.AddRange
                    (
                        developer?.Files?.Where(f => artifactIds.Contains(f.DocumentId))?
                        .ToList() ?? new List<PraxisDocument>()
                    );
                    files.AddRange
                    (
                        developer?.QuestionsList?
                        .SelectMany(c => c?.Files?.Where(f => artifactIds.Contains(f.DocumentId)))
                        ?.ToList() ?? new List<PraxisDocument>()
                    );
                    files.AddRange
                    (
                        developer?.QuestionsList?
                        .SelectMany(c => c?.QuestionOptions?.SelectMany(o => o.Files?.Where(f => artifactIds.Contains(f.DocumentId))))
                        ?.ToList() ?? new List<PraxisDocument>()
                    );

                    foreach (var file in files)
                    {
                        file.DocumentId = artifact.ItemId;
                        file.DocumentName = artifact.Name;
                    }

                    var libraryForms = developer?.ProcessGuideCheckList?
                        .SelectMany(c => c?.ProcessGuideTask?
                            .SelectMany(p => p?.LibraryForms?.Where(f => artifactIds.Contains(f.LibraryFormId)))
                        )?.ToList() ?? new List<PraxisLibraryEntityDetail>();

                    foreach (var libraryForm in libraryForms)
                    {
                        libraryForm.LibraryFormId = artifact.ItemId;
                        libraryForm.LibraryFormName = artifact.Name;
                    }
                    await _repository.UpdateAsync(d => d.ItemId == developer.ItemId, developer);
                    await UpdateAssignedTaskFormArtifacts(developer, artifact.ItemId, artifact);

                    _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(developer);
                }
            }
        }

        private async Task UpdateAssignedTaskFormArtifacts(
            PraxisForm praxisForm,
            string artifactId,
            ObjectArtifact artifact
            )
        {
            var assignTaskForms = _repository.GetItems<AssignedTaskForm>(f => f.ClonedFormId == praxisForm.ItemId).ToList();

            foreach (var assignTaskForm in assignTaskForms)
            {
                var currentTime = DateTime.UtcNow;
                var isPgExist = await _repository.ExistsAsync<PraxisProcessGuide>
                            (pg => !pg.IsMarkedToDelete && pg.ItemId == assignTaskForm.AssignedEntityId && 
                                    (pg.IsATemplate || (pg.TaskSchedule != null && pg.TaskSchedule.ToDateTime > currentTime)));

                if (!isPgExist) continue;


                var files = assignTaskForm?.ProcessGuideCheckList?
                        .SelectMany(c => c?.ProcessGuideTask?
                            .SelectMany(p => p?.Files?.Where(f => f?.DocumentId == artifactId))
                        )?.ToList() ?? new List<PraxisDocument>();

                files.AddRange
                (
                    assignTaskForm?.Files?.Where(f => f?.DocumentId == artifactId)?
                    .ToList() ?? new List<PraxisDocument>()
                );
                files.AddRange
                (
                    assignTaskForm?.QuestionsList?
                    .SelectMany(c => c?.Files?.Where(f => f?.DocumentId == artifactId))
                    ?.ToList() ?? new List<PraxisDocument>()
                );
                files.AddRange
                (
                    assignTaskForm?.QuestionsList?
                    .SelectMany(c => c?.QuestionOptions?.SelectMany(o => o.Files?.Where(f => f?.DocumentId == artifactId)))
                    ?.ToList() ?? new List<PraxisDocument>()
                );

                foreach (var file in files)
                {
                    file.DocumentId = artifact.ItemId;
                    file.DocumentName = artifact.Name;
                }

                var libraryForms = assignTaskForm?.ProcessGuideCheckList?
                        .SelectMany(c => c?.ProcessGuideTask?
                            .SelectMany(p => p?.LibraryForms?.Where(f => f.LibraryFormId == artifactId))
                        )?.ToList() ?? new List<PraxisLibraryEntityDetail>();

                foreach (var libraryForm in libraryForms)
                {
                    libraryForm.LibraryFormId = artifact.ItemId;
                    libraryForm.LibraryFormName = artifact.Name;
                }
                await _repository.UpdateAsync(f => f.ItemId == assignTaskForm.ItemId, assignTaskForm);
            }
        }
    }
}