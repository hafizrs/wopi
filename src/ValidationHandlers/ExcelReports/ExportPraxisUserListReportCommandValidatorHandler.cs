using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ExcelReports
{
    public class
        ExportPraxisUserListReportCommandValidatorHandler : IValidationHandler<ExportPraxisUserListReportCommand,
            CommandResponse>
    {
        private readonly ExportPraxisUserListReportCommandValidator _validator;

        public ExportPraxisUserListReportCommandValidatorHandler(ExportPraxisUserListReportCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(ExportPraxisUserListReportCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(ExportPraxisUserListReportCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            if (!validationResult.IsValid)
                return Task.FromResult(new CommandResponse(validationResult));

            var response = new CommandResponse();

            var isValidGuid = Guid.TryParse(command.ReportFileId, out _);

            if (!isValidGuid)
                response.SetError("Exception", "ReportFileId is not a valid GUID");

            if (command.PraxisRolesLookup == null)
            {
                response.SetError("Exception", "Roles data required.");
            }

            return Task.FromResult(response);
        }
    }
}