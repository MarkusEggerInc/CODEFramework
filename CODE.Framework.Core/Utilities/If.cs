using System;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// Static class providing convenience methods for common tasks
    /// </summary>
    public static class If
    {
        /// <summary>Checks if the provided type is the correct type and not null, and if so, runs the provided action</summary>
        /// <typeparam name="T">Type to check for</typeparam>
        /// <param name="instance">The object instance that is to be checked.</param>
        /// <param name="action">The code/action that is to be executed if the instance is of the right type and not null</param>
        public static void Real<T>(object instance, Action<T> action) where T : class
        {
            if (instance != null && instance is T)
                action(instance as T);
        }

        /// <summary>Checks if the provided types are the correct types and not null, and if so, runs the provided action</summary>
        /// <typeparam name="T1">The type of the first instance to check</typeparam>
        /// <typeparam name="T2">The type of the second instance to check</typeparam>
        /// <param name="instance">The object instance that is to be checked.</param>
        /// <param name="instance2">The second object instance that is to be checked.</param>
        /// <param name="action">The code/action that is to be executed if the instance2 are of the right type and not null</param>
        public static void Real<T1, T2>(object instance, object instance2, Action<T1, T2> action) where T1 : class where T2 : class
        {
            if (instance != null && instance is T1)
                if (instance2 != null && instance2 is T2)
                    action(instance as T1, instance2 as T2);
        }

        /// <summary>Executes the provided action if the provided instance is not null</summary>
        /// <param name="instance">Object instance to check</param>
        /// <param name="action">The action to execute if the instance is not null</param>
        public static void NotNull(object instance, Action action)
        {
            if (instance != null)
                action();
        }

        /// <summary>Executes the provided action if the provided instances are not null</summary>
        /// <param name="instance">Object instance to check</param>
        /// <param name="instance2">Object instance to check</param>
        /// <param name="action">The action to execute if the instances are not null</param>
        public static void NotNull(object instance, object instance2, Action action)
        {
            if (instance != null && instance2 != null)
                action();
        }

        /// <summary>Executes the provided action if the provided instances are not null</summary>
        /// <param name="instance">Object instance to check</param>
        /// <param name="instance2">Object instance to check</param>
        /// <param name="instance3">Object instance to check</param>
        /// <param name="action">The action to execute if the instances are not null</param>
        public static void NotNull(object instance, object instance2, object instance3, Action action)
        {
            if (instance != null && instance2 != null && instance3 != null)
                action();
        }

        /// <summary>Executes the provided action if the provided instances are not null</summary>
        /// <param name="instance">Object instance to check</param>
        /// <param name="instance2">Object instance to check</param>
        /// <param name="instance3">Object instance to check</param>
        /// <param name="instance4">Object instance to check</param>
        /// <param name="action">The action to execute if the instances are not null</param>
        public static void NotNull(object instance, object instance2, object instance3, object instance4, Action action)
        {
            if (instance != null && instance2 != null && instance3 != null && instance4 != null)
                action();
        }
    }
}
