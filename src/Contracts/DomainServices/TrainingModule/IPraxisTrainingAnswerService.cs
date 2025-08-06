using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisTrainingAnswerService
    {
        void UpdatePraxisAnswerSummary(PraxisTrainingAnswer praxisTrainingAnswer);
        void AddRowLevelSecurity(string itemId, string clientId);
        void RemoveRowLevelSecurity(string clientId);
        Task<EntityQueryResponse<PraxisTrainingAnswer>> GetTrainingAnswerData(string filter, string sort);

        Task<Dictionary<string, TrainingAnswerQueryResponse>> GetPraxisTrainingAnswerWithAssignedMembers(GetTrainingAnswersQuery query);
    }
}