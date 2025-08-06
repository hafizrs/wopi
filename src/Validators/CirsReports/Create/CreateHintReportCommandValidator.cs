using FluentValidation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Create;

public class CreateHintReportCommandValidator
    : AbstractCreateCirsReportCommandValidator<CreateHintReportCommand>
{
    public CreateHintReportCommandValidator()
    {
    }
}