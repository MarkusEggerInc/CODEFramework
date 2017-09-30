using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// Performs layout of child elements using a Windows 8 metro style approach
    /// </summary>
    public class MetroTiles : Panel
    {
        /// <summary>Display title for the group an item is in</summary>
        /// <remarks>This is a display value only and changes in localized versions</remarks>
        public static readonly DependencyProperty GroupTitleProperty = DependencyProperty.RegisterAttached("GroupTitle", typeof (string), typeof (MetroTiles), new PropertyMetadata(""));

        /// <summary>Internal name for the group an item is in</summary>
        /// <remarks>This name never changes, even in localized versions. Note that if this property is not set explicitly, it defaults to the group title.</remarks>
        public static readonly DependencyProperty GroupNameProperty = DependencyProperty.RegisterAttached("GroupName", typeof (string), typeof (MetroTiles), new PropertyMetadata(""));

        /// <summary>Default tile width mode used for all tiles that do not have an explicit mode set</summary>
        public static readonly DependencyProperty DefaultTileWidthProperty = DependencyProperty.Register("DefaultTileWidth", typeof (TileWidthModes), typeof (MetroTiles), new UIPropertyMetadata(TileWidthModes.Normal));

        /// <summary>Internal name for the group an item is in</summary>
        public static readonly DependencyProperty TileWidthModeProperty = DependencyProperty.RegisterAttached("TileWidthMode", typeof (TileWidthModes), typeof (MetroTiles), new PropertyMetadata(TileWidthModes.Default));

        /// <summary>Height of a single tile (which is uniform across all objects in a tile layout)</summary>
        public static readonly DependencyProperty TileHeightProperty = DependencyProperty.RegisterAttached("TileHeight", typeof (double), typeof (MetroTiles), new PropertyMetadata(150d, InvalidateEverything));

        /// <summary>Width of a single tile (which is uniform across all objects in a tile layout... double width tiles are twice this value + horizontal tile spacing)</summary>
        public static readonly DependencyProperty TileWidthProperty = DependencyProperty.RegisterAttached("TileWidth", typeof (double), typeof (MetroTiles), new PropertyMetadata(150d, InvalidateEverything));

        /// <summary>Visibility of the tile</summary>
        public static readonly DependencyProperty TileVisibilityProperty = DependencyProperty.RegisterAttached("TileVisibility", typeof(Visibility), typeof(MetroTiles), new PropertyMetadata(Visibility.Visible, InvalidateEverything));

        /// <summary>Horizontal spacing of a tiles within groups</summary>
        public static readonly DependencyProperty HorizontalTileSpacingProperty = DependencyProperty.RegisterAttached("HorizontalTileSpacing", typeof (double), typeof (MetroTiles), new PropertyMetadata(10d, InvalidateEverything));

        /// <summary>Vertical tile spacing within groups</summary>
        public static readonly DependencyProperty VerticalTileSpacingProperty = DependencyProperty.RegisterAttached("VerticalTileSpacing", typeof (double), typeof (MetroTiles), new PropertyMetadata(10d, InvalidateEverything));

        /// <summary>Horizontal spacing of a tiles within groups</summary>
        public static readonly DependencyProperty HorizontalGroupSpacingProperty = DependencyProperty.RegisterAttached("HorizontalGroupSpacing", typeof (double), typeof (MetroTiles), new PropertyMetadata(56d, InvalidateEverything));

        /// <summary>Top and left padding before the first tile</summary>
        public static readonly DependencyProperty ContentTopLeftPaddingProperty = DependencyProperty.Register("ContentTopLeftPadding", typeof (Size), typeof (MetroTiles), new UIPropertyMetadata(new Size(116, 0), InvalidateEverything));

        /// <summary>Defines whether text headers shall be rendered</summary>
        public static readonly DependencyProperty RenderHeadersProperty = DependencyProperty.Register("RenderHeaders", typeof (bool), typeof (MetroTiles), new UIPropertyMetadata(false, InvalidateEverything));

        /// <summary>Object used to render the captions</summary>
        public static readonly DependencyProperty HeaderRendererProperty = DependencyProperty.Register("HeaderRenderer", typeof (AutoLayoutHeaderRenderer), typeof (MetroTiles), new UIPropertyMetadata(null, InvalidateEverything));

        /// <summary>Font family used to render group headers</summary>
        public static readonly DependencyProperty HeaderFontFamilyProperty = DependencyProperty.Register("HeaderFontFamily", typeof (FontFamily), typeof (MetroTiles), new UIPropertyMetadata(new FontFamily("Segoe UI"), InvalidateEverything));

        /// <summary>Font style used to render group headers</summary>
        public static readonly DependencyProperty HeaderFontStyleProperty = DependencyProperty.Register("HeaderFontStyle", typeof (FontStyle), typeof (MetroTiles), new UIPropertyMetadata(FontStyles.Normal, InvalidateEverything));

        /// <summary>Font weight used to render group headers</summary>
        public static readonly DependencyProperty HeaderFontWeightProperty = DependencyProperty.Register("HeaderFontWeight", typeof (FontWeight), typeof (MetroTiles), new UIPropertyMetadata(FontWeights.Light, InvalidateEverything));

        /// <summary>Font size used to render group headers</summary>
        public static readonly DependencyProperty HeaderFontSizeProperty = DependencyProperty.Register("HeaderFontSize", typeof (double), typeof (MetroTiles), new UIPropertyMetadata(24d, InvalidateEverything));

        /// <summary>Foreground brush used to render group headers</summary>
        public static readonly DependencyProperty HeaderForegroundBrushProperty = DependencyProperty.Register("HeaderForegroundBrush", typeof (Brush), typeof (MetroTiles), new UIPropertyMetadata(Brushes.Black, InvalidateEverything));

        /// <summary>Spacing between the caption and the element</summary>
        public static readonly DependencyProperty HeaderSpacingProperty = DependencyProperty.Register("HeaderSpacing", typeof (double), typeof (MetroTiles), new UIPropertyMetadata(15d, InvalidateEverything));

        private readonly List<AutoHeaderTextRenderInfo> _headers = new List<AutoHeaderTextRenderInfo>();

        /// <summary>Default tile width mode used for all tiles that do not have an explicit mode set</summary>
        public TileWidthModes DefaultTileWidth
        {
            get { return (TileWidthModes) GetValue(DefaultTileWidthProperty); }
            set { SetValue(DefaultTileWidthProperty, value); }
        }

        /// <summary>Top and left padding before the first tile</summary>
        public Size ContentTopLeftPadding
        {
            get { return (Size) GetValue(ContentTopLeftPaddingProperty); }
            set { SetValue(ContentTopLeftPaddingProperty, value); }
        }

        /// <summary>Defines whether text headers shall be rendered</summary>
        public bool RenderHeaders
        {
            get { return (bool) GetValue(RenderHeadersProperty); }
            set { SetValue(RenderHeadersProperty, value); }
        }

        /// <summary>Object used to render the captions</summary>
        public AutoLayoutHeaderRenderer HeaderRenderer
        {
            get { return (AutoLayoutHeaderRenderer) GetValue(HeaderRendererProperty); }
            set { SetValue(HeaderRendererProperty, value); }
        }

        /// <summary>Font family used to render group headers</summary>
        public FontFamily HeaderFontFamily
        {
            get { return (FontFamily) GetValue(HeaderFontFamilyProperty); }
            set { SetValue(HeaderFontFamilyProperty, value); }
        }

        /// <summary>Font style used to render group headers</summary>
        public FontStyle HeaderFontStyle
        {
            get { return (FontStyle) GetValue(HeaderFontStyleProperty); }
            set { SetValue(HeaderFontStyleProperty, value); }
        }

        /// <summary>Font weight used to render group headers</summary>
        public FontWeight HeaderFontWeight
        {
            get { return (FontWeight) GetValue(HeaderFontWeightProperty); }
            set { SetValue(HeaderFontWeightProperty, value); }
        }

        /// <summary>Font size used to render group headers</summary>
        public double HeaderFontSize
        {
            get { return (double) GetValue(HeaderFontSizeProperty); }
            set { SetValue(HeaderFontSizeProperty, value); }
        }

        /// <summary>Foreground brush used to render group headers</summary>
        public Brush HeaderForegroundBrush
        {
            get { return (Brush) GetValue(HeaderForegroundBrushProperty); }
            set { SetValue(HeaderForegroundBrushProperty, value); }
        }

        /// <summary>Spacing between the caption and the element</summary>
        public double HeaderSpacing
        {
            get { return (double) GetValue(HeaderSpacingProperty); }
            set { SetValue(HeaderSpacingProperty, value); }
        }

        /// <summary>Get operation for GroupTitleProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static string GetGroupTitle(DependencyObject obj)
        {
            return (string) obj.GetValue(GroupTitleProperty);
        }

        /// <summary>Set operation for GroupTitleProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetGroupTitle(DependencyObject obj, string value)
        {
            obj.SetValue(GroupTitleProperty, value);
            var name = (string) obj.GetValue(GroupNameProperty);
            if (string.IsNullOrEmpty(name)) obj.SetValue(GroupNameProperty, value); // If no name is set explicitly, then we default the name to the same as the title
        }

        /// <summary>Get operation for GroupTitleProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static string GetGroupName(DependencyObject obj)
        {
            var name = (string) obj.GetValue(GroupNameProperty);
            if (string.IsNullOrEmpty(name)) name = (string) obj.GetValue(GroupTitleProperty); // If we can't find a name, we default back to the title
            return name;
        }

        /// <summary>Set operation for GroupTitleProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetGroupName(DependencyObject obj, string value)
        {
            obj.SetValue(GroupNameProperty, value);
        }

        /// <summary>Get operation for GroupTitleProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static TileWidthModes GetTileWidthMode(DependencyObject obj)
        {
            return (TileWidthModes) obj.GetValue(TileWidthModeProperty);
        }

        /// <summary>Set operation for GroupTitleProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetTileWidthMode(DependencyObject obj, TileWidthModes value)
        {
            obj.SetValue(TileWidthModeProperty, value);
        }

        /// <summary>Get operation for TileHeightProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static double GetTileHeight(DependencyObject obj)
        {
            return (double) obj.GetValue(TileHeightProperty);
        }

        /// <summary>Set operation for TileHeightProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetTileHeight(DependencyObject obj, double value)
        {
            obj.SetValue(TileHeightProperty, value);
        }

        /// <summary>
        /// Gets the tile visibility.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>Visibility.</returns>
        public static Visibility GetTileVisibility(DependencyObject obj)
        {
            return (Visibility)obj.GetValue(TileVisibilityProperty);
        }

        /// <summary>
        /// Sets the tile visibility.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="value">The value.</param>
        public static void SetTileVisibility(DependencyObject obj, Visibility value)
        {
            obj.SetValue(TileVisibilityProperty, value);
        }

        /// <summary>Get operation for TileHeightProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static double GetTileWidth(DependencyObject obj)
        {
            return (double) obj.GetValue(TileWidthProperty);
        }

        /// <summary>Set operation for TileHeightProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetTileWidth(DependencyObject obj, double value)
        {
            obj.SetValue(TileWidthProperty, value);
        }

        /// <summary>Get operation for HorizontalTileSpacingProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static double GetHorizontalTileSpacing(DependencyObject obj)
        {
            return (double) obj.GetValue(HorizontalTileSpacingProperty);
        }

        /// <summary>Set operation for HorizontalTileSpacingProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetHorizontalTileSpacing(DependencyObject obj, double value)
        {
            obj.SetValue(HorizontalTileSpacingProperty, value);
        }

        /// <summary>Get operation for VerticalTileSpacingProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static double GetVerticalTileSpacing(DependencyObject obj)
        {
            return (double) obj.GetValue(VerticalTileSpacingProperty);
        }

        /// <summary>Set operation for VerticalTileSpacingProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetVerticalTileSpacing(DependencyObject obj, double value)
        {
            obj.SetValue(VerticalTileSpacingProperty, value);
        }

        /// <summary>Get operation for HorizontalTileSpacingProperty dependency property</summary><param name="obj">The object the property really belongs to</param><returns>Actual property value.</returns>
        public static double GetHorizontalGroupSpacing(DependencyObject obj)
        {
            return (double) obj.GetValue(HorizontalGroupSpacingProperty);
        }

        /// <summary>Set operation for HorizontalTileSpacingProperty dependency property</summary><param name="obj">The object the property really belongs to</param><param name="value">The value to set.</param>
        public static void SetHorizontalGroupSpacing(DependencyObject obj, double value)
        {
            obj.SetValue(HorizontalGroupSpacingProperty, value);
        }

        /// <summary>Invalidates all layout, measurement, and rendering</summary>
        /// <param name="dependencyObject">One-To-Many panel to invalidate</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void InvalidateEverything(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            If.Real<MetroTiles>(dependencyObject, panel =>
            {
                panel.InvalidateArrange();
                panel.InvalidateMeasure();
                panel.InvalidateVisual();
            });
        }

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
            var arrangedSize = IterateChildren(finalSize, (child, rect) => child.Arrange(rect));
            return GeometryHelper.NewSize(arrangedSize.Width, finalSize.Height);
        }

        private Size IterateChildren(Size availableSize, Action<UIElement, Rect> methodToCall)
        {
            try
            {
                var absoluteTop = ContentTopLeftPadding.Height;
                var top = absoluteTop;
                var left = ContentTopLeftPadding.Width;

                var heightUsed = top;
                var widthUsed = left;

                var horizontalSpacing = GetHorizontalTileSpacing(this);
                var verticalSpacing = GetVerticalTileSpacing(this);
                var horizontalGroupSpacing = GetHorizontalGroupSpacing(this);

                var tileWidthNormal = GetTileWidth(this);
                var tileHeight = GetTileHeight(this);
                var tileWidthDouble = tileWidthNormal*2 + horizontalSpacing;
                var tileWidthTiny = (tileWidthNormal - horizontalSpacing)/2;

                var groups = GetChildrenByGroup();

                // We figure out whether we need to leave space for headers
                if (RenderHeaders)
                {
                    _headers.Clear();
                    var titles = new List<string>();
                    var foundTitle = false;
                    foreach (var groupKey in groups.Keys)
                    {
                        var group = groups[groupKey];
                        if (group.Count > 0)
                        {
                            var groupTitle = GetGroupTitleForObject(group[0]);
                            titles.Add(groupTitle);
                            if (!string.IsNullOrEmpty(groupTitle)) foundTitle = true;
                        }
                        else titles.Add(string.Empty);
                    }
                    if (foundTitle)
                    {
                        var maxHeaderHeight = 0d;
                        foreach (var title in titles)
                        {
                            var header = new AutoHeaderTextRenderInfo
                            {
                                Text = title,
                                FormattedText = new FormattedText(title,
                                    CultureInfo.CurrentUICulture,
                                    FlowDirection.LeftToRight,
                                    new Typeface(HeaderFontFamily, HeaderFontStyle, HeaderFontWeight, FontStretches.Normal),
                                    HeaderFontSize,
                                    HeaderForegroundBrush)
                            };
                            maxHeaderHeight = Math.Max(header.FormattedText.Height, maxHeaderHeight);
                            _headers.Add(header);
                        }
                        if (maxHeaderHeight > 0)
                        {
                            absoluteTop += maxHeaderHeight + HeaderSpacing;
                            top += maxHeaderHeight + HeaderSpacing;
                            heightUsed += maxHeaderHeight + HeaderSpacing;
                        }
                    }
                }

                var groupCount = -1;
                foreach (var group in groups.Values)
                {
                    groupCount++;
                    var groupWidth = tileWidthDouble;
                    var groupLeft = left;

                    var currentTinyCount = 0;
                    var currentNormalCount = 0;

                    var currentMaxColumnWidth = 0d;

                    foreach (var child in group)
                    {
                        TileWidthModes tileWidth;
                        var contentPresenter = child as ContentPresenter;
                        if (contentPresenter != null && contentPresenter.Content != null && contentPresenter.Content is DependencyObject)
                        {
                            var dependencyContent = (DependencyObject)contentPresenter.Content;
                            tileWidth = GetTileWidthMode(dependencyContent);
                            child.Visibility = MetroTiles.GetTileVisibility(dependencyContent);
                        }
                        else
                            tileWidth = GetTileWidthMode(child);
                        if (tileWidth == TileWidthModes.Default) tileWidth = DefaultTileWidth;

                        if (child.Visibility != Visibility.Visible) continue;

                        switch (tileWidth)
                        {
                            case TileWidthModes.Tiny:
                                if (currentTinyCount == 0 && top > absoluteTop && top + tileHeight > availableSize.Height) // We are beyond the bottom and can do something about it
                                {
                                    top = absoluteTop;
                                    left += tileWidthDouble + horizontalSpacing;
                                    groupWidth += currentMaxColumnWidth + horizontalSpacing;
                                    currentMaxColumnWidth = 0d;
                                }
                                var tinyAreaLeft = left;
                                if (currentNormalCount == 1) tinyAreaLeft += tileWidthNormal + horizontalSpacing;
                                switch (currentTinyCount)
                                {
                                    case 0:
                                        var tinyTileRect0 = GeometryHelper.NewRect(tinyAreaLeft, top, tileWidthTiny, tileWidthTiny);
                                        methodToCall(child, tinyTileRect0);
                                        heightUsed = Math.Max(heightUsed, tinyTileRect0.Bottom);
                                        widthUsed = Math.Max(widthUsed, tinyTileRect0.Right);
                                        currentMaxColumnWidth = Math.Max(currentMaxColumnWidth, currentNormalCount == 0 ? tileWidthTiny : tileWidthNormal + horizontalSpacing + tileWidthTiny);
                                        break;
                                    case 1:
                                        var tinyTileRect1 = GeometryHelper.NewRect(tinyAreaLeft + horizontalSpacing + tileWidthTiny, top, tileWidthTiny, tileWidthTiny);
                                        methodToCall(child, tinyTileRect1);
                                        heightUsed = Math.Max(heightUsed, tinyTileRect1.Bottom);
                                        widthUsed = Math.Max(widthUsed, tinyTileRect1.Right);
                                        currentMaxColumnWidth = Math.Max(currentMaxColumnWidth, currentNormalCount == 0 ? tileWidthTiny + horizontalSpacing + tileWidthTiny : tileWidthNormal + horizontalSpacing + tileWidthNormal);
                                        break;
                                    case 2:
                                        var tinyTileRect2 = GeometryHelper.NewRect(tinyAreaLeft, top + verticalSpacing + tileWidthTiny, tileWidthTiny, tileWidthTiny);
                                        methodToCall(child, tinyTileRect2);
                                        heightUsed = Math.Max(heightUsed, tinyTileRect2.Bottom);
                                        widthUsed = Math.Max(widthUsed, tinyTileRect2.Right);
                                        currentMaxColumnWidth = Math.Max(currentMaxColumnWidth, currentNormalCount == 0 ? tileWidthTiny + horizontalSpacing + tileWidthTiny : tileWidthNormal + horizontalSpacing + tileWidthNormal);
                                        break;
                                    case 3:
                                        var tinyTileRect3 = GeometryHelper.NewRect(tinyAreaLeft + horizontalSpacing + tileWidthTiny, top + verticalSpacing + tileWidthTiny, tileWidthTiny, tileWidthTiny);
                                        methodToCall(child, tinyTileRect3);
                                        heightUsed = Math.Max(heightUsed, tinyTileRect3.Bottom);
                                        widthUsed = Math.Max(widthUsed, tinyTileRect3.Right);
                                        currentMaxColumnWidth = Math.Max(currentMaxColumnWidth, currentNormalCount == 0 ? tileWidthTiny + horizontalSpacing + tileWidthTiny : tileWidthNormal + horizontalSpacing + tileWidthNormal);
                                        break;
                                }
                                currentTinyCount++;
                                if (currentTinyCount > 3)
                                {
                                    currentNormalCount++;
                                    currentTinyCount = 0;
                                }
                                if (currentNormalCount > 1)
                                {
                                    top += tileHeight + verticalSpacing;
                                    currentNormalCount = 0;
                                }
                                break;
                            case TileWidthModes.Normal:
                                if (currentNormalCount == 1 && currentTinyCount > 0)
                                {
                                    top += tileHeight + verticalSpacing;
                                    currentNormalCount = 0;
                                }
                                if (currentNormalCount == 0 && top > absoluteTop && top + tileHeight > availableSize.Height) // We are beyond the bottom and can do something about it
                                {
                                    top = absoluteTop;
                                    left += tileWidthDouble + horizontalSpacing;
                                    groupWidth += currentMaxColumnWidth + horizontalSpacing;
                                    currentMaxColumnWidth = 0d;
                                }
                                var normalTileRect = GeometryHelper.NewRect(currentNormalCount == 1 ? left + tileWidthNormal + horizontalSpacing : left, top, tileWidthNormal, tileHeight);
                                currentTinyCount = 0;
                                currentNormalCount++;
                                methodToCall(child, normalTileRect);
                                heightUsed = Math.Max(heightUsed, normalTileRect.Bottom);
                                widthUsed = Math.Max(widthUsed, normalTileRect.Right);
                                currentMaxColumnWidth = Math.Max(currentMaxColumnWidth, currentNormalCount == 1 ? tileWidthNormal : tileWidthDouble);
                                if (currentNormalCount > 1)
                                {
                                    top += tileHeight + verticalSpacing;
                                    currentNormalCount = 0;
                                }
                                break;
                            case TileWidthModes.Double:
                                if (currentNormalCount > 0) top += tileHeight + verticalSpacing;
                                if (currentTinyCount > 0) top += tileHeight + verticalSpacing;
                                if (top > absoluteTop && top + tileHeight > availableSize.Height) // We are beyond the bottom and can do something about it
                                {
                                    top = absoluteTop;
                                    left += tileWidthDouble + horizontalSpacing;
                                    groupWidth += currentMaxColumnWidth + horizontalSpacing;
                                }
                                currentNormalCount = 0;
                                currentTinyCount = 0;
                                var dobleTileRect = GeometryHelper.NewRect(left, top, tileWidthDouble, tileHeight);
                                methodToCall(child, dobleTileRect);
                                heightUsed = Math.Max(heightUsed, dobleTileRect.Bottom);
                                widthUsed = Math.Max(widthUsed, dobleTileRect.Right);
                                currentMaxColumnWidth = tileWidthDouble;
                                top += tileHeight + verticalSpacing;
                                break;
                            case TileWidthModes.DoubleSquare:
                                if (currentNormalCount > 0) top += tileHeight + verticalSpacing;
                                if (currentTinyCount > 0) top += tileHeight + verticalSpacing;
                                if (top > absoluteTop && top + tileWidthDouble > availableSize.Height) // We are beyond the bottom and can do something about it
                                {
                                    top = absoluteTop;
                                    left += tileWidthDouble + horizontalSpacing;
                                    groupWidth += currentMaxColumnWidth + horizontalSpacing;
                                }
                                currentNormalCount = 0;
                                currentTinyCount = 0;
                                var dobleSquareTileRect = GeometryHelper.NewRect(left, top, tileWidthDouble, tileWidthDouble);
                                methodToCall(child, dobleSquareTileRect);
                                heightUsed = Math.Max(heightUsed, dobleSquareTileRect.Bottom);
                                widthUsed = Math.Max(widthUsed, dobleSquareTileRect.Right);
                                currentMaxColumnWidth = tileWidthDouble;
                                top += tileWidthDouble + verticalSpacing;
                                break;
                        }

                        // Possible headers
                        if (RenderHeaders && _headers.Count > groupCount)
                        {
                            _headers[groupCount].RenderRect = GeometryHelper.NewRect(groupLeft, 0d, groupWidth, _headers[groupCount].FormattedText.Height +4);
                            InvalidateVisual();
                        }
                    }

                    top = absoluteTop;
                    left += horizontalGroupSpacing - horizontalSpacing;
                    left += currentMaxColumnWidth;
                }
                //return GeometryHelper.NewSize(left + tileWidthDouble, top + tileHeight);
                return GeometryHelper.NewSize(widthUsed, heightUsed);
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
        private Dictionary<string, List<UIElement>> GetChildrenByGroup()
        {
            var groups = new List<string>();
            foreach (DependencyObject child in Children)
            {
                var group = GetGroupNameForObject(child);
                if (!groups.Contains(group))
                    groups.Add(group);
            }

            var result = new Dictionary<string, List<UIElement>>();

            foreach (var group in groups)
                foreach (UIElement child in Children)
                    if (GetGroupNameForObject(child) == group)
                    {
                        if (!result.ContainsKey(group)) result.Add(group, new List<UIElement>());
                        result[group].Add(child);
                    }

            return result;
        }

        private string GetGroupNameForObject(DependencyObject d)
        {
            var group = GetGroupName(d);
            if (string.IsNullOrEmpty(group) && d is ContentPresenter)
            {
                var dependencyContent = ((ContentPresenter)d).Content as DependencyObject;
                if (dependencyContent != null)
                    group = GetGroupName(dependencyContent);
            }
            return group;
        }

        private string GetGroupTitleForObject(DependencyObject d)
        {
            var group = GetGroupTitle(d);
            if (string.IsNullOrEmpty(group) && d is ContentPresenter)
            {
                var dependencyContent = ((ContentPresenter)d).Content as DependencyObject;
                if (dependencyContent != null)
                    group = GetGroupTitle(dependencyContent);
            }
            return group;
        }

        /// <summary>
        /// Draws the content of a <see cref="T:System.Windows.Media.DrawingContext"/> object during the render pass of a <see cref="T:System.Windows.Controls.Panel"/> element.
        /// </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext"/> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (!RenderHeaders) return;

            if (HeaderRenderer == null) HeaderRenderer = new AutoLayoutHeaderRenderer();

            var offset = new Point();

            foreach (var header in _headers)
                HeaderRenderer.RenderHeader(dc, header, 1d, offset);
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
        Double,

        /// <summary>
        /// Uses the default set for the entire panel
        /// </summary>
        Default,

        /// <summary>
        /// Tiny tile size (quarter of the normal ones)
        /// </summary>
        Tiny,

        /// <summary>
        /// Square size of the double size (twice as large as double)
        /// </summary>
        DoubleSquare
    }
}