using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PraxisProcessGuideWithClientCompletion: PraxisProcessGuide
    {
        public IEnumerable<ProcessGuideClientInfo> ClientCompletion { get; set; }
        public IEnumerable<PraxisProcessGuideMinialInfo> StandardCloneGuides { get; set; }
        public PraxisProcessGuideWithClientCompletion(PraxisProcessGuide processGuide)
        {
            Budget = processGuide.Budget;
            ClientId = processGuide.ClientId;
            ClientName = processGuide.ClientName;
            Clients = processGuide.Clients;
            ControlledMembers = processGuide.ControlledMembers;
            Description = processGuide.Description;
            DueDate = processGuide.DueDate;
            FormId = processGuide.FormId;
            FormName = processGuide.FormName;
            IsActive = processGuide.IsActive;
            IsMarkedToDelete = processGuide.IsMarkedToDelete;
            ItemId = processGuide.ItemId;
            PatientDateOfBirth = processGuide.PatientDateOfBirth;
            PatientId = processGuide.PatientId;
            PatientName = processGuide.PatientName;
            Tags = processGuide.Tags;
            Title = processGuide.Title;
            TopicKey = processGuide.TopicKey;
            TopicValue = processGuide.TopicValue;
            CompletionStatus = processGuide.CompletionStatus;
            CompletionDate = processGuide.CompletionDate;
            ClientCompletionInfo = processGuide.ClientCompletionInfo;
            PraxisProcessGuideConfigId = processGuide.PraxisProcessGuideConfigId;
            TaskSchedule = processGuide.TaskSchedule;
            Shifts = processGuide.Shifts;
            RelatedEntityId = processGuide.RelatedEntityId;
            RelatedEntityName = processGuide.RelatedEntityName;
            IsATemplate = processGuide.IsATemplate;
            IsAClonedProcessGuide = processGuide.IsAClonedProcessGuide;
            OrganizationId = processGuide.OrganizationId;
            AssignVirtualMembers(processGuide);
            
        }

        private void AssignVirtualMembers(PraxisProcessGuide processGuide)
        {
            CreateDate = processGuide.CreateDate;
            CreatedBy = processGuide.CreatedBy;
            Language = processGuide.Language;
            LastUpdateDate = processGuide.LastUpdateDate;
            LastUpdatedBy = processGuide.LastUpdatedBy;
            TenantId = processGuide.TenantId;
            RolesAllowedToRead = processGuide.RolesAllowedToRead; 
            IdsAllowedToRead = processGuide.IdsAllowedToRead; 
            RolesAllowedToWrite = processGuide.RolesAllowedToWrite; 
            IdsAllowedToWrite = processGuide.IdsAllowedToWrite; 
            RolesAllowedToUpdate = processGuide.RolesAllowedToUpdate; 
            IdsAllowedToUpdate = processGuide.IdsAllowedToUpdate; 
            RolesAllowedToDelete = processGuide.RolesAllowedToDelete; 
            IdsAllowedToDelete = processGuide.IdsAllowedToDelete; 
        }
    }



    public class PraxisProcessGuideMinialInfo: PraxisProcessGuide
    {
        public IEnumerable<ProcessGuideClientInfo> ClientCompletion { get; set; }
        public PraxisProcessGuideMinialInfo(PraxisProcessGuide processGuide)
        {
            Budget = processGuide.Budget;
            ClientId = processGuide.ClientId;
            ClientName = processGuide.ClientName;
            Clients = processGuide.Clients;
            ControlledMembers = processGuide.ControlledMembers;
            Description = processGuide.Description;
            DueDate = processGuide.DueDate;
            FormId = processGuide.FormId;
            FormName = processGuide.FormName;
            IsActive = processGuide.IsActive;
            IsMarkedToDelete = processGuide.IsMarkedToDelete;
            ItemId = processGuide.ItemId;
            PatientDateOfBirth = processGuide.PatientDateOfBirth;
            PatientId = processGuide.PatientId;
            PatientName = processGuide.PatientName;
            Title = processGuide.Title;
            TopicKey = processGuide.TopicKey;
            TopicValue = processGuide.TopicValue;
            CompletionStatus = processGuide.CompletionStatus;
            CompletionDate = processGuide.CompletionDate;
            OrganizationId = processGuide.OrganizationId;
            ControlledMembers = processGuide.ControlledMembers;
            CreateDate = processGuide.CreateDate;
            CreatedBy = processGuide.CreatedBy;
            LastUpdateDate = processGuide.LastUpdateDate;
            TaskSchedule = processGuide.TaskSchedule;
            IsAClonedProcessGuide = processGuide.IsAClonedProcessGuide;
        }

    }
}



