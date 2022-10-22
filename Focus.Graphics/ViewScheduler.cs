using Silk.NET.Windowing;
using System.Collections.Concurrent;

namespace Focus.Graphics
{
    public class ViewScheduler : IScheduler, IDisposable
    {
        class ScheduledAction
        {
            private readonly Func<object?> action;

            public object? ReturnValue { get; private set; } = null;

            public ScheduledAction(Func<object?> action)
            {
                this.action = action;
            }

            public void Run()
            {
                ReturnValue = action();
            }
        }

        private readonly IView view;
        private readonly int dispatcherThreadId;
        private readonly ConcurrentQueue<ScheduledAction> scheduledActions = new();
        private readonly ManualResetEventSlim actionsFinished = new();

        public ViewScheduler(IView view)
        {
            this.view = view;
            dispatcherThreadId = Environment.CurrentManagedThreadId;
            view.Update += OnViewUpdate;
        }

        public void Dispose()
        {
            view.Update -= OnViewUpdate;
            GC.SuppressFinalize(this);
        }

        public void Run(Action action)
        {
            Run(VoidAction(action));
        }

        public T Run<T>(Func<T> action)
        {
            if (Environment.CurrentManagedThreadId == dispatcherThreadId)
                // Waiting on the update thread may deadlock.
                return action();

            var scheduledAction = new ScheduledAction(() => action());
            // TODO: Investigate this carefully for potential deadlocks or race conditions.
            actionsFinished.Reset();
            scheduledActions.Enqueue(scheduledAction);
            actionsFinished.Wait();
            return (T)scheduledAction.ReturnValue!;
        }

        private void OnViewUpdate(double _)
        {
            while (scheduledActions.TryDequeue(out var scheduledAction))
                scheduledAction.Run();
            actionsFinished.Set();
        }

        private static Func<object?> VoidAction(Action action)
        {
            return () =>
            {
                action();
                return null;
            };
        }
    }
}
