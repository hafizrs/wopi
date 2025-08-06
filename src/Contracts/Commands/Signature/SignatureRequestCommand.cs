using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.Signature
{
    public class SignatureRequestCommand
    {
        public string TrackingId { get; set; }
        public string Title { get; set; }
        public bool ReceiveRolloutEmail { get; set; }
        public int SignatureClass { get; set; }
        public List<string> FileIds { get; set; }
        public List<AddSignatoryCommand> AddSignatoryCommands { get; set; }
    }

    public class AddSignatoryCommand
    {
        public string Email { get; set; }
        public int ContractRole { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
