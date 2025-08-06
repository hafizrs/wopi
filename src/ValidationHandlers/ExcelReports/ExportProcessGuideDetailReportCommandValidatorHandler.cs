using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ExcelReports
{
    public class ExportProcessGuideDetailReportCommandValidatorHandler : IValidationHandler<ExportProcessGuideDetailReportCommand, CommandResponse>
    {
        private readonly ExportProcessGuideDetailReportCommandValidator _validator;

        public ExportProcessGuideDetailReportCommandValidatorHandler(
            ExportProcessGuideDetailReportCommandValidator validator)
        {
            _validator = validator;
        }
        public CommandResponse Validate(ExportProcessGuideDetailReportCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(ExportProcessGuideDetailReportCommand command)
        {
            var validationResult = _validator.IsSatisfiedby(command);

            if (!validationResult.IsValid)
                return Task.FromResult(new CommandResponse(validationResult));

            var response = new CommandResponse();

            bool isValidGuid = Guid.TryParse(command.ReportFileId, out _);

            if (!isValidGuid)
                response.SetError("Exception", "Guid is not valid of ReportFileId");

            return Task.FromResult(response);
        }
    }
}
