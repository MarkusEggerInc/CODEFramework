﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CODE.Framework.Core.Newtonsoft.Utilities;

namespace CODE.Framework.Core.Newtonsoft.Linq.JsonPath
{
    internal enum QueryOperator
    {
        None = 0,
        Equals = 1,
        NotEquals = 2,
        Exists = 3,
        LessThan = 4,
        LessThanOrEquals = 5,
        GreaterThan = 6,
        GreaterThanOrEquals = 7,
        And = 8,
        Or = 9
    }

    internal abstract class QueryExpression
    {
        public QueryOperator Operator { get; set; }

        public abstract bool IsMatch(JToken root, JToken t);
    }

    internal class CompositeExpression : QueryExpression
    {
        public CompositeExpression()
        {
            Expressions = new List<QueryExpression>();
        }

        public List<QueryExpression> Expressions { get; set; }

        public override bool IsMatch(JToken root, JToken t)
        {
            switch (Operator)
            {
                case QueryOperator.And:
                    foreach (var e in Expressions)
                        if (!e.IsMatch(root, t))
                            return false;
                    return true;
                case QueryOperator.Or:
                    foreach (var e in Expressions)
                        if (e.IsMatch(root, t))
                            return true;
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal class BooleanQueryExpression : QueryExpression
    {
        public object Left { get; set; }
        public object Right { get; set; }

        private IEnumerable<JToken> GetResult(JToken root, JToken t, object o)
        {
            var resultToken = o as JToken;
            if (resultToken != null)
                return new[] {resultToken};

            var pathFilters = o as List<PathFilter>;
            if (pathFilters != null)
                return JPath.Evaluate(pathFilters, root, t, false);

            return CollectionUtils.ArrayEmpty<JToken>();
        }

        public override bool IsMatch(JToken root, JToken t)
        {
            if (Operator == QueryOperator.Exists)
                return GetResult(root, t, Left).Any();

            using (var leftResults = GetResult(root, t, Left).GetEnumerator())
            {
                if (leftResults.MoveNext())
                {
                    var rightResultsEn = GetResult(root, t, Right);
                    var rightResults = rightResultsEn as ICollection<JToken> ?? rightResultsEn.ToList();

                    do
                    {
                        var leftResult = leftResults.Current;
                        foreach (var rightResult in rightResults)
                            if (MatchTokens(leftResult, rightResult))
                                return true;
                    } while (leftResults.MoveNext());
                }
            }

            return false;
        }

        private bool MatchTokens(JToken leftResult, JToken rightResult)
        {
            var leftValue = leftResult as JValue;
            var rightValue = rightResult as JValue;

            if (leftValue != null && rightValue != null)
                switch (Operator)
                {
                    case QueryOperator.Equals:
                        if (EqualsWithStringCoercion(leftValue, rightValue))
                            return true;
                        break;
                    case QueryOperator.NotEquals:
                        if (!EqualsWithStringCoercion(leftValue, rightValue))
                            return true;
                        break;
                    case QueryOperator.GreaterThan:
                        if (leftValue.CompareTo(rightValue) > 0)
                            return true;
                        break;
                    case QueryOperator.GreaterThanOrEquals:
                        if (leftValue.CompareTo(rightValue) >= 0)
                            return true;
                        break;
                    case QueryOperator.LessThan:
                        if (leftValue.CompareTo(rightValue) < 0)
                            return true;
                        break;
                    case QueryOperator.LessThanOrEquals:
                        if (leftValue.CompareTo(rightValue) <= 0)
                            return true;
                        break;
                    case QueryOperator.Exists:
                        return true;
                }
            else
                switch (Operator)
                {
                    case QueryOperator.Exists:
                    // you can only specify primitive types in a comparison
                    // notequals will always be true
                    case QueryOperator.NotEquals:
                        return true;
                }

            return false;
        }

        private bool EqualsWithStringCoercion(JValue value, JValue queryValue)
        {
            if (value.Equals(queryValue))
                return true;

            if (queryValue.Type != JTokenType.String)
                return false;

            var queryValueString = (string) queryValue.Value;

            string currentValueString;

            // potential performance issue with converting every value to string?
            switch (value.Type)
            {
                case JTokenType.Date:
                    using (var writer = StringUtils.CreateStringWriter(64))
                    {
                        if (value.Value is DateTimeOffset)
                            DateTimeUtils.WriteDateTimeOffsetString(writer, (DateTimeOffset) value.Value, DateFormatHandling.IsoDateFormat, null, CultureInfo.InvariantCulture);
                        else
                            DateTimeUtils.WriteDateTimeString(writer, (DateTime) value.Value, DateFormatHandling.IsoDateFormat, null, CultureInfo.InvariantCulture);

                        currentValueString = writer.ToString();
                    }
                    break;
                case JTokenType.Bytes:
                    currentValueString = Convert.ToBase64String((byte[]) value.Value);
                    break;
                case JTokenType.Guid:
                case JTokenType.TimeSpan:
                    currentValueString = value.Value.ToString();
                    break;
                case JTokenType.Uri:
                    currentValueString = ((Uri) value.Value).OriginalString;
                    break;
                default:
                    return false;
            }

            return string.Equals(currentValueString, queryValueString, StringComparison.Ordinal);
        }
    }
}