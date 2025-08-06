using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.Entities;
using System;
using System.Globalization;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactSearchQueryBuilderService : IObjectArtifactSearchQueryBuilderService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IObjectArtifactAuthorizationCheckerService _objectArtifactAuthorizationCheckerService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactFilterUtilityService _objectArtifactFilterUtilityService;

        public ObjectArtifactSearchQueryBuilderService(
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IObjectArtifactAuthorizationCheckerService objectArtifactAuthorizationCheckerService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactFilterUtilityService objectArtifactFilterUtilityService)
        {
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _objectArtifactAuthorizationCheckerService = objectArtifactAuthorizationCheckerService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactFilterUtilityService = objectArtifactFilterUtilityService;
        }

        #region Public methods

        public PipelineDefinition<BsonDocument, BsonDocument> BuildSearchPipeline(ObjectArtifactSearchCommand command)
        {
            BsonDocument matchDefinition = new BsonDocument("$match", new BsonDocument("$and", PrepareMatchFilter(command)));
            BsonDocument sortDefinition = new BsonDocument("$sort", new BsonDocument($"{command.Sort.PropertyName}", command.Sort.Direction));
            BsonDocument skipDefinition = new BsonDocument("$skip", (command.PageNumber - 1) * command.PageSize);
            BsonDocument limitDefinition = new BsonDocument("$limit", command.PageSize);

            var pipelineDefinition =
                IsARootDirectoryViewCommand(command) ?
                PrepareAllViewSearchPipeline(matchDefinition, sortDefinition, skipDefinition, limitDefinition) :
                command.Type == _objectArtifactUtilityService.GetLibraryViewModeName($"{LibraryViewModeEnum.FORM}") ?
                PrepareFormsSearchPipeline(matchDefinition, sortDefinition, skipDefinition, limitDefinition) :
                new BsonDocument[] { matchDefinition, sortDefinition, skipDefinition, limitDefinition };

            return pipelineDefinition;
        }

        public BsonDocument PrepareRoleWiseFilter(string organizationId, string departmentId, string parentId)
        {
            BsonDocument matchFilter;

            if (!string.IsNullOrWhiteSpace(parentId))
            {
                matchFilter = _objectArtifactFilterUtilityService.PrepareParentIdFilter(parentId);
            }
            else
            {
                matchFilter = !string.IsNullOrWhiteSpace(departmentId) ?
                    _objectArtifactFilterUtilityService.PrepareDepartmentIdFilter(departmentId, organizationId, true) :
                    !string.IsNullOrWhiteSpace(organizationId) ?
                    _objectArtifactFilterUtilityService.PrepareOrganizationIdFilter(organizationId) : null;
            }

            return matchFilter;
        }

        #endregion

        #region Form view Search Pipeline
        private BsonDocument[] PrepareFormsSearchPipeline(
            BsonDocument matchDefinition,
            BsonDocument sortDefinition,
            BsonDocument skipDefinition,
            BsonDocument limitDefinition)
        {
            var pipelineDefinition = new BsonDocument[]
            {
                matchDefinition, PrepareFormResponseGraphLookUpDefinition(), SetFormResponsesMaxLimit(), sortDefinition, skipDefinition, limitDefinition
            };

            return pipelineDefinition;
        }

        private BsonDocument PrepareFormResponseGraphLookUpDefinition()
        {
            BsonDocument graphLookUpDefinition = new BsonDocument("$graphLookup", new BsonDocument()
                                .Add("from", $"{nameof(ObjectArtifact)}s")
                                .Add("startWith", "$_id")
                                .Add("connectFromField", "_id")
                                .Add("connectToField", "MetaData.OriginalArtifactId.Value")
                                .Add("as", "FormResponses")
                                .Add("maxDepth", 0)
                                .Add("restrictSearchWithMatch", PrepareFormResponseMatchFilter())
                            );

            return graphLookUpDefinition;
        }

        private BsonDocument PrepareFormResponseMatchFilter()
        {
            var filters = new BsonArray()
                .Add(new BsonDocument("MetaData.IsAOriginalArtifact.Value", new BsonDocument("$ne", $"{(int)LibraryBooleanEnum.TRUE}")))
                .Add(_objectArtifactFilterUtilityService.PrepareReadPermissionFilter());

            return new BsonDocument("$and", filters);
        }

        private BsonDocument SetFormResponsesMaxLimit()
        {
            BsonDocument limitFormResponses = new BsonDocument("$addFields", new BsonDocument
                                        {
                                            { "FormResponseTotalCount", new BsonDocument("$size", "$FormResponses") },
                                            { "FormResponses", new BsonDocument("$slice", new BsonArray { "$FormResponses", 2 }) }
                                        });
            return limitFormResponses;
        }
        #endregion

        #region All view Search Pipeline

        private bool IsARootDirectoryViewCommand(ObjectArtifactSearchCommand command)
        {
            return
                (command.Type == _objectArtifactUtilityService.GetLibraryViewModeName($"{LibraryViewModeEnum.ALL}") || string.IsNullOrEmpty(command.Type)) &&
                string.IsNullOrWhiteSpace(command.ObjectArtifactId) &&
                string.IsNullOrWhiteSpace(command.ParentId) &&
                string.IsNullOrWhiteSpace(command.Text) &&
                command.IsRootSearch;
        }

        private BsonDocument[] PrepareAllViewSearchPipeline(
            BsonDocument initialMatchDefinition,
            BsonDocument sortDefinition,
            BsonDocument skipDefinition,
            BsonDocument limitDefinition)
        {
            return new BsonDocument[]
            {
                    initialMatchDefinition,
                    PrepareAllViewLookupDefinitionDefinition(),
                    PrepareAllViewUnwindDefinition(),
                    PrepareAllViewAddFiledsDefinition(),
                    PrepareAllViewFinalMatchDefinition(),
                    PrepareFormResponseGraphLookUpDefinition(),
                    SetFormResponsesMaxLimit(),
                    sortDefinition, skipDefinition, limitDefinition
            };
        }

        public BsonDocument PrepareAllViewLookupDefinitionDefinition()
        {
            return new BsonDocument("$lookup", new BsonDocument
            {
                { "from", $"{nameof(ObjectArtifact)}s" },
                { "localField", "ParentId" },
                { "foreignField", "_id" },
                { "as", "Parent" }
            });
        }

        public BsonDocument PrepareAllViewUnwindDefinition()
        {
            return new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$Parent" },
                { "preserveNullAndEmptyArrays", true }
            });
        }

        public BsonDocument PrepareAllViewAddFiledsDefinition()
        {
            return new BsonDocument("$addFields", new BsonDocument
            {
                { "IsParentShared", new BsonDocument
                    {
                        { "$cond", new BsonDocument
                            {
                                { "if", new BsonDocument
                                    {
                                        { "$and", new BsonArray
                                            {
                                                new BsonDocument
                                                {
                                                    { "$ne", new BsonArray { "$ParentId", BsonNull.Value } }
                                                },
                                                PrepareIsParentsharedMatchDefinition()
                                            }
                                        }
                                    }
                                },
                                { "then", true },
                                { "else", false }
                            }
                        }
                    }
                }
            });
        }

        private BsonDocument PrepareIsParentsharedMatchDefinition()
        {
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            var roles = GetLoggedInUserRoleBundle();

            var userIdFiler = new BsonDocument
            {
                { "$in", new BsonArray { userId, new BsonDocument("$ifNull", new BsonArray { "$Parent.IdsAllowedToRead", new BsonArray() }) } }
            };
            var filters = new BsonArray().Add(userIdFiler);

            roles.ForEach(role =>
            {
                var roleFilter = new BsonDocument
                {
                    { "$in", new BsonArray { role, new BsonDocument("$ifNull", new BsonArray { "$Parent.RolesAllowedToRead", new BsonArray() }) } }
                };
                filters.Add(roleFilter);
            });

            return new BsonDocument("$or", filters);
        }

        private List<string> GetLoggedInUserRoleBundle()
        {
            List<string> roles;
            if (_securityHelperService.IsAAdmin())
            {
                roles = new List<string>() { RoleNames.Admin };
            }
            else if (_securityHelperService.IsATaskController())
            {
                roles = new List<string>() { RoleNames.TaskController };
            }
            else
            {
                roles = _securityHelperService.GetLoggedInUserDepartmentLevelDynamicRoles()?.ToList();
                var orgRoles = _securityHelperService.GetLoggedInUserOrganizationGeneralAccessDynamicRole();
                if (orgRoles?.Count > 0)
                {
                    roles.AddRange(orgRoles);
                }
            }
            
            return roles;
        }

        public BsonDocument PrepareAllViewFinalMatchDefinition()
        {
            return new BsonDocument("$match", new BsonDocument
            {
                { "$or", new BsonArray
                    {
                        new BsonDocument
                        {
                            { "ParentId", BsonNull.Value }
                        },
                        new BsonDocument
                        {
                            { "IsParentShared", false }
                        }
                    }
                }
            });
        }

        #endregion

        private BsonArray PrepareMatchFilter(ObjectArtifactSearchCommand command)
        {
            var matchFilter = new BsonArray().Add(PrepareIsMarkedToDeleteFilter());

            if (!string.IsNullOrEmpty(command.ObjectArtifactId))
            {
                matchFilter.Add(_objectArtifactFilterUtilityService.PrepareReadPermissionFilter());
                matchFilter.Add(_objectArtifactFilterUtilityService.PrepareObjectArtifactIdFilter(command.ObjectArtifactId));
                
                var deptId = _securityHelperService.IsADepartmentLevelUser() ?
                            _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() : string.Empty;
                matchFilter.Add(_objectArtifactFilterUtilityService.PrepareExcludeSecretArtifactFilter(deptId));
                command.PageNumber = 1;
                command.PageSize = 1;
            }
            else
            {
                if (command.ArtifactType == ArtifactTypeEnum.Folder)
                {
                    var removeOrgFolderFilter = _objectArtifactFilterUtilityService.RemoveOrganizationFolderFromFilter(command?.OrganizationId);
                    if (removeOrgFolderFilter != null)
                    {
                        matchFilter.Add(removeOrgFolderFilter);
                    }
                }

                matchFilter.Add(_objectArtifactFilterUtilityService.PrepareReadPermissionFilter());

                var deptId = _securityHelperService.IsADepartmentLevelUser() ?
                            _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() : string.Empty;
                matchFilter.Add(_objectArtifactFilterUtilityService.PrepareExcludeSecretArtifactFilter(deptId));

                var roleWiseFilter = PrepareRoleWiseFilter(command.OrganizationId, command.DepartmentId, command.ParentId);
                if (roleWiseFilter?.Count() > 0)
                {
                    matchFilter.Add(roleWiseFilter);
                }

                var viewModeFilter = PrepareViewModeFilter(command);
                if (viewModeFilter != null)
                {
                    matchFilter.Add(viewModeFilter);
                }

                PrepareOtherSearchFilter(matchFilter, command);

                var textSearchFilter = _objectArtifactFilterUtilityService.PrepareObjectArtifactTextSearchFilter(command.Text);
                if (textSearchFilter != null)
                {
                    matchFilter.Add(textSearchFilter);
                }
            }

            return matchFilter;
        }

        private BsonDocument PrepareIsMarkedToDeleteFilter()
        {
            var filter = new BsonDocument(nameof(ObjectArtifact.IsMarkedToDelete), BsonValue.Create(false));
            return filter;
        }

        private BsonDocument PrepareViewModeFilter(ObjectArtifactSearchCommand command)
        {
            var viewMode = _objectArtifactUtilityService.GetLibraryViewModeKey(command.Type);
            var viewModeFilter =
                viewMode == $"{LibraryViewModeEnum.ALL}" ? PrepareAllViewFilter(command.ArtifactType) :
                viewMode == $"{LibraryViewModeEnum.APPROVAL_VIEW}" ? PrepareApprovalViewExcludingFilledFormFilter() :
                viewMode == $"{LibraryViewModeEnum.DOCUMENT}" ? PrepareWordsViewFilter() :
                viewMode == $"{LibraryViewModeEnum.MANUAL}" ? PrepareManualViewFilter() :
                PrepareFormsViewFilter();
            return viewModeFilter;
        }

        private BsonDocument PrepareAllViewFilter(ArtifactTypeEnum? artifactType)
        {
            var filter =
                artifactType == ArtifactTypeEnum.Folder ? _objectArtifactFilterUtilityService.PrepareArtifactTypeWiseFilter(ArtifactTypeEnum.Folder) :
                artifactType == ArtifactTypeEnum.File ? _objectArtifactFilterUtilityService.PrepareApprovedFileExcludingFilledFormFilter() :
                null;
            return filter;
        }

        private BsonDocument PrepareApprovalViewExcludingFilledFormFilter()
        {
            var filterValue =
                new BsonArray()
                .Add(_objectArtifactFilterUtilityService.PrepareExcludeFilledFormFilter())
                .Add(PrepareApprovalViewFilter());
            var filter = new BsonDocument("$and", filterValue);
            return filter;
        }

        private BsonDocument PrepareApprovalViewFilter()
        {
            var viewMode = $"{LibraryViewModeEnum.APPROVAL_VIEW}";
            var value = new string[]
                    {
                        ((int)LibraryFileApprovalStatusEnum.PENDING).ToString(),
                        ((int)LibraryFileApprovalStatusEnum.APPROVED).ToString(),
                        ((int)LibraryFileApprovalStatusEnum.PARTIALLY_APPROVED).ToString()
                    };
            var filter = new BsonDocument(GetTypeFilterPropertyName(viewMode), new BsonDocument("$in", new BsonArray(value)));
            return filter;
        }

        private BsonDocument PrepareWordsViewFilter()
        {
            var filterValue =
                new BsonArray()
                .Add(_objectArtifactFilterUtilityService.PrepareWordFileFilter())
                .Add(_objectArtifactFilterUtilityService.PrepareApprovedFileFilter());
            var filter = new BsonDocument("$and", filterValue);
            return filter;
        }

        private BsonDocument PrepareManualViewFilter()
        {
            var filterValue = new BsonArray
            {
                new BsonDocument("MetaData.FileType.Value", "9")
            };

            var filter = new BsonDocument("$and", filterValue);
            return filter;
        }

        private BsonDocument PrepareFormsViewFilter()
        {
            var filterValue =
                new BsonArray()
                .Add(_objectArtifactFilterUtilityService.PrepareFormFilter())
                .Add(_objectArtifactFilterUtilityService.PrepareOriginalArtifactFilter())
                .Add(_objectArtifactFilterUtilityService.PrepareApprovedFileFilter());
            var filter = new BsonDocument("$and", filterValue);
            return filter;
        }

        private void PrepareOtherSearchFilter(BsonArray filter, ObjectArtifactSearchCommand command)
        {
            var searchFilter = command.Filter;
            if (searchFilter == null) return;

            var viewMode = _objectArtifactUtilityService.GetLibraryViewModeKey(command.Type);

            if (!string.IsNullOrEmpty(searchFilter.Version))
            {
                if (double.TryParse(searchFilter.Version, out double versionNumber))
                {
                    filter.Add(new BsonDocument("$expr", new BsonDocument("$and", new BsonArray
                    {
                        new BsonDocument("$ne", new BsonArray { "$MetaData.Version.Value", BsonNull.Value }),
                        new BsonDocument("$gte", new BsonArray { BsonDocument.Parse("{$toDouble: '$MetaData.Version.Value'}"), 0 }),
                        new BsonDocument("$eq", new BsonArray
                        {
                            new BsonDocument("$toDouble", "$MetaData.Version.Value"),
                            versionNumber
                        })
                    })));
                }
                else filter.Add(new BsonDocument("MetaData.Version.Value", BsonValue.Create(searchFilter.Version)));
            }
            if (searchFilter.Status != null)
            {
                if (viewMode != $"{LibraryViewModeEnum.ALL}" ||
                    (viewMode == $"{LibraryViewModeEnum.ALL}" && (int)command.ArtifactType == 2))
                {
                    filter.Add(new BsonDocument("MetaData.Status.Value", BsonValue.Create(((int)searchFilter.Status).ToString())));
                }
            }
            if (searchFilter.ApprovalStatus != null)
            {
                PrepareApprovalStatusFilter(filter, searchFilter.ApprovalStatus);
            }
            if (searchFilter.UploadedDetail != null)
            {
                PrepareUploadedDetailFilter(filter, searchFilter.UploadedDetail);
            }
            if (searchFilter.ApprovedDetail != null)
            {
                PrepareApprovedDetailFilter(filter, searchFilter.ApprovedDetail);
            }
            if (searchFilter.AssignedDetail != null)
            {
                PrepareAssignedToFilter(filter, searchFilter.AssignedDetail);
            }
            if (searchFilter.FormFilledBy?.Count() > 0)
            {
                filter.Add(PrepareFormFilledByFilter(searchFilter.FormFilledBy));
            }
            if (searchFilter.FormFillPendingBy?.Count() > 0)
            {
                filter.Add(PrepareFormFillPendingByFilter(searchFilter.FormFillPendingBy));
            }
            if (searchFilter.FileFormats != null)
            {
               filter.Add(_objectArtifactFilterUtilityService.PrepareFileFormatFilter(searchFilter.FileFormats.Value));
            }
        }

        private void PrepareApprovalStatusFilter(BsonArray filter, LibraryFileApprovalStatusEnum? approvalStatus)
        {
            if (approvalStatus == LibraryFileApprovalStatusEnum.REAPPROVE)
            {
                filter.Add
                (
                    new BsonDocument
                    (
                        "MetaData.ReapproveProcessStartDate.Value",
                        new BsonDocument().Add("$lte", BsonValue.Create(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)))
                    )
                );
            }
            else
            {
                var orFilters = new BsonArray();
                if (approvalStatus == LibraryFileApprovalStatusEnum.PENDING)
                {
                    orFilters.Add(new BsonDocument("MetaData.ApprovalStatus.Value", BsonValue.Create(((int)LibraryFileApprovalStatusEnum.PARTIALLY_APPROVED).ToString())));
                }
                orFilters.Add(new BsonDocument("MetaData.ApprovalStatus.Value", BsonValue.Create(((int)approvalStatus).ToString())));
                filter.Add(new BsonDocument("$or", orFilters));
            }
        }

        private void PrepareUploadedDetailFilter(BsonArray filter, ActionWiseFilter actionFilter)
        {
            if (actionFilter == null) return;

            if (!string.IsNullOrEmpty(actionFilter.PerformedBy))
            {
                filter.Add(new BsonDocument(nameof(ObjectArtifact.CreatedBy), BsonValue.Create(actionFilter.PerformedBy)));
            }
            if (actionFilter.DateRange != null && actionFilter.DateRange.StartDate != null)
            {
                filter.Add
                (
                    new BsonDocument
                    (
                        nameof(ObjectArtifact.CreateDate),
                        new BsonDocument().Add("$gte", BsonDateTime.Create((DateTime)actionFilter.DateRange.StartDate))
                    )
                );
            }
            if (actionFilter.DateRange != null && actionFilter.DateRange.EndDate != null)
            {
                filter.Add
                (
                    new BsonDocument
                    (
                        nameof(ObjectArtifact.CreateDate),
                        new BsonDocument().Add("$lte", BsonDateTime.Create((DateTime)actionFilter.DateRange.EndDate))
                    )
                );
            }
        }

        private void PrepareApprovedDetailFilter(BsonArray filter, ActionWiseFilter actionFilter)
        {
            if (_securityHelperService.ArePrimitiveValuesNullOrEmpty(actionFilter)) return;

            filter.Add(new BsonDocument("ActivitySummary.ActivityName", BsonValue.Create(((int)ArtifactActivityName.APPROVAL).ToString())));

            if (!string.IsNullOrEmpty(actionFilter.PerformedBy))
            {
                filter.Add(new BsonDocument("ActivitySummary.ActivityPerformerModel.PerformedBy", BsonValue.Create(actionFilter.PerformedBy)));
            }
            if (actionFilter.DateRange != null && actionFilter.DateRange.StartDate != null)
            {
                filter.Add
                (
                    new BsonDocument
                    (
                        "ActivitySummary.ActivityPerformerModel.PerformedOn",
                        new BsonDocument().Add("$gte", BsonValue.Create(((DateTime)actionFilter.DateRange.StartDate).ToString("o", CultureInfo.InvariantCulture)))
                    )
                );
            }
            if (actionFilter.DateRange != null && actionFilter.DateRange.EndDate != null)
            {
                filter.Add
                (
                    new BsonDocument
                    (
                        "ActivitySummary.ActivityPerformerModel.PerformedOn",
                        new BsonDocument().Add("$lte", BsonValue.Create(((DateTime)actionFilter.DateRange.EndDate).ToString("o", CultureInfo.InvariantCulture)))
                    )
                );
            }
        }

        private void PrepareAssignedToFilter(BsonArray filter, AssignedToFilter actionFilter)
        {
            if (actionFilter == null) return;

            if (actionFilter.AssigneIds?.Count() > 0)
            {
                filter.Add(PrepareAssignedToPermissionFilter(actionFilter.AssigneIds));
            }
            if (actionFilter.DateRange != null && actionFilter.DateRange.StartDate != null)
            {
                filter.Add
                (
                    new BsonDocument
                    (
                        "MetaData.AssignedOn.Value",
                        new BsonDocument().Add("$gte", BsonValue.Create(((DateTime)actionFilter.DateRange.StartDate).ToString("o", CultureInfo.InvariantCulture)))
                    )
                );
            }
            if (actionFilter.DateRange != null && actionFilter.DateRange.EndDate != null)
            {
                filter.Add
                (
                    new BsonDocument
                    (
                        "MetaData.AssignedOn.Value",
                        new BsonDocument().Add("$lte", BsonValue.Create(((DateTime)actionFilter.DateRange.EndDate).ToString("o", CultureInfo.InvariantCulture)))
                    )
                );
            }
        }

        private BsonDocument PrepareAssignedToPermissionFilter(string[] assigneeUserIds)
        {
            var users = _objectArtifactUtilityService.GetUsersByIds(assigneeUserIds);
            var assigneeRoles = users.SelectMany(u => u.Roles).Distinct().ToArray();

            var readPermissionFilter = new BsonDocument()
                            .Add("$or", new BsonArray()
                                    .Add(new BsonDocument()
                                            .Add(nameof(ObjectArtifact.SharedUserIdList), new BsonDocument()
                                                    .Add("$in", new BsonArray(assigneeUserIds ?? Array.Empty<string>()))
                                            )
                                    )
                                    .Add(new BsonDocument()
                                            .Add(nameof(ObjectArtifact.SharedRoleList), new BsonDocument()
                                                    .Add("$in", new BsonArray(assigneeRoles ?? Array.Empty<string>()))
                                            )
                                    )
                            );

            return readPermissionFilter;
        }

        private BsonDocument PrepareFormFilledByFilter(string[] praxisUserIds)
        {
            var formFillActiomName = $"{(int)ArtifactActivityName.FORM_RESPONSE_COMPLETED}";
            var formActionElemMatchFilter = new BsonDocument()
                            .Add("$and", new BsonArray()
                                    .Add(new BsonDocument().Add("ActivityName", BsonValue.Create(formFillActiomName)))
                                    .Add(new BsonDocument().Add("ActivityPerformerModel",
                                            new BsonDocument("$elemMatch",
                                                    new BsonDocument().Add("PerformedBy",
                                                        new BsonDocument().Add("$in",
                                                            new BsonArray(praxisUserIds?.ToArray() ?? Array.Empty<string>()))))))
                            );
            var formActionFilter =
                new BsonDocument(nameof(ObjectArtifact.ActivitySummary), new BsonDocument("$elemMatch", formActionElemMatchFilter));
            return formActionFilter;
        }

        private BsonDocument PrepareFormFillPendingByFilter(string[] praxisUserIds)
        {
            var userIds = _objectArtifactUtilityService.GetPraxisUsersByIds(praxisUserIds).Select(pu => pu.UserId).ToArray();
            var assignedToFilter = PrepareAssignedToPermissionFilter(userIds);
            var formFillActiomName = $"{(int)ArtifactActivityName.FORM_RESPONSE_COMPLETED}";
            var formActionFilter = new BsonDocument(nameof(ObjectArtifact.ActivitySummary),
                                    new BsonDocument("$elemMatch",
                                        new BsonDocument().Add("$and",
                                            new BsonArray()
                                                .Add(new BsonDocument().Add("ActivityName", BsonValue.Create(formFillActiomName)))
                                                .Add(new BsonDocument().Add("ActivityPerformerModel",
                                                    new BsonDocument("$elemMatch",
                                                        new BsonDocument().Add("PerformedBy",
                                                            new BsonDocument().Add("$nin",
                                                                new BsonArray(praxisUserIds.ToArray()))))))
                            )));
            var filter = new BsonDocument().Add("$and", new BsonArray().Add(assignedToFilter).Add(formActionFilter));
            return filter;
        }

        private string GetTypeFilterPropertyName(string type)
        {
            return LibraryModuleConstants.LibraryViewModeFilterPropertyMap[type];
        }

        private string GetTypeFilterValue(string type)
        {
            return LibraryModuleConstants.LibraryViewModeFilterValueMap[type];
        }
    }
}
