using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ExcelReports
{
    public class
        ExportCirsReportCommandValidatorHandler : IValidationHandler<ExportReportCommand,
            CommandResponse>
    {
        private readonly ExportCirsReportCommandValidator _validator;

        public ExportCirsReportCommandValidatorHandler(ExportCirsReportCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(ExportReportCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(ExportReportCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            if (!validationResult.IsValid)
                return Task.FromResult(new CommandResponse(validationResult));

            var response = new CommandResponse();

            var isValidGuid = Guid.TryParse(command.ReportFileId, out _);

            if (!isValidGuid)
                response.SetError("Exception", "ReportFileId is not a valid GUID");

            return Task.FromResult(response);
        }
    }
}