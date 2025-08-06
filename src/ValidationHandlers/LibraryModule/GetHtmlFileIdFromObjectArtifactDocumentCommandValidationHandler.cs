using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class GetHtmlFileIdFromObjectArtifactDocumentCommandValidationHandler : IValidationHandler<GetHtmlFileIdFromObjectArtifactDocumentCommand, CommandResponse>
    {
        private readonly GetHtmlFileIdFromObjectArtifactDocumentCommandValidator _validator;

        public GetHtmlFileIdFromObjectArtifactDocumentCommandValidationHandler(GetHtmlFileIdFromObjectArtifactDocumentCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(GetHtmlFileIdFromObjectArtifactDocumentCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(GetHtmlFileIdFromObjectArtifactDocumentCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}
