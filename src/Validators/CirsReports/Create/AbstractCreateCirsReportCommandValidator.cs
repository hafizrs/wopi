using FluentValidation;
using System.Linq;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Create;

public abstract class AbstractCreateCirsReportCommandValidator<TCreateReportCommand>
    : AbstractValidator<TCreateReportCommand> where TCreateReportCommand : AbstractCreateCirsReportCommand
{
    protected AbstractCreateCirsReportCommandValidator()
    {
        RuleFor(command => command)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("Payload can't be null.")
            .NotEmpty().WithMessage("Payload can't be empty.");

        RuleFor(command => command.Tags)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("Tags can't be null.")
            .NotEmpty().WithMessage("Tags can't be empty.")
            .Must(ValidationHelper.IsValidTag).WithMessage("Tags is not valid");

        RuleFor(command => command.OrganizationId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("OrganizationId can't be null.")
            .NotEmpty().WithMessage("OrganizationId can't be empty.");

        RuleFor(command => command.AffectedInvolvedParties)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("AffectedInvolvedParties can't be null.")
            .NotEmpty().WithMessage("AffectedInvolvedParties can't be empty.");

        RuleFor(command => command.AttachmentIds)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .Must(ids => ids.All(id => !string.IsNullOrWhiteSpace(id)))
            .WithMessage("AttachmentId can't be empty.")
            .When(command => command.AttachmentIds != null);

        RuleFor(command => command.Title)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("Title can't be null.")
            .NotEmpty().WithMessage("Title can't be empty.");
    }

    public ValidationResult IsSatisfiedBy(TCreateReportCommand command)
    {
        var commandValidity = Validate(command);

        return !commandValidity.IsValid ? commandValidity : new ValidationResult();
    }
}
