namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class LibraryDirectoryGetCommand
    {
        public string ObjectArtifactId { get; set; }
        public string OrganizationId { get; set; }
        public string DepartmentId { get; set; }
        public string ParentId { get; set; }
        public string SearchText { get; set; }
        public bool IsDirectoryForFileUpload { get; set; }=false;
        public bool IsDirectoryForFilter { get; set; } = false;
    }
}