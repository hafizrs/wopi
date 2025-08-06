using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class ObjectArtifactUpdateCommandValidator : AbstractValidator<ObjectArtifactUpdateCommand>
    {
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;

        public ObjectArtifactUpdateCommandValidator(
            IObjectArtifactUtilityService objectArtifactUtilityService)
        {
            _objectArtifactUtilityService = objectArtifactUtilityService;

            RuleFor(command => command.ObjectArtifactId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAValidGuid)
                .WithMessage("{PropertyName} must be a valid GUID.");

            RuleFor(command => command.ObjectArtifactId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAValidObjectArtifact)
                .WithMessage("{PropertyName} is not valid");

            RuleFor(command => command)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAAuthorizedUser)
                .WithMessage("The user does not have required permissions");
        }

        public ValidationResult IsSatisfiedBy(ObjectArtifactUpdateCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static implicit operator ObjectArtifactUpdateCommandValidator(ObjectArtifactUpdateCommand v)
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

        private bool BeAAuthorizedUser(ObjectArtifactUpdateCommand command)
        {
            var objectArtifact = _objectArtifactUtilityService.GetEditableObjectArtifactById(command.ObjectArtifactId);
            if (objectArtifact == null)
            {
                return false;
            }

            return true;
        }

    }
}