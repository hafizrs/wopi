using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class ProcessOrganizationCreateUpdateCommandValidator : AbstractValidator<ProcessOrganizationCreateUpdateCommand>
    {
        private readonly IRepository _repository;
        public ProcessOrganizationCreateUpdateCommandValidator(IRepository repository)
        {
            _repository = repository;
            RuleFor(command => command.OrganizationData)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("OrganizationData can't be null.")
                .NotEmpty().WithMessage("OrganizationData can't be empty.");

            RuleFor(command => command.OrganizationData.ItemId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("OrganizationId can't be null.")
                .NotEmpty().WithMessage("OrganizationId can't be empty.")
                .Must(IsValidGuid).WithMessage("OrganizationId is not a valid guid.");

            RuleFor(command => command.OrganizationData)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(IsValidUserLimit).WithMessage("Invalid User Limit.");
        }
        private bool IsValidUserLimit(PraxisOrganization orgData)
        {
            var existingOrgData = _repository.GetItem<PraxisOrganization>(o => o.ItemId == orgData.ItemId);
            if (existingOrgData == null)
            {
                return orgData.UserLimit >= PraxisConstants.OrganizationMinimumUserLimit &&
                    orgData.UserLimit <= PraxisConstants.OrganizationMaximumUserLimit;
            }
            return true;
        }
        private bool IsValidGuid(string itemId)
        {
            bool isValidGuid = Guid.TryParse(itemId, out _);

            if (!isValidGuid)
            {
                return false;
            }

            return true;
        }

        public ValidationResult IsSatisfiedBy(ProcessOrganizationCreateUpdateCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static implicit operator ProcessOrganizationCreateUpdateCommandValidator(ProcessOrganizationCreateUpdateCommand v)
        {
            throw new NotImplementedException();
        }
    }
}