using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using System;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactReportsSharedDataResponseGeneratorService : IObjectArtifactReportsSharedDataResponseGeneratorService
    {
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly ILogger<ObjectArtifactReportsSharedDataResponseGeneratorService> _logger;
        private readonly IRiqsPediaViewControlService _riqsPediaViewControlService;

        public ObjectArtifactReportsSharedDataResponseGeneratorService(
            IObjectArtifactUtilityService objectArtifactUtilityService,
            ISecurityHelperService securityHelperService,
            ILogger<ObjectArtifactReportsSharedDataResponseGeneratorService> logger,
            IRiqsPediaViewControlService riqsPediaViewControlService)
        {
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _securityHelperService = securityHelperService;
            _logger = logger;
            _riqsPediaViewControlService = riqsPediaViewControlService;
        }

        public LibraryReportAssigneeDetail GetObjectArtifactAssigneeDetailResponse(
            string organizationId,
            IDictionary<string, MetaValuePair> metaData,
            List<SharedOrganizationInfo> sharedOrganizationList)
        {
            var response = new LibraryReportAssigneeDetail();
            if (sharedOrganizationList != null && !_objectArtifactUtilityService.IsAGeneralForm(metaData))
            {
                var assignedOnKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.ASSIGNED_ON}"];
                var dateTimestring = _objectArtifactUtilityService.GetMetaDataValueByKey(metaData, assignedOnKey);
                if (DateTime.TryParse(dateTimestring, out DateTime dateTimeValue)) response.AssignedOn = dateTimeValue;
                var riqsViewControl = _riqsPediaViewControlService.GetRiqsPediaViewControl().GetAwaiter().GetResult();
                response.AssignedOrganization = _securityHelperService.IsADepartmentLevelUser() && riqsViewControl?.IsAdminViewEnabled != true ?
                    null : PrepareAssignedOrganizationData(organizationId, sharedOrganizationList);
                response.AssignedDepartmentList = _securityHelperService.IsADepartmentLevelUser() && riqsViewControl?.IsAdminViewEnabled != true ?
                    PrepareRestrictedAssignedDepartmentData(organizationId, sharedOrganizationList) :
                    PrepareAssignedDepartmentData(organizationId, sharedOrganizationList);
            }
            return response;
        }


        private AssigneeSummary PrepareAssignedOrganizationData(
            string organizationId,
            List<SharedOrganizationInfo> sharedOrganizationList)
        {
            var sharedOrganization = sharedOrganizationList.FirstOrDefault(i => i.OrganizationId == organizationId);
            var assignedOrganization = sharedOrganization != null ?
                new AssigneeSummary()
                {
                    Id = sharedOrganization.OrganizationId,
                    Name = _objectArtifactUtilityService.GetOrganizationById(sharedOrganization.OrganizationId)?.ClientName
                } :
                null;
            return assignedOrganization;
        }

        private List<AssignedDepartment> PrepareRestrictedAssignedDepartmentData(
            string organizationId,
            List<SharedOrganizationInfo> sharedOrganizationList)
        {
            var assignedDepartmentList = new List<AssignedDepartment>();

            var departmentId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();

            var sharedOrganizations = sharedOrganizationList
                                    .GroupBy(i => i.OrganizationId)
                                    .Select(g => new SharedOrganizationInfo
                                    {
                                        OrganizationId = g.Key,
                                        Tags = g.SelectMany(gi => gi.Tags).ToArray(),
                                        SharedPersonList = g.SelectMany(gi => gi.SharedPersonList).ToList()
                                    }).ToList();
            var sharedOrganization = sharedOrganizations.FirstOrDefault(i => i.OrganizationId == organizationId);
            var sharedDepartment = sharedOrganizations.FirstOrDefault(d => d.OrganizationId == departmentId);

            if (sharedDepartment != null || sharedOrganization != null)
            {
                var userGroups =
                    sharedOrganization != null ? _securityHelperService.GetAllDepartmentLevelStaticRoles() :
                    sharedDepartment != null ? sharedDepartment.Tags :
                    null;

                var members =
                    sharedOrganization != null ? new string[] { } :
                    sharedDepartment != null ? sharedDepartment.SharedPersonList.ToArray() :
                    null;

                var praxisUsers = _objectArtifactUtilityService.GetDepartmentWiseAssignees(departmentId, userGroups, members);
                var assignedDepartment = new AssignedDepartment()
                {
                    Id = departmentId,
                    Name = _objectArtifactUtilityService.GetDepartmentById(departmentId)?.ClientName,
                    Assignees = PrepareDepartmentAssigneeList(praxisUsers)
                };
                assignedDepartmentList.Add(assignedDepartment);
            }

            return assignedDepartmentList;
        }

        private List<AssignedDepartment> PrepareAssignedDepartmentData(
            string organizationId,
            List<SharedOrganizationInfo> sharedOrganizationList)
        {
            var assignedDepartmentList = new List<AssignedDepartment>();

            var praxisUsers = _objectArtifactUtilityService.GetPraxisUsersByOrganizationId(organizationId);

            var groupedSharedRoles = sharedOrganizationList
                                    .Where(i => i.OrganizationId != organizationId)
                                    .GroupBy(i => i.OrganizationId)
                                    .Select(g => new SharedOrganizationInfo
                                    {
                                        OrganizationId = g.Key,
                                        Tags = g.SelectMany(gi => gi.Tags).ToArray(),
                                        SharedPersonList = g.SelectMany(gi => gi.SharedPersonList).ToList()
                                    }).ToList();

            foreach (var item in groupedSharedRoles)
            {
                if (item.OrganizationId != organizationId)
                {
                    var assignedDepartment = new AssignedDepartment()
                    {
                        Id = item.OrganizationId,
                        Name = _objectArtifactUtilityService.GetDepartmentById(item.OrganizationId)?.ClientName,
                        Assignees = PrepareDepartmentAssigneeList(praxisUsers, item.OrganizationId, item.Tags, item.SharedPersonList.ToArray())
                    };
                    assignedDepartmentList.Add(assignedDepartment);
                }
            }

            return assignedDepartmentList;
        }

        private List<AssigneeSummary> PrepareDepartmentAssigneeList(List<PraxisUser> praxisUsers)
        {
            var assignees =
                praxisUsers.Select(pu =>
                new AssigneeSummary
                {
                    Id = pu.ItemId,
                    Name = pu.DisplayName
                })
                .OrderBy(pu => pu.Name)
                .ToList();

            return assignees;
        }

        private List<AssigneeSummary> PrepareDepartmentAssigneeList(
            List<PraxisUser> praxisUsers,
            string departmentId,
            string[] roles,
            string[] ids)
        {
            var members = praxisUsers
                .Where(
                    pu => ids.Contains(pu.ItemId) ||
                    (pu.ClientList.Any(c => c.ClientId == departmentId && c.Roles.Any(r => roles.Contains(r)))))
                .Select(pu =>
                    new AssigneeSummary
                    {
                        Id = pu.ItemId,
                        Name = pu.DisplayName
                    })
                .OrderBy(pu => pu.Name)
                .ToList();

            return members;
        }
    }
}
