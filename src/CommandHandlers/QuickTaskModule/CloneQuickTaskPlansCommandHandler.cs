using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;

namespace Selise.Ecap.SC.PraxisMonitor.CommandHandlers.QuickTaskModule
{
    public class CloneQuickTaskPlansCommandHandler : ICommandHandler<CloneQuickTaskPlansCommand, CommandResponse>
    {
        private readonly IQuickTaskService _quickTaskService;
        private readonly ILogger<CloneQuickTaskPlansCommandHandler> _logger;

        public CloneQuickTaskPlansCommandHandler(
            IQuickTaskService quickTaskService,
            ILogger<CloneQuickTaskPlansCommandHandler> logger
        )
        {
            _quickTaskService = quickTaskService;
            _logger = logger;
        }

        public CommandResponse Handle(CloneQuickTaskPlansCommand command)
        {
            return HandleAsync(command).Result;
        }

        public async Task<CommandResponse> HandleAsync(CloneQuickTaskPlansCommand command)
        {
            var response = new CommandResponse();

            try
            {
                _logger.LogInformation("Enter {ClassName} with payload: {Payload}",
                    nameof(CloneQuickTaskPlansCommandHandler), JsonConvert.SerializeObject(command));

                await _quickTaskService.CloneQuickTaskPlans(command);

                _logger.LogInformation("Handled By {ClassName} with payload: {Payload}",
                    nameof(CloneQuickTaskPlansCommandHandler), JsonConvert.SerializeObject(command));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in {ClassName} -> {MethodName}",
                    nameof(CloneQuickTaskPlansCommandHandler), nameof(HandleAsync));

                _logger.LogError(ex, "Exception Message: {Message} Exception Details: {StackTrace}", ex.Message,
                    ex.StackTrace);
            }

            return response;
        }
    }
} 