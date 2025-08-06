using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using RabbitMQ.Client.Impl;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CirsReports;
using Selise.Ecap.Entities.PrimaryEntities.DWT;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;
using Microsoft.Extensions.Logging;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;

public class CirsReportQueryService : ICirsReportQueryService
{
    private const double THUMBNAIL_SIZE = 64.0;

    private readonly ILogger<CirsReportQueryService> _logger;
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly IRepository _repository;
    private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
    private readonly IPraxisFileService _praxisFileService;
    private readonly ICirsPermissionService _cirsPermissionService;
    private readonly ISecurityHelperService _securityHelperService;

    public CirsReportQueryService(
        ILogger<CirsReportQueryService> logger,
        ISecurityContextProvider securityContextProvider,
        IRepository repository,
        IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
        IPraxisFileService praxisFileService,
        IPraxisProcessGuideService praxisProcessGuideService,
        ICirsPermissionService cirsPermissionService,
        ISecurityHelperService securityHelperService)
    {
        _logger = logger;
        _securityContextProvider = securityContextProvider;
        _repository = repository;
        _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
        _praxisFileService = praxisFileService;
        _cirsPermissionService = cirsPermissionService;
        _securityHelperService = securityHelperService;
    }

    public async Task<List<CirsReportResponse>> GetReportsAsync(GetCirsReportQuery query)
    {
        try
        {
            var praxisClient = (await GetPraxisClientsAsync(query.PraxisClientId)).FirstOrDefault();
            var haveOfficerPermission = _cirsPermissionService.HaveAllUnitViewPermissions(_securityContextProvider.GetSecurityContext().UserId, query.DashboardNameEnum).GetAwaiter().GetResult();

            var dashboardPermission = await _cirsPermissionService.GetCirsDashboardPermissionAsync(query.PraxisClientId, query.DashboardNameEnum, true);
            var loggedInUserPermission = _cirsPermissionService.GetPermissionsByDashBoardName(query.DashboardNameEnum, dashboardPermission);

            var isACirsAdmin = await _cirsPermissionService.IsACirsAdminAsync(
                query.PraxisClientId,
                query.DashboardNameEnum,
                praxisClient, dashboardPermission);

            var cirsBoard = GetCirsBoards(query, loggedInUserPermission);
            var cirsGenericReportsCollection =
                _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<CirsGenericReport>("CirsGenericReports");


            var filter = GetCirsReportQueryFilter(query, praxisClient.ParentOrganizationId, isACirsAdmin, dashboardPermission, haveOfficerPermission, loggedInUserPermission);

            var projection = GetCirsReportQueryProjection();

            var cirsReportDocuments = await cirsGenericReportsCollection
                .Find(filter)
                .Sort("{Rank: 1}")
                .Project(projection)
                .ToListAsync();

            var cirsReports = cirsReportDocuments.Select(i => BsonSerializer.Deserialize<CirsReportData>(i)).ToList();

            FormatCirsReportSequenceEnumber(cirsReports);
            PopulateCirsBoard(cirsBoard, cirsReports);

            await SetServiceResponsesAsync(cirsReports, query, haveOfficerPermission, loggedInUserPermission);

            return cirsBoard;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in GetReportsAsync.\nMessage: {Message}.\nDetails: {StackTrace}", ex.Message, ex.StackTrace);
            throw;
        }
    }

    public Task<List<CirsGenericReport>> GetReportsAsync(GetCirsReportByIdsQuery query)
    {
        return Task.FromResult(_repository.GetItems<CirsGenericReport>(c => query.CirsReportIds.Contains(c.ItemId)).ToList());
    }

    public async Task<List<CirsGenericReport>> GetFaultReportsAsync(GetFaultReportByEquipmentIdQuery query)
    {
        try
        {
            var builder = Builders<CirsGenericReport>.Filter;
            var filter = builder.Eq(r => r.IsMarkedToDelete, false) &
                         builder.Eq(r => r.IsActive, true) &
                         builder.Eq(r => r.CirsDashboardName, CirsDashboardName.Fault) &
                         builder.Exists(r => r.MetaData) &
                         builder.Exists(r => r.MetaData["EquipmentId"]) &
                         builder.Eq(r => r.MetaData["EquipmentId"], query.EquipmentId) &
                         builder.Eq(r => r.OrganizationId, query.OrganizationId) &
                         builder.Eq(r => r.ClientId, query.PraxisClientId);

            var filterModel = await PreparePermissionFilterModel(query.PraxisClientId, query.OrganizationId, CirsDashboardName.Fault, query.IsActive, query.CirsReportId);
            var permissionFiler = await _cirsPermissionService.GetPermissionFilter(filterModel);
            filter &= permissionFiler;

            var reportCollection = _ecapMongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<CirsGenericReport>("CirsGenericReports");
            var projection = Builders<CirsGenericReport>.Projection
                .Include(r => r.ItemId)
                .Include(r => r.CreateDate)
                .Include(r => r.CreatedBy)
                .Include(r => r.Tags)
                .Include(r => r.Title)
                .Include(r => r.Status)
                .Include(r => r.Description)
                .Include(r => r.Remarks)
                .Include(r => r.MetaData);
            var reports = await reportCollection
                .Find(filter)
                .Project(projection)
                .ToListAsync();
            return reports?
                .Select(i => BsonSerializer.Deserialize<CirsGenericReport>(i))?
                .ToList() ?? new List<CirsGenericReport>();
        }
        catch(Exception ex)
        {
            throw new Exception("Error while fetching fault reports", ex);
        }
    }

    private async Task<GetPermissionFilterModel> PreparePermissionFilterModel(string clientId, string organizationId, CirsDashboardName dashboardName, bool isActive, string cirsReportId)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();
        var praxisClient = (await GetPraxisClientsAsync(clientId)).FirstOrDefault();
        var haveOfficerPermission = await _cirsPermissionService.HaveAllUnitViewPermissions(_securityContextProvider.GetSecurityContext().UserId, dashboardName);

        var dashboardPermission = await _cirsPermissionService.GetCirsDashboardPermissionAsync(clientId, dashboardName, true);
        var loggedInUserPermission = _cirsPermissionService.GetPermissionsByDashBoardName(dashboardName, dashboardPermission);

        var isACirsAdmin = await _cirsPermissionService.IsACirsAdminAsync(
            clientId,
            dashboardName,
            praxisClient, dashboardPermission);

        return new GetPermissionFilterModel
        {
            LoggedInUserPermission = loggedInUserPermission,
            DashboardName = CirsDashboardName.Fault,
            ClientId = clientId,
            ClientOrganizationId = organizationId,
            IsActive = isActive,
            CirsReportId = cirsReportId,
            HaveOfficerPermission = haveOfficerPermission,
            PraxisClient = praxisClient,
            IsACirsAdmin = isACirsAdmin,
            DashboardPermission = dashboardPermission
        };
    }


    private async Task SetServiceResponsesAsync(List<CirsReportData> cirsReportDatas, GetCirsReportQuery query, bool haveOfficerPermission, Dictionary<string, bool> loggedInUserPermission)
    {
        if (cirsReportDatas == null || !cirsReportDatas.Any())
        {
            return;
        }

        var createdByUserIds = cirsReportDatas.Select(data => data.CreatedBy).ToList();
        var reporters = cirsReportDatas.Select(data => data.ReportedBy)
                                    .Where(c => c != null && !string.IsNullOrEmpty(c.UserId))
                                    .Select(c => c.UserId).ToList();

        var afftectedUserIds = cirsReportDatas
            .Where(data => data.AffectedInvolvedParties != null)
            .SelectMany(data => data.AffectedInvolvedParties)
            .Where(party => party.InvolvedUsers != null)
            .SelectMany(party => party.InvolvedUsers)
            .Where(user => user != null && !string.IsNullOrEmpty(user.UserId))
            .Select(user => user.UserId)
            .ToList();

        var responsebleUserIds = cirsReportDatas
            .Where(data => data.ResponsibleUsers != null)
            .SelectMany(data => data.ResponsibleUsers)
            .Where(user => user != null && !string.IsNullOrEmpty(user.UserId))
            .Select(user => user.UserId)
            .ToList();

        var allUserIds = createdByUserIds.Concat(afftectedUserIds).ToList();
        allUserIds.AddRange(responsebleUserIds);
        allUserIds.AddRange(reporters);

        allUserIds = allUserIds.Distinct().ToList();

        var praxisUsers = await GetPraxisUsersAsync(allUserIds);

        var attachmentIds = cirsReportDatas
            .Where(data => data.AttachmentIds != null)
            .SelectMany(data => data.AttachmentIds)
            .Distinct().ToList();

        var filesInfo = await _praxisFileService.GetFilesInfoFromStorage(attachmentIds);

        // Fetch PraxisClient documents from the database
        var praxisClientIds = cirsReportDatas
            .Where(data => data.AffectedInvolvedParties != null)
            .SelectMany(data => data.AffectedInvolvedParties)
            .Where(party => !string.IsNullOrEmpty(party.PraxisClientId))
            .Select(party => party.PraxisClientId)
            .Distinct()
            .ToList();

        var reporterClientIds = cirsReportDatas
            .Where(data => data.MetaData != null)
            .Select(data => data.MetaData.GetValueOrDefault("ReporterClientId")?.ToString())
            .Where(data => !string.IsNullOrWhiteSpace(data));

        var clientIds = cirsReportDatas
            .Where(data => !string.IsNullOrEmpty(data.ClientId))
            .Select(data => data.ClientId)
            .Distinct()
            .ToList();
        praxisClientIds = praxisClientIds.Concat(clientIds).Distinct().ToList();

        var praxisClients = await GetPraxisClientsAsync(praxisClientIds.Concat(reporterClientIds).Distinct().ToArray());

        var praxisClientDictionary = praxisClients.ToDictionary(client => client.ItemId);

        var artifactInfos = cirsReportDatas?
            .Select(data => data?.AttachedDocuments)?
            .Where(data => data != null)?
            .SelectMany(data => data)?
            .ToList()
            ?? new List<ReportingAttachmentFile>();

        artifactInfos.AddRange(
            cirsReportDatas?
            .Select(data => data?.AttachedForm)?
            .Where(data => data != null)?
            .ToList()
            ?? new List<ReportingAttachmentFile>()
        );

        var securityContext = _securityContextProvider.GetSecurityContext();
        var praxisClient = praxisClients.FirstOrDefault(c => c.ItemId == query.PraxisClientId);

        foreach (var reportData in cirsReportDatas)
        {
            var isSentAnonymously = reportData?.MetaData?.TryGetValue($"{CommonCirsMetaKey.IsSentAnonymously}", out object sendAnonymous) == true
                                    && sendAnonymous?.ToString() == ((int)CirsBooleanEnum.True).ToString();

            reportData.IsSentAnonymously = isSentAnonymously;
            
            reportData.ResponsibleUsers = reportData.ResponsibleUsers?
                .Select(involvedUser => praxisUsers.FirstOrDefault(user => involvedUser.UserId == user?.UserId))?
                .Where(user => user != null)?.Distinct()?
                .ToList() ?? new List<PraxisUser>();

            reportData.AffectedInvolvedData = reportData.AffectedInvolvedParties?.Select(party => new InvolvementResponse
            {
                PraxisClientId = party.PraxisClientId,
                PraxisClientName = praxisClientDictionary.GetValueOrDefault(party.PraxisClientId)?.ClientName,
                InvolvedUsers = praxisUsers.Where(user =>
                    party.InvolvedUsers != null && 
                    party.InvolvedUsers.Select(u => u.UserId).Contains(user?.UserId)).Distinct().ToArray(),
                InvolvedAt = party.InvolvedAt,
                InvolvedParty = party.InvolvedParty
            })?.ToList() ?? new List<InvolvementResponse>();

            if (!string.IsNullOrEmpty(reportData.ClientId)) 
                reportData.ClientName = praxisClientDictionary.GetValueOrDefault(reportData.ClientId)?.ClientName;

            reportData.Attachments = filesInfo
                .Where(info => reportData.AttachmentIds != null && reportData.AttachmentIds.Contains(info.ItemId))
                .Select(info => new FileResponse
                {
                    FileStorageId = info.ItemId,
                    Name = info.Name,
                    DocumentId = GetArtifactByFileId(artifactInfos, info.ItemId)
                }).ToList();

            reportData.ReporterClientName = praxisClients.FirstOrDefault(
                client => client.ItemId == reportData.MetaData
                    .GetValueOrDefault("ReporterClientId")?.ToString())
                ?.ClientName;

            reportData.Permissions = PreparePermission(reportData, loggedInUserPermission, securityContext, praxisClient, haveOfficerPermission);

            if (isSentAnonymously)
            {
                reportData.CreatedBy = null;
                reportData.ReportedBy = null;
                reportData.CreatorUser = null;
            }
            else
            {
                reportData.CreatorUser = praxisUsers.FirstOrDefault(user => user.UserId == reportData.CreatedBy);
                reportData.ReportedBy = !string.IsNullOrEmpty(reportData.ReportedBy?.UserId) ? praxisUsers.FirstOrDefault(user => user.UserId == reportData.ReportedBy?.UserId) : null;
            }

        }
    }

    private Dictionary<string, bool> PreparePermission
    (
        CirsReportData report, 
        Dictionary<string, bool> loggedInUserPermission, 
        SecurityContext securityContext,
        PraxisClient client,
        bool haveOfficerPermission
    )
    {
        if (loggedInUserPermission == null) loggedInUserPermission = new Dictionary<string, bool>();

        var permissions = new Dictionary<string, bool>();
        var userId = securityContext.UserId;
        //var isCreator = report?.CreatedBy == userId;
        var isEquipmentManager = report.EquipmentManagers?.Contains(userId) ?? false;

        var canEditReport = haveOfficerPermission || (loggedInUserPermission.ContainsKey(CirsPermissionValue.CanEditReport)
                            ? loggedInUserPermission[CirsPermissionValue.CanEditReport]
                            : false) || isEquipmentManager ;

        var haveThisUnitPermission = true;
        var processAllOrgPermission = !(report.CirsDashboardName == CirsDashboardName.Incident || report.CirsDashboardName == CirsDashboardName.Complain);

        if (_securityHelperService.IsADepartmentLevelUser() && client.ItemId != _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser())
        {
            haveThisUnitPermission = false;
        }

        foreach (var permissionKey in CirsModuleConstants.CirsReportDataPermissionKeys)
        {
            if (!string.IsNullOrEmpty(permissionKey))
            {
                var isEnable = false;
                if (loggedInUserPermission.TryGetValue(permissionKey, out bool permissionValue))
                {
                    isEnable = permissionValue;
                }

                // check available permissions
                if (permissionKey == CirsPermissionValue.CanViewReport)
                {
                    isEnable = true;
                }
                else if (permissionKey == CirsPermissionValue.CanEditReport)
                {
                    isEnable = (isEnable || haveOfficerPermission || isEquipmentManager) && report.IsActive && (haveThisUnitPermission || processAllOrgPermission);
                }
                else if (permissionKey == CirsPermissionValue.CanCloneReport)
                {
                    isEnable = CirsDashboardName.Incident == report.CirsDashboardName && (isEnable || canEditReport || isEquipmentManager) && report.IsActive && (haveThisUnitPermission || processAllOrgPermission);
                }
                else if (permissionKey == CirsPermissionValue.CanMoveReport)
                {
                    isEnable = (isEnable || canEditReport || isEquipmentManager) && report.IsActive && (haveThisUnitPermission || processAllOrgPermission);
                }
                else if (permissionKey == CirsPermissionValue.CanCreateProcessGuide)
                {
                    isEnable = report.IsActive && canEditReport && client.Navigations.Any(n => n.Name == "PROCESS_GUIDE") && haveThisUnitPermission;
                }
                else if (permissionKey == CirsPermissionValue.CanCreateToDo)
                {
                    isEnable = report.IsActive && canEditReport && client.Navigations.Any(n => n.Name == "OPEN_ITEMS") && haveThisUnitPermission;
                }

                permissions.Add(permissionKey, isEnable);
            }
        }

        return permissions;
    }

    private string GetArtifactByFileId(List<ReportingAttachmentFile> artifacts, string fileId)
    {
        var artifact = artifacts?.Find(a => a.FileStorageId == fileId);
        return artifact?.ItemId;
    }

    private async Task<List<PraxisClient>> GetPraxisClientsAsync(params string[] praxisClientIds)
    {
        var collection = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<PraxisClient>("PraxisClients");
        var filter = Builders<PraxisClient>.Filter.In(client => client.ItemId, praxisClientIds);
        var projection = Builders<PraxisClient>.Projection
            .Include(client => client.ItemId)
            .Include(client => client.ClientName)
            .Include(client => client.ParentOrganizationId)
            .Include(client => client.Navigations);

        return await collection.Find(filter).Project<PraxisClient>(projection).ToListAsync();
    }

    private async Task<List<PraxisUser>> GetPraxisUsersAsync(List<string> userIds)
    {
        var praxisUserCollection = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<PraxisUser>("PraxisUsers");

        var filter = Builders<PraxisUser>.Filter.In(u => u.UserId, userIds);

        var projection = Builders<PraxisUser>.Projection
            .Include(u => u.ItemId)
            .Include(u => u.UserId)
            .Include(u => u.DisplayName)
            .Include(u => u.ClientName)
            .Include(u => u.Image)
            .Include(u => u.Active)
            .Include(u => u.Salutation)
            .Include(u => u.Phone)
            .Include(u => u.Email);

        var praxisUserDocuments = await praxisUserCollection
            .Find(filter)
            .Project(projection)
            .ToListAsync();

        return praxisUserDocuments.Select(doc => BsonSerializer.Deserialize<PraxisUser>(doc)).ToList();
    }

    private static List<CirsReportResponse> GetCirsBoards(GetCirsReportQuery query, Dictionary<string, bool> loggedInUserPermission)
    {
        var cirsBoards = new List<CirsReportResponse>();
        var isShow = true;
        if (loggedInUserPermission?.ContainsKey(CirsPermissionValue.HideToBeApprovedColumn) == true) isShow = !loggedInUserPermission[CirsPermissionValue.HideToBeApprovedColumn];

        var statusEnumValues = query.DashboardNameEnum.StatusEnumValues(isShow);

        foreach (var status in statusEnumValues)
        {
            cirsBoards.Add(new CirsReportResponse
            {
                Status = status.ToString(),
                Data = new List<CirsReportData>()
            });
        }

        return cirsBoards;
    }

    private FilterDefinition<CirsGenericReport> GetCirsReportQueryFilter(
        GetCirsReportQuery query, 
        string clientOrganizationId, 
        bool isACirsAdmin, 
        CirsDashboardPermission dashboardPermission, bool haveOfficerPermission,
        Dictionary<string, bool> loggedInUserPermission
    )
    {
        var securityContext = _securityContextProvider.GetSecurityContext();

        var builder = Builders<CirsGenericReport>.Filter;
        var filter =
            builder.Eq(r => r.IsMarkedToDelete, false) &
            builder.Eq(r => r.IsActive, query.IsActive);

        if (!string.IsNullOrEmpty(query.PraxisClientId))
        {
            filter &= builder.Where(r =>
            r.AffectedInvolvedParties != null &&
            r.AffectedInvolvedParties.Any(party => party.PraxisClientId == query.PraxisClientId));
        }

        filter &= builder.Eq(r => r.CirsDashboardName, query.DashboardNameEnum);

        var praxisClient = GetPraxisClientsAsync(query.PraxisClientId).GetAwaiter().GetResult().FirstOrDefault();

        var filterModel = new GetPermissionFilterModel
        {
            LoggedInUserPermission = loggedInUserPermission,
            DashboardName = query.DashboardNameEnum,
            ClientId = query.PraxisClientId,
            ClientOrganizationId = clientOrganizationId,
            IsActive = query.IsActive,
            CirsReportId = query.CirsReportId,
            HaveOfficerPermission = haveOfficerPermission,
            PraxisClient = praxisClient,
            IsACirsAdmin = isACirsAdmin,
            DashboardPermission = dashboardPermission
        };

        var permissionFilter = _cirsPermissionService.GetPermissionFilter(filterModel)
            .GetAwaiter().GetResult();
        filter &= permissionFilter;

        if (!string.IsNullOrWhiteSpace(query.TextSearchKey))
        {
            filter &= builder.Regex("Title", $"/{query.TextSearchKey}/i");
        }

        if (query.CreateDateFilter?.From != null)
        {
            filter &= builder.Gte(r => r.CreateDate, query.CreateDateFilter.From);
        }

        if (query.CreateDateFilter?.To != null)
        {
            filter &= builder.Lte(r => r.CreateDate, query.CreateDateFilter.To);
        }

        return filter;
    }

    private ProjectionDefinition<CirsGenericReport> GetCirsReportQueryProjection()
    {
        return
            Builders<CirsGenericReport>.Projection
                .Include(r => r.ItemId)
                .Include(r => r.CreateDate)
                .Include(r => r.CreatedBy)
                .Include(r => r.Tags)
                .Include(r => r.SequenceNumber)
                .Include(r => r.CirsDashboardName)
                .Include(r => r.Title)
                .Include(r => r.Status)
                .Include(r => r.StatusChangeLog)
                .Include(r => r.KeyWords)
                .Include(r => r.Description)
                .Include(r => r.IsActive)
                .Include(r => r.ClientId)
                .Include(r => r.Remarks)
                .Include(r => r.AttachmentIds)
                .Include(r => r.AffectedInvolvedParties)
                .Include(r => r.ProcessGuideAttachments)
                .Include(r => r.OpenItemAttachments)
                .Include(r => r.LibraryFormResponses)
                .Include(r => r.AttachedDocuments)
                .Include(r => r.AttachedForm)
                .Include(r => r.ExternalReporters)
                .Include(r => r.ResponsibleUsers)
                .Include(r => r.OriginatorInfo)
                .Include(r => r.Rank)
                .Include(r => r.ReportedBy)
                .Include(r => r.MetaData)
                .Include(r => r.RiskManagementAttachments)
                .Include(r => r.EquipmentManagers);
    }

    private void FormatCirsReportSequenceEnumber(List<CirsReportData> cirsReports)
    {
        cirsReports.ForEach(cirsReport =>
        {
            cirsReport.SequenceNumber = cirsReport.Tags[0] == PraxisTag.IsValidCirsReport ? $"#{cirsReport.SequenceNumber}" : $"#{cirsReport.SequenceNumber}(A)";
        });
    }

    private void PopulateCirsBoard(List<CirsReportResponse> cirsBoard, List<CirsReportData> cirsReports)
    {
        var groupedCirsReports = GetGroupedCirsReports(cirsReports);

        cirsBoard.ForEach((c) =>
        {
            var cirsReports = groupedCirsReports.FirstOrDefault(gi => gi.Status == c.Status);
            if (cirsReports != null)
            {
                c.Data = cirsReports.Data;
            }
        });
    }

    private List<CirsReportResponse> GetGroupedCirsReports(List<CirsReportData> cirsReports)
    {
        var groupedCirsReports = cirsReports
            .GroupBy(u => u.Status)
            .Select(grp => new CirsReportResponse
            {
                Status = grp.Key,
                Data = grp.ToList()
            })
            .ToList();

        return groupedCirsReports;
    }
}