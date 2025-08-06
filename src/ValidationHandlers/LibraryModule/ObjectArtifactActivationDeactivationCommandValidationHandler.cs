using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class ObjectArtifactActivationDeactivationCommandValidationHandler  : IValidationHandler<ObjectArtifactActivationDeactivationCommand, CommandResponse>
    {
        private readonly ObjectArtifactActivationDeactivationCommandValidator _validator;

        public ObjectArtifactActivationDeactivationCommandValidationHandler (ObjectArtifactActivationDeactivationCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(ObjectArtifactActivationDeactivationCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(ObjectArtifactActivationDeactivationCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}