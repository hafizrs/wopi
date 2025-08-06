using Selise.Ecap.SC.PraxisMonitor.Commands;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers;

public class DeleteCockpitObjectArtifactSummaryCommandValidationHandler : IValidationHandler<DeleteCockpitObjectArtifactSummaryCommand, CommandResponse>
{
    private readonly DeleteCockpitObjectArtifactSummaryCommandValidator
        _deleteCockpitObjectArtifactSummaryCommandValidator;

    public DeleteCockpitObjectArtifactSummaryCommandValidationHandler(
        DeleteCockpitObjectArtifactSummaryCommandValidator deleteCockpitObjectArtifactSummaryCommandValidator)
    {
        _deleteCockpitObjectArtifactSummaryCommandValidator = deleteCockpitObjectArtifactSummaryCommandValidator;
    }
    public CommandResponse Validate(DeleteCockpitObjectArtifactSummaryCommand command)
    {
        throw new System.NotImplementedException();
    }

    public async Task<CommandResponse> ValidateAsync(DeleteCockpitObjectArtifactSummaryCommand command)
    {
        var validationResult = _deleteCockpitObjectArtifactSummaryCommandValidator.IsSatisfiedby(command);
        return !validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse();
    }
}