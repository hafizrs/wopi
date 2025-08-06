using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class ObjectArtifactSearchCommandValidator : AbstractValidator<ObjectArtifactSearchCommand>
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRepository _repository;
        public ObjectArtifactSearchCommandValidator(
            IRepository repository,
            ISecurityContextProvider securityContextProvider
            )
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            RuleFor(command => command.Type)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(IsValidViewMode).WithMessage("Type is not valid")
                .When(command => !string.IsNullOrWhiteSpace(command.Type));

            RuleFor(command => command.ArtifactType)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("ArtifactType can't be null")
                .When(command => string.IsNullOrWhiteSpace(command.Type) || command.Type == "all");

            RuleFor(command => command.ArtifactType)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(IsValidArtifactType).WithMessage("ArtifactType is not valid")
                .When(command => command.ArtifactType != null );
            RuleFor(command => command.ObjectArtifactId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(haveAnyArtifact).WithMessage("ARTIFACT_DOES_NOT_EXIST")
                .When(command => !string.IsNullOrEmpty(command.ObjectArtifactId));

            RuleFor(command => command.ObjectArtifactId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(HasArtifactPermissionByArtifactId).WithMessage("USER_DOES_NOT_HAVE_PERMISSION")
                .When(command => !string.IsNullOrEmpty(command.ObjectArtifactId));
        }

        private bool haveAnyArtifact(string value)
        {
            var artifact = _repository.GetItem<ObjectArtifact>(o => o.ItemId == value && !o.IsMarkedToDelete);
            return artifact != null;
        }

        private bool HasArtifactPermissionByArtifactId(string value)
        {
            //var roles = _securityContextProvider.GetSecurityContext().Roles.ToList();
            //var userId = _securityContextProvider.GetSecurityContext().UserId;

            //if (!roles.Any() || userId == null) return false;

            //var artifact = _repository.GetItem<ObjectArtifact>(o => o.ItemId == value);
            //if(artifact.IdsAllowedToRead.ToList().Contains(userId) || artifact.RolesAllowedToRead.ToList().Any(item => roles.Contains(item)))
            //{
            //    return true;
            //}
            return true;
        }

        private bool IsValidViewMode(string value)
        {
            var validStatusList = LibraryModuleConstants.LibraryViewModeList.Select(v => v.Value).ToArray();
            return validStatusList.Contains(value);
        }

        private bool IsValidArtifactType(ArtifactTypeEnum? value)
        {
            return value == ArtifactTypeEnum.File || value == ArtifactTypeEnum.Folder;
        }

        public ValidationResult IsSatisfiedBy(ObjectArtifactSearchCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static implicit operator ObjectArtifactSearchCommandValidator(ObjectArtifactSearchCommand v)
        {
            throw new NotImplementedException();
        }
    }
}