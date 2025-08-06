using FluentValidation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Create;

public class CreateIdeaReportCommandValidator
    : AbstractCreateCirsReportCommandValidator<CreateIdeaReportCommand>
{
    public CreateIdeaReportCommandValidator()
    {
        RuleFor(command => command.ReporterClientId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("ReporterClientId can't be null.")
            .NotEmpty().WithMessage("ReporterClientId can't be empty.");
    }
}
