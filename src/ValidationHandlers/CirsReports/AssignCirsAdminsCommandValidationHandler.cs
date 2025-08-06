using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;
using Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.CirsReports;

public class AssignCirsAdminsCommandValidationHandler : IValidationHandler<AssignCirsAdminsCommand, CommandResponse>
{
    private readonly AssignCirsAdminsCommandValidator _validator;

    public AssignCirsAdminsCommandValidationHandler(AssignCirsAdminsCommandValidator validator)
    {
        _validator = validator;
    }

    public CommandResponse Validate(AssignCirsAdminsCommand command)
    {
        return ValidateAsync(command).Result;
    }

    public Task<CommandResponse> ValidateAsync(AssignCirsAdminsCommand command)
    {
        var validationResult = _validator.IsSatisfiedBy(command);

        return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
    }
}