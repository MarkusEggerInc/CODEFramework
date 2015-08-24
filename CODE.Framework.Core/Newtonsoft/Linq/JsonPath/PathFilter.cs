using System.Collections.Generic;
using System.Globalization;
using CODE.Framework.Core.Newtonsoft.Utilities;

namespace CODE.Framework.Core.Newtonsoft.Linq.JsonPath
{
    internal abstract class PathFilter
    {
        public abstract IEnumerable<JToken> ExecuteFilter(IEnumerable<JToken> current, bool errorWhenNoMatch);

        protected static JToken GetTokenIndex(JToken t, bool errorWhenNoMatch, int index)
        {
            var a = t as JArray;
            var c = t as JConstructor;

            if (a != null)
            {
                if (a.Count > index) return a[index];
                if (errorWhenNoMatch) throw new JsonException("Index {0} outside the bounds of JArray.".FormatWith(CultureInfo.InvariantCulture, index));
                return null;
            }
            if (c != null)
            {
                if (c.Count > index) return c[index];
                if (errorWhenNoMatch) throw new JsonException("Index {0} outside the bounds of JConstructor.".FormatWith(CultureInfo.InvariantCulture, index));
                return null;
            }
            if (errorWhenNoMatch) throw new JsonException("Index {0} not valid on {1}.".FormatWith(CultureInfo.InvariantCulture, index, t.GetType().Name));
            return null;
        }
    }
}