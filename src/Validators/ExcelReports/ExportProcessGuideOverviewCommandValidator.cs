using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports
{
    public class ExportProcessGuideOverviewCommandValidator : AbstractValidator<ExportProcessGuideCaseOverviewReportCommand>
    {
        public ExportProcessGuideOverviewCommandValidator()
        {
            RuleFor(command => command.ReportHeader)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("Report Name required.");

            RuleFor(command => command.ReportHeader)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("Report Name required.");

            RuleFor(command => command.ReportFileId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("ReportFileId required.");

            RuleFor(command => command.FileNameWithExtension)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("FileNameWithExtension required.");

            RuleFor(command => command.LanguageKey)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("Language key required.");
        }

        public ValidationResult IsSatisfiedby(ExportProcessGuideCaseOverviewReportCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
