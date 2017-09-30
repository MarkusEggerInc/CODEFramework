using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// This panel can lay out primary/secondary element views in a horizontal fashion, with added features, such as collapsing the secondary part.
    /// </summary>
    public class PrimarySecondaryHorizontalPanel : Panel
    {
        /// <summary>
        /// Defines whether the secondary part of the UI can be collapsed
        /// </summary>
        /// <value><c>true</c> if this instance can collapse secondary; otherwise, <c>false</c>.</value>
        public bool CanCollapseSecondary
        {
            get { return (bool) GetValue(CanCollapseSecondaryProperty); }
            set { SetValue(CanCollapseSecondaryProperty, value); }
        }

        /// <summary>
        /// Defines whether the secondary part of the UI can be collapsed
        /// </summary>
        public static readonly DependencyProperty CanCollapseSecondaryProperty = DependencyProperty.Register("CanCollapseSecondary", typeof (bool), typeof (PrimarySecondaryHorizontalPanel), new PropertyMetadata(true));

        /// <summary>
        /// Indicates whether the secondary element is currently collapsed
        /// </summary>
        /// <value><c>true</c> if this instance is secondary element collapsed; otherwise, <c>false</c>.</value>
        public bool IsSecondaryElementCollapsed
        {
            get { return (bool) GetValue(IsSecondaryElementCollapsedProperty); }
            set { SetValue(IsSecondaryElementCollapsedProperty, value); }
        }

        /// <summary>
        /// Indicates whether the secondary element is currently collapsed
        /// </summary>
        public static readonly DependencyProperty IsSecondaryElementCollapsedProperty = DependencyProperty.Register("IsSecondaryElementCollapsed", typeof (bool), typeof (PrimarySecondaryHorizontalPanel), new PropertyMetadata(false, OnIsSecondaryElementCollapsedChanged));

        /// <summary>
        /// Fires when the collapse state of the secondary element changes
        /// </summary>
        /// <param name="d">The panel object</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnIsSecondaryElementCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var panel = d as PrimarySecondaryHorizontalPanel;
            if (panel == null) return;
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
            panel.InvalidateVisual();
        }

        /// <summary>
        /// Spacing between primary and secondary elements
        /// </summary>
        /// <value>The element spacing.</value>
        public double ElementSpacing
        {
            get { return (double) GetValue(ElementSpacingProperty); }
            set { SetValue(ElementSpacingProperty, value); }
        }

        /// <summary>
        /// Spacing between primary and secondary elements
        /// </summary>
        public static readonly DependencyProperty ElementSpacingProperty = DependencyProperty.Register("ElementSpacing", typeof (double), typeof (PrimarySecondaryHorizontalPanel), new PropertyMetadata(3d));

        /// <summary>
        /// Width available to the secondary element
        /// </summary>
        /// <value>The width of the secondary element.</value>
        public double SecondaryElementWidth
        {
            get { return (double) GetValue(SecondaryElementWidthProperty); }
            set { SetValue(SecondaryElementWidthProperty, value); }
        }

        /// <summary>
        /// Width available to the secondary element
        /// </summary>
        public static readonly DependencyProperty SecondaryElementWidthProperty = DependencyProperty.Register("SecondaryElementWidth", typeof (double), typeof (PrimarySecondaryHorizontalPanel), new PropertyMetadata(200d));

        /// <summary>
        /// Header renderer used to render visual aspects of the panel
        /// </summary>
        /// <value>The header renderer.</value>
        public IPrimarySecondaryHorizontalPanelHeaderRenderer HeaderRenderer
        {
            get { return (IPrimarySecondaryHorizontalPanelHeaderRenderer)GetValue(HeaderRendererProperty); }
            set { SetValue(HeaderRendererProperty, value); }
        }
        /// <summary>
        /// Header renderer used to render visual aspects of the panel
        /// </summary>
        public static readonly DependencyProperty HeaderRendererProperty = DependencyProperty.Register("HeaderRenderer", typeof(IPrimarySecondaryHorizontalPanelHeaderRenderer), typeof(PrimarySecondaryHorizontalPanel), new PropertyMetadata(null));

        /// <summary>
        /// Defines the desired location of the secondary element
        /// </summary>
        /// <value>The secondary area location.</value>
        public SecondaryAreaLocation SecondaryAreaLocation
        {
            get { return (SecondaryAreaLocation)GetValue(SecondaryAreaLocationProperty); }
            set { SetValue(SecondaryAreaLocationProperty, value); }
        }
        /// <summary>
        /// Defines the desired location of the secondary element
        /// </summary>
        public static readonly DependencyProperty SecondaryAreaLocationProperty = DependencyProperty.Register("SecondaryAreaLocation", typeof(SecondaryAreaLocation), typeof(PrimarySecondaryHorizontalPanel), new PropertyMetadata(SecondaryAreaLocation.Left));

        /// <summary>
        /// Background brush for the primary area
        /// </summary>
        /// <value>The primary area background.</value>
        public Brush PrimaryAreaBackground
        {
            get { return (Brush)GetValue(PrimaryAreaBackgroundProperty); }
            set { SetValue(PrimaryAreaBackgroundProperty, value); }
        }
        /// <summary>
        /// Background brush for the primary area
        /// </summary>
        public static readonly DependencyProperty PrimaryAreaBackgroundProperty = DependencyProperty.Register("PrimaryAreaBackground", typeof(Brush), typeof(PrimarySecondaryHorizontalPanel), new PropertyMetadata(null, OnPrimaryAreaBackgroundChanged));
        /// <summary>
        /// Fires when the primary area background brush changes
        /// </summary>
        /// <param name="d">The panel object</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnPrimaryAreaBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var panel = d as PrimarySecondaryHorizontalPanel;
            if (panel == null) return;
            panel.InvalidateVisual();
        }

        /// <summary>
        /// Background brush for the secondary area
        /// </summary>
        /// <value>The secondary area background.</value>
        public Brush SecondaryAreaBackground
        {
            get { return (Brush)GetValue(SecondaryAreaBackgroundProperty); }
            set { SetValue(SecondaryAreaBackgroundProperty, value); }
        }
        /// <summary>
        /// Background brush for the secondary area
        /// </summary>
        public static readonly DependencyProperty SecondaryAreaBackgroundProperty = DependencyProperty.Register("SecondaryAreaBackground", typeof(Brush), typeof(PrimarySecondaryHorizontalPanel), new PropertyMetadata(null, OnSecondaryAreaBackgroundChanged));
        /// <summary>
        /// Fires when the secondary area background color changes
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnSecondaryAreaBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var panel = d as PrimarySecondaryHorizontalPanel;
            if (panel == null) return;
            panel.InvalidateVisual();
        }

        private Rect _currentPrimaryArea;
        private Rect _currentSecondaryArea;

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (HeaderRenderer == null)
                HeaderRenderer = new StandardPrimarySecondaryHorizontalPanelHeaderRenderer();

            foreach (var element in Children.OfType<UIElement>().Where(e => e.Visibility != Visibility.Collapsed))
                if (SimpleView.GetUIElementType(element) == UIElementTypes.Primary)
                {
                    if (CanCollapseSecondary && IsSecondaryElementCollapsed)
                    {
                        var minimumCollapsedWidth = 0d;
                        var mainAreaLeft = 0d;
                        if (HeaderRenderer != null)
                        {
                            minimumCollapsedWidth = HeaderRenderer.GetMinimumCollapsedAreaWidth(this);
                            if (SecondaryAreaLocation == SecondaryAreaLocation.Left) mainAreaLeft = minimumCollapsedWidth + (minimumCollapsedWidth > 0d ? ElementSpacing : 0d);
                        }
                        var primaryArea = GeometryHelper.NewRect(mainAreaLeft, 0d, availableSize.Width - minimumCollapsedWidth - (minimumCollapsedWidth > 0d ? ElementSpacing : 0d), availableSize.Height);
                        if (HeaderRenderer != null) primaryArea = HeaderRenderer.GetPrimaryClientArea(primaryArea, this);
                        element.Measure(primaryArea.Size);
                    }
                    else
                    {
                        var currentX = 0d;
                        if (SecondaryAreaLocation == SecondaryAreaLocation.Left) currentX = SecondaryElementWidth + ElementSpacing;
                        var primaryArea = GeometryHelper.NewRect(currentX, 0d, Math.Max(availableSize.Width - SecondaryElementWidth - ElementSpacing, 0), availableSize.Height);
                        if (HeaderRenderer != null) primaryArea = HeaderRenderer.GetPrimaryClientArea(primaryArea, this);
                        element.Measure(primaryArea.Size);
                    }
                }
                else
                {
                    if (CanCollapseSecondary && IsSecondaryElementCollapsed)
                        element.Measure(GeometryHelper.NewSize(0, availableSize.Height));
                    else
                    {
                        var secondaryArea = SecondaryAreaLocation == SecondaryAreaLocation.Left ? GeometryHelper.NewRect(0d, 0d, SecondaryElementWidth, availableSize.Height) : GeometryHelper.NewRect(availableSize.Width - SecondaryElementWidth, 0d, SecondaryElementWidth, availableSize.Height);
                        if (HeaderRenderer != null) secondaryArea = HeaderRenderer.GetSecondaryClientArea(secondaryArea, this);
                        element.Measure(secondaryArea.Size);
                    }
                }
            return base.MeasureOverride(availableSize);
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var element in Children.OfType<UIElement>().Where(e => e.Visibility != Visibility.Collapsed))
                if (SimpleView.GetUIElementType(element) == UIElementTypes.Primary)
                {
                    if (CanCollapseSecondary && IsSecondaryElementCollapsed)
                    {
                        var minimumCollapsedWidth = 0d;
                        var mainAreaLeft = 0d;
                        if (HeaderRenderer != null)
                        {
                            minimumCollapsedWidth = HeaderRenderer.GetMinimumCollapsedAreaWidth(this);
                            if (SecondaryAreaLocation == SecondaryAreaLocation.Left) mainAreaLeft = minimumCollapsedWidth + (minimumCollapsedWidth > 0d ? ElementSpacing : 0d);
                        }
                        var primaryArea = GeometryHelper.NewRect(mainAreaLeft, 0d, finalSize.Width - minimumCollapsedWidth - (minimumCollapsedWidth > 0d ? ElementSpacing : 0d), finalSize.Height);
                        if (HeaderRenderer != null) primaryArea = HeaderRenderer.GetPrimaryClientArea(primaryArea, this);
                        _currentPrimaryArea = primaryArea;
                        element.Arrange(primaryArea);
                    }
                    else
                    {
                        var currentX = 0d;
                        if (SecondaryAreaLocation == SecondaryAreaLocation.Left) currentX = SecondaryElementWidth + ElementSpacing;
                        var primaryArea = GeometryHelper.NewRect(currentX, 0d, Math.Max(finalSize.Width - SecondaryElementWidth - ElementSpacing, 0), finalSize.Height);
                        if (HeaderRenderer != null) primaryArea = HeaderRenderer.GetPrimaryClientArea(primaryArea, this);
                        _currentPrimaryArea = primaryArea;
                        element.Arrange(primaryArea);
                    }
                }
                else
                {
                    if (CanCollapseSecondary && IsSecondaryElementCollapsed)
                    {
                        _currentSecondaryArea = GeometryHelper.NewRect(-100000d, -100000d, 0d, finalSize.Height);
                        element.Arrange(_currentSecondaryArea);
                    }
                    else
                    {
                        var secondaryArea = SecondaryAreaLocation == SecondaryAreaLocation.Left ? GeometryHelper.NewRect(0d, 0d, SecondaryElementWidth, finalSize.Height) : GeometryHelper.NewRect(finalSize.Width - SecondaryElementWidth, 0d, SecondaryElementWidth, finalSize.Height);
                        if (HeaderRenderer != null) secondaryArea = HeaderRenderer.GetSecondaryClientArea(secondaryArea, this);
                        _currentSecondaryArea = secondaryArea;
                        element.Arrange(secondaryArea);
                    }
                }
            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        /// Draws the content of a <see cref="T:System.Windows.Media.DrawingContext" /> object during the render pass of a <see cref="T:System.Windows.Controls.Panel" /> element.
        /// </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext" /> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (PrimaryAreaBackground != null) dc.DrawRectangle(PrimaryAreaBackground, null, GeometryHelper.NewRect(_currentPrimaryArea.X, 0, _currentPrimaryArea.Width, ActualHeight));
            if (SecondaryAreaBackground != null) dc.DrawRectangle(SecondaryAreaBackground, null, GeometryHelper.NewRect(_currentSecondaryArea.X, 0, _currentSecondaryArea.Width, ActualHeight));

            if (HeaderRenderer != null)
                HeaderRenderer.Render(dc, _currentPrimaryArea,_currentSecondaryArea, this);
        }

        /// <summary>
        /// Invoked when an unhandled Mouse Down attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. This event data reports details about the mouse button that was pressed and the handled state.</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (HeaderRenderer != null)
            {
                if (HeaderRenderer.Click(e.GetPosition(this), _currentPrimaryArea, _currentSecondaryArea, this))
                {
                    e.Handled = true;
                    return;
                }
            }

            base.OnMouseDown(e);
        }
    }

    /// <summary>
    /// Interface for a stylable header renderer used by the PrimarySecondaryHorizontalPanel
    /// </summary>
    public interface IPrimarySecondaryHorizontalPanelHeaderRenderer
    {
        /// <summary>
        /// determines the size of the client area for the primary element, based on the overall area allocated to it
        /// </summary>
        /// <param name="totalPrimaryArea">The total primary area.</param>
        /// <param name="parentPanel">The parent panel.</param>
        /// <returns>The client size (area usable by the contained element, which excludes areas needed for header information)</returns>
        Rect GetPrimaryClientArea(Rect totalPrimaryArea, PrimarySecondaryHorizontalPanel parentPanel);

        /// <summary>
        /// determines the size of the client area for the secondary element, based on the overall area allocated to it
        /// </summary>
        /// <param name="totalSecondaryArea">The total secondary area.</param>
        /// <param name="parentPanel">The parent panel.</param>
        /// <returns>The client size (area usable by the contained element, which excludes areas needed for header information)</returns>
        Rect GetSecondaryClientArea(Rect totalSecondaryArea, PrimarySecondaryHorizontalPanel parentPanel);

        /// <summary>
        /// Renders additional graphical elements
        /// </summary>
        /// <param name="dc">The dc.</param>
        /// <param name="currentPrimaryArea">The current primary area.</param>
        /// <param name="currentSecondaryArea">The current secondary area.</param>
        /// <param name="parentPanel">The parent panel.</param>
        void Render(DrawingContext dc, Rect currentPrimaryArea, Rect currentSecondaryArea, PrimarySecondaryHorizontalPanel parentPanel);

        /// <summary>
        /// Returns the minimum width of a collapsed secondary area
        /// </summary>
        /// <param name="parentPanel">The parent panel.</param>
        /// <returns>Minimum width of the area</returns>
        double GetMinimumCollapsedAreaWidth(PrimarySecondaryHorizontalPanel parentPanel);

        /// <summary>
        /// Called when a click on the panel background happens
        /// </summary>
        /// <param name="location">The location of the click.</param>
        /// <param name="currentPrimaryArea">The current primary area.</param>
        /// <param name="currentSecondaryArea">The current secondary area.</param>
        /// <param name="parentPanel">The parent panel.</param>
        /// <returns>True if the click event has been handled and no further processing is needed</returns>
        bool Click(Point location, Rect currentPrimaryArea, Rect currentSecondaryArea, PrimarySecondaryHorizontalPanel parentPanel);
    }

    /// <summary>
    /// Standard renderer for the primary/secondary horizontal panel
    /// </summary>
    public class StandardPrimarySecondaryHorizontalPanelHeaderRenderer : FrameworkElement, IPrimarySecondaryHorizontalPanelHeaderRenderer
    {
        /// <summary>
        /// Brush used to render a collapsed state icon for a collapsed primary area
        /// </summary>
        /// <value>The collapsed secondary area icon.</value>
        public Brush CollapsedSecondaryAreaIcon
        {
            get { return (Brush)GetValue(CollapsedSecondaryAreaIconProperty); }
            set { SetValue(CollapsedSecondaryAreaIconProperty, value); }
        }
        /// <summary>
        /// Brush used to render a collapsed state icon for a collapsed primary area
        /// </summary>
        public static readonly DependencyProperty CollapsedSecondaryAreaIconProperty = DependencyProperty.Register("CollapsedSecondaryAreaIcon", typeof(Brush), typeof(StandardPrimarySecondaryHorizontalPanelHeaderRenderer), new PropertyMetadata(null));

        /// <summary>
        /// Brush used to render an expanded state icon for an expanded primary area
        /// </summary>
        /// <value>The expanded secondary area icon.</value>
        public Brush ExpandedSecondaryAreaIcon
        {
            get { return (Brush)GetValue(ExpandedSecondaryAreaIconProperty); }
            set { SetValue(ExpandedSecondaryAreaIconProperty, value); }
        }
        /// <summary>
        /// Brush used to render an expanded state icon for an expanded primary area
        /// </summary>
        public static readonly DependencyProperty ExpandedSecondaryAreaIconProperty = DependencyProperty.Register("ExpandedSecondaryAreaIcon", typeof(Brush), typeof(StandardPrimarySecondaryHorizontalPanelHeaderRenderer), new PropertyMetadata(null));

        /// <summary>
        /// Size of the expand/collapse icon
        /// </summary>
        /// <value>The size of the expand collapse icon.</value>
        public Size ExpandCollapseIconSize
        {
            get { return (Size)GetValue(ExpandCollapseIconSizeProperty); }
            set { SetValue(ExpandCollapseIconSizeProperty, value); }
        }
        /// <summary>
        /// Size of the expand/collapse icon
        /// </summary>
        public static readonly DependencyProperty ExpandCollapseIconSizeProperty = DependencyProperty.Register("ExpandCollapseIconSize", typeof(Size), typeof(StandardPrimarySecondaryHorizontalPanelHeaderRenderer), new PropertyMetadata(new Size(16, 16)));

        /// <summary>
        /// Defines the margin around the header icon
        /// </summary>
        /// <value>The icon margin.</value>
        public Thickness IconMargin
        {
            get { return (Thickness)GetValue(IconMarginProperty); }
            set { SetValue(IconMarginProperty, value); }
        }
        /// <summary>
        /// Defines the margin around the header icon
        /// </summary>
        public static readonly DependencyProperty IconMarginProperty = DependencyProperty.Register("IconMargin", typeof(Thickness), typeof(StandardPrimarySecondaryHorizontalPanelHeaderRenderer), new PropertyMetadata(null));

        /// <summary>
        /// Indicates whether the icon size and placement is to be considered and reserved when laying out other objects,
        /// or whether the icon will simply overlap other elements
        /// </summary>
        /// <value><c>true</c> if the icon is to be ignored for layout, otherwise, <c>false</c>.</value>
        public bool IgnoreIconSizeForLayout
        {
            get { return (bool)GetValue(IgnoreIconSizeForLayoutProperty); }
            set { SetValue(IgnoreIconSizeForLayoutProperty, value); }
        }
        /// <summary>
        /// Indicates whether the icon size and placement is to be considered and reserved when laying out other objects,
        /// or whether the icon will simply overlap other elements
        /// </summary>
        public static readonly DependencyProperty IgnoreIconSizeForLayoutProperty = DependencyProperty.Register("IgnoreIconSizeForLayout", typeof(bool), typeof(StandardPrimarySecondaryHorizontalPanelHeaderRenderer), new PropertyMetadata(false));

        /// <summary>
        /// determines the size of the client area for the primary element, based on the overall area allocated to it
        /// </summary>
        /// <param name="totalPrimaryArea">The total primary area.</param>
        /// <param name="parentPanel">The parent panel.</param>
        /// <returns>The client size (area usable by the contained element, which excludes areas needed for header information)</returns>
        public Rect GetPrimaryClientArea(Rect totalPrimaryArea, PrimarySecondaryHorizontalPanel parentPanel)
        {
            return totalPrimaryArea;
        }

        /// <summary>
        /// determines the size of the client area for the secondary element, based on the overall area allocated to it
        /// </summary>
        /// <param name="totalSecondaryArea">The total secondary area.</param>
        /// <param name="parentPanel">The parent panel.</param>
        /// <returns>The client size (area usable by the contained element, which excludes areas needed for header information)</returns>
        public Rect GetSecondaryClientArea(Rect totalSecondaryArea, PrimarySecondaryHorizontalPanel parentPanel)
        {
            if (IgnoreIconSizeForLayout)
                return GeometryHelper.NewRect(totalSecondaryArea.X, totalSecondaryArea.Y, totalSecondaryArea.Width, totalSecondaryArea.Height);
            return GeometryHelper.NewRect(totalSecondaryArea.X, totalSecondaryArea.Y + ExpandCollapseIconSize.Height + 4, totalSecondaryArea.Width, totalSecondaryArea.Height - ExpandCollapseIconSize.Height - 4);
        }

        /// <summary>
        /// Renders additional graphical elements
        /// </summary>
        /// <param name="dc">The dc.</param>
        /// <param name="currentPrimaryArea">The current primary area.</param>
        /// <param name="currentSecondaryArea">The current secondary area.</param>
        /// <param name="parentPanel">The parent panel.</param>
        public void Render(DrawingContext dc, Rect currentPrimaryArea, Rect currentSecondaryArea, PrimarySecondaryHorizontalPanel parentPanel)
        {
            if (parentPanel.CanCollapseSecondary)
            {
                if (parentPanel.IsSecondaryElementCollapsed)
                {
                    if (CollapsedSecondaryAreaIconProperty != null)
                        dc.DrawRectangle(CollapsedSecondaryAreaIcon, null, GeometryHelper.NewRect(new Point(parentPanel.ActualWidth - ExpandCollapseIconSize.Width + IconMargin.Left, IconMargin.Top), GeometryHelper.NewSize(ExpandCollapseIconSize.Width - IconMargin.Left - IconMargin.Right, ExpandCollapseIconSize.Height - IconMargin.Top - IconMargin.Bottom)));
                }
                else
                {
                    if (ExpandedSecondaryAreaIcon != null)
                        dc.DrawRectangle(ExpandedSecondaryAreaIcon, null, GeometryHelper.NewRect(new Point(currentSecondaryArea.X + IconMargin.Left, IconMargin.Top), GeometryHelper.NewSize(ExpandCollapseIconSize.Width - IconMargin.Left - IconMargin.Right, ExpandCollapseIconSize.Height - IconMargin.Top - IconMargin.Bottom)));
                }
            }
        }

        /// <summary>
        /// Returns the minimum width of a collapsed secondary area
        /// </summary>
        /// <param name="parentPanel">The parent panel.</param>
        /// <returns>Minimum width of the area</returns>
        public double GetMinimumCollapsedAreaWidth(PrimarySecondaryHorizontalPanel parentPanel)
        {
            return ExpandCollapseIconSize.Width;
        }

        /// <summary>
        /// Called when a click on the panel background happens
        /// </summary>
        /// <param name="location">The location of the click.</param>
        /// <param name="currentPrimaryArea">The current primary area.</param>
        /// <param name="currentSecondaryArea">The current secondary area.</param>
        /// <param name="parentPanel">The parent panel.</param>
        /// <returns>True if the click event has been handled and no further processing is needed</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool Click(Point location, Rect currentPrimaryArea, Rect currentSecondaryArea, PrimarySecondaryHorizontalPanel parentPanel)
        {
            if (location.Y > ExpandCollapseIconSize.Height + 4) return false;

            if (parentPanel.IsSecondaryElementCollapsed)
            {
                if (parentPanel.SecondaryAreaLocation == SecondaryAreaLocation.Left)
                {
                    if (location.X <= ExpandCollapseIconSize.Width)
                    {
                        parentPanel.IsSecondaryElementCollapsed = !parentPanel.IsSecondaryElementCollapsed;
                        return true;
                    }
                }
                else
                {
                    if (location.X >= parentPanel.ActualWidth - ExpandCollapseIconSize.Width)
                    {
                        parentPanel.IsSecondaryElementCollapsed = !parentPanel.IsSecondaryElementCollapsed;
                        return true;
                    }
                }
            }
            else
            {
                if (location.X >= currentSecondaryArea.Left && location.X <= currentSecondaryArea.Right)
                {
                    parentPanel.IsSecondaryElementCollapsed = !parentPanel.IsSecondaryElementCollapsed;
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Defines where the secondary area should be placed by the layout engine
    /// </summary>
    public enum SecondaryAreaLocation
    {
        /// <summary>
        /// Docked on the left
        /// </summary>
        Left,
        /// <summary>
        /// Docked on the right
        /// </summary>
        Right
    }
}
