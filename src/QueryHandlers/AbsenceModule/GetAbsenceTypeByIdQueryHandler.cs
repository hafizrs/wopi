using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.AbsenceModule
{
    public class GetAbsenceTypeByIdQueryHandler : IQueryHandler<GetAbsenceTypeByIdQuery, QueryHandlerResponse>
    {
        private readonly IAbsenceOverviewService _absenceOverviewService;
        private readonly ILogger<GetAbsenceTypeByIdQueryHandler> _logger;

        public GetAbsenceTypeByIdQueryHandler(
            IAbsenceOverviewService absenceOverviewService,
            ILogger<GetAbsenceTypeByIdQueryHandler> logger)
        {
            _absenceOverviewService = absenceOverviewService;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(GetAbsenceTypeByIdQuery query)
        {
            throw new NotImplementedException();
        }

        public Task<QueryHandlerResponse> HandleAsync(GetAbsenceTypeByIdQuery query)
        {
            throw new NotImplementedException();
        }
    }
}