namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class GenerateDocumentUsingTemplateEngineCommand: GeneratePdfUsingTemplateEngineCommand
    {
        public string FilterString { get; set; }
        public string SortBy { get; set; }
        public bool ForExternalUser { get; set; } = false;
        public string EntityName { get; set; }
    }
}