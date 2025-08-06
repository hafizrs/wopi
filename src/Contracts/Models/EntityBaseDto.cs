using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.Wopi.Contracts.Models
{
    public class EntityBaseDto
    {
        public string ItemId { get; set; }
        public string Language { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreateDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public IEnumerable<string> Tags { get; set; }
    }
}
