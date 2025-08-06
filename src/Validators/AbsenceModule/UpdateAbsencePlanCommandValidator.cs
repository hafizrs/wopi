using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.AbsenceModule
{
    public class UpdateAbsencePlanCommandValidator : AbstractValidator<UpdateAbsencePlanCommand>
    {
        private readonly IRepository _repository;

        public UpdateAbsencePlanCommandValidator(IRepository repository)
        {
            _repository = repository;

            RuleFor(x => x.ItemId)
                .NotEmpty()
                .WithMessage("ItemId is required");

            RuleFor(x => x.AffectedUserId)
                .NotEmpty()
                .WithMessage("AffectedUserId is required");

            RuleFor(x => x.AbsenceTypeId)
                .NotEmpty()
                .WithMessage("AbsenceTypeId is required");

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("StartDate is required");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("EndDate is required");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("EndDate must be greater than StartDate");

            RuleFor(x => x)
                .MustAsync(ExistsAsync)
                .WithMessage((command, context) => $"Absence plan with id '{command.ItemId}' does not exist");

            RuleFor(x => x)
                .MustAsync(AbsenceTypeExistsAsync)
                .WithMessage((command, context) => $"Absence type with id '{command.AbsenceTypeId}' does not exist");
        }

        private async Task<bool> ExistsAsync(UpdateAbsencePlanCommand command, CancellationToken ct)
        {
            return await _repository.ExistsAsync<RiqsAbsencePlan>(ap => ap.ItemId == command.ItemId && !ap.IsMarkedToDelete);
        }

        private async Task<bool> AbsenceTypeExistsAsync(UpdateAbsencePlanCommand command, CancellationToken ct)
        {
            return await _repository.ExistsAsync<RiqsAbsenceType>(at => at.ItemId == command.AbsenceTypeId);
        }

        public async Task<ValidationResult> IsSatisfiedby(UpdateAbsencePlanCommand command)
        {
            var commandValidity = await ValidateAsync(command);
            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }
}