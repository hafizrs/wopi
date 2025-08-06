using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class AssignTemplateToEquipmentCommandValidator : AbstractValidator<AssignTemplateToEquipmentCommand>
    {
        private readonly IRepository _repository;
        public AssignTemplateToEquipmentCommandValidator(IRepository repository)
        {
            RuleLevelCascadeMode = CascadeMode.Stop;
            RuleFor(x => x.EquipmentId)
                .NotEmpty().WithMessage("Equipment ID is required.")
                .MustAsync(async (x, cancellationToken) =>
                    await EquipmentExistsAsync(x, cancellationToken))
                .WithMessage("Equipment not found.");
            _repository = repository;
        }

        private async Task<bool> EquipmentExistsAsync(string equipmentId, CancellationToken cancellationToken)
        {
            return await _repository.ExistsAsync<PraxisEquipment>(e => e.ItemId == equipmentId && !e.IsMarkedToDelete);
        }

        private async Task<bool> TemplateExistsAsync(List<string> templateIds, CancellationToken cancellationToken)
        {
            return await _repository.ExistsAsync<PraxisReportTemplate>(t => templateIds.Contains(t.ItemId) && !t.IsMarkedToDelete);
        }

        public ValidationResult IsSatisfiedBy(AssignTemplateToEquipmentCommand command)
        {
            var validationResult = ValidateAsync(command).Result;
            return !validationResult.IsValid ? validationResult : new ValidationResult();
        }
    }
}
