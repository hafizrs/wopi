using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports;

public class DeleteCirsReportsCommandValidator
    : AbstractValidator<DeleteCirsReportsCommand>
{
    public DeleteCirsReportsCommandValidator()
    {
        RuleFor(command => command.CirsReportIds)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("CirsReportIds can't be null.")
            .NotEmpty().WithMessage("CirsReportIds can't be empty.");
    }

    public ValidationResult IsSatisfiedBy(DeleteCirsReportsCommand command)
    {
        var commandValidity = Validate(command);

        return !commandValidity.IsValid ? commandValidity : new ValidationResult();
    }
}
