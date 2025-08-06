using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class CreateDocumentEditMappingRecordCommandValidator : AbstractValidator<CreateDocumentEditMappingRecordCommand>
    {
        public CreateDocumentEditMappingRecordCommandValidator()
        {
            

            RuleFor(x => x.ObjectArtifactId)
                .NotEmpty()
                .Must(BeAValidGuid);
        }

        public bool BeAValidGuid(string guid)
        {
            return Guid.TryParse(guid, out _);
        }

        public ValidationResult IsSatisfiedBy(CreateDocumentEditMappingRecordCommand command)
        {
            var validationResult = Validate(command);

            return !validationResult.IsValid ? validationResult : new ValidationResult();
        }

    }
}
