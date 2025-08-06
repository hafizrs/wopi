using System.Collections.Generic;
using EventHandlers.Models;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts;

namespace EventHandlers.Services
{
   public interface IPdfGeneratorService
    {
        Task GeneratePdf(PdfGeneratorPayload pdfGeneratorPayload);
        Task GeneratePdfReport(IDictionary<string, string> eventReferenceData);
        Task GeneratePdfUsingV1(GeneratePdfUsingTemplateEnginePayload pdfGeneratorPayload);
    }
}
