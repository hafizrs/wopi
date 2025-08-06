using FluentValidation;
using System.Linq;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Update;

public abstract class AbstractUpdateCirsReportCommandValidator<TUpdateReportCommand> 
    : AbstractValidator<TUpdateReportCommand> where TUpdateReportCommand : AbstractUpdateCirsReportCommand
{
    protected AbstractUpdateCirsReportCommandValidator()
    {
        RuleFor(command => command)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("Payload can't be null.")
            .NotEmpty().WithMessage("Payload can't be empty.")
            .Must(IsValidCirsReportStatus).WithMessage("Not a valid Report Status.");

        RuleFor(command => command.CirsReportId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("CirsReportId can't be null.")
            .NotEmpty().WithMessage("CirsReportId can't be empty.");

        RuleFor(command => command.AffectedInvolvedParties)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty().WithMessage("AffectedInvolvedParties can't be empty.")
            .When(commad => commad.AffectedInvolvedParties != null);

        RuleFor(command => command.Tags)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty().WithMessage("Tags can't be empty.")
            .Must(ValidationHelper.IsValidTag).WithMessage("Tags is not valid")
            .Must(ValidationHelper.IsValidCirsReport).WithMessage("Not a valid Cirs Report")
            .When(command => command.Tags != null);

        RuleFor(command => command.Title)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty().WithMessage("Title can't be empty.")
            .When(command => command.Title != null);

        RuleFor(command => command.AttachmentIds)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .Must(ids => ids.All(id => !string.IsNullOrWhiteSpace(id)))
            .WithMessage("AttachmentIds can't be empty.")
            .When(command => command.AttachmentIds != null);

        RuleFor(command => command.Status)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty().WithMessage("Status can't be empty.")
            .When(command => command.Status != null);

        RuleFor(command => command.RankDetails)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty().WithMessage("RankDetails can't be empty.")
            .When(command => command.RankDetails != null);

        RuleFor(command => command.RankDetails.RankAfterId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty().WithMessage("RankDetails.RankAfterId can't be empty.")
            .When(command => command.RankDetails?.RankAfterId != null);

        RuleFor(command => command.RankDetails.RankBeforeId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty().WithMessage("RankDetails.RankBeforeId can't be empty.")
            .When(command => command.RankDetails?.RankBeforeId != null);
    }

    public ValidationResult IsSatisfiedBy(TUpdateReportCommand command)
    {
        var commandValidity = Validate(command);

        return !commandValidity.IsValid ? commandValidity : new ValidationResult();
    }

    private static bool IsValidCirsReportStatus(TUpdateReportCommand command)
        => command.CirsDashboardName.GetCirsReportStatusEnumValues().Contains(command.Status);
}
