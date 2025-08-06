using FluentValidation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Create;

public class CreateComplainReportCommandValidator
    : AbstractCreateCirsReportCommandValidator<CreateComplainReportCommand>
{
    public CreateComplainReportCommandValidator()
    {
    }
}