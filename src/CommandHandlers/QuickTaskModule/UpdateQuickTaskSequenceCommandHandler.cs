using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.CommandHandlers.QuickTaskModule
{
    public class UpdateQuickTaskSequenceCommandHandler : ICommandHandler<UpdateQuickTaskSequenceCommand, CommandResponse>
    {
        private readonly ILogger<UpdateQuickTaskSequenceCommandHandler> _logger;
        private readonly IQuickTaskService _quickTaskService;
        public UpdateQuickTaskSequenceCommandHandler(
            ILogger<UpdateQuickTaskSequenceCommandHandler> logger,
            IQuickTaskService quickTaskService
            )
        {
            _logger = logger;
            _quickTaskService = quickTaskService;
        }

        public CommandResponse Handle(UpdateQuickTaskSequenceCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> HandleAsync(UpdateQuickTaskSequenceCommand command)
        {
            var response = new CommandResponse();
            _logger.LogInformation("Enter {HandlerName} with payload: {Payload}.",
                nameof(UpdateQuickTaskSequenceCommandHandler), JsonConvert.SerializeObject(command));

            try
            {
                await _quickTaskService.UpdateQuickTaskSequence(command.QuickTaskIds?.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occured during quick task sequence update");
                _logger.LogError(ex, "Exception Message: {Message}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled By {HandlerName} with payload: {Payload}.",
                nameof(UpdateQuickTaskSequenceCommandHandler), JsonConvert.SerializeObject(command));

            return response;
        }
    }
} 