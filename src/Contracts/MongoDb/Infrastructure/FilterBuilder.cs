using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.SC.Wopi.Contracts.MongoDb.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Selise.Ecap.SC.Wopi.Contracts.MongoDb.Infrastructure
{
    public static class FilterBuilder
    {
        public static FilterDefinition<BsonDocument> Filters(Dictionary<string, FilterDefinition<BsonDocument>> propertyWiseFilters)
        {
            FilterDefinition<BsonDocument> filterDefinition = null;
            if (propertyWiseFilters != null)
            {
                foreach (KeyValuePair<string, FilterDefinition<BsonDocument>> propertyWiseFilter in propertyWiseFilters)
                {
                    filterDefinition = ((filterDefinition == null) ? propertyWiseFilter.Value : (filterDefinition & propertyWiseFilter.Value));
                }
            }

            return filterDefinition;
        }

        public static Dictionary<string, FilterDefinition<BsonDocument>> PropertyWiseFilters(List<EntityQueryFilter> dataFilters)
        {
            Dictionary<string, FilterDefinition<BsonDocument>> dictionary = new Dictionary<string, FilterDefinition<BsonDocument>>();
            foreach (EntityQueryFilter dataFilter in dataFilters)
            {
                if (dictionary.ContainsKey(dataFilter.PropertyName))
                {
                    throw new Exception("DataFilters embeded same property in different fields");
                }

                string[] array = dataFilter.Values.ToArray();
                string propertyName = dataFilter.PropertyName;
                switch (dataFilter.Operator)
                {
                    case Operators.And:
                        {
                            string propertyName13 = dataFilter.PropertyName;
                            object[] values = array;
                            dictionary.Add(propertyName, AndFilter(propertyName13, values));
                            break;
                        }
                    case Operators.Or:
                        {
                            string propertyName12 = dataFilter.PropertyName;
                            object[] values = array;
                            dictionary.Add(propertyName, OrFilter(propertyName12, values));
                            break;
                        }
                    case Operators.Eql:
                        {
                            string propertyName11 = dataFilter.PropertyName;
                            object[] values = array;
                            dictionary.Add(propertyName, EqlFilter(propertyName11, values));
                            break;
                        }
                    case Operators.Gt:
                        {
                            string propertyName10 = dataFilter.PropertyName;
                            object[] values = array;
                            dictionary.Add(propertyName, GtFilter(propertyName10, values));
                            break;
                        }
                    case Operators.Gte:
                        {
                            string propertyName9 = dataFilter.PropertyName;
                            object[] values = array;
                            dictionary.Add(propertyName, GteFilter(propertyName9, values));
                            break;
                        }
                    case Operators.Lt:
                        {
                            string propertyName8 = dataFilter.PropertyName;
                            object[] values = array;
                            dictionary.Add(propertyName, LtFilter(propertyName8, values));
                            break;
                        }
                    case Operators.Lte:
                        {
                            string propertyName7 = dataFilter.PropertyName;
                            object[] values = array;
                            dictionary.Add(propertyName, LteFilter(propertyName7, values));
                            break;
                        }
                    case Operators.Ne:
                        {
                            string propertyName6 = dataFilter.PropertyName;
                            object[] values = array;
                            dictionary.Add(propertyName, NeFilter(propertyName6, values));
                            break;
                        }
                    case Operators.Inc:
                        {
                            string propertyName5 = dataFilter.PropertyName;
                            object[] values = array;
                            dictionary.Add(propertyName, IncFilter(propertyName5, values));
                            break;
                        }
                    case Operators.In:
                        {
                            string propertyName4 = dataFilter.PropertyName;
                            object[] values = array;
                            dictionary.Add(propertyName, InFilter(propertyName4, values));
                            break;
                        }
                    case Operators.Nin:
                        {
                            string propertyName3 = dataFilter.PropertyName;
                            object[] values = array;
                            dictionary.Add(propertyName, NinFilter(propertyName3, values));
                            break;
                        }
                    case Operators.Regex:
                        {
                            string propertyName2 = dataFilter.PropertyName;
                            object[] values = array;
                            dictionary.Add(propertyName, RegexFilter(propertyName2, values));
                            break;
                        }
                }
            }

            return dictionary;
        }

        public static bool TrySQLParsedFilterBuild(Type type, ParsedSQLQuery query, out string errorMessage, out FilterDefinition<BsonDocument> filter)
        {
            query = PrepareQuery(query);
            Dictionary<string, FilterDefinition<BsonDocument>> dictionary = new Dictionary<string, FilterDefinition<BsonDocument>>();
            errorMessage = null;
            filter = null;
            int num = 0;
            PropertyInfo[] properties = type.GetProperties();
            ParsedSQLFilter[] dataFilters = query.DataFilters;
            foreach (ParsedSQLFilter complexDataFilter in dataFilters)
            {
                PropertyInfo propertyInfo = properties.FirstOrDefault((PropertyInfo x) => x.Name.Equals((complexDataFilter.PropertyName == "_id") ? "ItemId" : complexDataFilter.PropertyName, StringComparison.InvariantCultureIgnoreCase));
                if (propertyInfo == null && complexDataFilter.PropertyName.IndexOf(".") == -1)
                {
                    throw new Exception($"Property = {complexDataFilter.PropertyName} does not exist in entity {type.FullName}");
                }

                object[] values = ExactParse(complexDataFilter.Values.Select((ComplexValue x) => x.Value).ToArray(), (propertyInfo == null) ? null : propertyInfo.PropertyType);
                if (dictionary.ContainsKey(complexDataFilter.PropertyName))
                {
                    throw new Exception("DataFilters embeded same property in different fields");
                }

                string key = $"{complexDataFilter.PropertyName}_{++num}";
                switch (complexDataFilter.Operator)
                {
                    case Operators.And:
                        dictionary.Add(key, AndFilter(complexDataFilter.PropertyName, values));
                        break;
                    case Operators.Or:
                        dictionary.Add(key, OrFilter(complexDataFilter.PropertyName, values));
                        break;
                    case Operators.Eql:
                        dictionary.Add(key, EqlFilter(complexDataFilter.PropertyName, values));
                        break;
                    case Operators.Gt:
                        dictionary.Add(key, GtFilter(complexDataFilter.PropertyName, values));
                        break;
                    case Operators.Gte:
                        dictionary.Add(key, GteFilter(complexDataFilter.PropertyName, values));
                        break;
                    case Operators.Lt:
                        dictionary.Add(key, LtFilter(complexDataFilter.PropertyName, values));
                        break;
                    case Operators.Lte:
                        dictionary.Add(key, LteFilter(complexDataFilter.PropertyName, values));
                        break;
                    case Operators.Ne:
                        dictionary.Add(key, NeFilter(complexDataFilter.PropertyName, values));
                        break;
                    case Operators.Inc:
                        dictionary.Add(key, IncFilter(complexDataFilter.PropertyName, values));
                        break;
                    case Operators.In:
                        dictionary.Add(key, InFilter(complexDataFilter.PropertyName, values));
                        break;
                    case Operators.Nin:
                        dictionary.Add(key, NinFilter(complexDataFilter.PropertyName, values));
                        break;
                    case Operators.Regex:
                        dictionary.Add(key, RegexFilter(complexDataFilter.PropertyName, values));
                        break;
                }
            }

            try
            {
                filter = (string.IsNullOrEmpty(query.Shuffler) ? Salvation(dictionary) : PrisonBreak(dictionary, query.Shuffler));
                if (query.RowLevelSecurityDataFilters != null && query.RowLevelSecurityDataFilters.Length != 0)
                {
                    if (filter != null)
                    {
                        filter &= BuildSecurityFilter(query.RowLevelSecurityDataFilters);
                    }
                    else
                    {
                        filter = BuildSecurityFilter(query.RowLevelSecurityDataFilters);
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Shuffler expression is not valid. Additional info: {ex.Message}";
                filter = null;
                return false;
            }

            return true;
        }

        public static IEnumerable<string> SplitAndKeep(string s, char[] delims)
        {
            int start = 0;
            while (true)
            {
                int num;
                int index = (num = s.IndexOfAny(delims, start));
                if (num == -1)
                {
                    break;
                }

                if (index - start > 0)
                {
                    yield return s.Substring(start, index - start);
                }

                yield return s.Substring(index, 1);
                start = index + 1;
            }

            if (start < s.Length)
            {
                yield return s.Substring(start);
            }
        }

        public static BsonDocument BuildProjectDocument(List<string> fields)
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            if (fields != null)
            {
                foreach (string field in fields)
                {
                    dictionary.Add(field, 1);
                }
            }

            return new BsonDocument(dictionary);
        }

        public static BsonDocument SortBuilder(List<SortObjects> sortBy)
        {
            BsonDocument bsonDocument = new BsonDocument();
            if (sortBy != null)
            {
                foreach (SortObjects item in sortBy)
                {
                    bsonDocument.Add(item.PropName, (item.SortOrder == SortOrder.Ascending) ? 1 : (-1));
                }
            }

            return bsonDocument;
        }

        public static BsonDocument PrepareProjection(List<string> fields)
        {
            BsonDocument bsonDocument = new BsonDocument();
            foreach (string field in fields)
            {
                bsonDocument.Add(new BsonElement(field, 1));
            }

            return bsonDocument;
        }

        private static FilterDefinition<BsonDocument> BuildSecurityFilter(DataFilter[] securityFilters)
        {
            var enumerable = from s in securityFilters
                             group s by s.PropertyName into g
                             select new
                             {
                                 PropertyName = g.Key,
                                 Values = g.Select((DataFilter site) => new { site.Value })
                             };
            List<FilterDefinition<BsonDocument>> list = new List<FilterDefinition<BsonDocument>>();
            foreach (var item in enumerable)
            {
                IEnumerable<string> values = item.Values.Select(x => x.Value);
                list.Add(Builders<BsonDocument>.Filter.In(item.PropertyName, new BsonArray(values)));
            }

            return Builders<BsonDocument>.Filter.Or(list);
        }

        private static FilterDefinition<BsonDocument> PrisonBreak(Dictionary<string, FilterDefinition<BsonDocument>> propertyWiseFilters, string shuffler)
        {
            if (shuffler[0] != '(')
            {
                throw new Exception("Shuffler syntax is invalid, contact server team");
            }

            string[] array = SplitAndKeep(shuffler, new char[4] { '(', ')', '&', '|' }).ToArray();
            int num = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (char.IsLetter(array[i][0]) || array[i][0].Equals('_'))
                {
                    array[i] = $"{array[i]}_{++num}";
                }
            }

            Stack<object> stack = new Stack<object>();
            for (int j = 0; j < array.Length; j++)
            {
                if (array[j] == "(")
                {
                    stack.Push("(");
                }
                else if (array[j] == ")")
                {
                    if (!stack.Any())
                    {
                        throw new Exception("Shuffler syntax is invalid, contact server team");
                    }

                    FilterDefinition<BsonDocument> filterDefinition = null;
                    string text = string.Empty;
                    while (true)
                    {
                        dynamic val = stack.Pop();
                        if (val.GetType() != typeof(string))
                        {
                            filterDefinition = ((!string.IsNullOrEmpty(text)) ? ((!(text == "&")) ? ((FilterDefinition<BsonDocument>)(filterDefinition | val)) : ((FilterDefinition<BsonDocument>)(filterDefinition & val))) : ((FilterDefinition<BsonDocument>)val));
                            continue;
                        }

                        if (val == "(")
                        {
                            break;
                        }

                        if (val == "&" || val == "|")
                        {
                            text = val;
                        }
                    }

                    stack.Push(filterDefinition);
                }
                else if (array[j] == "&" || array[j] == "|")
                {
                    stack.Push(array[j]);
                }
                else
                {
                    FilterDefinition<BsonDocument> item = propertyWiseFilters[array[j]];
                    stack.Push(item);
                }
            }

            if (!stack.Any())
            {
                throw new Exception("Shuffler syntax is invalid, contact server team");
            }

            dynamic val2 = stack.Pop();
            if (val2 is string)
            {
                throw new Exception("Shuffler syntax is invalid, contact server team");
            }

            return val2;
        }

        private static FilterDefinition<BsonDocument> Salvation(Dictionary<string, FilterDefinition<BsonDocument>> propertyWiseFilters)
        {
            FilterDefinition<BsonDocument> filterDefinition = null;
            foreach (KeyValuePair<string, FilterDefinition<BsonDocument>> propertyWiseFilter in propertyWiseFilters)
            {
                filterDefinition = ((filterDefinition == null) ? propertyWiseFilter.Value : (filterDefinition & propertyWiseFilter.Value));
            }

            return filterDefinition;
        }

        private static dynamic[] ExactParse(string[] values, Type propertyType)
        {
            return values.Select((string value) => Parse(value, propertyType)).ToArray();
        }

        private static dynamic Parse(string value, Type dataType)
        {
            int num = value.IndexOf("[") + 1;
            int num2 = value.IndexOf("]") - 1;
            if (num > 0)
            {
                dataType = ((value.IndexOf("INT") != -1) ? typeof(int) : ((value.IndexOf("BOOL") != -1) ? typeof(bool) : ((value.IndexOf("DATE") != -1) ? typeof(DateTime) : ((value.IndexOf("DOUBLE") == -1) ? typeof(string) : typeof(double)))));
                value = value.Substring(num, num2 - num + 1);
            }

            if (dataType.IsArray)
            {
                dataType = dataType.GetElementType();
            }

            if (dataType.IsGenericType && dataType.GetGenericTypeDefinition() == typeof(List<>))
            {
                dataType = dataType.GetGenericArguments().First();
            }

            if (typeof(string) == dataType)
            {
                return value;
            }

            if (typeof(int) == dataType)
            {
                return Convert.ToInt32(value);
            }

            if (typeof(bool) == dataType)
            {
                return Convert.ToBoolean(value);
            }

            if (typeof(DateTime) == dataType)
            {
                return ConvertToDateTime(value);
            }

            if (typeof(double) == dataType)
            {
                return Convert.ToDouble(value);
            }

            if (typeof(Guid) == dataType)
            {
                return value;
            }

            if (dataType.BaseType == typeof(Enum))
            {
                return (int)Enum.Parse(dataType, value, ignoreCase: true);
            }

            return null;
        }

        private static DateTime ConvertToDateTime(string value)
        {
            char c = '.';
            int[] array = new int[7];
            string[] array2 = value.Split(new char[5] { '-', ':', 'T', '.', 'Z' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < array2.Length; i++)
            {
                array[i] = Convert.ToInt32(array2[i]);
            }

            if (value[value.Length - 1] < '0' || value[value.Length - 1] > '9')
            {
                c = value[value.Length - 1];
            }

            DateTime dateTime = new DateTime(array[0], array[1], array[2], array[3], array[4], array[5], array[6]);
            if (c == '.')
            {
                return dateTime;
            }

            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        private static ParsedSQLQuery PrepareQuery(ParsedSQLQuery query)
        {
            ParsedSQLFilter[] dataFilters = query.DataFilters;
            foreach (ParsedSQLFilter parsedSQLFilter in dataFilters)
            {
                if (parsedSQLFilter.PropertyName.Equals("itemId", StringComparison.InvariantCultureIgnoreCase))
                {
                    parsedSQLFilter.PropertyName = "_id";
                }
            }

            query.Shuffler = Regex.Replace(query.Shuffler, "itemid", "_id", RegexOptions.IgnoreCase);
            return query;
        }

        private static void Swap(ref dynamic lhs, ref dynamic rhs)
        {
            object obj = lhs;
            lhs = rhs;
            rhs = obj;
        }

        private static FilterDefinition<BsonDocument> NinFilter(string propertyName, dynamic[] values)
        {
            if (!values.Any())
            {
                throw new Exception("Failed to build & filter");
            }

            List<object> list = new List<object>();
            foreach (dynamic val in values)
            {
                dynamic val2 = val.GetType().IsValueType;
                if (!val2 && val.Equals("$null", StringComparison.InvariantCultureIgnoreCase))
                {
                    list.Add(null);
                }

                if (!val2 && val.Equals("$empty", StringComparison.InvariantCultureIgnoreCase))
                {
                    list.Add(string.Empty);
                }

                list.Add(val);
            }

            return Builders<BsonDocument>.Filter.Nin(propertyName, list);
        }

        private static FilterDefinition<BsonDocument> InFilter(string propertyName, dynamic[] values)
        {
            if (!values.Any())
            {
                throw new Exception("Failed to build & filter");
            }

            List<object> list = new List<object>();
            foreach (dynamic val in values)
            {
                dynamic val2 = val.GetType().IsValueType;
                if (!val2 && val.Equals("$null", StringComparison.InvariantCultureIgnoreCase))
                {
                    list.Add(null);
                }

                if (!val2 && val.Equals("$empty", StringComparison.InvariantCultureIgnoreCase))
                {
                    list.Add(string.Empty);
                }

                list.Add(val);
            }

            return Builders<BsonDocument>.Filter.In(propertyName, list);
        }

        private static FilterDefinition<BsonDocument> IncFilter(string propertyName, dynamic[] values)
        {
            if (values.Length < 2)
            {
                throw new Exception("Not enough argument for inclusion query");
            }

            int num = 0;
            if (values[0] is int)
            {
                num = new Comparer<int>((int x, int y) => x.CompareTo(y)).Compare(values[0], values[1]);
            }
            else if (values[0] is double)
            {
                num = new Comparer<double>((double x, double y) => x.CompareTo(y)).Compare(values[0], values[1]);
            }
            else if (values[0] is long)
            {
                num = new Comparer<long>((long x, long y) => x.CompareTo(y)).Compare(values[0], values[1]);
            }
            else if (values[0] is DateTime)
            {
                num = new Comparer<DateTime>((DateTime x, DateTime y) => x.CompareTo(y)).Compare(values[0], values[1]);
            }

            if (num > 0)
            {
                Swap(ref values[0], ref values[1]);
            }

            return Builders<BsonDocument>.Filter.Gte(propertyName, values[0]) & Builders<BsonDocument>.Filter.Lte(propertyName, values[1]);
        }

        private static FilterDefinition<BsonDocument> NeFilter(string propertyName, dynamic[] values)
        {
            if (!values.Any())
            {
                throw new Exception("Failed to build & filter");
            }

            dynamic val = values[0].GetType().IsValueType;
            if ((!val))
            {
                if (values.Any((dynamic v) => v.Equals("$null", StringComparison.InvariantCultureIgnoreCase)))
                {
                    return Builders<BsonDocument>.Filter.Ne(propertyName, BsonNull.Value);
                }

                if (values.Any((dynamic v) => v.Equals("$empty", StringComparison.InvariantCultureIgnoreCase)))
                {
                    return Builders<BsonDocument>.Filter.Ne(propertyName, string.Empty);
                }
            }

            return Builders<BsonDocument>.Filter.Ne(propertyName, values[0]);
        }

        private static FilterDefinition<BsonDocument> LteFilter(string propertyName, dynamic[] values)
        {
            if (!values.Any())
            {
                throw new Exception("Failed to build & filter");
            }

            return Builders<BsonDocument>.Filter.Lte(propertyName, values[0]);
        }

        private static FilterDefinition<BsonDocument> LtFilter(string propertyName, dynamic[] values)
        {
            if (!values.Any())
            {
                throw new Exception("Failed to build & filter");
            }

            return Builders<BsonDocument>.Filter.Lt(propertyName, values[0]);
        }

        private static FilterDefinition<BsonDocument> GteFilter(string propertyName, dynamic[] values)
        {
            if (!values.Any())
            {
                throw new Exception("Failed to build & filter");
            }

            return Builders<BsonDocument>.Filter.Gte(propertyName, values[0]);
        }

        private static FilterDefinition<BsonDocument> GtFilter(string propertyName, dynamic[] values)
        {
            if (!values.Any())
            {
                throw new Exception("Failed to build & filter");
            }

            return Builders<BsonDocument>.Filter.Gt(propertyName, values[0]);
        }

        private static FilterDefinition<BsonDocument> EqlFilter(string propertyName, dynamic[] values)
        {
            if (!values.Any())
            {
                throw new Exception("Failed to build & filter");
            }

            dynamic val = values[0].GetType().IsValueType;
            if ((!val))
            {
                if (values.Any((dynamic v) => v.Equals("$null", StringComparison.InvariantCultureIgnoreCase)))
                {
                    return Builders<BsonDocument>.Filter.Eq(propertyName, BsonNull.Value);
                }

                if (values.Any((dynamic v) => v.Equals("$empty", StringComparison.InvariantCultureIgnoreCase)))
                {
                    return Builders<BsonDocument>.Filter.Eq(propertyName, string.Empty);
                }
            }

            return Builders<BsonDocument>.Filter.Eq(propertyName, values[0]);
        }

        private static FilterDefinition<BsonDocument> OrFilter(string propertyName, dynamic[] values)
        {
            if (!values.Any())
            {
                throw new Exception("Failed to build & filter");
            }

            FilterDefinition<BsonDocument> filterDefinition = Builders<BsonDocument>.Filter.Eq(propertyName, values[0]);
            for (int i = 1; i < values.Length; i++)
            {
                filterDefinition = (FilterDefinition<BsonDocument>)(filterDefinition | Builders<BsonDocument>.Filter.Eq(propertyName, values[i]));
            }

            return filterDefinition;
        }

        private static FilterDefinition<BsonDocument> AndFilter(string propertyName, dynamic[] values)
        {
            if (!values.Any())
            {
                throw new Exception("Failed to build & filter");
            }

            FilterDefinition<BsonDocument> filterDefinition = Builders<BsonDocument>.Filter.Eq(propertyName, values[0]);
            for (int i = 1; i < values.Length; i++)
            {
                filterDefinition = (FilterDefinition<BsonDocument>)(filterDefinition & Builders<BsonDocument>.Filter.Eq(propertyName, values[i]));
            }

            return filterDefinition;
        }

        private static FilterDefinition<BsonDocument> RegexFilter(string propertyName, dynamic[] values)
        {
            if (!values.Any())
            {
                throw new Exception("Failed to build & filter");
            }

            return Builders<BsonDocument>.Filter.Regex(propertyName, new BsonRegularExpression(values[0]));
        }
    }
}
