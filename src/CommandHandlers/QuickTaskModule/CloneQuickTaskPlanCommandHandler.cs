using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.CommandHandlers.QuickTaskModule
{
    public class CloneQuickTaskPlanCommandHandler : ICommandHandler<CloneQuickTaskPlanCommand, CommandResponse>
    {
        private readonly IQuickTaskService _service;
        private readonly ILogger<CloneQuickTaskPlanCommandHandler> _logger;
        public CloneQuickTaskPlanCommandHandler(IQuickTaskService service, ILogger<CloneQuickTaskPlanCommandHandler> logger)
        {
            _service = service;
            _logger = logger;
        }
        public CommandResponse Handle(CloneQuickTaskPlanCommand command)
        {
            throw new NotImplementedException();
        }
        public async Task<CommandResponse> HandleAsync(CloneQuickTaskPlanCommand command)
        {
            var response = new CommandResponse();
            _logger.LogInformation("Enter {HandlerName} with payload:{Payload}.", nameof(CloneQuickTaskPlanCommand), JsonConvert.SerializeObject(command));
            try
            {
                await _service.CloneQuickTaskPlan(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in {HandlerName}", nameof(CloneQuickTaskPlanCommandHandler));
                _logger.LogError(ex, "Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled By {HandlerName} with payload:{Payload}.", nameof(CloneQuickTaskPlanCommandHandler), JsonConvert.SerializeObject(command));
            return response;
        }
    }
} 