using System.Collections.Generic;

namespace CODE.Framework.Core.Newtonsoft.Linq.JsonPath
{
    internal class ArrayMultipleIndexFilter : PathFilter
    {
        public List<int> Indexes { get; set; }

        public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, bool errorWhenNoMatch)
        {
            foreach (var t in current)
            foreach (var i in Indexes)
            {
                var v = GetTokenIndex(t, errorWhenNoMatch, i);

                if (v != null)
                    yield return v;
            }
        }
    }
}