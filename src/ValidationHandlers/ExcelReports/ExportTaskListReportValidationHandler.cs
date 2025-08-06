using System;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ExcelReports
{
    public class ExportTaskListReportValidationHandler : IValidationHandler<ExportTaskListReportCommand, CommandResponse>
    {
        private readonly ExportTaskListReportCommandValidator validator;
        private readonly IPraxisClientService praxisClientService;

        public ExportTaskListReportValidationHandler(ExportTaskListReportCommandValidator validator, IPraxisClientService praxisClientService)
        {
            this.validator = validator;
            this.praxisClientService = praxisClientService;
        }

        public CommandResponse Validate(ExportTaskListReportCommand command)
        {
            throw new NotImplementedException();
        }
      
        public async Task<CommandResponse> ValidateAsync(ExportTaskListReportCommand command)
        {
            var validationResult = validator.IsSatisfiedby(command);

            if (!validationResult.IsValid)
                return new CommandResponse(validationResult);

            var response = new CommandResponse();

            bool isValidGuid = Guid.TryParse(command.ReportFileId, out _);

            if (!isValidGuid)
                response.SetError("Exception", "Guid is not valid of ReportFileId");

            PraxisClient client = await praxisClientService.GetPraxisClient(command.ClientId);

            if (client == null)
                response.SetError("Exception", "Client not found for given ClientId");

            return response;
        }
    }
}