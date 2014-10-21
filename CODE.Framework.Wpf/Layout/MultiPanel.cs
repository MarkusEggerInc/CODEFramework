using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// This layout panel arranges objects in multiple "panels", typically multiple rows of panels
    /// </summary>
    public class MultiPanel : Panel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiPanel"/> class.
        /// </summary>
        public MultiPanel()
        {
            SizeChanged += (s, e) => InvalidateVisual();
        }

        /// <summary>
        /// Margin between panels
        /// </summary>
        public double Spacing
        {
            get { return (double)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }
        /// <summary>
        /// Margin between panels
        /// </summary>
        public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register("Spacing", typeof(double), typeof(MultiPanel), new PropertyMetadata(5d));

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var visibleChildren = Children.Cast<UIElement>().Where(child => child.Visibility == Visibility.Visible).ToList();
            var childCount = visibleChildren.Count;
            var realSize = availableSize;
            if (double.IsPositiveInfinity(realSize.Width)) realSize.Width = 1000000d;
            if (double.IsPositiveInfinity(realSize.Height)) realSize.Height = 100 * childCount;
            var usableHeight = realSize.Height - (Spacing * 2);
            if (childCount > 1) usableHeight -= Spacing * (childCount - 1);
            var rowLeaders = visibleChildren.Where(child => !SimpleView.GetFlowsWithPrevious(child)).ToList();
            var calculatedPanelHeight = (int)(usableHeight / rowLeaders.Count);
            var heightPerPanel = calculatedPanelHeight > 0 ? calculatedPanelHeight : 100d;

            var followerElementFound = false;

            foreach (var leadElement in rowLeaders)
                if (!followerElementFound)
                {
                    // First, we check what other controls are in the same row
                    UIElement followerElement = null;
                    for (var childCount2 = 0; childCount2 < visibleChildren.Count; childCount2++)
                    {
                        if (visibleChildren[childCount2] == leadElement)
                            // We found the leader, so we check whether the next item is a follower
                            if (childCount2 < visibleChildren.Count - 1)
                                foreach (var child in visibleChildren)
                                    if (SimpleView.GetFlowsWithPrevious(child))
                                    {
                                        // We found a follower
                                        followerElement = child;
                                        followerElementFound = true;
                                        break;
                                    }
                        if (followerElement != null) break;
                    }
                    if (followerElement != null)
                    {
                        followerElement.Measure(new Size(100000, Math.Max(heightPerPanel, 0d)));
                        var followerWidth = followerElement.DesiredSize.Width;
                        if (followerWidth + (Spacing*2) > availableSize.Width) followerWidth = availableSize.Width - (Spacing*2);
                        if (followerWidth > 0d) followerElement.Measure(new Size(followerWidth, Math.Max(heightPerPanel, 0d)));
                        leadElement.Measure(new Size(Math.Max(availableSize.Width - (Spacing * 3) - followerWidth, 0), Math.Max(heightPerPanel, 0d)));
                    }
                    else
                        leadElement.Measure(new Size(availableSize.Width - (Spacing*2), Math.Max(heightPerPanel, 0d)));
                }
                else
                    leadElement.Measure(new Size(availableSize.Width - (Spacing*2), Math.Max(heightPerPanel, 0d)));

            return realSize;
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var visibleChildren = Children.Cast<UIElement>().Where(child => child.Visibility == Visibility.Visible).ToList();
            var childCount = visibleChildren.Count;
            var usableHeight = finalSize.Height - (Spacing * 2);
            if (childCount > 1) usableHeight -= Spacing * (childCount - 1);
            var rowLeaders = visibleChildren.Where(child => !SimpleView.GetFlowsWithPrevious(child)).ToList();
            var heightPerPanel = (int)(usableHeight / rowLeaders.Count);

            var currentTop = Spacing;
            var followerElementFound = false;

            foreach (var leadElement in rowLeaders)
                if (!followerElementFound)
                {
                    // First, we check what other controls are in the same row
                    UIElement followerElement = null;
                    for (var childCount2 = 0; childCount2 < visibleChildren.Count; childCount2++)
                    {
                        if (visibleChildren[childCount2] == leadElement)
                            // We found the leader, so we check whether the next item is a follower
                            if (childCount2 < visibleChildren.Count - 1)
                                foreach (var child in visibleChildren)
                                    if (SimpleView.GetFlowsWithPrevious(child))
                                    {
                                        // We found a follower
                                        followerElement = child;
                                        followerElementFound = true;
                                        break;
                                    }
                        if (followerElement != null) break;
                    }
                    if (followerElement != null)
                    {
                        var followerWidth = followerElement.DesiredSize.Width;
                        if (finalSize.Width > 0 && followerWidth + (Spacing*2) > finalSize.Width) followerWidth = finalSize.Width - (Spacing*2);
                        followerElement.Arrange(new Rect(Math.Max(finalSize.Width - followerWidth - Spacing, 0), currentTop, followerWidth, Math.Max(heightPerPanel, 0d)));
                        leadElement.Arrange(new Rect(Spacing, currentTop, Math.Max(finalSize.Width - (Spacing * 3) - followerWidth, 0), Math.Max(heightPerPanel, 0d)));
                    }
                    else
                        leadElement.Arrange(new Rect(Spacing, currentTop, finalSize.Width - (Spacing*2), Math.Max(heightPerPanel, 0d)));
                    currentTop += (int) (heightPerPanel + Spacing);
                }
                else
                {
                    leadElement.Arrange(new Rect(Spacing, currentTop, finalSize.Width - (Spacing*2), Math.Max(heightPerPanel, 0d)));
                    currentTop += (int) (heightPerPanel + Spacing);
                }

            return finalSize;
        }
    }
}
