using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class ObjectArtifactApprovalCommandValidator : AbstractValidator<ObjectArtifactApprovalCommand>
    {
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IObjectArtifactAuthorizationCheckerService _objectArtifactAuthorizationCheckerService;

        public ObjectArtifactApprovalCommandValidator 
        (
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IObjectArtifactAuthorizationCheckerService objectArtifactAuthorizationCheckerService
        )
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _objectArtifactAuthorizationCheckerService = objectArtifactAuthorizationCheckerService;

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
                .Must(CanApproveObjectArtifact)
                .WithMessage("The user does not have required permissions");
        }

        public ValidationResult IsSatisfiedBy(ObjectArtifactApprovalCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static implicit operator ObjectArtifactApprovalCommandValidator(ObjectArtifactApprovalCommand v)
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

        private bool CanApproveObjectArtifact(ObjectArtifactApprovalCommand command)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var userId = securityContext.UserId;

            var objectArtifact = _repository.GetItem<ObjectArtifact>(o => o.ItemId == command.ObjectArtifactId && !o.IsMarkedToDelete);
            if (objectArtifact == null)
            {
                return false;
            }

            if (
                !_objectArtifactAuthorizationCheckerService.IsAReapprovedArtifact(objectArtifact.MetaData, true)
                && _objectArtifactAuthorizationCheckerService.CanApproveObjectArtifact(objectArtifact)
            )
            {
                return true;
            }
            return false;
        }
    }
}