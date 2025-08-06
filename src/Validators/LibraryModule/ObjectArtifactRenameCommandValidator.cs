using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class ObjectArtifactRenameCommandValidator : AbstractValidator<ObjectArtifactRenameCommand>
    {
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;

        public ObjectArtifactRenameCommandValidator(
            IObjectArtifactUtilityService objectArtifactUtilityService)
        {
            _objectArtifactUtilityService = objectArtifactUtilityService;

            RuleFor(command => command.ObjectArtifactId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAValidGuid)
                .WithMessage("{PropertyName} must be a valid GUID.");

            RuleFor(command => command.Name)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("{PropertyName} can't be null or empty");

            RuleFor(command => command.ObjectArtifactId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAValidObjectArtifact)
                .WithMessage("{PropertyName} is not valid");

            RuleFor(command => command)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAAuthorizedUser)
                .WithMessage("The user does not have required permissions");

        }

        public ValidationResult IsSatisfiedBy(ObjectArtifactRenameCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static implicit operator ObjectArtifactRenameCommandValidator(ObjectArtifactRenameCommand v)
        {
            throw new NotImplementedException();
        }

        public static bool BeAValidGuid(string guid)
        {
            return Guid.TryParse(guid, out _);
        }

        private bool BeAValidObjectArtifact(string objectArtifactId)
        {
            var objectArtifact = _objectArtifactUtilityService.GetObjectArtifactById(objectArtifactId);
            if (objectArtifact == null)
            {
                return false;
            }

            return true;
        }

        private bool BeAAuthorizedUser(ObjectArtifactRenameCommand command)
        {
            var objectArtifact = _objectArtifactUtilityService.GetWritableObjectArtifactById(command.ObjectArtifactId);
            if (objectArtifact == null)
            {
                return false;
            }

            return true;
        }

    }
}