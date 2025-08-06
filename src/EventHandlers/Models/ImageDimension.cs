using System.Collections.Generic;

namespace EventHandlers.Models
{
    public class ImageDimension
    {
        protected ImageDimension() { }
        public static Dictionary<string, Dimension> Dimensions { get; set; } = new Dictionary<string, Dimension>()
        {
            { "Resize-Image-1024-1024", new Dimension { Width = 1024, Height = 1024 } }

        };
    }
}
