using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.CommandHandlers.QuickTaskModule
{
    public class DeleteQuickTaskCommandHandler : ICommandHandler<DeleteQuickTaskCommand, CommandResponse>
    {
        private readonly IQuickTaskService _service;
        private readonly ILogger<DeleteQuickTaskCommandHandler> _logger;
        public DeleteQuickTaskCommandHandler(IQuickTaskService service, ILogger<DeleteQuickTaskCommandHandler> logger)
        {
            _service = service;
            _logger = logger;
        }
        public CommandResponse Handle(DeleteQuickTaskCommand command)
        {
            throw new NotImplementedException();
        }
        public async Task<CommandResponse> HandleAsync(DeleteQuickTaskCommand command)
        {
            var response = new CommandResponse();
            _logger.LogInformation("Enter {HandlerName} with payload:{Payload}.", nameof(DeleteQuickTaskCommandHandler), JsonConvert.SerializeObject(command));
            try
            {
                await _service.DeleteQuickTask(command.QuickTaskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in {HandlerName}", nameof(DeleteQuickTaskCommandHandler));
                _logger.LogError(ex, "Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled By {HandlerName} with payload:{Payload}.", nameof(DeleteQuickTaskCommandHandler), JsonConvert.SerializeObject(command));
            return response;
        }
    }
} 