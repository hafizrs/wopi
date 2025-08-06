using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports
{
    public class ExportCirsReportCommandValidator : AbstractValidator<ExportReportCommand>
    {
        public ExportCirsReportCommandValidator()
        {
        }

        public ValidationResult IsSatisfiedBy(ExportReportCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }
}