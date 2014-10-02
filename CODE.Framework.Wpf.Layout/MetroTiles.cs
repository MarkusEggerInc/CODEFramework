using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// Performs layout of child elements using a Windows 8 metro style approach
    /// </summary>
    public class MetroTiles : Panel
    {
        /// <summary>Display title for the group an item is in</summary>
        /// <remarks>This is a display value only and changes in localized versions</remarks>
        public static readonly DependencyProperty GroupTitleProperty = DependencyProperty.RegisterAttached("GroupTitle", typeof(string), typeof(MetroTiles), new PropertyMetadata(""));
        /// <summary>Get operation for GroupTitleProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static string GetGroupTitle(DependencyObject obj) { return (string)obj.GetValue(GroupTitleProperty); }
        /// <summary>Set operation for GroupTitleProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetGroupTitle(DependencyObject obj, string value)
        {
            obj.SetValue(GroupTitleProperty, value);
            string name = (string)obj.GetValue(GroupNameProperty);
            if (string.IsNullOrEmpty(name)) obj.SetValue(GroupNameProperty, value); // If no name is set explicitly, then we default the name to the same as the title
        }

        /// <summary>Internal name for the group an item is in</summary>
        /// <remarks>This name never changes, even in localized versions. Note that if this property is not set explicitly, it defaults to the group title.</remarks>
        public static readonly DependencyProperty GroupNameProperty = DependencyProperty.RegisterAttached("GroupName", typeof(string), typeof(MetroTiles), new PropertyMetadata(""));
        /// <summary>Get operation for GroupTitleProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static string GetGroupName(DependencyObject obj)
        {
            string name = (string)obj.GetValue(GroupNameProperty);
            if (string.IsNullOrEmpty(name)) name = (string)obj.GetValue(GroupTitleProperty); // If we can't find a name, we default back to the title
            return name;
        }
        /// <summary>Set operation for GroupTitleProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetGroupName(DependencyObject obj, string value) { obj.SetValue(GroupNameProperty, value); }

        /// <summary>Internal name for the group an item is in</summary>
        public static readonly DependencyProperty TileWidthModeProperty = DependencyProperty.RegisterAttached("TileWidthMode", typeof(TileWidthModes), typeof(MetroTiles), new PropertyMetadata(TileWidthModes.Normal));
        /// <summary>Get operation for GroupTitleProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static TileWidthModes GetTileWidthMode(DependencyObject obj) { return (TileWidthModes)obj.GetValue(TileWidthModeProperty); }
        /// <summary>Set operation for GroupTitleProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetTileWidthMode(DependencyObject obj, TileWidthModes value) { obj.SetValue(TileWidthModeProperty, value); }

        /// <summary>Height of a single tile (which is uniform across all objects in a tile layout)</summary>
        public static readonly DependencyProperty TileHeightProperty = DependencyProperty.RegisterAttached("TileHeight", typeof(double), typeof(MetroTiles), new PropertyMetadata(120d));
        /// <summary>Get operation for TileHeightProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static double GetTileHeight(DependencyObject obj) { return (double)obj.GetValue(TileHeightProperty); }
        /// <summary>Set operation for TileHeightProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetTileHeight(DependencyObject obj, double value) { obj.SetValue(TileHeightProperty, value); }

        /// <summary>Width of a single tile (which is uniform across all objects in a tile layout... double width tiles are twice this value + horizontal tile spacing)</summary>
        public static readonly DependencyProperty TileWidthProperty = DependencyProperty.RegisterAttached("TileWidth", typeof(double), typeof(MetroTiles), new PropertyMetadata(120d));
        /// <summary>Get operation for TileHeightProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static double GetTileWidth(DependencyObject obj) { return (double)obj.GetValue(TileWidthProperty); }
        /// <summary>Set operation for TileHeightProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetTileWidth(DependencyObject obj, double value) { obj.SetValue(TileWidthProperty, value); }

        /// <summary>Horizontal spacing of a tiles within groups</summary>
        public static readonly DependencyProperty HorizontalTileSpacingProperty = DependencyProperty.RegisterAttached("HorizontalTileSpacing", typeof(double), typeof(MetroTiles), new PropertyMetadata(8d));
        /// <summary>Get operation for HorizontalTileSpacingProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static double GetHorizontalTileSpacing(DependencyObject obj) { return (double)obj.GetValue(HorizontalTileSpacingProperty); }
        /// <summary>Set operation for HorizontalTileSpacingProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetHorizontalTileSpacing(DependencyObject obj, double value) { obj.SetValue(HorizontalTileSpacingProperty, value); }

        /// <summary>Vertical tile spacing within groups</summary>
        public static readonly DependencyProperty VerticalTileSpacingProperty = DependencyProperty.RegisterAttached("VerticalTileSpacing", typeof(double), typeof(MetroTiles), new PropertyMetadata(8d));
        /// <summary>Get operation for VerticalTileSpacingProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static double GetVerticalTileSpacing(DependencyObject obj) { return (double)obj.GetValue(VerticalTileSpacingProperty); }
        /// <summary>Set operation for VerticalTileSpacingProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetVerticalTileSpacing(DependencyObject obj, double value) { obj.SetValue(VerticalTileSpacingProperty, value); }

        /// <summary>Horizontal spacing of a tiles within groups</summary>
        public static readonly DependencyProperty HorizontalGroupSpacingProperty = DependencyProperty.RegisterAttached("HorizontalGroupSpacing", typeof(double), typeof(MetroTiles), new PropertyMetadata(56d));
        /// <summary>Get operation for HorizontalTileSpacingProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static double GetHorizontalGroupSpacing(DependencyObject obj) { return (double)obj.GetValue(HorizontalGroupSpacingProperty); }
        /// <summary>Set operation for HorizontalTileSpacingProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetHorizontalGroupSpacing(DependencyObject obj, double value) { obj.SetValue(HorizontalGroupSpacingProperty, value); }

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement"/>-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>
        /// The size that this element determines it needs during layout, based on its calculations of child element sizes.
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            return IterateChildren(availableSize, (child, rect) => child.Measure(rect.Size));
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement"/> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            IterateChildren(finalSize, (child, rect) => child.Arrange(rect));
            return finalSize;
        }

        private Size IterateChildren(Size availableSize, Action<UIElement, Rect> methodToCall)
        {
            try
            {
                double top = 0d;
                double left = 0d;

                double horizontalSpacing = GetHorizontalTileSpacing(this);
                double verticalSpacing = GetVerticalTileSpacing(this);
                double horizontalGroupSpacing = GetHorizontalGroupSpacing(this);

                double tileWidthNormal = GetTileWidth(this);
                double tileHeight = GetTileHeight(this);
                double tileWidthDouble = tileWidthNormal * 2 + horizontalSpacing;

                var groups = GetChildrenByGroup();
                int groupCounter = 0;
                foreach (var group in groups.Values)
                {
                    if (groupCounter > 0) left += horizontalGroupSpacing + tileWidthDouble;
                    groupCounter++;
                    int lastNormalColumn = -1;
                    foreach (UIElement child in group)
                    {
                        bool isNormalWidth = GetTileWidthMode(child) == TileWidthModes.Normal;
                        double width = isNormalWidth ? tileWidthNormal : tileWidthDouble;
                        if (!isNormalWidth)
                        {
                            if (lastNormalColumn > -1)
                            {
                                top += tileHeight + verticalSpacing;
                                if (top + tileHeight > availableSize.Height)
                                {
                                    // Need to overflow into the second column, since we are out of room
                                    top = 0d;
                                    left += tileWidthDouble + horizontalSpacing;
                                }
                            }
                            lastNormalColumn = -1;
                                // This is a double-wide column, so we need to reset the square column counter
                        }

                        methodToCall(child,
                                     new Rect(
                                         lastNormalColumn == -1 ? left : left + tileWidthNormal + horizontalSpacing, top,
                                         width, tileHeight));

                        if (!isNormalWidth)
                            top += tileHeight + verticalSpacing; // Double width tiles always use up an entire column
                        else if (lastNormalColumn == 0)
                        {
                            lastNormalColumn = -1;
                                // filled the horizontal space in the column, so we need to go to the next line. Otherwise, another tile still fits
                            top += tileHeight + verticalSpacing;
                        }
                        else lastNormalColumn++;

                        if (top + tileHeight > availableSize.Height)
                        {
                            // Need to overflow into the second column, since we are out of room
                            top = 0d;
                            left += tileWidthDouble + horizontalSpacing;
                        }
                    }
                    top = 0d;
                }
                return new Size(left + tileWidthDouble, top + tileHeight);
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Goes through the entire collection of child elements and returns them by groups
        /// </summary>
        /// <returns></returns>
        private Dictionary<string,List<UIElement>> GetChildrenByGroup()
        {
            var groups = new List<string>();
            foreach (UIElement child in Children)
            {
                string group = GetGroupName(child);
                if (!groups.Contains(group))
                    groups.Add(group);
            }

            var result = new Dictionary<string, List<UIElement>>();

            foreach (var group in groups)
                foreach (UIElement child in Children)
                    if (GetGroupName(child) == group)
                    {
                        if (!result.ContainsKey(group)) result.Add(group, new List<UIElement>());
                        result[group].Add(child);
                    }

            return result;
        }
    }

    /// <summary>
    /// Standard supported tile widths
    /// </summary>
    public enum TileWidthModes
    {
        /// <summary>
        /// Normal (typically square)
        /// </summary>
        Normal,
        /// <summary>
        /// Double-wide
        /// </summary>
        Double
    }
}
