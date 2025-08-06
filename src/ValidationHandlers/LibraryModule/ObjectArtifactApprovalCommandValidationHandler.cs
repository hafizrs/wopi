using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class ObjectArtifactApprovalCommandValidationHandler  : IValidationHandler<ObjectArtifactApprovalCommand, RiqsCommandResponse>
    {
        private readonly ObjectArtifactApprovalCommandValidator _validator;

        public ObjectArtifactApprovalCommandValidationHandler(
            ObjectArtifactApprovalCommandValidator validator)
        {
            _validator = validator;
        }

        public RiqsCommandResponse Validate(ObjectArtifactApprovalCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<RiqsCommandResponse> ValidateAsync(ObjectArtifactApprovalCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new RiqsCommandResponse() : new RiqsCommandResponse(validationResult));
        }

    }
}