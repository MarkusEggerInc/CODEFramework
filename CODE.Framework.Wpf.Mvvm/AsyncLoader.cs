﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// This class provides async loading behavior useful to view models
    /// </summary>
    public static class AsyncWorker
    {
        /// <summary>
        /// Initializes static members of the <see cref="AsyncWorker"/> class.
        /// </summary>
        static AsyncWorker()
        {
            AttemptToHandleBackgroundExceptions = false;
            AssignDefaultValuesToDefaultObjectByConvention = true;
        }

        /// <summary>
        /// If set to true, the async worker tries to automatically handle exceptions thrown 
        /// by the background operation, and returns a default object to the foreground method
        /// </summary>
        /// <value><c>true</c> if [attempt to handle background exceptions]; otherwise, <c>false</c>.</value>
        public static bool AttemptToHandleBackgroundExceptions { get; set; }

        /// <summary>
        /// Defines whether a default object created by automatic background exception handling 
        /// is populated with Success = false, and FailureInformation = ex.Message
        /// </summary>
        /// <remarks>Only applicable if AttemptToHandleBackgroundExceptions = true</remarks>
        public static bool AssignDefaultValuesToDefaultObjectByConvention { get; set; }

        /// <summary>
        /// Executes the method provided by the first parameter (on a background thread) and passes the result to the second method (on the current thread)
        /// </summary>
        /// <typeparam name="TResult">The result produced by the first method, which is to be passed to the second method</typeparam>
        /// <param name="worker">The method used to perform the operation</param>
        /// <param name="completeMethod">The load complete method.</param>
        /// <param name="statusObject">(Optional) status object that can have its status automatically updated (on the foreground thread)</param>
        /// <param name="operationStatus">The status the optional loader is set to while the operation is in progress</param>
        public static void Execute<TResult>(Func<TResult> worker, Action<TResult> completeMethod, IModelStatus statusObject = null, ModelStatus operationStatus = ModelStatus.Loading)
        {
            SetModelStatusThreadSafe(statusObject, operationStatus, 1);

            worker.BeginInvoke(ar =>
                {
                    try
                    {
                        var result = worker.EndInvoke(ar);
                        Application.Current.Dispatcher.BeginInvoke(new Action<Action<TResult>, TResult, IModelStatus>(ExecuteDispatch), new object[] {completeMethod, result, statusObject});
                    }
                    catch (Exception ex)
                    {
                        if (!AttemptToHandleBackgroundExceptions) throw;

                        try
                        {
                            // We attempt to create a default instance of the expected object since the operation failed
                            var defaultInstance = Activator.CreateInstance<TResult>();

                            // We see if we can assign some default values by convention
                            if (AssignDefaultValuesToDefaultObjectByConvention)
                            {
                                var instanceType = defaultInstance.GetType();
                                var successProperty = instanceType.GetProperty("Success");
                                if (successProperty != null) successProperty.SetValue(defaultInstance, false, null);
                                var failureInformationProperty = instanceType.GetProperty("Failure Information");
                                if (failureInformationProperty != null) failureInformationProperty.SetValue(defaultInstance, ex.Message, null);
                            }

                            Application.Current.Dispatcher.BeginInvoke(new Action<Action<TResult>, TResult, IModelStatus>(ExecuteDispatch), new object[] {completeMethod, defaultInstance, statusObject});
                        }
                        catch (Exception)
                        {
                            // Well, now everything failed, so we re-throw the original exception (not the new one!) after all
                            throw ex;
                        }
                    }

                }, null);
        }

        /// <summary>
        /// Executes all the worker delegates on a background thread, waits for them all to complete, and then  passes the result to the complete method (on the original foreground thread)
        /// </summary>
        /// <typeparam name="TResult">The result produced by the first worker method, which is to be passed to the complete method as the first parameter</typeparam>
        /// <typeparam name="TResult2">The result produced by the second worker method, which is to be passed to the complete method as the second parameter</typeparam>
        /// <param name="worker">The first method used to perform the operation</param>
        /// <param name="worker2">The second method used to perform the operation</param>
        /// <param name="completeMethod">Method to fire when all workers have completed.</param>
        /// <param name="statusObject">(Optional) status object that can have its status automatically updated (on the foreground thread)</param>
        /// <param name="operationStatus">The status the optional loader is set to while the operation is in progress</param>
        public static void Execute<TResult, TResult2>(Func<TResult> worker, Func<TResult2> worker2, Action<TResult, TResult2> completeMethod, IModelStatus statusObject = null, ModelStatus operationStatus = ModelStatus.Loading)
        {
            SetModelStatusThreadSafe(statusObject, operationStatus, 1);

            Execute(() =>
                {
                    var state1 = new MultiExecuteState<TResult>(worker, new ManualResetEvent(false));
                    var state2 = new MultiExecuteState<TResult2>(worker2, new ManualResetEvent(false));
                    var eventArray = new WaitHandle[] {state1.ResetEvent, state2.ResetEvent};

                    QueueWorkerForExecution(state1);
                    QueueWorkerForExecution(state2);

                    WaitHandle.WaitAll(eventArray);

                    return new {Result1 = state1.Result, Result2 = state2.Result};
                }, r =>
                    {
                        completeMethod(r.Result1, r.Result2);
                        SetModelStatusThreadSafe(statusObject, ModelStatus.NotApplicable, -1);
                    });
        }

        /// <summary>
        /// Executes all the worker delegates on a background thread, waits for them all to complete, and then  passes the result to the complete method (on the original foreground thread)
        /// </summary>
        /// <typeparam name="TResult">The result produced by the first worker method, which is to be passed to the complete method as the first parameter</typeparam>
        /// <typeparam name="TResult2">The result produced by the second worker method, which is to be passed to the complete method as the second parameter</typeparam>
        /// <typeparam name="TResult3">The result produced by the third worker method, which is to be passed to the complete method as the third parameter</typeparam>
        /// <param name="worker">The first method used to perform the operation</param>
        /// <param name="worker2">The second method used to perform the operation</param>
        /// <param name="worker3">The third method used to perform the operation</param>
        /// <param name="completeMethod">Method to fire when all workers have completed.</param>
        /// <param name="statusObject">(Optional) status object that can have its status automatically updated (on the foreground thread)</param>
        /// <param name="operationStatus">The status the optional loader is set to while the operation is in progress</param>
        public static void Execute<TResult, TResult2, TResult3>(Func<TResult> worker, Func<TResult2> worker2, Func<TResult3> worker3, Action<TResult, TResult2, TResult3> completeMethod, IModelStatus statusObject = null, ModelStatus operationStatus = ModelStatus.Loading)
        {
            SetModelStatusThreadSafe(statusObject, operationStatus, 1);

            Execute(() =>
                {
                    var state1 = new MultiExecuteState<TResult>(worker, new ManualResetEvent(false));
                    var state2 = new MultiExecuteState<TResult2>(worker2, new ManualResetEvent(false));
                    var state3 = new MultiExecuteState<TResult3>(worker3, new ManualResetEvent(false));
                    var eventArray = new WaitHandle[] {state1.ResetEvent, state2.ResetEvent, state3.ResetEvent};

                    QueueWorkerForExecution(state1);
                    QueueWorkerForExecution(state2);
                    QueueWorkerForExecution(state3);

                    WaitHandle.WaitAll(eventArray);

                    return new {Result1 = state1.Result, Result2 = state2.Result, Result3 = state3.Result};
                }, r =>
                    {
                        completeMethod(r.Result1, r.Result2, r.Result3);
                        SetModelStatusThreadSafe(statusObject, ModelStatus.NotApplicable, -1);
                    });
        }

        /// <summary>
        /// Executes all the worker delegates on a background thread, waits for them all to complete, and then  passes the result to the complete method (on the original foreground thread)
        /// </summary>
        /// <typeparam name="TResult">The result produced by the first worker method, which is to be passed to the complete method as the first parameter</typeparam>
        /// <typeparam name="TResult2">The result produced by the second worker method, which is to be passed to the complete method as the second parameter</typeparam>
        /// <typeparam name="TResult3">The result produced by the third worker method, which is to be passed to the complete method as the third parameter</typeparam>
        /// <typeparam name="TResult4">The result produced by the fourth worker method, which is to be passed to the complete method as the fourth parameter</typeparam>
        /// <param name="worker">The first method used to perform the operation</param>
        /// <param name="worker2">The second method used to perform the operation</param>
        /// <param name="worker3">The third method used to perform the operation</param>
        /// <param name="worker4">The fourth method used to perform the operation</param>
        /// <param name="completeMethod">Method to fire when all workers have completed.</param>
        /// <param name="statusObject">(Optional) status object that can have its status automatically updated (on the foreground thread)</param>
        /// <param name="operationStatus">The status the optional loader is set to while the operation is in progress</param>
        public static void Execute<TResult, TResult2, TResult3, TResult4>(Func<TResult> worker, Func<TResult2> worker2, Func<TResult3> worker3, Func<TResult4> worker4, Action<TResult, TResult2, TResult3, TResult4> completeMethod, IModelStatus statusObject = null, ModelStatus operationStatus = ModelStatus.Loading)
        {
            SetModelStatusThreadSafe(statusObject, operationStatus, 1);

            Execute(() =>
                {
                    var state1 = new MultiExecuteState<TResult>(worker, new ManualResetEvent(false));
                    var state2 = new MultiExecuteState<TResult2>(worker2, new ManualResetEvent(false));
                    var state3 = new MultiExecuteState<TResult3>(worker3, new ManualResetEvent(false));
                    var state4 = new MultiExecuteState<TResult4>(worker4, new ManualResetEvent(false));
                    var eventArray = new WaitHandle[] {state1.ResetEvent, state2.ResetEvent, state3.ResetEvent, state4.ResetEvent};

                    QueueWorkerForExecution(state1);
                    QueueWorkerForExecution(state2);
                    QueueWorkerForExecution(state3);
                    QueueWorkerForExecution(state4);

                    WaitHandle.WaitAll(eventArray);

                    return new {Result1 = state1.Result, Result2 = state2.Result, Result3 = state3.Result, Result4 = state4.Result};
                }, r =>
                    {
                        completeMethod(r.Result1, r.Result2, r.Result3, r.Result4);
                        SetModelStatusThreadSafe(statusObject, ModelStatus.NotApplicable, -1);
                    });
        }

        /// <summary>
        /// Executes all the worker delegates on a background thread, waits for them all to complete, and then  passes the result to the complete method (on the original foreground thread)
        /// </summary>
        /// <typeparam name="TResult">The result produced by the first worker method, which is to be passed to the complete method as the first parameter</typeparam>
        /// <typeparam name="TResult2">The result produced by the second worker method, which is to be passed to the complete method as the second parameter</typeparam>
        /// <typeparam name="TResult3">The result produced by the third worker method, which is to be passed to the complete method as the third parameter</typeparam>
        /// <typeparam name="TResult4">The result produced by the fourth worker method, which is to be passed to the complete method as the fourth parameter</typeparam>
        /// <typeparam name="TResult5">The result produced by the fourth worker method, which is to be passed to the complete method as the fourth parameter</typeparam>
        /// <param name="worker">The first method used to perform the operation</param>
        /// <param name="worker2">The second method used to perform the operation</param>
        /// <param name="worker3">The third method used to perform the operation</param>
        /// <param name="worker4">The fourth method used to perform the operation</param>
        /// <param name="worker5">The fourth method used to perform the operation</param>
        /// <param name="completeMethod">Method to fire when all workers have completed.</param>
        /// <param name="statusObject">(Optional) status object that can have its status automatically updated (on the foreground thread)</param>
        /// <param name="operationStatus">The status the optional loader is set to while the operation is in progress</param>
        public static void Execute<TResult, TResult2, TResult3, TResult4, TResult5>(Func<TResult> worker, Func<TResult2> worker2, Func<TResult3> worker3, Func<TResult4> worker4, Func<TResult5> worker5, Action<TResult, TResult2, TResult3, TResult4, TResult5> completeMethod, IModelStatus statusObject = null, ModelStatus operationStatus = ModelStatus.Loading)
        {
            SetModelStatusThreadSafe(statusObject, operationStatus, 1);

            Execute(() =>
                {
                    var state1 = new MultiExecuteState<TResult>(worker, new ManualResetEvent(false));
                    var state2 = new MultiExecuteState<TResult2>(worker2, new ManualResetEvent(false));
                    var state3 = new MultiExecuteState<TResult3>(worker3, new ManualResetEvent(false));
                    var state4 = new MultiExecuteState<TResult4>(worker4, new ManualResetEvent(false));
                    var state5 = new MultiExecuteState<TResult5>(worker5, new ManualResetEvent(false));
                    var eventArray = new WaitHandle[] {state1.ResetEvent, state2.ResetEvent, state3.ResetEvent, state4.ResetEvent, state5.ResetEvent};

                    QueueWorkerForExecution(state1);
                    QueueWorkerForExecution(state2);
                    QueueWorkerForExecution(state3);
                    QueueWorkerForExecution(state4);
                    QueueWorkerForExecution(state5);

                    WaitHandle.WaitAll(eventArray);

                    return new {Result1 = state1.Result, Result2 = state2.Result, Result3 = state3.Result, Result4 = state4.Result, Result5 = state5.Result};
                }, r =>
                    {
                        completeMethod(r.Result1, r.Result2, r.Result3, r.Result4, r.Result5);
                        SetModelStatusThreadSafe(statusObject, ModelStatus.NotApplicable, -1);
                    });
        }

        private static void QueueWorkerForExecution<TResult>(MultiExecuteState<TResult> multiExecuteState)
        {
            ThreadPool.QueueUserWorkItem(state =>
                {
                    var realState = state as MultiExecuteState<TResult>;
                    if (realState == null) return;
                    realState.Result = realState.Worker();
                    realState.ResetEvent.Set();
                }, multiExecuteState);
        }

        /// <summary>Performs exacution of the loaded method back on the main thread and resets the status if need be</summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="completeMethod">The complete method.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="statusObject">The status object.</param>
        private static void ExecuteDispatch<TResult>(Action<TResult> completeMethod, TResult parameter, IModelStatus statusObject)
        {
            completeMethod(parameter);
            SetModelStatusThreadSafe(statusObject, ModelStatus.NotApplicable, -1);
        }

        private static Thread _backgroundProcess;
        private static Dictionary<Guid, SchedulerItem> _intervalProcessess;

        private static void SchedulerLoop()
        {
            while (true)
            {
                var internalValues = new List<SchedulerItem>(_intervalProcessess.Values.Count);
                lock (_intervalProcessess)
                    internalValues.AddRange(_intervalProcessess.Values);

                foreach (var item in internalValues)
                    if (item.MustRun())
                        item.Run();
                Thread.Sleep(100); // For performance reasons, we pause the thread for 1/10th of a second
            }
        }

        /// <summary>Pauses an existing continuous process</summary>
        /// <param name="processId">The ID of the process that is to be paused</param>
        /// <returns>True if a previously running process is paused</returns>
        /// <remarks>Also returns true of the process has been previously paused</remarks>
        public static bool PauseContinuousProcess(Guid processId)
        {
            if (_intervalProcessess == null) return false;
            if (_intervalProcessess.ContainsKey(processId))
            {
                _intervalProcessess[processId].IsPaused = true;
                return true;
            }
            return false;
        }

        /// <summary>Pauses an existing continuous process</summary>
        /// <param name="processId">The ID of the process that is to be resumed</param>
        /// <returns>True if the process is resumed</returns>
        /// <remarks>Also returns true of the process has previously been running</remarks>
        public static bool ResumeContinuousProcess(Guid processId)
        {
            if (_intervalProcessess == null) return false;
            if (_intervalProcessess.ContainsKey(processId))
            {
                _intervalProcessess[processId].IsPaused = false;
                return true;
            }
            return false;
        }

        /// <summary>Queues an existing continuous process to run immediately</summary>
        /// <param name="processId">The ID of the process that is to be triggered immediately</param>
        public static bool TriggerContinuousProcess(Guid processId)
        {
            if (_intervalProcessess == null) return false;
            if (_intervalProcessess.ContainsKey(processId))
            {
                _intervalProcessess[processId].QueueImmediately();
                return true;
            }
            return false;
        }
        
        /// <summary>Stops (removes) an existing continuous process</summary>
        /// <param name="processId">The ID of the process that is to be stopped</param>
        /// <returns>True if a previously running process is stopped</returns>
        public static bool StopContinuousProcess(Guid processId)
        {
            if (_intervalProcessess == null) return false;
            if (_intervalProcessess.ContainsKey(processId))
            {
                lock (_intervalProcessess)
                    _intervalProcessess.Remove(processId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Continuously executes the specified worker on the scheduled interval
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="worker">The method executed on a background thread.</param>
        /// <param name="completeMethod">The method executed on the UI thread</param>
        /// <param name="interval">The interval at which the method runs (should not be smaller than 250ms)</param>
        /// <param name="statusObject">An object that has its status updated every time the process runs (typically a view model or similar object)</param>
        /// <param name="operationStatus">The status that is to be set on the status object while the method runs.</param>
        /// <param name="processId">The ID of the continuously running process (can be used to later to pause, resume, or stop a process)</param>
        /// <param name="threadPriority">The thread priority for the background thread (default == BelowNormal).</param>
        public static void Execute<TResult>(Func<TResult> worker, Action<TResult> completeMethod, TimeSpan interval, IModelStatus statusObject = null, ModelStatus operationStatus = ModelStatus.Loading, Guid processId = new Guid(), ThreadPriority threadPriority = ThreadPriority.BelowNormal)
        {
            if (processId == Guid.Empty) processId = Guid.NewGuid();

            if (_backgroundProcess == null)
            {
                _intervalProcessess = new Dictionary<Guid, SchedulerItem>();
                _backgroundProcess = new Thread(SchedulerLoop) {IsBackground = true, Priority = threadPriority};
                _backgroundProcess.Start();
            }

            lock (_intervalProcessess)
                _intervalProcessess.Add(processId, new SchedulerItem
                    {
                        Interval = interval,
                        RunMethods = () => Execute(worker, completeMethod, statusObject, operationStatus)
                    });
        }

        /// <summary>
        /// Sets the model status in a thread safe fashion on the UI thread.
        /// </summary>
        /// <param name="statusObject">The status object.</param>
        /// <param name="status">The status.</param>
        /// <param name="operationCount">The operation count (usually -1, 0, or 1 depending on whether the count is to be increased, decreased, or left alone).</param>
        /// <param name="autoSetReadyStatus">if set to <c>true</c> the status is automatically set to Ready whenever the operation count reaches 0.</param>
        private static void SetModelStatusThreadSafe(IModelStatus statusObject, ModelStatus status, int operationCount, bool autoSetReadyStatus = true)
        {
            if (statusObject == null) return;

            Application.Current.Dispatcher.BeginInvoke(new Action<IModelStatus, ModelStatus, int, bool>((statusObject2, status2, operationCount2, autoSetReadyStatus2) =>
                {
                    lock (statusObject2)
                    {
                        if (status2 != ModelStatus.NotApplicable)
                            statusObject2.ModelStatus = status2;
                        if (operationCount2 != 0)
                            statusObject2.OperationsInProgress += operationCount2;
                        if (autoSetReadyStatus2 && statusObject2.OperationsInProgress == 0)
                            statusObject2.ModelStatus = ModelStatus.Ready;
                    }
                }), new object[] { statusObject, status, operationCount, autoSetReadyStatus });
        }

        private class SchedulerItem
        {
            public SchedulerItem()
            {
                LastRun = DateTime.MinValue;
            }

            public TimeSpan Interval { get; set; }
            private DateTime LastRun { get; set; }
            public bool IsPaused { get; set; }

            public Action RunMethods { private get; set; }

            public void Run()
            {
                if (RunMethods == null) return;

                RunMethods();

                lock (this)
                    LastRun = DateTime.Now;
            }

            public bool MustRun()
            {
                lock (this)
                {
                    if (IsPaused) return false;
                    if (LastRun == DateTime.MinValue) return true;
                    if ((LastRun + Interval) < DateTime.Now) return true;
                }
                return false;
            }

            public void QueueImmediately()
            {
                lock (this)
                    LastRun = DateTime.MinValue;
            }
        }
    }

    /// <summary>
    /// For internal use only
    /// </summary>
    /// <typeparam name="TResult">The type of the t result.</typeparam>
    public class MultiExecuteState<TResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiExecuteState{TResult}"/> class.
        /// </summary>
        /// <param name="worker">The worker.</param>
        /// <param name="manualResetEvent">The manual reset event.</param>
        public MultiExecuteState(Func<TResult> worker, ManualResetEvent manualResetEvent)
        {
            Worker = worker;
            ResetEvent = manualResetEvent;
        }
        /// <summary>
        /// Background worker delegate.
        /// </summary>
        /// <value>The worker.</value>
        public Func<TResult> Worker { get; private set; }
        /// <summary>
        /// Manual reset event to enable wait-all
        /// </summary>
        /// <value>The reset event.</value>
        public ManualResetEvent ResetEvent { get; private set; }
        /// <summary>
        /// Result produced by the worker
        /// </summary>
        /// <value>The result.</value>
        public TResult Result { get; set; }
    }
}