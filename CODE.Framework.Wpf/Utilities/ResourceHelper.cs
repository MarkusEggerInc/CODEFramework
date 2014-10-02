using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.Utilities
{
    /// <summary>
    /// This class provides useful helper methods for dealing with XAML resources
    /// </summary>
    public static class ResourceHelper
    {
        /// <summary>Inspects a drawing brush and replaces dynamically bound Brush resources with the provided ones</summary>
        /// <param name="drawing">Drawing Brush</param>
        /// <param name="replacementBrushes">Dictionary of replacement brushes</param>
        /// <remarks>This helper function is useful for loading assets such as icons and make them take on the local brushes</remarks>
        public static void ReplaceDynamicDrawingBrushResources(DrawingBrush drawing, Dictionary<object, Brush> replacementBrushes)
        {
            If.Real<DrawingGroup>(drawing.Drawing, group => InspectDrawingGroup(@group, replacementBrushes));
        }

        /// <summary>Iterates over a drawing group and replaces brush resources</summary>
        /// <param name="group">Group to inspect</param>
        /// <param name="replacementBrushes">Replacement brushes</param>
        private static void InspectDrawingGroup(DrawingGroup group, IDictionary<object, Brush> replacementBrushes)
        {
            foreach (var groupItem in group.Children)
            {
                var subGroup = groupItem as DrawingGroup;
                if (subGroup != null)
                {
                    InspectDrawingGroup(subGroup, replacementBrushes);
                    continue;
                }

                var geometry = groupItem as GeometryDrawing;
                if (geometry == null) continue;

                // Checking the brush
                var local = geometry.ReadLocalValue(GeometryDrawing.BrushProperty);
                if (local != null)
                {
                    var localType = local.GetType();
                    if (localType.Name == "ResourceReferenceExpression")
                    {
                        var resourceKeyProperty = localType.GetProperty("ResourceKey");
                        if (resourceKeyProperty != null)
                        {
                            var key = resourceKeyProperty.GetValue(local, null) as string;
                            if (!string.IsNullOrEmpty(key))
                            {
                                if (replacementBrushes.ContainsKey(key))
                                    geometry.Brush = replacementBrushes[key];
                            }
                        }
                    }
                }

                // Checking the pen
                var pen = geometry.Pen;
                if (pen != null)
                {
                    var local2 = pen.ReadLocalValue(Pen.BrushProperty);
                    if (local2 == null) continue;

                    var localType2 = local2.GetType();
                    if (localType2.Name != "ResourceReferenceExpression") continue;

                    var resourceKeyProperty2 = localType2.GetProperty("ResourceKey");
                    if (resourceKeyProperty2 == null) continue;

                    var key2 = resourceKeyProperty2.GetValue(local2, null) as string;
                    if (string.IsNullOrEmpty(key2)) continue;

                    if (replacementBrushes.ContainsKey(key2))
                        pen.Brush = replacementBrushes[key2];
                }
            }
        }

        /// <summary>Returns all the brush resources in an element's resource collection</summary>
        /// <param name="element">The element that defines the resources</param>
        /// <param name="existingCollection">A collection of resources that are already known</param>
        /// <param name="clone">If true, the brushes will be cloned, so they can be manipulated at will without impacting other uses of the same brush</param>
        /// <returns>Collection of brush resources</returns>
        public static Dictionary<object, Brush> GetBrushResources(FrameworkElement element, Dictionary<object, Brush> existingCollection = null, bool clone = true)
        {
            var result = existingCollection ?? new Dictionary<object, Brush>();

            if (element.Resources == null) return result;

            foreach (var key in element.Resources.Keys)
            {
                var brush = element.Resources[key] as Brush;
                if (brush != null)
                {
                    if (clone) brush = brush.Clone();
                    result.Add(key.ToString(), brush);
                }
            }

            if (element.Resources.MergedDictionaries != null)
                foreach (var dictionary in element.Resources.MergedDictionaries)
                    foreach (var key in dictionary.Keys)
                    {
                        var brush = dictionary[key] as Brush;
                        if (brush != null)
                        {
                            if (clone) brush = brush.Clone();
                            if (result.ContainsKey(key)) result[key] = brush;
                            result.Add(key.ToString(), brush);
                        }

                    }

            return result;
        }
    }
}