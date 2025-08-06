using FluentValidation;
using FluentValidation.Results;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class DeleteDataCommandValidator : AbstractValidator<DeleteDataCommand>
    {
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;

        public DeleteDataCommandValidator(
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider,
            ISecurityContextProvider securityContextProvider,
            IRepository repository)
        {
            RuleFor(command => command.ItemId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("ItemId can't be null.")
                .NotEmpty().WithMessage("ItemId can't be empty.")
                .Must(IsValidGuid).WithMessage("Guid is not valid of ItemId");

            RuleFor(command => command.EntityName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("EntityName can't be null.")
                .NotEmpty().WithMessage("EntityName can't be empty.");

            RuleFor(command => command.ActionName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("ActionName can't be null.")
                .NotEmpty().WithMessage("ActionName can't be empty.");

            RuleFor(command => command.Context)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Context can't be null.")
                .NotEmpty().WithMessage("Context can't be empty.");

            RuleFor(command => command.EntityName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(EntityRegistered)
                .WithMessage("EntityName does not exist.")
                .DependentRules(() =>
                {
                    RuleFor(command => command.EntityName)
                        .Cascade(CascadeMode.StopOnFirstFailure)
                        .Must(BeValidEntityName)
                        .WithMessage("Data can't be delete for this Entity Name.")
                        .DependentRules(() =>
                        {
                            RuleFor(command => command)
                                .Cascade(CascadeMode.StopOnFirstFailure)
                                .Must(BeValidRole)
                                .WithMessage("Your role is not sufficient enough to delete data.");
                        });
                })
                .When(c => c.EntityName != "PraxisClientSubCategory" && c.EntityName != "PraxisUserAdditionalInfo" );

            RuleFor(command => command)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeValidDataCount)
                .WithMessage($"You can't delete {nameof(PraxisAssessment)} entity data.")
                .When(c => c.EntityName == nameof(PraxisAssessment));

            RuleFor(command => command)
              .Cascade(CascadeMode.StopOnFirstFailure)
              .Must(SystemAdmin)
              .WithMessage($"You can't delete {nameof(User)} entity data.")
              .When(c => c.EntityName == nameof(User));

            RuleFor(command => command)
              .Cascade(CascadeMode.StopOnFirstFailure)
              .Must(IsOrganizationAdminUser)
              .WithMessage($"CannotDeleteOrganizationAdminUser")
              .When(c => c.EntityName == nameof(User));

            RuleFor(command => command)
              .Cascade(CascadeMode.StopOnFirstFailure)
              .Must(IsOrganizationAdminDepartment)
              .WithMessage($"CannotDeleteOrganizationAdminDepartment")
              .When(c => c.EntityName == nameof(PraxisClient) && string.IsNullOrEmpty(c.AdditionalInfosItemId));

            RuleFor(command => command)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(IsCategorySubCategoryAlreadyUsed)
                .WithMessage("Data already used in other entity. Data can't be delete.")
                .DependentRules(() =>
                {
                    RuleFor(command => command)
                        .Cascade(CascadeMode.StopOnFirstFailure)
                        .Must(IsDataExists)
                        .WithMessage($"Data doesn't exists.");
                })
                .When(c => c.EntityName == nameof(PraxisClientCategory) || c.EntityName == "PraxisClientSubCategory");

            _mongoDbDataContextProvider = mongoDbDataContextProvider;
            _securityContextProvider = securityContextProvider;
            _repository = repository;
        }

        public bool EntityRegistered(string entityName)
        {
            var filter = new BsonDocument("name", $"{entityName}s");
            var collections = _mongoDbDataContextProvider.GetTenantDataContext()
                .ListCollections(new ListCollectionsOptions { Filter = filter });
            return collections.Any();
        }
        
        public bool BeValidEntityName(string entityName)
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

        private bool IsValidGuid(string itemId)
        {
            bool isValidGuid = Guid.TryParse(itemId, out _);

            if (!isValidGuid)
            {
                return false;
            }

            return true;
        }

        private bool BeValidRole(DeleteDataCommand command)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var roleList = securityContext.Roles.ToList();
            switch (command.EntityName)
            {
                case nameof(PraxisOrganization):
                    return roleList.Contains("admin") || roleList.Contains("task_controller");
                case nameof(PraxisRoom):
                    return roleList.Contains("admin") || roleList.Contains("poweruser") || roleList.Contains("task_controller");
                case nameof(PraxisEquipment):
                    return roleList.Contains("admin") || roleList.Contains("poweruser") || roleList.Contains("task_controller");
                case nameof(PraxisEquipmentMaintenance):
                    return roleList.Contains("admin") || roleList.Contains("poweruser") || roleList.Contains("task_controller");
                case nameof(PraxisTraining):
                    return roleList.Contains("admin") || roleList.Contains("poweruser") || roleList.Contains("task_controller");
                case nameof(PraxisRisk):
                    return roleList.Contains("admin") || roleList.Contains("poweruser") || roleList.Contains("task_controller");
                case nameof(PraxisAssessment):
                    return roleList.Contains("admin") || roleList.Contains("poweruser") || roleList.Contains("task_controller");
                case nameof(PraxisClientCategory):
                    return roleList.Contains("admin") || roleList.Contains("poweruser") || roleList.Contains("task_controller");
                case nameof(PraxisForm):
                    return OwnItem(command) || roleList.Contains("admin") || roleList.Contains("poweruser") || roleList.Contains("task_controller");
                case "PraxisClientSubCategory":
                    return roleList.Contains("admin") || roleList.Contains("poweruser") || roleList.Contains("task_controller");
                case nameof(User):
                    return roleList.Contains("admin") || roleList.Contains("poweruser") || roleList.Contains("task_controller");
                case nameof(PraxisProcessGuide):
                    return roleList.Contains("admin") || roleList.Contains("poweruser") || roleList.Contains("task_controller");
                case nameof(PraxisClient):
                    return roleList.Contains("admin") || roleList.Contains("poweruser") || roleList.Contains("task_controller");
                case nameof(PraxisUserAdditionalInfo):
                case nameof(PraxisReport):
                    return roleList.Contains("admin") || roleList.Contains("poweruser") || roleList.Contains("leitung") || roleList.Contains("task_controller");
                default:
                    return false;
            }
        }

        private bool BeValidDataCount(DeleteDataCommand command)
        {
            var existingAssessment = _repository.GetItem<PraxisAssessment>(a => a.ItemId == command.ItemId && !a.IsMarkedToDelete);
            if (existingAssessment != null)
            {
                var assessmentList = _repository
                    .GetItems<PraxisAssessment>(
                        a => a.RiskId == existingAssessment.RiskId && !a.IsMarkedToDelete
                    )
                    .ToList();
                return assessmentList.Count != 1;
            }

            return false;
        }

        private bool IsOrganizationAdminDepartment(DeleteDataCommand command)
        {
            var dept = _repository.GetItem<PraxisClient>(c => c.ItemId == command.ItemId && !c.IsMarkedToDelete);
            if (dept != null)
            {
                var orgData = _repository.GetItem<PraxisOrganization>
                    (o => o.ItemId == dept.ParentOrganizationId && !o.IsMarkedToDelete);
                if (orgData != null)
                {
                    var praxisUserIds = new List<string>();
                    if (!string.IsNullOrEmpty(orgData.AdminUserId))
                    {
                        praxisUserIds.Add(orgData.AdminUserId);
                    }
                    if (!string.IsNullOrEmpty(orgData.DeputyAdminUserId))
                    {
                        praxisUserIds.Add(orgData.DeputyAdminUserId);
                    }
                    if (praxisUserIds.Count > 0)
                    {
                        var clientIds = _repository.GetItems<PraxisUser>
                            (pu => !pu.IsMarkedToDelete && praxisUserIds.Contains(pu.ItemId))?.AsEnumerable()
                            .Select(pu => pu.ClientList.FirstOrDefault(c => c.IsPrimaryDepartment))?
                            .Where(c => c != null && !string.IsNullOrEmpty(c.ClientId))
                            .Select(c => c.ClientId)?.ToList();

                        if (clientIds != null && clientIds.Contains(command.ItemId))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private bool IsOrganizationAdminUser(DeleteDataCommand command)
        {
            var user = _repository.GetItem<PraxisUser>(pu => pu.UserId == command.ItemId && !pu.IsMarkedToDelete);
            if (user != null)
            {
                var praxisUserId = user.ItemId;
                var orgData = _repository.GetItem<PraxisOrganization>
                    (o => (o.AdminUserId == praxisUserId || o.DeputyAdminUserId == praxisUserId) && !o.IsMarkedToDelete);

                if (orgData != null) return false;
            }
            return true;
        }

        private bool SystemAdmin(DeleteDataCommand command)
        {
            var user = _repository.GetItem<User>(a => a.ItemId == command.ItemId && !a.IsMarkedToDelete);
            if (user != null && user.Roles.Contains("system_admin"))
            {
                return false;
            }
            return true;
        }
        
        private bool IsCategorySubCategoryAlreadyUsed(DeleteDataCommand command)
        {
            var propertyName = string.Empty;
            var propertyNames = string.Empty;

            if (command.EntityName == nameof(PraxisClientCategory))
            {
                propertyName = "CategoryId";
                propertyNames = "CategoryIds";
            }
            else if (command.EntityName == "PraxisClientSubCategory")
            {
                propertyName = "SubCategoryId";
                propertyNames = "SubCategoryIds";
            }

            var filter = Builders<BsonDocument>.Filter.Eq($"{propertyName}", command.ItemId) & Builders<BsonDocument>.Filter.Eq("IsMarkedToDelete", false);
            var taskFilter = Builders<BsonDocument>.Filter.In($"{propertyNames}", new string[] { command.ItemId }) & Builders<BsonDocument>.Filter.Eq("IsMarkedToDelete", false);
            var entityList = new List<string>
            {
                nameof(PraxisEquipment),
                nameof(PraxisRisk),
                nameof(PraxisOpenItemConfig),
                nameof(PraxisTaskConfig)
            };

            foreach (var entity in entityList)
            {
                var collection = _mongoDbDataContextProvider.GetTenantDataContext().GetCollection<BsonDocument>(string.Format("{0}s", entity));
                var currentFilter = filter;
                if (entity == nameof(PraxisOpenItemConfig) || entity == nameof(PraxisTaskConfig))
                {
                    currentFilter = taskFilter;
                }

                var dataList = collection.Find(currentFilter).ToList();

                if (dataList.Count != 0)
                {
                    return false;
                }
            }

            return true;
        }
        
        private bool IsDataExists(DeleteDataCommand command)
        {
            var collection = _mongoDbDataContextProvider.GetTenantDataContext().GetCollection<BsonDocument>("PraxisClientCategorys");
            if (command.EntityName == nameof(PraxisClientCategory))
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", command.ItemId) & Builders<BsonDocument>.Filter.Eq("IsMarkedToDelete", false);
                var categoryData = collection.Find(filter).FirstOrDefault();
                if (categoryData != null)
                {
                    return true;
                }
            }
            else if (command.EntityName == "PraxisClientSubCategory")
            {
                var filter = Builders<BsonDocument>.Filter.Eq("SubCategories.ItemId", command.ItemId) & Builders<BsonDocument>.Filter.Eq("IsMarkedToDelete", false);
                var categoryData = collection.Find(filter).FirstOrDefault();
                if (categoryData != null)
                {
                    return true;
                }
            }
            return false;
        }
        
        public ValidationResult IsSatisfiedby(DeleteDataCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
        private bool OwnItem(DeleteDataCommand command)
        {
            var form = _repository.GetItem<PraxisForm>(f => f.ItemId == command.ItemId && !f.IsMarkedToDelete);
            if (form != null)
            {
                var userId = _securityContextProvider.GetSecurityContext().UserId;
                return form.CreatedBy == userId;
            }
            return false;
        }
    }
}
