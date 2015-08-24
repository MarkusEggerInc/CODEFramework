using System.Collections.Generic;

namespace CODE.Framework.Core.Newtonsoft.Linq.JsonPath
{
    internal class ScanFilter : PathFilter
    {
        public string Name { get; set; }

        public override IEnumerable<JToken> ExecuteFilter(IEnumerable<JToken> current, bool errorWhenNoMatch)
        {
            foreach (var root in current)
            {
                if (Name == null)
                    yield return root;

                var value = root;
                var container = root;

                while (true)
                {
                    if (container != null && container.HasValues)
                        value = container.First;
                    else
                    {
                        while (value != null && value != root && value == value.Parent.Last)
                            value = value.Parent;
                        if (value == null || value == root) break;
                        value = value.Next;
                    }

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