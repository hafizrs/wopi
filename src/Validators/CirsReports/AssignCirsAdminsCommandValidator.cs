using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports;

public class AssignCirsAdminsCommandValidator : AbstractValidator<AssignCirsAdminsCommand>
{
    public AssignCirsAdminsCommandValidator()
    {
        RuleFor(command => command.OrganizationId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("OrganizationId can't be null.")
            .NotEmpty().WithMessage("OrganizationId can't be empty.");

        RuleFor(command => command.PraxisClientId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("PraxisClientId can't be null.")
            .NotEmpty().WithMessage("PraxisClientId can't be empty.");

        RuleFor(command => command.DashboardName)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("CirsDashboardName can't be null.")
            .NotEmpty().WithMessage("CirsDashboardName can't be empty.");

        RuleFor(command => command.DashboardNameEnum)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("CirsDashboardName must be valid.");
    }

    public ValidationResult IsSatisfiedBy(AssignCirsAdminsCommand command)
    {
        var commandValidity = Validate(command);

        return !commandValidity.IsValid ? commandValidity : new ValidationResult();
    }
}