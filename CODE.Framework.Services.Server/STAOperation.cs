using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading;

namespace CODE.Framework.Services.Server
{
    /// <summary>STA (Single Thread Appartment) Behavior Attribute (used to force execution of methods in WCF into STA mode)</summary>
    /// <remarks>In some very rare cases, it may be desirable to force service calls into a single threaded appartment model (server-side). This attribute can be used to decorate such service methods/operations, which will force STA processing.</remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class STAOperationAttribute : Attribute, IOperationBehavior
    {
        /// <summary>
        /// Implement to pass data at runtime to bindings to support custom behavior.
        /// </summary>
        /// <param name="operationDescription">The operation being examined. Use for examination only. If the operation description is modified, the results are undefined.</param>
        /// <param name="bindingParameters">The collection of objects that binding elements require to support the behavior.</param>
        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
            // intentionally blank
        }

        /// <summary>
        /// Implements a modification or extension of the client across an operation.
        /// </summary>
        /// <param name="operationDescription">The operation being examined. Use for examination only. If the operation description is modified, the results are undefined.</param>
        /// <param name="clientOperation">The run-time object that exposes customization properties for the operation described by <paramref name="operationDescription"/>.</param>
        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
            // intentionally blank
        }

        /// <summary>
        /// Implements a modification or extension of the service across an operation.
        /// </summary>
        /// <param name="operationDescription">The operation being examined. Use for examination only. If the operation description is modified, the results are undefined.</param>
        /// <param name="dispatchOperation">The run-time object that exposes customization properties for the operation described by <paramref name="operationDescription"/>.</param>
        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            dispatchOperation.Invoker = new STAInvoker(dispatchOperation.Invoker);
        }

        /// <summary>
        /// Implement to confirm that the operation meets some intended criteria.
        /// </summary>
        /// <param name="operationDescription">The operation being examined. Use for examination only. If the operation description is modified, the results are undefined.</param>
        public void Validate(OperationDescription operationDescription)
        {
            // intentionally blank
        }

        /// <summary>
        /// Class used to perform STA invocation as forced (and used) by the STAOperationAttribute class
        /// </summary>
        private class STAInvoker : IOperationInvoker
        {
            private IOperationInvoker InnerOperationInvoker { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="STAInvoker"/> class.
            /// </summary>
            /// <param name="operationInvoker">The operation invoker.</param>
            public STAInvoker(IOperationInvoker operationInvoker)
            {
                InnerOperationInvoker = operationInvoker;
            }

            /// <summary>
            /// Returns an <see cref="T:System.Array"/> of parameter objects.
            /// </summary>
            /// <returns>
            /// The parameters that are to be used as arguments to the operation.
            /// </returns>
            public object[] AllocateInputs()
            {
                return InnerOperationInvoker.AllocateInputs();
            }

            /// <summary>
            /// Used as a single worker thread for all the STA operations.
            /// </summary>
            private static Thread _workerThread;

            /// <summary>
            /// Stack of operations to be executed
            /// </summary>
            private static Stack<Action> _pooledActions;
            /// <summary>
            /// Dictionary of results created by the STA actions.
            /// </summary>
            private static Dictionary<Guid, object> _pooledResults;

            /// <summary>
            /// Returns an object and a set of output objects from an instance and set of input objects.
            /// </summary>
            /// <param name="instance">The object to be invoked.</param>
            /// <param name="inputs">The inputs to the method.</param>
            /// <param name="outputs">The outputs from the method.</param>
            /// <returns>The return value.</returns>
            public object Invoke(object instance, object[] inputs, out object[] outputs)
            {
                object result = null;
                var context = OperationContext.Current;

                if (_pooledActions == null) _pooledActions = new Stack<Action>();
                if (_pooledResults == null) _pooledResults = new Dictionary<Guid, object>();

                var internalId = Guid.NewGuid(); // Used as an internal ID to match potentially multiple background operations creating results with the desired request

                if (_workerThread == null)
                {
                    _workerThread = new Thread(StartSTAPool)
                    {
                        Name = "CODE Framework STA WCF Invoker Thread",
                        IsBackground = true,
                        Priority = ThreadPriority.Lowest
                    };
                    _workerThread.SetApartmentState(ApartmentState.STA);
                    _workerThread.Start();
                }

                lock (_pooledActions)
                    _pooledActions.Push(delegate
                    {
                        using (new OperationContextScope(context))
                        {
                            object[] staOutputs;
                            result = InnerOperationInvoker.Invoke(instance, inputs, out staOutputs);
                            lock (_pooledResults)
                                _pooledResults.Add(internalId, result);
                        }
                    });

                _workerThread.Priority = ThreadPriority.Normal;

                while (!IsResultReady(internalId)) { } // Polling for the desired result
                outputs = new[] { GetResult(internalId) };

                return result;
            }

            /// <summary>
            /// Checks the result pool to see if the result is ready yet
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            private bool IsResultReady(Guid id)
            {
                lock (_pooledResults)
                    return _pooledResults.ContainsKey(id);
            }

            /// <summary>
            /// Retrives the result form the result pool
            /// </summary>
            /// <param name="id">Identifies the specific result set we are interested in</param>
            /// <returns>Array of result objects, or null (if result is not present)</returns>
            private object GetResult(Guid id)
            {
                lock (_pooledResults)
                    if (_pooledResults.ContainsKey(id))
                    {
                        var results = _pooledResults[id];
                        _pooledResults.Remove(id);
                        return results;
                    }
                return null;
            }

            /// <summary>
            /// Simple loop that checks for actions to be executed within this simple thread pool
            /// </summary>
            private void StartSTAPool()
            {
                while (true)
                {
                    Action action = null;
                    lock (_pooledActions)
                        if (_pooledActions.Count > 0)
                            action = _pooledActions.Pop();
                    if (action != null) action();

                    bool mustSuspend = false;
                    lock (_pooledActions)
                        if (_pooledActions.Count == 0)
                            mustSuspend = true;
                    if (mustSuspend)
                    {
                        _workerThread.Priority = ThreadPriority.Lowest;
                        Thread.Sleep(50);
                    }
                }
                // Note: This funciton never returns, which is OK since it is designed to run on a background thread,
                //       which will eventually be aborted and also be suspended and resumed
            }

            /// <summary>
            /// An asynchronous implementation of the <see cref="M:System.ServiceModel.Dispatcher.IOperationInvoker.Invoke(System.Object,System.Object[],System.Object[]@)"/> method.
            /// </summary>
            /// <param name="instance">The object to be invoked.</param>
            /// <param name="inputs">The inputs to the method.</param>
            /// <param name="callback">The asynchronous callback object.</param>
            /// <param name="state">Associated state data.</param>
            /// <returns>
            /// A <see cref="T:System.IAsyncResult"/> used to complete the asynchronous call.
            /// </returns>
            public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
            {
                return InnerOperationInvoker.InvokeBegin(instance, inputs, callback, state);
            }

            /// <summary>
            /// The asynchronous end method.
            /// </summary>
            /// <param name="instance">The object invoked.</param>
            /// <param name="outputs">The outputs from the method.</param>
            /// <param name="result">The <see cref="T:System.IAsyncResult"/> object.</param>
            /// <returns>The return value.</returns>
            public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
            {
                return InnerOperationInvoker.InvokeEnd(instance, out outputs, result);
            }

            /// <summary>
            /// Gets a value that specifies whether the <see cref="M:System.ServiceModel.Dispatcher.IOperationInvoker.Invoke(System.Object,System.Object[],System.Object[]@)"/> or <see cref="M:System.ServiceModel.Dispatcher.IOperationInvoker.InvokeBegin(System.Object,System.Object[],System.AsyncCallback,System.Object)"/> method is called by the dispatcher.
            /// </summary>
            /// <value></value>
            /// <returns>true if the dispatcher invokes the synchronous operation; otherwise, false.</returns>
            public bool IsSynchronous
            {
                get { return InnerOperationInvoker.IsSynchronous; }
            }
        }
    }
}