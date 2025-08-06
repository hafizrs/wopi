using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class UserLogOutQueryHandler : IQueryHandler<UserLogOutQuery, UserLogOutResponse>
    {
        private readonly ILogger<UserLogOutQueryHandler> _logger;
        private readonly INotificationService _notificationProviderService;

        public UserLogOutQueryHandler(
            ILogger<UserLogOutQueryHandler> logger,
            INotificationService notificationProviderService)
        {
            _logger = logger;
            _notificationProviderService = notificationProviderService;
        }
        public UserLogOutResponse Handle(UserLogOutQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<UserLogOutResponse> HandleAsync(UserLogOutQuery query)
        {
            var response = new UserLogOutResponse();
            
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(UserLogOutQueryHandler), JsonConvert.SerializeObject(query));
            try
            {
                var result = new
                {
                    NotifiySubscriptionId = query.UserId,
                    Success = true
                };
                
                await _notificationProviderService.UserLogOutNotification(true, query.UserId, result, query.Context,
                    query.ActionName);

                response.StatusCode = 200;
                response.Message = string.Empty;
                
                _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                    nameof(UserLogOutQueryHandler), JsonConvert.SerializeObject(response));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(UserLogOutQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 500;
                response.Message = ex.Message;
                return response;
            }
        }
    }
}
