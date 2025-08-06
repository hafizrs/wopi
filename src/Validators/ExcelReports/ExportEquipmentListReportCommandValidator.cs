using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports
{
    public class ExportEquipmentListReportCommandValidator : AbstractValidator<ExportEquipmentListReportCommand>
    {
        public ExportEquipmentListReportCommandValidator()
        {
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

            RuleFor(command => command.FilterString)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("FilterString required.");
        }

        public ValidationResult IsSatisfiedby(ExportEquipmentListReportCommand command)
        {
           var commandValidity = Validate(command);

           if (!commandValidity.IsValid) return commandValidity;

           return new ValidationResult();
        }
    }
}
