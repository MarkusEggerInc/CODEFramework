using System;
using System.Windows;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for AsyncTest.xaml
    /// </summary>
    public partial class AsyncTest : Window
    {
        public AsyncTest()
        {
            InitializeComponent();

            var taskId = Guid.NewGuid();
            AsyncWorker.Execute(() => DateTime.Now.ToString(), time => display.Text += time + "\r\n", new TimeSpan(0, 0, 0, 5), processId: taskId);
            AsyncWorker.Execute(() => "Hello! " + Environment.TickCount.ToString(), time => display.Text += time + "\r\n", new TimeSpan(0, 0, 0, 2));

            AsyncWorker.PauseContinuousProcess(taskId);
            AsyncWorker.ResumeContinuousProcess(taskId);
            AsyncWorker.StopContinuousProcess(taskId);
        }
    }
}
