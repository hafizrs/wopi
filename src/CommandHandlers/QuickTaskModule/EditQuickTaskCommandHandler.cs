using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.CommandHandlers.QuickTaskModule
{
    public class EditQuickTaskCommandHandler : ICommandHandler<EditQuickTaskCommand, CommandResponse>
    {
        private readonly IQuickTaskService _service;
        private readonly ILogger<EditQuickTaskCommandHandler> _logger;
        public EditQuickTaskCommandHandler(IQuickTaskService service, ILogger<EditQuickTaskCommandHandler> logger)
        {
            _service = service;
            _logger = logger;
        }
        public CommandResponse Handle(EditQuickTaskCommand command)
        {
            throw new NotImplementedException();
        }
        public async Task<CommandResponse> HandleAsync(EditQuickTaskCommand command)
        {
            var response = new CommandResponse();
            _logger.LogInformation("Enter {HandlerName} with payload:{Payload}.", nameof(EditQuickTaskCommandHandler), JsonConvert.SerializeObject(command));
            try
            {
                await _service.EditQuickTask(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in {HandlerName}", nameof(EditQuickTaskCommandHandler));
                _logger.LogError(ex, "Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled By {HandlerName} with payload:{Payload}.", nameof(EditQuickTaskCommandHandler), JsonConvert.SerializeObject(command));
            return response;
        }
    }
} 