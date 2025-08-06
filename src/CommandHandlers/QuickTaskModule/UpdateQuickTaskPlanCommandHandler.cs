using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.CommandHandlers.QuickTaskModule
{
    public class UpdateQuickTaskPlanCommandHandler : ICommandHandler<UpdateQuickTaskPlanCommand, CommandResponse>
    {
        private readonly IQuickTaskService _service;
        private readonly ILogger<UpdateQuickTaskPlanCommandHandler> _logger;
        public UpdateQuickTaskPlanCommandHandler(IQuickTaskService service, ILogger<UpdateQuickTaskPlanCommandHandler> logger)
        {
            _service = service;
            _logger = logger;
        }
        public CommandResponse Handle(UpdateQuickTaskPlanCommand command)
        {
            throw new NotImplementedException();
        }
        public async Task<CommandResponse> HandleAsync(UpdateQuickTaskPlanCommand command)
        {
            var response = new CommandResponse();
            _logger.LogInformation("Enter {HandlerName} with payload:{Payload}.", nameof(UpdateQuickTaskPlanCommandHandler), JsonConvert.SerializeObject(command));
            try
            {
                await _service.UpdateQuickTaskPlan(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in {HandlerName}", nameof(UpdateQuickTaskPlanCommandHandler));
                _logger.LogError(ex, "Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled By {HandlerName} with payload:{Payload}.", nameof(UpdateQuickTaskPlanCommandHandler), JsonConvert.SerializeObject(command));
            return response;
        }
    }
} 