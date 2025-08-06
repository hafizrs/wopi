using SeliseBlocks.Genesis.Framework.PDS.Entity;

namespace Selise.Ecap.SC.Wopi.Contracts.Models
{
    public class TagValidator: EntityBase
    {
        public string TagName {get; set;}
        public string ValidatorType {get; set;}
        public bool ValidationRequire {get; set;}
        public string AppliesToEntity {get; set;}
    }
}