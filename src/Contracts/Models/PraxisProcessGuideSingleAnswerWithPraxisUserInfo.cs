using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PraxisProcessGuideSingleAnswerWithPraxisUserInfo : PraxisProcessGuideSingleAnswer
    {
        public PraxisProcessGuideSingleAnswerWithPraxisUserInfo(
            PraxisProcessGuideSingleAnswer singleAnswer, string submittedBy, string clientId
        )
        {
            Files = singleAnswer.Files;
            Remarks = singleAnswer.Remarks;
            ActualBudget = singleAnswer.ActualBudget;
            FileIds = singleAnswer.FileIds;
            QuestionId = singleAnswer.QuestionId;
            SubmittedBy = submittedBy;
            SubmittedOn = singleAnswer.SubmittedOn;
            ClientId = clientId;
            MetaDataList = singleAnswer.MetaDataList;
        }

        public string SubmittedBy { get; set; }
        public string ClientId { get; set; }
    }
}