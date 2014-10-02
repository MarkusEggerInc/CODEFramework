using System;
using System.ComponentModel;
using System.Windows;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.Behavior
{
    /// <summary>
    /// Encapsulates state information and zero or more ICommands into an attachable object. 
    /// </summary>
    /// <remarks>
    /// This is an infrastructure class. Behavior authors should derive from Behavior instead of from this class.
    /// </remarks>
    public abstract class Behavior : IAttachedObject
    {
        /// <summary>
        /// Attaches to the specified object.
        /// </summary>
        /// <param name="dependencyObject">The object to attach to.</param>
        public void Attach(DependencyObject dependencyObject)
        {
            if (dependencyObject != AssociatedObject)
            {
                if (AssociatedObject != null) throw new InvalidOperationException("Cannot Host Behavior Multiple Times. Each behavior can only be attached to a single object.");

                _associatedObject = dependencyObject;
                OnAssociatedObjectChanged();
                OnAttachedInternal();
                OnAttached();
            }
        }

        private DependencyObject _associatedObject;

        /// <summary>
        /// Fires the associated object changed event (if used)
        /// </summary>
        private void OnAssociatedObjectChanged()
        {
            if (AssociatedObjectChanged != null)
                AssociatedObjectChanged(this, new EventArgs());
        }

        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject. 
        /// </summary>
        /// <remarks>Override this to hook up functionality to the AssociatedObject.</remarks>
        protected virtual void OnAttached() { }
        /// <summary>
        /// Similar to OnAttached(), but used for internal purposes only. Do not override.
        /// </summary>
        /// <remarks>Do not override this method. Override OnAttached() instead.</remarks>
        [Browsable(false)]
        protected virtual void OnAttachedInternal() { }

        /// <summary>
        /// Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred. 
        /// </summary>
        /// <remarks>Override this to unhook functionality from the AssociatedObject.</remarks>
        protected virtual void OnDetaching() { }

        /// <summary>
        /// Detaches this instance from its associated object.
        /// </summary>
        public void Detach()
        {
            OnDetaching();
            _associatedObject = null;
            OnAssociatedObjectChanged();
        }

        /// <summary>
        /// Fires when the assopciated object changes
        /// </summary>
        public EventHandler AssociatedObjectChanged;

        /// <summary>
        /// Represents the object the instance is attached to.
        /// </summary>
        /// <value>Associated object or null</value>
        public DependencyObject AssociatedObject
        {
            get { return _associatedObject; }
        }
    }

    /// <summary>
    /// Encapsulates state information and zero or more ICommands into an attachable object. 
    /// </summary>
    /// <typeparam name="TAttached">The type the behavior can be attached to.</typeparam>
    /// <remarks>
    /// Behavior is the base class for providing attachable state and commands to an object. 
    /// The types the Behavior can be attached to can be controlled by the generic parameter. 
    /// Override OnAttached() and OnDetaching() methods to hook and unhook any necessary 
    /// handlers from the AssociatedObject. 
    /// </remarks>
    public abstract class Behavior<TAttached> : Behavior where TAttached : FrameworkElement
    {
        /// <summary>
        /// Represents the object the instance is attached to.
        /// </summary>
        /// <value>Associated object or null</value>
        protected new TAttached AssociatedObject
        {
            get { return (TAttached)base.AssociatedObject; }
        }

        /// <summary>
        /// Attempts to find a resource within the current resource lookup chain.
        /// (Typically such a resource would be associated with the object this behavior is attached to,
        /// or its parent UI, or it may be available generally in the application).
        /// </summary>
        /// <param name="resourceKey">Resource Key</param>
        /// <returns>Resource if found, or null</returns>
        public virtual object FindResource(string resourceKey)
        {
            if (AssociatedObject == null) return null;
            return AssociatedObject.TryFindResource(resourceKey);
        }

        /// <summary>
        /// Attempts to find a style resource within the current resource lookup chain.
        /// </summary>
        /// <param name="styleResourceKey">Resource key of the style</param>
        /// <returns>Style if found, otherwise null</returns>
        public virtual Style FindStyle(string styleResourceKey)
        {
            if (AssociatedObject == null) return null;
            var resource = AssociatedObject.TryFindResource(styleResourceKey);
            if (resource == null) return null;
            return resource as Style;
        }

        /// <summary>
        /// Tries to find an object by name in the associated UI.
        /// </summary>
        /// <param name="elementName">Name associated with the element (X:Name)</param>
        /// <returns>Object reference or null if not found</returns>
        public virtual object FindElement(string elementName)
        {
            if (AssociatedObject == null) return null;
            try
            {
                return AssociatedObject.FindName(elementName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to find the root UI object the current associated object lives in.
        /// </summary>
        /// <returns>Object reference or null</returns>
        public virtual DependencyObject FindRootElement()
        {
            if (AssociatedObject == null) return null;

            FrameworkElement element = AssociatedObject;

            while (element.Parent != null)
                element = element.Parent as FrameworkElement;

            return element;
        }

        /// <summary>
        /// Applies the specified style to the specified object
        /// </summary>
        /// <param name="styleResourceKey">Key of the style resource that is to be assigned</param>
        /// <param name="elementName">Name (X:Name) of the element the style is to be applied to.</param>
        /// <returns>True if successful</returns>
        public virtual bool ApplyStyleToObject(string styleResourceKey, string elementName)
        {
            var style = FindStyle(styleResourceKey);
            var element = FindElement(elementName);
            bool response = false;
            If.Real<FrameworkElement, Style>(element, style, (el2, s2) => { response = ApplyStyleToObject(s2, el2); });
            return response;
        }

        /// <summary>
        /// Applies the specified style to the specified object
        /// </summary>
        /// <param name="style">Style to assign</param>
        /// <param name="elementName">Name (X:Name) of the element the style is to be applied to.</param>
        /// <returns>True if successful</returns>
        public virtual bool ApplyStyleToObject(Style style, string elementName)
        {
            var element = FindElement(elementName);
            if (element == null) return false;
            var frameworkElement = element as FrameworkElement;
            if (frameworkElement == null) return false;

            return ApplyStyleToObject(style, frameworkElement);
        }

        /// <summary>
        /// Applies the specified style to the specified object
        /// </summary>
        /// <param name="styleResourceKey">Key of the style resource that is to be assigned</param>
        /// <param name="obj">Object to assign the style to</param>
        /// <returns>True if successful</returns>
        public virtual bool ApplyStyleToObject(string styleResourceKey, FrameworkElement obj)
        {
            var style = FindStyle(styleResourceKey);
            if (style == null) return false;

            return ApplyStyleToObject(style, obj);
        }

        /// <summary>
        /// Applies the specified style to the specified object
        /// </summary>
        /// <param name="style">Style to assign</param>
        /// <param name="obj">Object to assign the style to</param>
        /// <returns>True if successful</returns>
        public virtual bool ApplyStyleToObject(Style style, FrameworkElement obj)
        {
            obj.Style = style;
            return true;
        }
    }
}
