using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetTrainingAnswersQueryHandler : IQueryHandler<GetTrainingAnswersQuery, QueryHandlerResponse>
    {
        private readonly IPraxisTrainingAnswerService _trainingAnswerService;

        public GetTrainingAnswersQueryHandler(IPraxisTrainingAnswerService trainingAnswerService)
        {
            _trainingAnswerService = trainingAnswerService;
        }

        public QueryHandlerResponse Handle(GetTrainingAnswersQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetTrainingAnswersQuery query)
        {
            try
            {
                var result = await _trainingAnswerService.GetPraxisTrainingAnswerWithAssignedMembers(query);
                return new QueryHandlerResponse
                {
                    Results = result,
                    TotalCount = result.Count
                };
            }
            catch (Exception e)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    TotalCount = 0,
                    ErrorMessage = e.Message
                };
            }
        }
    }
}