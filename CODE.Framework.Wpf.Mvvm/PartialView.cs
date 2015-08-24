using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Placeholder control used to host partial view information
    /// </summary>
    /// <example>
    /// // Call CustomerController.Detail() and use the result to populate the partial view
    /// *lt;PartialView Controller="Customer" Action="Detail" /&gt;
    /// // Call CustomerController.Detail(id) and use the result to populate the partial view
    /// *lt;PartialView Controller="Customer" Action="Detail" Parameters="{Binding CustomerId}" /&gt;
    /// </example>
    /// <remarks>If there is no special model returned by the controller associated with this partial view, then the partial view runs within the current default data context.</remarks>
    public class PartialView : ContentControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartialView"/> class.
        /// </summary>
        public PartialView()
        {
            Loaded += (s, e) => LoadView();
            DataContextChanged += (s, e) => LoadView();
        }

        /// <summary>Controller associated with this partial view</summary>
        public string Controller
        {
            get { return (string) GetValue(ControllerProperty); }
            set { SetValue(ControllerProperty, value); }
        }

        /// <summary>Controller associated with this partial view</summary>
        public static readonly DependencyProperty ControllerProperty = DependencyProperty.Register("Controller", typeof (string), typeof (PartialView), new UIPropertyMetadata("", (d, e) => If.Real<PartialView>(d, d2 => d2.LoadView())));

        /// <summary>Controller action associated with this partial view</summary>
        /// <remarks>Actions always have to be specified for partial views, even when the action name is a default.</remarks>
        public string Action
        {
            get { return (string) GetValue(ActionProperty); }
            set { SetValue(ActionProperty, value); }
        }

        /// <summary>Controller action associated with this partial view</summary>
        public static readonly DependencyProperty ActionProperty = DependencyProperty.Register("Action", typeof (string), typeof (PartialView), new UIPropertyMetadata("", (d, e) => If.Real<PartialView>(d, d2 => d2.LoadView())));

        /// <summary>Name of the view that is to be loaded</summary>
        /// <remarks>This property is only respected if no action is set, which would overrule the view name</remarks>
        public string View
        {
            get { return (string) GetValue(ViewProperty); }
            set { SetValue(ViewProperty, value); }
        }

        /// <summary>Controller action associated with this partial view</summary>
        public static readonly DependencyProperty ViewProperty = DependencyProperty.Register("View", typeof (string), typeof (PartialView), new UIPropertyMetadata("", (d, e) => If.Real<PartialView>(d, d2 => d2.LoadView())));

        /// <summary>Explicitly set model for the partial view</summary>
        public object Model
        {
            get { return GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        /// <summary>Explicitly set model for the partial view</summary>
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof (object), typeof (PartialView), new UIPropertyMetadata(null, (d, e) => If.Real<PartialView>(d, d2 => If.Real<FrameworkElement>(d2.Content, content => content.DataContext = e.NewValue))));

        /// <summary>
        /// Parameter to be passed to the view action that is called to load the partial view
        /// </summary>
        public object Parameter
        {
            get { return GetValue(ParameterProperty); }
            set { SetValue(ParameterProperty, value); }
        }

        /// <summary>
        /// Parameter to be passed to the view action that is called to load the partial view
        /// </summary>
        public static readonly DependencyProperty ParameterProperty = DependencyProperty.Register("Parameter", typeof (object), typeof (PartialView), new PropertyMetadata(null, (d, e) => If.Real<PartialView>(d, d2 => If.Real<FrameworkElement>(d2.Content, content => content.DataContext = e.NewValue))));

        /// <summary>Method used internally to load the view based on the provided information</summary>
        private void LoadView()
        {
            if (!IsLoaded) return;
            if (string.IsNullOrEmpty(Controller)) return;

            if (!string.IsNullOrEmpty(Action))
            {
                var hasParameter = Parameter != null;
                if (!hasParameter)
                {
                    var parameterBindingExpression = GetBindingExpression(ParameterProperty);
                    if (parameterBindingExpression != null) return; // We have a binding, but no value yet, so we wait for this method to be re-triggered by an update through the binding
                }   
                var context = !hasParameter ? Mvvm.Controller.Action(Controller, Action) : Mvvm.Controller.Action(Controller, Action, new {parameter = Parameter});
                If.Real<ViewResult>(context.Result, v =>
                {
                    Content = v.View;
                    if (v.View.DataContext == null && v.Model != null) DataContext = v.Model;
                    else if (v.View.DataContext == null)
                    {
                        var element = this as FrameworkElement;
                        while (element != null)
                        {
                            if (element.DataContext != null)
                            {
                                v.View.DataContext = element.DataContext;
                                break;
                            }
                            if (element.Parent == null)
                            {
                                // We reached the root view. All we can do at this point is bind the
                                // data context of the current view to the data context of the root view
                                v.View.SetBinding(DataContextProperty, new Binding("DataContext") {Source = element, Mode = BindingMode.OneWay});
                                element = null;
                            }
                            else element = element.Parent as FrameworkElement;
                        }
                    }
                });
            }
            else if (!string.IsNullOrEmpty(View))
            {
                var context = Mvvm.Controller.ViewOnly(Controller, View);
                If.Real<ViewResult>(context.Result, v =>
                {
                    Content = v.View;
                    if (v.View.DataContext == null && v.Model != null) v.View.DataContext = v.Model;
                    else if (v.View.DataContext == null && Model != null) v.View.DataContext = Model;
                    else
                    {
                        var element = this as FrameworkElement;
                        while (element != null)
                        {
                            if (element.DataContext != null)
                            {
                                v.View.DataContext = element.DataContext;
                                break;
                            }
                            if (element.Parent == null)
                            {
                                // We reached the root view. All we can do at this point is bind the
                                // data context of the current view to the data context of the root view
                                v.View.SetBinding(DataContextProperty, new Binding("DataContext") {Source = element, Mode = BindingMode.OneWay});
                                element = null;
                            }
                            else element = element.Parent as FrameworkElement;
                        }
                    }
                });
            }
        }
    }
}