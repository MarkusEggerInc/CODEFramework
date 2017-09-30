using System.Collections.Generic;

namespace CODE.Framework.Core.Newtonsoft.Linq.JsonPath
{
    internal class QueryFilter : PathFilter
    {
        public QueryExpression Expression { get; set; }

        public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, bool errorWhenNoMatch)
        {
            foreach (var t in current)
            foreach (var v in t)
                if (Expression.IsMatch(root, v))
                    yield return v;
        }
    }
}