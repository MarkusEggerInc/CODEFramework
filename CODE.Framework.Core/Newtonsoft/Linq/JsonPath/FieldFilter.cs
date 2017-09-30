using System.Collections.Generic;
using System.Globalization;
using CODE.Framework.Core.Newtonsoft.Utilities;

namespace CODE.Framework.Core.Newtonsoft.Linq.JsonPath
{
    internal class FieldFilter : PathFilter
    {
        public string Name { get; set; }

        public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, bool errorWhenNoMatch)
        {
            foreach (var t in current)
            {
                var o = t as JObject;
                if (o != null)
                {
                    if (Name != null)
                    {
                        var v = o[Name];

                        if (v != null)
                            yield return v;
                        else if (errorWhenNoMatch)
                            throw new JsonException("Property '{0}' does not exist on JObject.".FormatWith(CultureInfo.InvariantCulture, Name));
                    }
                    else
                    {
                        foreach (var p in o)
                            yield return p.Value;
                    }
                }
                else
                {
                    if (errorWhenNoMatch)
                        throw new JsonException("Property '{0}' not valid on {1}.".FormatWith(CultureInfo.InvariantCulture, Name ?? "*", t.GetType().Name));
                }
            }
        }
    }
}