using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetValidFileUploadRequestQueryHandler : IQueryHandler<GetValidFileUploadRequestQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetValidFileUploadRequestQueryHandler> _logger;
        private readonly IDepartmentSubscriptionService _departmentSubscriptionService;

        public GetValidFileUploadRequestQueryHandler(
            ILogger<GetValidFileUploadRequestQueryHandler> logger,
            IDepartmentSubscriptionService departmentSubscriptionService)
        {
            _logger = logger;
            _departmentSubscriptionService = departmentSubscriptionService;
        }

        public QueryHandlerResponse Handle(GetValidFileUploadRequestQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetValidFileUploadRequestQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {QueryName}",
                nameof(GetValidFileUploadRequestQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                CheckValidUploadFileRequestResponse checkValidUploadFileRequest = await _departmentSubscriptionService.GetValidUploadFileRequestInDepartmentSubscription(query);
                response.Data = checkValidUploadFileRequest;
                response.StatusCode = 0;
                response.TotalCount = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetValidFileUploadRequestQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}",
                nameof(GetValidFileUploadRequestQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
