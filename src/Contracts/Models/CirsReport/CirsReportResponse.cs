using MongoDB.Bson.Serialization.Attributes;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models.CirsReport;

public class CirsReportResponse
{
    public string Status { get; set; }
    public List<CirsReportData> Data { get; set; }
}

public class CirsReportData
{
    [BsonId]
    public string ItemId { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreatedBy { get; set; }
    public string[] Tags { get; set; }
    public string SequenceNumber { get; set; }
    public CirsDashboardName CirsDashboardName { get; set; }
    public string Title { get; set; }
    public string Status { get; set; }
    public List<StatusChangeEvent> StatusChangeLog { get; set; }
    public IEnumerable<string> KeyWords { get; set; }
    public string Description { get; set; }
    public string Remarks { get; set; }
    public string ClientId { get; set; }
    public string ClientName { get; set; }
    public bool IsActive { get; set; }
    public List<FileResponse> Attachments { get; set; }
    public string NextCirsReportId { get; set; }
    public PraxisUser CreatorUser { get; set; }
    public List<PraxisUser> ResponsibleUsers { get; set; }
    public IEnumerable<InvolvementResponse> AffectedInvolvedData { get; set; }
    public Dictionary<string, object> MetaData { get; set; }
    public string ReporterClientName { get; set; }
    public List<ProcessGuideAttachment> ProcessGuideAttachments { get; set; }
    public List<PraxisLibraryFormResponse> LibraryFormResponses { get; set; }
    public List<ReportingAttachmentFile> AttachedDocuments { get; set; }
    public ReportingAttachmentFile AttachedForm { get; set; }
    public PraxisUser ReportedBy { get; set; }
    public List<RiskManagementAttachment> RiskManagementAttachments { get; set; }

    // helper properties
    [JsonIgnore]
    public IEnumerable<AffectedInvolvedParty> AffectedInvolvedParties { get; set; }
    [JsonIgnore]
    public IEnumerable<string> AttachmentIds { get; set; }
    public List<ExternalReporter> ExternalReporters { get; set; }
    public List<OpenItemAttachment> OpenItemAttachments { get; set; }
    public OriginatorInfo OriginatorInfo { get; set; }
    public ulong Rank { get; set; }
    public bool IsSentAnonymously { get; set; }
    public Dictionary<string, bool> Permissions { get; set; }
    public List<string> EquipmentManagers { get; set; }
    public List<string> RolesDisallowedToRead { get; set; }
}

public class FileResponse
{
    public string FileStorageId { get; set; }

    public string Name { get; set; }

    public string Url { get; set; }
    public string DocumentId { get; set; }
}

public class InvolvementResponse
{
    public string PraxisClientId { get; set; }

    public string PraxisClientName { get; set; }

    public PraxisUser[] InvolvedUsers { get; set; } = null;
    public PraxisKeyValue InvolvedParty { get; set; } = null;

    public DateTime InvolvedAt { get; set; }
}

public class CirsReportEvent
{
    public string ReportId { get; set; }
    public bool IsColumnChanged { get; set; }
    public bool IsUpdate { get; set; }
}