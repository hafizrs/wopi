using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Dms
{
    public class Library : ILibraryForm
    {
        public string LibraryFormId { get; set; }
        public string LibraryFormName { get; set; }
        public string FormId => LibraryFormId;
        public string FormName => LibraryFormName;
    }
}