using System.Windows;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// An interface for an object that can be attached to another object. 
    /// </summary>
    public interface IAttachedObject
    {
        /// <summary>
        /// Attaches to the specified object. 
        /// </summary>
        /// <param name="dependencyObject">The object to attach to.</param>
        void Attach(DependencyObject dependencyObject);

        /// <summary>
        /// Detaches this instance from its associated object. 
        /// </summary>
        void Detach();

        /// <summary>
        /// Represents the object the instance is attached to.
        /// </summary>
        DependencyObject AssociatedObject { get; }
    }
}
