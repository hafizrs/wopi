using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeliseBlocks.GraphQL.Models;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisEntityService : IPraxisEntityService
    {
        private readonly IRepository _repository;

        public PraxisEntityService(IRepository repository)
        {
            _repository = repository;
        }

        private string[] SyncEntityReadPermissions(
            string[] userReadableFields,
            EntityPermissionAttribute[] attributes,
            List<string> additionalFields
        )
        {
            var allPropertiesOfEntity = new List<string>();
            allPropertiesOfEntity.AddRange(userReadableFields);
            allPropertiesOfEntity.AddRange(attributes.Select(attribute => attribute.ColumnName).ToList());
            allPropertiesOfEntity.AddRange(additionalFields);
            return allPropertiesOfEntity.Distinct().ToArray();
        }

        public async Task<bool> SetReadPermissionForEntity(SetReadPermissionForEntityCommand command)
        {
            try
            {
                var userReadableDatum = _repository.GetItem<UserReadableData>(datum => datum.EntityName.Equals(command.EntityName));
                var entityColumnPermissionDatum = _repository.GetItem<EntityColumnPermisssion>(datum => datum.EntityName.Equals(command.EntityName));

                var isNewUserReadableDatum = false;
                if (userReadableDatum == null)
                {
                    isNewUserReadableDatum = true;
                    userReadableDatum = new UserReadableData
                    {
                        EntityName = command.EntityName,
                        ItemId = Guid.NewGuid().ToString(),
                        UserReadableFields = new string[] { },
                    };
                }

                var isNewEntityColumnPermissionDatum = false;
                if (entityColumnPermissionDatum == null)
                {
                    isNewEntityColumnPermissionDatum = true;
                    entityColumnPermissionDatum = new EntityColumnPermisssion
                    {
                        EntityName = command.EntityName,
                        ItemId = Guid.NewGuid().ToString(),
                        Permissions = new[] { RoleNames.Admin, RoleNames.AppUser },
                        Attributes = new EntityPermissionAttribute[] { },
                        ChildEntities = new string[] { }
                    };
                }

                var allPropertiesOfEntity = SyncEntityReadPermissions(
                    userReadableDatum.UserReadableFields,
                    entityColumnPermissionDatum.Attributes,
                    command.AdditionalFields ?? new List<string>()
                );

                userReadableDatum.UserReadableFields = allPropertiesOfEntity;

                if (isNewUserReadableDatum)
                {
                    await _repository.SaveAsync(userReadableDatum);
                }
                else
                {
                    await _repository.UpdateAsync(datum => datum.EntityName.Equals(command.EntityName),
                        userReadableDatum);
                }


                var attributesAsList = entityColumnPermissionDatum.Attributes.ToList();
                foreach (var property in allPropertiesOfEntity)
                {
                    var columnExists = attributesAsList.FindIndex(attribute => attribute.ColumnName.Equals(property)) !=
                                       -1;
                    if (!columnExists)
                    {
                        attributesAsList.Add(new EntityPermissionAttribute
                        {
                            ColumnName = property,
                            Permissions = entityColumnPermissionDatum.Permissions
                        });
                    }
                }

                entityColumnPermissionDatum.Attributes = attributesAsList.ToArray();

                if (isNewEntityColumnPermissionDatum)
                {
                    await _repository.SaveAsync(entityColumnPermissionDatum);
                }
                else
                {
                    await _repository.UpdateAsync(datum => datum.EntityName.Equals(command.EntityName),
                        entityColumnPermissionDatum);
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}