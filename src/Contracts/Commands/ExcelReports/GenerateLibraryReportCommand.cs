using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ObjectArtifactSearchCommand;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class GenerateLibraryReportCommand : ExportReportCommand
    {
        public string Type { get; set; }
        public ArtifactTypeEnum? ArtifactType { get; set; }
        public string Text { get; set; }
        public string ParentId { get; set; }
        public SearchFilter SearchFilter { get; set; }
    }
}
