using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ExcelReports
{
    public class ExportTrainingReportCommandValidatorHandler : IValidationHandler<ExportTrainingReportCommand, CommandResponse>
    {
        private readonly ExportTrainingReportCommandValidator _validator;
        private readonly IPraxisClientService _praxisClientService;

        public ExportTrainingReportCommandValidatorHandler(
            ExportTrainingReportCommandValidator validator,
            IPraxisClientService praxisClientService)
        {
            _validator = validator;
            _praxisClientService = praxisClientService;
        }

        public CommandResponse Validate(ExportTrainingReportCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> ValidateAsync(ExportTrainingReportCommand command)
        {
            var validationResult = _validator.IsSatisfiedby(command);

            if (!validationResult.IsValid)
                return new CommandResponse(validationResult);

            var response = new CommandResponse();

            bool isValidGuid = Guid.TryParse(command.ReportFileId, out _);

            if (!isValidGuid)
                response.SetError("Exception", "Guid is not valid of ReportFileId");

            var client = await _praxisClientService.GetPraxisClient(command.ClientId);

            if (client == null)
                response.SetError("Exception", "Client not found for given ClientId");

            return response;
        }
    }
}
