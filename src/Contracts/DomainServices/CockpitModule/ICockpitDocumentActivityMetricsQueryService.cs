using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule
{
    public interface ICockpitDocumentActivityMetricsQueryService
    {
        Task<List<CockpitDocumentActivityMetricsResponse>> InitiateGetCockpitDocumentActivityMetrics(CockpitDocumentActivityMetricsQuery query);
    }
}