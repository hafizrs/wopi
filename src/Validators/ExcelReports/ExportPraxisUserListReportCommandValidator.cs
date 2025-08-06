using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports
{
    public class ExportPraxisUserListReportCommandValidator: AbstractValidator<ExportPraxisUserListReportCommand>
    {
        public ExportPraxisUserListReportCommandValidator()
        {
            RuleFor(command => command.ReportFileId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("ReportFileId required.");

            RuleFor(command => command.ReportName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("FileNameWithExtension required.");
        }

        public ValidationResult IsSatisfiedBy(ExportPraxisUserListReportCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }
}