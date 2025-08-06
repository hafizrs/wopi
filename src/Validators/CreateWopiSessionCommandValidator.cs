using FluentValidation;
using FluentValidation.Results;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.WopiMonitor.Contracts.Commands.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Contracts.DomainServices.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Contracts.Models.WopiModule;
using System.Linq;

namespace Selise.Ecap.SC.WopiMonitor.Validators
{
    public class CreateWopiSessionCommandValidator : AbstractValidator<CreateWopiSessionCommand>
    {
        private readonly IRepository _repository;
        private readonly IWopiPermissionService _wopiPermissionService;

        public CreateWopiSessionCommandValidator(IRepository repository, IWopiPermissionService wopiPermissionService)
        {
            _repository = repository;
            _wopiPermissionService = wopiPermissionService;

            RuleFor(command => command.FileUrl)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("File URL can't be null.")
                .NotEmpty().WithMessage("File URL can't be empty.")
                .Must(BeValidUrl).WithMessage("File URL must be a valid URL");

            RuleFor(command => command.DepartmentId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Department Id can't be null.")
                .NotEmpty().WithMessage("Department Id can't be empty.")
                .Must((departmentId) => _wopiPermissionService.HasDepartmentPermission(departmentId))
                .WithMessage("User does not have permission to create WOPI session for selected department");

            RuleFor(command => command.FileName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("File name can't be null.")
                .NotEmpty().WithMessage("File name can't be empty.");
        }

        private bool BeValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        public ValidationResult IsSatisfiedby(CreateWopiSessionCommand command)
        {
            var commandValidity = Validate(command);
            if (!commandValidity.IsValid) return commandValidity;
            return new ValidationResult();
        }
    }
} 