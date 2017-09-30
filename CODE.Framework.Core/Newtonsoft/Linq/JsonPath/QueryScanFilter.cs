using System.Collections.Generic;

namespace CODE.Framework.Core.Newtonsoft.Linq.JsonPath
{
    internal class QueryScanFilter : PathFilter
    {
        public QueryExpression Expression { get; set; }

        public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, bool errorWhenNoMatch)
        {
            foreach (var t in current)
            {
                var c = t as JContainer;
                if (c != null)
                {
                    foreach (var d in c.DescendantsAndSelf())
                        if (Expression.IsMatch(root, d))
                            yield return d;
                }
                else
                {
                    if (Expression.IsMatch(root, t))
                        yield return t;
                }
            }
        }
    }
}