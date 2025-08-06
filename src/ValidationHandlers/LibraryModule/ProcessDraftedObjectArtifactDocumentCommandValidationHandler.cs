using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class ProcessDraftedObjectArtifactDocumentCommandValidationHandler : IValidationHandler<ProcessDraftedObjectArtifactDocumentCommand, CommandResponse>
    {
        private readonly ProcessDraftedObjectArtifactDocumentCommandValidator _validator;

        public ProcessDraftedObjectArtifactDocumentCommandValidationHandler(ProcessDraftedObjectArtifactDocumentCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(ProcessDraftedObjectArtifactDocumentCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(ProcessDraftedObjectArtifactDocumentCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}
