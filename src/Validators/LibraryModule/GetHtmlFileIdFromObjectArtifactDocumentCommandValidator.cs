using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class GetHtmlFileIdFromObjectArtifactDocumentCommandValidator : AbstractValidator<GetHtmlFileIdFromObjectArtifactDocumentCommand>
    {
        private readonly ISecurityHelperService _securityHelperService;
        public GetHtmlFileIdFromObjectArtifactDocumentCommandValidator(ISecurityHelperService securityHelperService)
        {
            _securityHelperService = securityHelperService;

            RuleFor(command => command.ObjectArtifactId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(IsAAuthorizedUser)
                .WithMessage("Only department level & admin B users get this data.");

            RuleFor(command => command.ObjectArtifactId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAValidGuid)
                .WithMessage("{PropertyName} must be a valid GUID.");

            RuleFor(command => command.SubscriptionId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAValidGuid)
                .WithMessage("{PropertyName} must be a valid GUID.");
        }

        public ValidationResult IsSatisfiedBy(GetHtmlFileIdFromObjectArtifactDocumentCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static bool BeAValidGuid(string guid)
        {
            return Guid.TryParse(guid, out _);
        }

        public bool IsAAuthorizedUser(string _)
        {
            return _securityHelperService.IsADepartmentLevelUser() || _securityHelperService.IsAAdminBUser() || _securityHelperService.IsAGroupAdminUser();
        }
    }
}
