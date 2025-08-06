using MongoDB.Bson.Serialization.Attributes;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

#nullable enable
namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;

[BsonIgnoreExtraElements]
public class CirsGenericReport : EntityBase
{
    public string OrganizationId { get; set; } = null!;
    public string SequenceNumber { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Status { get; set; } = null!;
    public List<StatusChangeEvent> StatusChangeLog { get; set; } = new List<StatusChangeEvent>();
    public IEnumerable<string> KeyWords { get; set; } = new List<string>();
    public string? Description { get; set; }
    public string? Remarks { get; set; }
    public string ClientId { get; set; } = null!;
    public IEnumerable<string>? AttachmentIds { get; set; }
    public bool IsActive { get; set; }
    public string? NextCirsReportId { get; set; }
    public CirsDashboardName CirsDashboardName { get; set; }
    public IEnumerable<AffectedInvolvedParty>? AffectedInvolvedParties { get; set; }
    public IEnumerable<InvolvedUser>? ResponsibleUsers { get; set; }
    public Dictionary<string, object?> MetaData { get; set; } = new();
    public List<ProcessGuideAttachment>? ProcessGuideAttachments { get; set; }
    public List<ReportingAttachmentFile>? AttachedDocuments { get; set; }
    public ReportingAttachmentFile? AttachedForm { get; set; }
    public List<OpenItemAttachment>? OpenItemAttachments { get; set; }
    public List<PraxisLibraryFormResponse>? LibraryFormResponses { get; set; }
    public List<ExternalReporter>? ExternalReporters { get; set; }
    public OriginatorInfo? OriginatorInfo { get; set; }
    public ulong Rank { get; set; }
    public InvolvedUser? ReportedBy { get; set; }
    public List<CirsEditHistory>? CirsEditHistory { get; set; }

    [BsonIgnore]
    [JsonIgnore]
    public DateTime DueDate => CreateDate.AddDays(14);
    public List<RiskManagementAttachment>? RiskManagementAttachments { get; set; }
    public List<string> EquipmentManagers { get; set; } = new List<string>();
    public List<string> RolesDisallowedToRead { get; set; } = new List<string>();
}

public class CirsEditHistory
{
    public string? PropertyName { get; set; }
    public List<CirsActivityPerformerModel>? CirsActivityPerformerModel { get; set; }
}

public class CirsActivityPerformerModel
{
    public string? CurrentResponse { get; set; }
    public string? PerformedBy { get; set; }
    public string? PerformedOn { get; set; }
}

public class ExternalReporter
{
    public MinimalSupplierInfo SupplierInfo { get; set; }
    public string Remarks { get; set; }
}

public class MinimalSupplierInfo
{
    public string SupplierId { get; set; }
    public string SupplierName { get; set; }
    public string SupplierEmail { get; set; }
    public string ExternalUserId { get; set; }
}

public class ProcessGuideAttachment
{
    public string? FormId { get; set; }

    public string? ProcessGuideId { get; set; }

    public string? ProcessGuideTitle { get; set; }

    public string? ProcessGuideDescription { get; set; }
    public int CompletionStatus { get; set; }
    public InvolvedUser? AssignedBy { get; set; }
    public List<InvolvedUser>? AssignedUsers { get; set; }
    public List<string>? AssignedGroup { get; set; }
    public DateTime? DueDate { get; set; }
}

public class AffectedInvolvedParty
{
    public string PraxisClientId { get; set; } = null!;
    public InvolvedUser[]? InvolvedUsers { get; set; } = null;
    public PraxisKeyValue? InvolvedParty { get; set; } = null;
    public DateTime InvolvedAt { get; set; }
}

public class InvolvedUser
{
    public string PraxisUserId { get; set; } = null;
    public string UserId { get; set; } = null;
    public string DisplayName { get; set; }
    public PraxisImage? Image { get; set; }
    public string Email { get; set; }
}

public class OpenItemAttachment
{
    public string? OpenItemId { get; set; }
    public string? OpenItemName { get; set; }
    public string? CompletionStatus { get; set; }
    public InvolvedUser? AssignedBy { get; set; }
    public List<InvolvedUser>? AssignedUsers { get; set; }
    public List<string>? AssignedGroup { get; set; }
    public DateTime? DueDate { get; set; }
}
public class OriginatorInfo
{
    public PraxisAddress? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? SourceType { get; set; }
    public string? Name { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Company { get; set; }
    public bool? StayAnonymous { get; set; }
    public bool? IncludeContactDetails { get; set; }
}

public class RiskManagementAttachment
{
    public string? RiskItemId { get; set; }
    public string? RiskName { get; set; }
    public bool? IsResolved { get; set; }
    public List<InvolvedUser>? RiskOwners { get; set; }
    public List<InvolvedUser>? RiskProfessionals { get; set; }
}

public class ReportingInfo
{
    public string? ReportingId { get; set; }
    public DateTime? AssignedOn { get; set; }
}