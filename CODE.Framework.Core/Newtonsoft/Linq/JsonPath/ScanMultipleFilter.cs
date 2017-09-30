using System.Collections.Generic;

namespace CODE.Framework.Core.Newtonsoft.Linq.JsonPath
{
    internal class ScanMultipleFilter : PathFilter
    {
        public List<string> Names { get; set; }

        public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, bool errorWhenNoMatch)
        {
            foreach (var c in current)
            {
                var value = c;
                var container = c as JContainer;

                while (true)
                {
                    value = GetNextScanValue(c, container, value);
                    if (value == null)
                        break;

                    var e = value as JProperty;
                    if (e != null)
                        foreach (var name in Names)
                            if (e.Name == name)
                                yield return e.Value;

                    container = value as JContainer;
                }
            }
        }
    }
}