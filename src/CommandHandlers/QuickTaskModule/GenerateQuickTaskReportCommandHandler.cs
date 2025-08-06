using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using System.Threading.Tasks;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.CommandHandlers.QuickTaskModule
{
    public class GenerateQuickTaskReportCommandHandler : ICommandHandler<GenerateQuickTaskReportCommand, CommandResponse>
    {
        private readonly IExportReportService _exportReportService;
        private readonly ILogger<GenerateQuickTaskReportCommandHandler> _logger;
        public GenerateQuickTaskReportCommandHandler(IExportReportService exportReportService, ILogger<GenerateQuickTaskReportCommandHandler> logger)
        {
            _exportReportService = exportReportService;
            _logger = logger;
        }
        public CommandResponse Handle(GenerateQuickTaskReportCommand command)
        {
            throw new NotImplementedException();
        }
        public async Task<CommandResponse> HandleAsync(GenerateQuickTaskReportCommand command)
        {
            _logger.LogInformation("Enter {CommandName} with payload: {CommandPayload}.", nameof(GenerateQuickTaskReportCommand), JsonConvert.SerializeObject(command));
            var success = await _exportReportService.GenerateQuickTaskReportAsync(command);
            _logger.LogInformation("{CommandName} -> {Status}", nameof(GenerateQuickTaskReportCommand), success ? "successful" : "failed");
            _logger.LogInformation("Handled By {HandlerName}.", nameof(GenerateQuickTaskReportCommandHandler));
            return new CommandResponse();
        }
    }
} 