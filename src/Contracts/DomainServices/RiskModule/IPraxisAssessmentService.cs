using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisAssessmentService
    {
        PraxisAssessment GetPraxisAssessment(string itemId);
        PraxisAssessment GetRecentPraxisAssessment(string riskItemId);
        void UpdateRecentAssessment(string riskId);
        List<PraxisAssessment> GetAllPraxisAssessment();
        void AddRowLevelSecurity(string itemId, string clientId);
        void RemoveRowLevelSecurity(string clientId);
    }
}
