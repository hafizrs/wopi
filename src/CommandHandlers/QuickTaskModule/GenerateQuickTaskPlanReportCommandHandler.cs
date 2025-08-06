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
    public class GenerateQuickTaskPlanReportCommandHandler : ICommandHandler<GenerateQuickTaskPlanReportCommand, CommandResponse>
    {
        private readonly IExportReportService _exportReportService;
        private readonly ILogger<GenerateQuickTaskPlanReportCommandHandler> _logger;
        public GenerateQuickTaskPlanReportCommandHandler(IExportReportService exportReportService, ILogger<GenerateQuickTaskPlanReportCommandHandler> logger)
        {
            _exportReportService = exportReportService;
            _logger = logger;
        }
        public CommandResponse Handle(GenerateQuickTaskPlanReportCommand command)
        {
            throw new NotImplementedException();
        }
        public async Task<CommandResponse> HandleAsync(GenerateQuickTaskPlanReportCommand command)
        {
            _logger.LogInformation("Enter {CommandName} with payload: {CommandPayload}.", nameof(GenerateQuickTaskPlanReportCommand), JsonConvert.SerializeObject(command));
            var success = await _exportReportService.GenerateQuickTaskPlanReportAsync(command);
            _logger.LogInformation("{CommandName} -> {Status}", nameof(GenerateQuickTaskPlanReportCommand), success ? "successful" : "failed");
            _logger.LogInformation("Handled By {HandlerName}.", nameof(GenerateQuickTaskPlanReportCommandHandler));
            return new CommandResponse();
        }
    }
} 