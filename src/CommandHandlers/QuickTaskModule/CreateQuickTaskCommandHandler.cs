using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.CommandHandlers.QuickTaskModule
{
    public class CreateQuickTaskCommandHandler : ICommandHandler<CreateQuickTaskCommand, CommandResponse>
    {
        private readonly IQuickTaskService _service;
        private readonly ILogger<CreateQuickTaskCommandHandler> _logger;
        public CreateQuickTaskCommandHandler(IQuickTaskService service, ILogger<CreateQuickTaskCommandHandler> logger)
        {
            _service = service;
            _logger = logger;
        }
        public CommandResponse Handle(CreateQuickTaskCommand command)
        {
            throw new NotImplementedException();
        }
        public async Task<CommandResponse> HandleAsync(CreateQuickTaskCommand command)
        {
            var response = new CommandResponse();
            _logger.LogInformation("Enter {HandlerName} with payload:{Payload}.", nameof(CreateQuickTaskCommand), JsonConvert.SerializeObject(command));
            try
            {
                await _service.CreateQuickTask(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in {HandlerName}", nameof(CreateQuickTaskCommandHandler));
                _logger.LogError(ex, "Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled By {HandlerName} with payload:{Payload}.", nameof(CreateQuickTaskCommandHandler), JsonConvert.SerializeObject(command));
            return response;
        }
    }
} 