using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.WopiMonitor.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Contracts.DomainServices.WopiModule;

namespace Selise.Ecap.SC.WopiMonitor.Validators
{
    public class DeleteWopiSessionCommandValidator : AbstractValidator<DeleteWopiSessionCommand>
    {
        private readonly IWopiPermissionService _wopiPermissionService;
        
        public DeleteWopiSessionCommandValidator(IWopiPermissionService wopiPermissionService)
        {
            _wopiPermissionService = wopiPermissionService;
            
            RuleFor(command => command.SessionId)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("Session ID for deleting record can't be null.")
               .NotEmpty().WithMessage("Session ID for deleting can't be empty.")
               .Must((sessionId) => _wopiPermissionService.HasWopiSessionPermission(sessionId))
               .WithMessage("User does not have permission to delete WOPI session");
        }

        public ValidationResult IsSatisfiedby(DeleteWopiSessionCommand command)
        {
            var commandValidity = Validate(command);
            if (!commandValidity.IsValid) return commandValidity;
            return new ValidationResult();
        }
    }
} 