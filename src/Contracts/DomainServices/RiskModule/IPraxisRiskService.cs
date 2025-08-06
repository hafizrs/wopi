using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisRiskService
    {
        PraxisRisk GetPraxisRisk(string itemId);
        List<PraxisRisk> GetAllPraxisRisk();
        void UpdateRecentAssessment(string riskId);
        void AddRowLevelSecurity(string itemId, string clientId);
        void RemoveRowLevelSecurity(string clientId);
        string GetCurrentRiskValue(PraxisAssessment assessment);
        Task<Dictionary<string, List<PraxisRiskChartData>>> GetPraxisRiskChartData(GetPraxisRiskChartDataQuery query);
        Task UpdateAttachmentInReporting(string riskId);
    }
}
