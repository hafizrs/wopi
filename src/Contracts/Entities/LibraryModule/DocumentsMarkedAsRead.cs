using System;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.PDS.Entity;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;

public class DocumentsMarkedAsRead : EntityBase
{
    public string ReadByUserId { get; set; }
    public string ReadByUserName { get; set; }
    public string ObjectArtifactId { get; set; }
    public DateTime ReadOn { get; set; }
    public string DepartmentId { get; set; }
    public string OrganizationId { get; set; }
}