using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Dms
{
    public class TaskReference : ILibraryForm
    {
        public string TaskReferenceId { get; set; }
        public string TaskReferenceTitle { get; set; }
        public string FormId => TaskReferenceId;
        public string FormName => TaskReferenceTitle;
    }
}