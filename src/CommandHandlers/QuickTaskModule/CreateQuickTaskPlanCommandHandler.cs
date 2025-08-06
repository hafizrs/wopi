using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.CommandHandlers.QuickTaskModule
{
    public class CreateQuickTaskPlanCommandHandler : ICommandHandler<CreateQuickTaskPlanCommand, CommandResponse>
    {
        private readonly IQuickTaskService _service;
        private readonly ILogger<CreateQuickTaskPlanCommandHandler> _logger;
        public CreateQuickTaskPlanCommandHandler(IQuickTaskService service, ILogger<CreateQuickTaskPlanCommandHandler> logger)
        {
            _service = service;
            _logger = logger;
        }
        public CommandResponse Handle(CreateQuickTaskPlanCommand command)
        {
            throw new NotImplementedException();
        }
        public async Task<CommandResponse> HandleAsync(CreateQuickTaskPlanCommand command)
        {
            var response = new CommandResponse();
            _logger.LogInformation("Enter {HandlerName} with payload:{Payload}.", nameof(CreateQuickTaskPlanCommand), JsonConvert.SerializeObject(command));
            try
            {
                await _service.CreateQuickTaskPlan(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in {HandlerName}", nameof(CreateQuickTaskPlanCommandHandler));
                _logger.LogError(ex, "Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled By {HandlerName} with payload:{Payload}.", nameof(CreateQuickTaskPlanCommandHandler), JsonConvert.SerializeObject(command));
            return response;
        }
    }
} 