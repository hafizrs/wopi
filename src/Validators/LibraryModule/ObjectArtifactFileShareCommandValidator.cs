using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class ObjectArtifactFileShareCommandValidator : AbstractValidator<ObjectArtifactFileShareCommand>
    {
        public ObjectArtifactFileShareCommandValidator()
        {
        }

        public ValidationResult IsSatisfiedBy(ObjectArtifactFileShareCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static implicit operator ObjectArtifactFileShareCommandValidator(ObjectArtifactFileShareCommand v)
        {
            throw new NotImplementedException();
        }
    }
}