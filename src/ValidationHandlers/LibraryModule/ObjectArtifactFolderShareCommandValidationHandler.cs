using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class ObjectArtifactFolderShareCommandValidationHandler : IValidationHandler<ObjectArtifactFolderShareCommand, RiqsCommandResponse>
    {
        private readonly ObjectArtifactFolderShareCommandValidator _validator;

        public ObjectArtifactFolderShareCommandValidationHandler(ObjectArtifactFolderShareCommandValidator validator)
        {
            _validator = validator;
        }

        public RiqsCommandResponse Validate(ObjectArtifactFolderShareCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<RiqsCommandResponse> ValidateAsync(ObjectArtifactFolderShareCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new RiqsCommandResponse() : new RiqsCommandResponse(validationResult));
        }
    }
}