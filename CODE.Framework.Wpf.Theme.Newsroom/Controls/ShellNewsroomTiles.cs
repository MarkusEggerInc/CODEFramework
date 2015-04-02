using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CODE.Framework.Wpf.Layout;
using CODE.Framework.Wpf.Mvvm;
using CODE.Framework.Wpf.Theme.Newsroom.Classes;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Theme.Newsroom.Controls
{
    /// <summary>
    /// Special button class used for Newsroom styled tiles
    /// </summary>
    public class NewsroomTileButton : Button
    {
        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        public Brush Icon
        {
            get { return (Brush)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(Brush), typeof(NewsroomTileButton), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the caption.
        /// </summary>
        /// <value>The caption.</value>
        public string Caption
        {
            get { return (string)GetValue(CaptionProperty); }
            set { SetValue(CaptionProperty, value); }
        }
        /// <summary>
        /// Gets or sets the caption.
        /// </summary>
        public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register("Caption", typeof(string), typeof(NewsroomTileButton), new PropertyMetadata(""));

        /// <summary>
        /// Gets or sets the significance.
        /// </summary>
        /// <value>The significance.</value>
        public ViewActionSignificance Significance
        {
            get { return (ViewActionSignificance)GetValue(SignificanceProperty); }
            set { SetValue(SignificanceProperty, value); }
        }
        /// <summary>
        /// Gets or sets the significance.
        /// </summary>
        public static readonly DependencyProperty SignificanceProperty = DependencyProperty.Register("Significance", typeof(ViewActionSignificance), typeof(NewsroomTileButton), new PropertyMetadata(ViewActionSignificance.Normal, OnSignificanceChanged));

        /// <summary>
        /// Fires when the SignificanceProperty changes
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnSignificanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as NewsroomTileButton;
            if (button == null) return;
            button.HighSignificanceVisible = (button.Significance == ViewActionSignificance.AboveNormal || button.Significance == ViewActionSignificance.Highest) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Defines whether high significance items are visible
        /// </summary>
        /// <value>The high significance visible.</value>
        public Visibility HighSignificanceVisible
        {
            get { return (Visibility)GetValue(HighSignificanceVisibleProperty); }
            set { SetValue(HighSignificanceVisibleProperty, value); }
        }
        /// <summary>
        /// Defines whether high significance items are visible
        /// </summary>
        public static readonly DependencyProperty HighSignificanceVisibleProperty = DependencyProperty.Register("HighSignificanceVisible", typeof(Visibility), typeof(NewsroomTileButton), new PropertyMetadata(Visibility.Collapsed));
    }

    /// <summary>
    /// Special version of the MetroTiles layout specific to start screens
    /// </summary>
    public class ShellNewsroomTiles : MetroTiles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShellNewsroomTiles"/> class.
        /// </summary>
        public ShellNewsroomTiles()
        {
            RenderHeaders = true;

            DataContextChanged += (s, e) =>
            {
                var actionsHost = DataContext as IHaveActions;
                if (actionsHost == null) return;
                actionsHost.ActionsChanged += (s2, e2) =>
                {
                    LoadTiles();
                    if (e2.NewItems == null) return;
                    foreach (var action in e2.NewItems)
                    {
                        var viewAction = action as ViewAction;
                        if (viewAction != null)
                            viewAction.PropertyChanged += (s3, e3) =>
                            {
                                if (e3.PropertyName == "Availability" || string.IsNullOrEmpty(e3.PropertyName))
                                {
                                    InvalidateMeasure();
                                    InvalidateArrange();
                                }
                            };
                    }
                };

                LoadTiles();
            };
        }

        private void LoadTiles()
        {
            // TODO: This method could probably be be optimized! We can probably get away without reloading everything every time.

            lock (this)
            {
                foreach (var child in Children)
                {
                    var childButton = child as Button;
                    if (childButton != null)
                    {
                        childButton.Content = null;
                        childButton.DataContext = null;
                    }
                }
                Children.Clear();

                var actionsHost = DataContext as IHaveActions;
                if (actionsHost != null)
                {
                    List<IViewAction> currentActions;
                    lock (actionsHost.Actions) // Trying to do this as quickly as possible to avoid threading problems
                        currentActions = actionsHost.Actions.OrderBy(a => a.CategoryOrder).ToList();

                    RemoveAllMenuKeyBindings();

                    foreach (var action in currentActions)
                    {
                        var button = new NewsroomTileButton();
                        button.SetResourceReference(StyleProperty, "Newsroom-Control-TileButton");
                        button.Command = action;

                        var visibilityBinding = new Binding("Availability") {Source = action, Converter = new AvailabilityToVisibilityConverter()};
                        button.SetBinding(VisibilityProperty, visibilityBinding);

                        var iconBinding = new Binding("PopulatedBrush") {Source = action};
                        button.SetBinding(NewsroomTileButton.IconProperty, iconBinding);

                        var captionBinding = new Binding("Caption") { Source = action };
                        button.SetBinding(NewsroomTileButton.CaptionProperty, captionBinding);

                        var significanceBinding = new Binding("Significance") { Source = action };
                        button.SetBinding(NewsroomTileButton.SignificanceProperty, significanceBinding);

                        if (action.ActionView == null)
                        {
                            var realAction = action as ViewAction;
                            if (realAction != null && !realAction.HasBrush && !realAction.HasExecuteDelegate && string.IsNullOrEmpty(action.Caption))
                                continue; // Not adding this since is has no brush and no execute and no caption

                            //switch (action.Significance)
                            //{
                            //    case ViewActionSignificance.Highest:
                            //        var view = Controller.LoadView("CODEFrameworkStandardViewTileWideSquare");
                            //        button.Content = view;
                            //        view.DataContext = action.ActionViewModel ?? action;
                            //        //action.ActionView = view; // No need to re-load this later...
                            //        SetTileWidthMode(button, TileWidthModes.DoubleSquare);
                            //        break;
                            //    case ViewActionSignificance.AboveNormal:
                            //        var view2 = Controller.LoadView("CODEFrameworkStandardViewTileWide");
                            //        button.Content = view2;
                            //        view2.DataContext = action.ActionViewModel ?? action;
                            //        //action.ActionView = view2; // No need to re-load this later...
                            //        SetTileWidthMode(button, TileWidthModes.Double);
                            //        break;
                            //    case ViewActionSignificance.Normal:
                            //    case ViewActionSignificance.BelowNormal:
                            //        var view3 = Controller.LoadView("CODEFrameworkStandardViewTileNarrow");
                            //        button.Content = view3;
                            //        view3.DataContext = action.ActionViewModel ?? action;
                            //        //action.ActionView = view3; // No need to re-load this later...
                            //        SetTileWidthMode(button, TileWidthModes.Normal);
                            //        break;
                            //    case ViewActionSignificance.Lowest:
                            //        var view4 = Controller.LoadView("CODEFrameworkStandardViewTileTiny");
                            //        button.Content = view4;
                            //        view4.DataContext = action.ActionViewModel ?? action;
                            //        //action.ActionView = view4; // No need to re-load this later...
                            //        SetTileWidthMode(button, TileWidthModes.Tiny);
                            //        break;
                            //}
                        }
                        else
                        {
                            button.Content = action.ActionView;
                            if (action.ActionView.DataContext == null) action.ActionView.DataContext = action.ActionViewModel ?? action;
                            switch (action.Significance)
                            {
                                case ViewActionSignificance.Highest:
                                    SetTileWidthMode(button, TileWidthModes.DoubleSquare);
                                    break;
                                case ViewActionSignificance.AboveNormal:
                                    SetTileWidthMode(button, TileWidthModes.Double);
                                    break;
                                case ViewActionSignificance.Normal:
                                case ViewActionSignificance.BelowNormal:
                                    SetTileWidthMode(button, TileWidthModes.Normal);
                                    break;
                                case ViewActionSignificance.Lowest:
                                    SetTileWidthMode(button, TileWidthModes.Tiny);
                                    break;
                            }
                        }

                        if (action.Categories.Count > 0)
                        {
                            SetGroupName(button, action.Categories[0].Id);
                            SetGroupTitle(button, action.Categories[0].Caption);
                        }
                        else
                        {
                            SetGroupName(button, string.Empty);
                            SetGroupTitle(button, string.Empty);
                        }

                        Children.Add(button);

                        if (action.AccessKey != ' ')
                            _menuKeyBindings.Add(new ViewActionMenuKeyBinding(action) {Key = action.ShortcutKey, Modifiers = action.ShortcutModifiers});
                    }

                    CreateAllMenuKeyBindings();
                }
            }
        }

        private readonly List<ViewActionMenuKeyBinding> _menuKeyBindings = new List<ViewActionMenuKeyBinding>();

        /// <summary>
        /// Removes all key bindings from the current window that were associated with a view category menu
        /// </summary>
        private void CreateAllMenuKeyBindings()
        {
            var window = ElementHelper.FindVisualTreeParent<Window>(this);
            if (window == null) return;

            foreach (var binding in _menuKeyBindings)
                window.InputBindings.Add(binding);
        }

        /// <summary>
        /// Removes all key bindings from the current window that were associated with a view category menu
        /// </summary>
        private void RemoveAllMenuKeyBindings()
        {
            _menuKeyBindings.Clear();

            var window = ElementHelper.FindVisualTreeParent<Window>(this);
            if (window == null) return;

            var bindingIndex = 0;
            while (true)
            {
                if (bindingIndex >= window.InputBindings.Count) break;
                var binding = window.InputBindings[bindingIndex];
                if (binding is ViewActionMenuKeyBinding)
                    window.InputBindings.RemoveAt(bindingIndex); // We remove the item from the collection and start over with the remove operation since now all indexes changed
                else
                    bindingIndex++;
            }
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
            var requiredSize = base.MeasureOverride(availableSize);
            requiredSize.Width += 20;
            return requiredSize;
        }
    }
}