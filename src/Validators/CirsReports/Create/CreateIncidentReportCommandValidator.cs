using FluentValidation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Create;

public class CreateIncidentReportCommandValidator
    : AbstractCreateCirsReportCommandValidator<CreateIncidentReportCommand>
{
    public CreateIncidentReportCommandValidator()
    {
        RuleFor(command => command.Topic)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("Topic can't be null.")
            .NotEmpty().WithMessage("Topic can't be empty.")
            .Must(ValidationHelper.IsValidIncidentTopic);
    }
}