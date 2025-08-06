using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class ObjectArtifactActivationDeactivationCommandValidator : AbstractValidator<ObjectArtifactActivationDeactivationCommand>
    {
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IObjectArtifactAuthorizationCheckerService _objectArtifactAuthorizationCheckerService;

        public ObjectArtifactActivationDeactivationCommandValidator
        (
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IObjectArtifactAuthorizationCheckerService ojectArtifactAuthorizationCheckerService
        )
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _objectArtifactAuthorizationCheckerService = ojectArtifactAuthorizationCheckerService;


            RuleFor(command => command.ObjectArtifactId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAValidGuid)
                .WithMessage("{PropertyName} must be a valid GUID.");

            RuleFor(command => command.Activate)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull().WithMessage("Activate command can't be null.");

            RuleFor(command => command.ObjectArtifactId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAValidObjectArtifact)
                .WithMessage("{PropertyName} is not valid");
            
            RuleFor(command => command.ObjectArtifactId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeInactiveInAllRelatedReference)
                .WithMessage("The artifact is being used in elsewhere.")
                .When(command => command.Activate == false);

            RuleFor(command => command)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(CanActiveInactiveObjectArtifact)
                .WithMessage("The user does not have required permissions");
        }

        public ValidationResult IsSatisfiedBy(ObjectArtifactActivationDeactivationCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static implicit operator ObjectArtifactActivationDeactivationCommandValidator(ObjectArtifactActivationDeactivationCommand v)
        {
            throw new NotImplementedException();
        }

        public static bool BeAValidGuid(string guid)
        {
            return Guid.TryParse(guid, out _);
        }

        private bool BeAValidObjectArtifact(string objectArtifactId)
        {
            var objectArtifact = _repository.GetItem<ObjectArtifact>(o => o.ItemId == objectArtifactId && !o.IsMarkedToDelete);
            if (objectArtifact == null)
            {
                return false;
            }

            return true;
        }

        private bool CanActiveInactiveObjectArtifact(ObjectArtifactActivationDeactivationCommand command)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var userId = securityContext.UserId;

            var objectArtifact = _repository.GetItem<ObjectArtifact>(o => o.ItemId == command.ObjectArtifactId && !o.IsMarkedToDelete);
            if (objectArtifact == null)
            {
                return false;
            }

            if (_objectArtifactAuthorizationCheckerService.CanActiveInactiveObjectArtifact(objectArtifact))
            {
                return true;
            }
            return false;
        }
        private bool BeInactiveInAllRelatedReference(string objectArtifactId)
        {
            return !_objectArtifactAuthorizationCheckerService.IsArtifactBeingUsed(objectArtifactId);
        }
    }
}