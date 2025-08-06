using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers;

public class DeleteMultipleDataCommandValidationHandler : IValidationHandler<DeleteMultipleDataCommand, CommandResponse>
{
    private readonly DeleteMultipleDataCommandValidator _deleteMultipleDataCommandValidator;

    public DeleteMultipleDataCommandValidationHandler(DeleteMultipleDataCommandValidator deleteMultipleDataCommandValidator)
    {
        _deleteMultipleDataCommandValidator = deleteMultipleDataCommandValidator;
    }
    public CommandResponse Validate(DeleteMultipleDataCommand command)
    {
        throw new System.NotImplementedException();
    }

    public Task<CommandResponse> ValidateAsync(DeleteMultipleDataCommand command)
    {
        var validationResult = _deleteMultipleDataCommandValidator.IsSatisfiedBy(command);
        return Task.FromResult(!validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse());
    }
}