using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.AbsenceModule
{
    public class CreateAbsencePlanCommandValidator : AbstractValidator<CreateAbsencePlanCommand>
    {
        private readonly IRepository _repository;

        public CreateAbsencePlanCommandValidator(IRepository repository)
        {
            _repository = repository;

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

            RuleFor(x => x.DepartmentId)
                .NotEmpty()
                .WithMessage("DepartmentId is required");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("EndDate must be greater than StartDate");

            RuleFor(x => x)
                .MustAsync(AbsenceTypeExists)
                .WithMessage((command, context) => $"Absence type with id '{command.AbsenceTypeId}' does not exist in department '{command.DepartmentId}'");

            RuleFor(x => x.AffectedUserId)
                .MustAsync(UserExists)
                .WithMessage((command, context) => $"User with id '{command.AffectedUserId}' does not exist or is inactive");
        }

        private async Task<bool> AbsenceTypeExists(CreateAbsencePlanCommand command, CancellationToken ct)
        {
            return await _repository.ExistsAsync<RiqsAbsenceType>(at => at.ItemId == command.AbsenceTypeId && at.DepartmentId == command.DepartmentId);
        }

        private async Task<bool> UserExists(string userId, CancellationToken ct)
        {
            return await _repository.ExistsAsync<PraxisUser>(u => u.ItemId == userId && !u.IsMarkedToDelete && u.Active);
        }

        public async Task<ValidationResult> IsSatisfiedby(CreateAbsencePlanCommand command)
        {
            var commandValidity = await ValidateAsync(command);
            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }
}