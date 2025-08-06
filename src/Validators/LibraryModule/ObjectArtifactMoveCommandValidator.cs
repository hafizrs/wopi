using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class ObjectArtifactMoveCommandValidator : AbstractValidator<ObjectArtifactMoveCommand>
    {
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactAuthorizationCheckerService _objectArtifactAuthorizationCheckerService;

        public ObjectArtifactMoveCommandValidator(
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactAuthorizationCheckerService objectArtifactAuthorizationCheckerService)
        {
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactAuthorizationCheckerService = objectArtifactAuthorizationCheckerService;

            RuleFor(command => command)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(command => BeAValidGuidList(command.ObjectArtifactIds))
                .WithMessage("ObjectArtifactId must be a valid GUID.")
                .Must(command => BeAValidObjectArtifactList(command.ObjectArtifactIds))
                .WithMessage("Item not found.")
                .Must(BeAValidMoveRequestList)
                .WithMessage("Item already exists in the requested directory.");

            RuleFor(command => command)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(command => BeAValidGuid(command.NewParentId))
                .WithMessage("NewParentId must be a valid GUID.")
                .Must(command => BeAValidObjectArtifact(command.NewParentId))
                .WithMessage("Parent directory not found.")
                .When(command => !string.IsNullOrWhiteSpace(command.NewParentId));
        }

        public ValidationResult IsSatisfiedBy(ObjectArtifactMoveCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static implicit operator ObjectArtifactMoveCommandValidator(ObjectArtifactMoveCommand v)
        {
            throw new NotImplementedException();
        }

        private bool BeAValidGuidList(List<string> objectArtifactIds)
        {
            return objectArtifactIds.All(BeAValidGuid);
        }

        public static bool BeAValidGuid(string guid)
        {
            return Guid.TryParse(guid, out _);
        }

        private bool BeAValidObjectArtifactList(List<string> objectArtifactIds)
        {
            return objectArtifactIds.All(BeAValidObjectArtifact);
        }

        private bool BeAValidObjectArtifact(string objectArtifactId)
        {
            var objectArtifact = _objectArtifactUtilityService.GetWritableObjectArtifactById(objectArtifactId);
            if (objectArtifact == null)
            {
                return false;
            }

            return true;
        }

        private bool BeAValidMoveRequestList(ObjectArtifactMoveCommand command)
        {
            return command.ObjectArtifactIds.All(r => BeAValidMoveRequest(r, command.NewParentId));
        }

        private bool BeAValidMoveRequest(string objectArtifactId, string parentId)
        {
            var objectArtifact = _objectArtifactUtilityService.GetWritableObjectArtifactById(objectArtifactId);
            var newParentId = !string.IsNullOrWhiteSpace(parentId) ? parentId : null;
            if (objectArtifact?.ParentId == newParentId || objectArtifactId == parentId)
            {
                return false;
            }

            return true;
        }
    }
}