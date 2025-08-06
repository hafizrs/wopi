using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class ObjectArtifactMoveCommandValidationHandler  : IValidationHandler<ObjectArtifactMoveCommand, RiqsCommandResponse>
    {
        private readonly ObjectArtifactMoveCommandValidator _validator;

        public ObjectArtifactMoveCommandValidationHandler (ObjectArtifactMoveCommandValidator validator)
        {
            _validator = validator;
        }

        public RiqsCommandResponse Validate(ObjectArtifactMoveCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<RiqsCommandResponse> ValidateAsync(ObjectArtifactMoveCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new RiqsCommandResponse() : new RiqsCommandResponse(validationResult));
        }
    }
}