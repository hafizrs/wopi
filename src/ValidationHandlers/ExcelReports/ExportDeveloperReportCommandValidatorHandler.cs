using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ExcelReports
{
    public class ExportDeveloperReportCommandValidatorHandler : IValidationHandler<ExportDeveloperReportCommand, CommandResponse>
    {
        private readonly ExportDeveloperReportCommandValidator _validator;
        private readonly IPraxisClientService _praxisClientService;

        public ExportDeveloperReportCommandValidatorHandler(
            ExportDeveloperReportCommandValidator validator,
            IPraxisClientService praxisClientService)
        {
            _validator = validator;
            _praxisClientService = praxisClientService;
        }
        public CommandResponse Validate(ExportDeveloperReportCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> ValidateAsync(ExportDeveloperReportCommand command)
        {
            var validationResult = _validator.IsSatisfiedby(command);

            if (!validationResult.IsValid)
                return new CommandResponse(validationResult);

            var response = new CommandResponse();

            bool isValidGuid = Guid.TryParse(command.ReportFileId, out _);

            if (!isValidGuid)
                response.SetError("Exception", "Guid is not valid of ReportFileId");

            if (!command.IsReportForAllData)
            {
                if (string.IsNullOrEmpty(command.ClientId))
                {
                    response.SetError("Exception", "ClientId required.");
                }
                var client = await _praxisClientService.GetPraxisClient(command.ClientId);

                if (client == null)
                    response.SetError("Exception", "Client not found for given ClientId");
                if (string.IsNullOrEmpty(command.FilterString))
                {
                    response.SetError("Exception", "FilterString required.");
                }
            }
            return response;
        }
    }
}
