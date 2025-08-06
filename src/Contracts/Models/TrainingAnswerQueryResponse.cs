using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class TrainingAnswerQueryResponse
    {
        public IEnumerable<string> AnswersPendingBy { get; set; }
        public IEnumerable<string> AnswersSubmittedBy { get; set; }
        public IEnumerable<string> AssignedMembers { get; set; }
        public IEnumerable<string> NotPassedMembers { get; set; }
        public IEnumerable<PraxisTrainingAnswer> TrainingAnswers { get; set; }

        public TrainingAnswerQueryResponse()
        {
            AnswersPendingBy = new List<string>();
            AnswersSubmittedBy = new List<string>();
            AssignedMembers = new List<string>();
            NotPassedMembers = new List<string>();
            TrainingAnswers = new List<PraxisTrainingAnswer>();
        }
    }
}