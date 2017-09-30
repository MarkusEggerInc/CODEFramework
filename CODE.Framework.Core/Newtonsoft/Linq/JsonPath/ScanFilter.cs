using System.Collections.Generic;

namespace CODE.Framework.Core.Newtonsoft.Linq.JsonPath
{
    internal class ScanFilter : PathFilter
    {
        public string Name { get; set; }

        public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, bool errorWhenNoMatch)
        {
            foreach (var c in current)
            {
                if (Name == null)
                    yield return c;

                var value = c;
                var container = c as JContainer;

                while (true)
                {
                    value = GetNextScanValue(c, container, value);
                    if (value == null)
                        break;

                    var e = value as JProperty;
                    if (e != null)
                    {
                        if (e.Name == Name)
                            yield return e.Value;
                    }
                    else
                    {
                        if (Name == null)
                            yield return value;
                    }

                    container = value as JContainer;
                }
            }
        }
    }
}