using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports;

public class MoveToOtherDashboardCommandValidator
    : AbstractValidator<MoveToOtherDashboardCommand>
{
    public MoveToOtherDashboardCommandValidator()
    {
        RuleFor(command => command.CirsReportId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("CirsReportId can't be null.")
            .NotEmpty().WithMessage("CirsReportId can't be empty.");
    }

    public ValidationResult IsSatisfiedBy(MoveToOtherDashboardCommand command)
    {
        var commandValidity = Validate(command);

        return !commandValidity.IsValid ? commandValidity : new ValidationResult();
    }
}
