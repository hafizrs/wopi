using Selise.Ecap.Entities;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ObjectArtifactSearchCommand
    {
        public string ObjectArtifactId { get; set; }
        public string OrganizationId { get; set; }
        public string DepartmentId { get; set; }
        public string ParentId { get; set; }
        public string Type { get; set; }
        public ArtifactTypeEnum? ArtifactType { get; set; }
        public string Text { get; set; }
        public bool IsRootSearch { get; set; } = true;
        public SearchFilter Filter { get; set; }
        public SortByModel Sort { get; set; } =
            new SortByModel()
            {
                PropertyName = nameof(ObjectArtifact.CreateDate),
                Direction = SortDirectionEnum.Descending
            };
        public int PageSize { get; set; } = 50;
        public int PageNumber { get; set; } = 1;
    }

    public class SearchFilter
    {
        public LibraryFileStatusEnum? Status { get; set; }
        public LibraryFileApprovalStatusEnum? ApprovalStatus { get; set; }
        public string Version { get; set; }
        public ActionWiseFilter UploadedDetail { get; set; }
        public ActionWiseFilter EditedDetail { get; set; }
        public ActionWiseFilter ApprovedDetail { get; set; }
        public AssignedToFilter AssignedDetail { get; set; }
        public string[] FormFilledBy { get; set; }
        public string[] FormFillPendingBy { get; set; }
        public LibraryFileTypeEnum? FileFormats { get; set; }
    }

    public class DateRange
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ActionWiseFilter
    {
        public DateRange DateRange { get; set; }
        public string PerformedBy { get; set; }
    }

    public class AssignedToFilter
    {
        public DateRange DateRange { get; set; }
        public string[] AssigneIds { get; set; }
    }
}
