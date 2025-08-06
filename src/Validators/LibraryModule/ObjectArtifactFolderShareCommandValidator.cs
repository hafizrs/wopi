using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class ObjectArtifactFolderShareCommandValidator : AbstractValidator<ObjectArtifactFolderShareCommand>
    {
        public ObjectArtifactFolderShareCommandValidator()
        {
        }

        public ValidationResult IsSatisfiedBy(ObjectArtifactFolderShareCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static implicit operator ObjectArtifactFolderShareCommandValidator(ObjectArtifactFolderShareCommand v)
        {
            throw new NotImplementedException();
        }
    }
}