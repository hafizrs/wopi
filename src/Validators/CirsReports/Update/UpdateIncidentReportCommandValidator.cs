using FluentValidation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Update;

public class UpdateIncidentReportCommandValidator
    : AbstractUpdateCirsReportCommandValidator<UpdateIncidentReportCommand>
{
    public UpdateIncidentReportCommandValidator()
    {
        RuleFor(command => command)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("Command can't be null.");

        RuleFor(command => command.Topic)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty().WithMessage("Topic can't be empty.")
            .Must(ValidationHelper.IsValidIncidentTopic)
            .When(command => command.Topic != null);
    }
}