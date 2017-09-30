using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CODE.Framework.Core.Newtonsoft.Utilities;

namespace CODE.Framework.Core.Newtonsoft.Linq.JsonPath
{
    internal class FieldMultipleFilter : PathFilter
    {
        public List<string> Names { get; set; }

        public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, bool errorWhenNoMatch)
        {
            foreach (var t in current)
            {
                var o = t as JObject;
                if (o != null)
                {
                    foreach (var name in Names)
                    {
                        var v = o[name];

                        if (v != null)
                            yield return v;

                        if (errorWhenNoMatch)
                            throw new JsonException("Property '{0}' does not exist on JObject.".FormatWith(CultureInfo.InvariantCulture, name));
                    }
                }
                else
                {
                    if (errorWhenNoMatch)
                        throw new JsonException("Properties {0} not valid on {1}.".FormatWith(CultureInfo.InvariantCulture, string.Join(", ", Names.Select(n => "'" + n + "'").ToArray()), t.GetType().Name));
                }
            }
        }
    }
}