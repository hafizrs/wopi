using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class ObjectArtifactFileShareCommandValidationHandler : IValidationHandler<ObjectArtifactFileShareCommand, RiqsCommandResponse>
    {
        private readonly ObjectArtifactFileShareCommandValidator _validator;

        public ObjectArtifactFileShareCommandValidationHandler(ObjectArtifactFileShareCommandValidator validator)
        {
            _validator = validator;
        }

        public RiqsCommandResponse Validate(ObjectArtifactFileShareCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<RiqsCommandResponse> ValidateAsync(ObjectArtifactFileShareCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new RiqsCommandResponse() : new RiqsCommandResponse(validationResult));
        }
    }
}