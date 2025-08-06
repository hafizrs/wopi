using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.CommandHandlers.QuickTaskModule
{
    public class DeleteQuickTaskPlanCommandHandler : ICommandHandler<DeleteQuickTaskPlanCommand, CommandResponse>
    {
        private readonly IQuickTaskService _service;
        private readonly ILogger<DeleteQuickTaskPlanCommandHandler> _logger;
        public DeleteQuickTaskPlanCommandHandler(IQuickTaskService service, ILogger<DeleteQuickTaskPlanCommandHandler> logger)
        {
            _service = service;
            _logger = logger;
        }
        public CommandResponse Handle(DeleteQuickTaskPlanCommand command)
        {
            throw new NotImplementedException();
        }
        public async Task<CommandResponse> HandleAsync(DeleteQuickTaskPlanCommand command)
        {
            var response = new CommandResponse();
            _logger.LogInformation("Enter {HandlerName} with payload:{Payload}.", nameof(DeleteQuickTaskPlanCommandHandler), JsonConvert.SerializeObject(command));
            try
            {
                if (command.QuickTaskPlanIds != null && command.QuickTaskPlanIds.Count > 0)
                {
                    await _service.DeleteQuickTaskPlan(command.QuickTaskPlanIds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in {HandlerName}", nameof(DeleteQuickTaskPlanCommandHandler));
                _logger.LogError(ex, "Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled By {HandlerName} with payload:{Payload}.", nameof(DeleteQuickTaskPlanCommandHandler), JsonConvert.SerializeObject(command));
            return response;
        }
    }
} 