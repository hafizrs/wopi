using FluentValidation.Resources;
using Microsoft.VisualBasic;
using Selise.Ecap.Entities.PrimaryEntities.SAA;
using Selise.Ecap.Entities.PrimaryEntities.SLPC;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PraxisFormWithPermission : PraxisForm
    {
        public Dictionary<string, bool> Permissions { get; set; }
        public PraxisFormWithPermission(PraxisForm praxisForm)
        {
            MetaDataList = praxisForm.MetaDataList;
            Title = praxisForm.Title;
            Description = praxisForm.Description;
            PurposeOfFormKey = praxisForm.PurposeOfFormKey;
            PurposeOfFormValue = praxisForm.PurposeOfFormValue;
            TopicKey = praxisForm.TopicKey;
            TopicValue = praxisForm.TopicValue;
            Qualification = praxisForm.Qualification;
            Files = praxisForm.Files;
            QuestionsList = praxisForm.QuestionsList;
            ProcessGuideCheckList = praxisForm.ProcessGuideCheckList;
            Clients = praxisForm.Clients;
            ClientInfos = praxisForm.ClientInfos;
            ClientId = praxisForm.ClientId;
            OrganizationId = praxisForm.OrganizationId;
            OrganizationIds = praxisForm.OrganizationIds;
            QuestionedBy = praxisForm.QuestionedBy;
            AnsweredBy = praxisForm.AnsweredBy;
            IsATemplate = praxisForm.IsATemplate;
            AdditionalDescription = praxisForm.AdditionalDescription;
            IsCreatedByAdmin = praxisForm.IsCreatedByAdmin;
            ItemId = praxisForm.ItemId;
            AssignVirtualMembers(praxisForm);
        }

        private void AssignVirtualMembers(PraxisForm praxisForm)
        {
            CreateDate = praxisForm.CreateDate;
            CreatedBy = praxisForm.CreatedBy;
            Language = praxisForm.Language;
            LastUpdateDate = praxisForm.LastUpdateDate;
            LastUpdatedBy = praxisForm.LastUpdatedBy;
            TenantId = praxisForm.TenantId;
        }
    }
}
