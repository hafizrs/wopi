using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Helpers
{
    public class GetFilteredSQLQuery
    {
        public static ParsedSQLQuery GetParsedSQLQuery<T>(string command)
        {
            int num = 0;
            ParsedSQLQuery parsedSQLQuery = new ParsedSQLQuery();
            string[] keyWords = EcapSqlRules.KeyWords;
            string[] array = keyWords;
            foreach (string text in array)
            {
                command = Regex.Replace(command, text, text, RegexOptions.IgnoreCase);
            }

            string[] array2 = command.Split(keyWords, StringSplitOptions.RemoveEmptyEntries);
            bool onlyCount = false;
            parsedSQLQuery.Fields = ParseFields(array2[num++], ref onlyCount);
            parsedSQLQuery.EntityName = ParseEntityName(array2[num++]);
            parsedSQLQuery.OnlyCount = onlyCount;
            string shuffler = string.Empty;
            if (command.IndexOf("where") != -1)
            {
                parsedSQLQuery.DataFilters = ParseDataFilters(array2[num++], ref shuffler, GetEntityType<T>());
            }
            else
            {
                parsedSQLQuery.DataFilters = new ParsedSQLFilter[0];
            }

            while (shuffler.IndexOf("()", StringComparison.Ordinal) != -1)
            {
                shuffler = shuffler.Replace("()", string.Empty);
            }

            shuffler = (parsedSQLQuery.Shuffler = "(" + shuffler + ")");
            parsedSQLQuery.SortBy = ParseOrderBy((command.IndexOf("orderby") != -1) ? array2[num++] : string.Empty);
            parsedSQLQuery.PageNumber = ((command.IndexOf("pagenumber") != -1) ? ParsePageArgs(array2[num++]) : new int?(0));
            parsedSQLQuery.PageLimit = ((command.IndexOf("pagesize") != -1) ? ParsePageArgs(array2[num++]) : new int?(int.MaxValue));
            parsedSQLQuery.Skip = ((command.IndexOf("skip") != -1) ? ParsePageArgs(array2[num++]) : new int?(0));
            return parsedSQLQuery;
        }

        private static string OperatorRefactor(string command)
        {
            string[] operators = EcapSqlRules.Operators;
            string[] operatorsExt = EcapSqlRules.OperatorsExt;
            string[] operatorSymbols = EcapSqlRules.OperatorSymbols;
            string[] operatorSymbolExts = EcapSqlRules.OperatorSymbolExts;
            for (int i = 0; i < operators.Length; i++)
            {
                command = command.Replace(operators[i], operatorSymbols[i]);
            }

            for (int j = 0; j < operators.Length; j++)
            {
                command = command.Replace(operatorSymbolExts[j], operatorsExt[j]);
            }

            return command;
        }

        private static string RemoveUnnecessarySpace(string s)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (IsARegexFunc(s.Substring(i, Math.Min(5, s.Length - i))))
                {
                    for (; i < s.Length; i++)
                    {
                        if (s[i] == ')')
                        {
                            stringBuilder.Append(s[i]);
                            break;
                        }

                        stringBuilder.Append(s[i]);
                    }
                }
                else if (s[i] != ' ')
                {
                    stringBuilder.Append(s[i]);
                }
            }

            return stringBuilder.ToString();
        }

        private static bool IsARegexFunc(string arg)
        {
            return !string.IsNullOrWhiteSpace(arg) && arg.Equals("__reg");
        }

        private static ParsedSQLFilter[] ParseDataFilters(string s, ref string shuffler, Type entity = null)
        {
            string text = s;
            s = OperatorRefactor(s);
            if (s.IndexOf("*", StringComparison.Ordinal) != -1 || s.IndexOf("<>", StringComparison.Ordinal) != -1)
            {
                shuffler = string.Empty;
                return new List<ParsedSQLFilter>().ToArray();
            }

            List<ParsedSQLFilter> list = new List<ParsedSQLFilter>();
            string[] array = s.Split(EcapSqlRules.DatafilterParsingTokens, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> dic = new Dictionary<string, string>();
            SortedDictionary<string, string> sortedDictionary = new SortedDictionary<string, string>();
            for (int i = 0; i < array.Length; i += 2)
            {
                string[] array2 = (from t in array[i + 1].Trim().Split(EcapSqlRules.DatafilterValueParsingTokens, StringSplitOptions.RemoveEmptyEntries)
                                   select t.Trim()).ToArray();
                string text2 = array[i].Trim().Split(EcapSqlRules.DatafilterPropertyParsingTokensStrings, StringSplitOptions.RemoveEmptyEntries)[0];
                text2 = text2.Replace(" ", string.Empty);
                ParsedSQLFilter parsedSQLFilter = new ParsedSQLFilter
                {
                    PropertyName = text2,
                    Values = ParseValues(array2),
                    Operator = SelectOperator(array2[0])
                };
                if (parsedSQLFilter.Operator == Operators.Regex)
                {
                    string queryableTextField = GetQueryableTextField(entity, parsedSQLFilter.PropertyName);
                    sortedDictionary.Add(parsedSQLFilter.PropertyName, queryableTextField);
                    parsedSQLFilter.PropertyName = queryableTextField;
                }

                list.Add(parsedSQLFilter);
            }

            shuffler = BuildShuffler(s, dic);
            shuffler = shuffler.Replace(" ", string.Empty);
            shuffler = sortedDictionary.Aggregate(shuffler, (string current, KeyValuePair<string, string> oldProp) => current.Replace(oldProp.Key, oldProp.Value));
            return list.ToArray();
        }

        private static string GetQueryableTextField(Type entity, string propertyName)
        {
            return propertyName;
        }

        private static string BuildShuffler(string s, Dictionary<string, string> dic)
        {
            foreach (string opertors in EcapSqlRules.OpertorsList)
            {
                while (true)
                {
                    int num = s.IndexOf(opertors + "(", StringComparison.Ordinal);
                    if (num == -1)
                    {
                        break;
                    }

                    int num2 = s.IndexOf(")", num);
                    if (num2 == -1)
                    {
                        throw new Exception("Shuffler expression is not valid");
                    }

                    s = s.Remove(num, num2 - num + 1);
                }
            }

            s = s.Replace("=", string.Empty);
            s = s.Replace("<", string.Empty);
            s = s.Replace(">", string.Empty);
            return s;
        }

        private static ComplexValue[] ParseValues(string[] sideTokens)
        {
            Operators operators = SelectOperator(sideTokens[0]);
            if (operators == Operators.In)
            {
                List<ComplexValue> list = new List<ComplexValue>();
                for (int i = ((sideTokens.Length > 1) ? 1 : 0); i < sideTokens.Length; i++)
                {
                    if (sideTokens[i].Equals(" ") || string.IsNullOrWhiteSpace(sideTokens[i]) || string.IsNullOrEmpty(sideTokens[i]))
                    {
                        continue;
                    }

                    if (sideTokens[i].StartsWith("'"))
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        bool flag = false;
                        for (; i < sideTokens.Length; i++)
                        {
                            stringBuilder.AppendFormat("{0}, ", sideTokens[i].Trim(new char[1] { '\'' }));
                            flag = sideTokens[i].EndsWith("'");
                            if (flag)
                            {
                                break;
                            }
                        }

                        if (!flag)
                        {
                            throw new Exception("Quoted value did not ended properly");
                        }

                        list.Add(new ComplexValue
                        {
                            Value = stringBuilder.ToString().TrimEnd(Array.Empty<char>()).TrimEnd(new char[1] { ',' })
                                .Replace("~", "(")
                                .Replace("^", ")"),
                            FilterType = FilterType.Simple
                        });
                    }
                    else
                    {
                        list.Add(new ComplexValue
                        {
                            Value = sideTokens[i].Replace("~", "(").Replace("^", ")"),
                            FilterType = FilterType.Simple
                        });
                    }
                }

                return list.ToArray();
            }

            List<ComplexValue> list2 = new List<ComplexValue>();
            for (int j = ((sideTokens.Length > 1) ? 1 : 0); j < sideTokens.Length; j++)
            {
                if (!sideTokens[j].Equals(" ") && !string.IsNullOrWhiteSpace(sideTokens[j]) && !string.IsNullOrEmpty(sideTokens[j]))
                {
                    if (sideTokens[j][0] == '{')
                    {
                        throw new Exception("Parsing failed due to developer's stupidity");
                    }

                    list2.Add(new ComplexValue
                    {
                        Value = sideTokens[j],
                        FilterType = FilterType.Simple
                    });
                }
            }

            return list2.ToArray();
        }

        private static Operators SelectOperator(string sideToken)
        {
            sideToken = sideToken.ToLower();
            sideToken = sideToken.Replace(" ", string.Empty);
            return sideToken switch
            {
                "__and" => Operators.And,
                "__or" => Operators.Or,
                "__eql" => Operators.Eql,
                "__gt" => Operators.Gt,
                "__gte" => Operators.Gte,
                "__lt" => Operators.Lt,
                "__lte" => Operators.Lte,
                "__ne" => Operators.Ne,
                "__inc" => Operators.Inc,
                "__in" => Operators.In,
                "__nin" => Operators.Nin,
                "__reg" => Operators.Regex,
                _ => throw new Exception("Invalid Operator"),
            };
        }

        private static int? ParsePageArgs(string s)
        {
            string[] array = s.Split(EcapSqlRules.PageArgumentsTokens, StringSplitOptions.RemoveEmptyEntries);
            if (array.Length == 0 || !int.TryParse(array[0], out var result))
            {
                throw new Exception("Invalid PagneNumber Argument");
            }

            return result;
        }

        private static SortObjects[] ParseOrderBy(string s)
        {
            List<SortObjects> list = new List<SortObjects>();
            string[] array = s.Split(EcapSqlRules.OrderByTokens, StringSplitOptions.RemoveEmptyEntries);
            if ((array.Length & 1) == 1)
            {
                throw new Exception("Invalid Sort Order Argument");
            }

            for (int i = 0; i < array.Length; i += 2)
            {
                list.Add(new SortObjects
                {
                    PropName = array[i],
                    SortOrder = ((!array[i + 1].Equals("__asc")) ? SortOrder.Descending : SortOrder.Ascending)
                });
            }

            return list.ToArray();
        }

        private static string ParseEntityName(string s)
        {
            string[] array = s.Split(EcapSqlRules.EntityNameParseTokens, StringSplitOptions.RemoveEmptyEntries);
            if (!array.Any())
            {
                throw new Exception("EntityName is required!");
            }

            return array[0];
        }

        private static string[] ParseFields(string s, ref bool onlyCount)
        {
            onlyCount = s.IndexOf("__count", StringComparison.Ordinal) != -1;
            return s.Split(EcapSqlRules.FieldsParseTokens, StringSplitOptions.RemoveEmptyEntries);
        }

        public static Type GetEntityType<T>()
        {
            return typeof(T);
        }
    }
}
