using System.Collections.Generic;

namespace Selise.Ecap.SC.Wopi.Contracts.MongoDb.Infrastructure
{
    public static class EcapSqlRules
    {
        public class SpecialOperators
        {
            public const string Count = "__count";
        }

        public const string Select = "select";

        public const string From = "from";

        public const string Where = "where";

        public const string OrderBy = "orderby";

        public const string PageNumber = "pagenumber";

        public const string PageSize = "pagesize";

        public const string Skip = "skip";

        public const string WildCard = "*";

        public const string EmptyParameter = "<>";

        public static readonly string[] KeyWords = new string[7] { "select", "from", "where", "orderby", "pagenumber", "pagesize", "skip" };

        public static string[] Operators = new string[2] { "__and", "__or" };

        public static string[] OperatorsExt = new string[2] { "__and(", "__or(" };

        public static string[] OperatorSymbols = new string[2] { "&", "|" };

        public static string[] OperatorSymbolExts = new string[2] { "&(", "|(" };

        public static readonly string[] DatafilterParsingTokens = new string[3] { "=", "&", "|" };

        public static readonly string[] DatafilterPropertyParsingTokensStrings = new string[5] { "(", ")", ",", "<", ">" };

        public static readonly string[] DatafilterValueParsingTokens = new string[5] { "(", ")", ",", "<", ">" };

        public static readonly string[] PageArgumentsTokens = new string[4] { "=", " ", "<", ">" };

        public static readonly string[] OrderByTokens = new string[3] { "<", ">", " " };

        public static readonly char[] EntityNameParseTokens = new char[4] { '<', '>', ',', ' ' };

        public static readonly string[] FieldsParseTokens = new string[8] { "<", ">", ",", " ", "__count", "*", "(", ")" };

        public static readonly List<string> ShufflerOperators = new List<string> { "&", "|", "(", ")" };

        public static readonly List<string> OpertorsList = new List<string>
        {
            "__or", "__eql", "__gt", "__gte", "__lt", "__lte", "__ne", "__inc", "__in", "__nin",
            "__reg"
        };
    }
}
