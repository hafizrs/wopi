using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports;

public class ActiveInactiveCirsReportCommandValidator
    : AbstractValidator<ActiveInactiveCirsReportCommand>
{
    public ActiveInactiveCirsReportCommandValidator()
    {
        RuleFor(command => command.CirsReportId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("CirsReportId can't be null.")
            .NotEmpty().WithMessage("CirsReportId can't be empty.");
    }

    public ValidationResult IsSatisfiedBy(ActiveInactiveCirsReportCommand command)
    {
        var commandValidity = Validate(command);

        return !commandValidity.IsValid ? commandValidity : new ValidationResult();
    }
}
