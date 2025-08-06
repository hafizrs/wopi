using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule
{
    public class CreateAbsencePlanCommand
    {
        public string AffectedUserId { get; set; }
        public string AbsenceTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Remarks { get; set; }
        public List<object> Attachments { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
    }
}