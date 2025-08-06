using System.Text;
using System.Web;
using HtmlAgilityPack;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public class HtmlToTextAgilityPackage
    {
        public static string ExtractStyledText(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var sb = new StringBuilder();

            foreach (var node in doc.DocumentNode.DescendantsAndSelf())
            {
                if (node.NodeType == HtmlNodeType.Text)
                {
                    sb.Append(HttpUtility.HtmlDecode(node.InnerText));
                }
                else if (node.NodeType == HtmlNodeType.Element)
                {
                    // Extract styles from attributes (e.g., color, font-style)
                    string style = node.GetAttributeValue("style", null);
                    if (!string.IsNullOrEmpty(style))
                    {
                        sb.Append($" [{style}]");
                    }
                }
            }

            return sb.ToString();
        }
    }
}