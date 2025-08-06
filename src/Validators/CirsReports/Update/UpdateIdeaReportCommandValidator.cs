using FluentValidation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Update;

public class UpdateIdeaReportCommandValidator
    : AbstractUpdateCirsReportCommandValidator<UpdateIdeaReportCommand>
{
    public UpdateIdeaReportCommandValidator()
    {
        RuleFor(command => command.ReporterClientId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty().WithMessage("ReporterClientId can't be empty.")
            .When(command => command.ReporterClientId != null);
    }
}
