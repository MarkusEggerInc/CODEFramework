using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CODE.Framework.Wpf.Layout;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>Content presenter specific to hosting views</summary>
    public class ViewContentControl : ContentControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewContentControl"/> class.
        /// </summary>
        public ViewContentControl()
        {
            VerticalContentAlignment = VerticalAlignment.Stretch;
            HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        /// <summary>Custom view content property used to host a view</summary>
        public object ViewContent
        {
            get { return GetValue(ViewContentProperty); }
            set { SetValue(ViewContentProperty, value); }
        }

        /// <summary>Custom view content property used to host a view</summary>
        public static readonly DependencyProperty ViewContentProperty = DependencyProperty.Register("ViewContent", typeof (object), typeof (ViewContentControl), new UIPropertyMetadata(null, ViewContentPropertyChanged));

        /// <summary>Handler for view content changes</summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void ViewContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var content = d as ViewContentControl;
            var ui = e.NewValue as UIElement;
            if (content == null) return;
            
            content.Content = ui;
            SetSizeStrategyOnHost(ui, content);

            if (ui == null) return;
            if (ui.InputBindings.Count < 1) return;

            var parent = ui as FrameworkElement;
            if (parent == null) return;
            while (parent.Parent != null)
            {
                var newParent = parent.Parent as FrameworkElement;
                if (newParent != null) parent = newParent;
                else break;
            }
            foreach (InputBinding binding in ui.InputBindings)
            {
                var keyBinding = binding as KeyBinding;
                if (keyBinding == null) continue;

                var found = parent.InputBindings.OfType<KeyBinding>().Any(keyBinding2 => keyBinding2.Key == keyBinding.Key && keyBinding2.Modifiers == keyBinding.Modifiers);
                if (!found)
                    parent.InputBindings.Add(keyBinding);
            }
        }

        /// <summary>Attached property to set the view's host object that is size strategy aware</summary>
        public FrameworkElement SizeStrategyHost
        {
            get { return (FrameworkElement)GetValue(SizeStrategyHostProperty); }
            set { SetValue(SizeStrategyHostProperty, value); }
        }

        /// <summary>Attached property to set the view's host object that is size strategy aware</summary>
        /// <remarks>This attached property can be attached to any UI Element to define a view size strategy aware object </remarks>
        public static readonly DependencyProperty SizeStrategyHostProperty = DependencyProperty.Register("SizeStrategyHost", typeof (FrameworkElement), typeof (ViewContentControl), new PropertyMetadata(null, SizeStrategyHostPropertyChanged));

        /// <summary>Handler for view content changes</summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void SizeStrategyHostPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var content = d as ViewContentControl;
            if (content == null) return;
            SetSizeStrategyOnHost(content.ViewContent as View, content);
        }

        /// <summary>Sets the size strategy on host.</summary>
        /// <param name="view">The view.</param>
        /// <param name="viewContent">Content of the view.</param>
        private static void SetSizeStrategyOnHost(DependencyObject view, ViewContentControl viewContent)
        {
            if (view == null) return;
            if (viewContent == null) return;

            var host = viewContent.SizeStrategyHost;
            if (host == null) return;
            var gridHost = host as SizeStrategyAwareGrid;
            if (gridHost == null) return;

            gridHost.SizeStrategy = SimpleView.GetSizeStrategy(view);
        }

        ///// <summary>
        ///// Called to remeasure a control.
        ///// </summary>
        ///// <param name="constraint">The maximum size that the method can return.</param>
        ///// <returns>
        ///// The size of the control, up to the maximum specified by <paramref name="constraint"/>.
        ///// </returns>
        //protected override Size MeasureOverride(Size constraint)
        //{
        //    var view = Content as Document;
        //    if (view != null)
        //    {
        //        var strategy = SimpleView.GetSizeStrategy(view);
        //        if (strategy == ViewSizeStrategies.UseMaximumSizeAvailable)
        //        {
        //            var result = base.MeasureOverride(constraint);
        //            if (!Double.IsInfinity(result.Width) && !Double.IsInfinity(constraint.Width)) result.Width = Math.Max(result.Width, constraint.Width);
        //            if (!Double.IsInfinity(result.Height) && !Double.IsInfinity(constraint.Height)) result.Height = Math.Max(result.Height, constraint.Height);
        //            return result;
        //        }
        //    }
        //    return base.MeasureOverride(constraint);
        //}

        ///// <summary>
        ///// Positions the single child element and determines the content of a <see cref="T:System.Windows.Controls.ContentPresenter"/> object.
        ///// </summary>
        ///// <param name="arrangeSize">The size that this <see cref="T:System.Windows.Controls.ContentPresenter"/> object should use to arrange its child element.</param>
        ///// <returns>
        ///// The actual size needed by the element.
        ///// </returns>
        //protected override Size ArrangeOverride(Size arrangeSize)
        //{
        //    var view = Content as Document;
        //    if (view != null)
        //    {
        //        var strategy = SimpleView.GetSizeStrategy(view);
        //        if (strategy == ViewSizeStrategies.UseMaximumSizeAvailable)
        //        {
        //             double width = Math.Max(ActualWidth, arrangeSize.Width);
        //            double height = Math.Max(ActualHeight, arrangeSize.Height);
        //            view.Arrange(new Rect(0, 0, width, height));
        //            return arrangeSize;
        //        }
        //    }
        //    return base.ArrangeOverride(arrangeSize);
        //}
    }

    /// <summary>Grid class that is aware of size strategies</summary>
    public class SizeStrategyAwareGrid : Grid
    {
        /// <summary>Defines the size strategy employed by this grid</summary>
        public ViewSizeStrategies SizeStrategy
        {
            get { return (ViewSizeStrategies)GetValue(SizeStrategyProperty); }
            set { SetValue(SizeStrategyProperty, value); }
        }

        /// <summary>Defines the size strategy employed by this grid</summary>
        public static readonly DependencyProperty SizeStrategyProperty = DependencyProperty.Register("SizeStrategy", typeof(ViewSizeStrategies), typeof(SizeStrategyAwareGrid), new UIPropertyMetadata(ViewSizeStrategies.UseMinimumSizeRequired, OnSizeStrategyChanged));

        private static void OnSizeStrategyChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var grid = d as SizeStrategyAwareGrid;
            if (grid == null) return;

            if (grid.SizeStrategy == ViewSizeStrategies.UseMaximumSizeAvailable)
            {
                grid.VerticalAlignment = VerticalAlignment.Stretch;
                grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
            else 
            {
                grid.VerticalAlignment = VerticalAlignment.Center;
                grid.HorizontalAlignment = HorizontalAlignment.Center;
            }

            grid.InvalidateMeasure();
            grid.InvalidateArrange();
            grid.InvalidateVisual();
        }
    }
}
