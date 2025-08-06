using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Validators;

public class DeleteMultipleDataCommandValidator : AbstractValidator<DeleteMultipleDataCommand>
{
    private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
    private readonly ISecurityContextProvider _securityContextProvider;
    public DeleteMultipleDataCommandValidator(
        IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider, ISecurityContextProvider securityContextProvider)
    {
        RuleFor(command => command.ItemIds)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull()
            .WithMessage("ItemIds must not be null.")
            .NotEmpty()
            .WithMessage("ItemIds must not be empty.")
            .Must(ContainAtLeastOneItem)
            .WithMessage("ItemIds must contain at least one item.")
            .Must(IsValidGuid)
            .WithMessage("ItemIds must contain valid GUID.")
            .Must(ContainNoNullItem)
            .WithMessage("ItemIds must not contain null item.")
            .Must(ContainNoEmptyItem)
            .WithMessage("ItemIds must not contain empty item.")
            .Must(ContainNoDuplicateItem)
            .WithMessage("ItemIds must not contain duplicate item.");
        RuleFor(command => command.EntityName)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull()
            .WithMessage("EntityName must not be null.")
            .NotEmpty()
            .WithMessage("EntityName must not be empty.")
            .Must(EntityRegistered)
            .WithMessage("EntityName does not exist.")
            .Must(BeValidEntityName)
            .DependentRules(() =>
            {
                RuleFor(command => command)
                    .Cascade(CascadeMode.StopOnFirstFailure)
                    .Must(BeValidRole)
                    .WithMessage("Your role is not sufficient enough to delete data.");
            });

        _mongoDbDataContextProvider = ecapMongoDbDataContextProvider;
        _securityContextProvider = securityContextProvider;
    }

    public ValidationResult IsSatisfiedBy(DeleteMultipleDataCommand command)
    {
        var commandValidity = Validate(command);

        return !commandValidity.IsValid ? commandValidity : new ValidationResult();
    }
    private bool IsValidGuid(List<string> itemIds)
    {
        return itemIds.All(itemId => Guid.TryParse(itemId, out _));
    }
    private bool ContainAtLeastOneItem(List<string> itemIds)
    {
        return itemIds.Count > 0;
    }
    private bool ContainNoNullItem(List<string> itemIds)
    {
        return itemIds.All(id => id != null);
    }
    private bool ContainNoEmptyItem(List<string> itemIds)
    {
        return itemIds.All(id => !string.IsNullOrWhiteSpace(id));
    }
    private bool ContainNoDuplicateItem(List<string> itemIds)
    {
        return itemIds.Distinct().Count() == itemIds.Count;
    }
    private bool EntityRegistered(string entityName)
    {
        var filter = new BsonDocument("name", $"{entityName}s");
        var collections = _mongoDbDataContextProvider.GetTenantDataContext()
            .ListCollections(new ListCollectionsOptions { Filter = filter });
        return collections.Any();
    }
    private bool BeValidEntityName(string entityName)
    {
        var entityList = new List<string>
        {
            nameof(PraxisOrganization),
            nameof(PraxisRoom),
            nameof(PraxisEquipment),
            nameof(PraxisEquipmentMaintenance),
            nameof(PraxisTraining),
            nameof(PraxisRisk),
            nameof(PraxisAssessment),
            nameof(PraxisClientCategory),
            nameof(PraxisForm),
            "PraxisClientSubCategory",
            nameof(User),
            nameof(PraxisProcessGuide),
            nameof(PraxisClient),
            nameof(PraxisUserAdditionalInfo),
            nameof(PraxisReport)
        };

        return entityList.Any(r => r.Contains(entityName));
    }
    private bool BeValidRole(DeleteDataCommand command)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();
        var roleList = securityContext.Roles.ToList();
        return command.EntityName switch
        {
            nameof(PraxisOrganization) => HasAdminAccess(roleList),
            nameof(PraxisRoom) => HasPowerUserAccess(roleList),
            nameof(PraxisEquipment) => HasPowerUserAccess(roleList),
            nameof(PraxisEquipmentMaintenance) => HasPowerUserAccess(roleList),
            nameof(PraxisTraining) => HasPowerUserAccess(roleList),
            nameof(PraxisRisk) => HasPowerUserAccess(roleList),
            nameof(PraxisAssessment) => HasPowerUserAccess(roleList),
            nameof(PraxisClientCategory) => HasPowerUserAccess(roleList),
            nameof(PraxisForm) => HasPowerUserAccess(roleList),
            "PraxisClientSubCategory" => HasPowerUserAccess(roleList),
            nameof(User) => HasPowerUserAccess(roleList),
            nameof(PraxisProcessGuide) => HasPowerUserAccess(roleList),
            nameof(PraxisClient) => HasPowerUserAccess(roleList),
            nameof(PraxisUserAdditionalInfo) or nameof(PraxisReport) => HasManagementAccess(roleList),
            _ => false
        };
    }

    private bool HasAdminAccess(List<string> roles)
    {
        return roles.Contains("admin") || roles.Contains("task_controller");
    }
    private bool HasPowerUserAccess(List<string> roles)
    {
        return roles.Contains("poweruser") || HasAdminAccess(roles);
    }
    private bool HasManagementAccess(List<string> roles)
    {
        return roles.Contains("leitung") || HasPowerUserAccess(roles);
    }
}