using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports
{
    public interface IHtmlFromTemplateGeneratorService
    {
        public Task<bool> GenerateHtml(TemplateEnginePayload templateEnginePayload);
    }
}