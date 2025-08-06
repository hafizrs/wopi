using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisProcessGuideService
    {
        Task<EntityQueryResponse<PraxisProcessGuideWithClientCompletion>> GetPraxisProcessGuide(GetProcessGuideQuery filter);
        Task<EntityQueryResponse<ProcessGuideDetailsResponse>> GetPraxisProcessGuideDetails(List<string> processGuideIds, string praxisClientId = null, int timezoneOffsetInMinutes = 0);
        Task<bool> UpdateProcessGuideCompletionStatus(List<string> processGuideIds);
        Task<bool> UpdateProcessGuideControlledMemberIds();
        Task<EntityQueryResponse<PraxisProcessGuide>> GetProcessGuideData(string filter, string sort);
        bool AddRowLevelSecurity(string itemId, string userId);
        Task<PraxisGenericReportResult> PrepareProcessGuidePhotoDocumentationData(GetReportQuery filter);
        Task<IEnumerable<PraxisDocument>> ProcessFilesForReportAsync(List<PraxisDocument> files);
        List<string> GetProcessGuideIds(string configId);
        Task<bool> UpdateRowLevelSecurity(string processGuideId);
    }
}