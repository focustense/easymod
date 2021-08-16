using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public enum BuildTaskState { NotStarted, Running, Paused, Cancelled, Failed, Completed }

    public interface IBuildTask
    {
        Exception? Exception { get; }
        IObservable<int> ItemCount { get; }
        IObservable<int> ItemIndex { get; }
        IObservable<string> ItemName { get; }
        string Name { get; }
        IObservable<BuildTaskState> State { get; }
    }

    public interface ICancellableBuildTask : IBuildTask
    {
        void Cancel();
    }

    public interface IBuildTask<TResult> : ICancellableBuildTask, IDisposable
    {
        Task<TResult> Start(BuildSettings settings);
    }

    public interface INameable
    {
        string Name { get; set; }
    }

    public abstract class BuildTask<TResult> : IBuildTask<TResult>, INameable
    {
        public Exception? Exception { get; private set; }
        public string Name { get; set; } = string.Empty;

        protected BehaviorSubject<int> ItemCount { get; } = new(0);
        protected BehaviorSubject<int> ItemIndex { get; } = new(0);
        protected BehaviorSubject<string> ItemName = new(string.Empty);
        protected BehaviorSubject<BuildTaskState> State = new(BuildTaskState.NotStarted);

        protected CancellationToken CancellationToken => cancellationTokenSource.Token;

        IObservable<int> IBuildTask.ItemCount => ItemCount;
        IObservable<int> IBuildTask.ItemIndex => ItemIndex;
        IObservable<string> IBuildTask.ItemName => ItemName;
        IObservable<BuildTaskState> IBuildTask.State => State;

        private readonly CancellationTokenSource cancellationTokenSource = new();

        private bool isDisposed;

        public void Cancel()
        {
            if (!isDisposed)
                cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<TResult> Start(BuildSettings settings)
        {
            State.OnNext(BuildTaskState.Running);
            try
            {
                CancellationToken.ThrowIfCancellationRequested();
                TResult result;
                try
                {
                    result = await Run(settings);
                }
                // Unwrapping the AggregateException here, as opposed to the outer exception handlers, allows us to
                // reuse those handlers without having to write a special one for the Aggregate case.
                catch (AggregateException ex)
                {
                    throw ex.InnerExceptions[0];
                }
                State.OnNext(BuildTaskState.Completed);
                return result;
            }
            catch (TaskCanceledException)
            {
                State.OnNext(BuildTaskState.Cancelled);
                throw;
            }
            catch (OperationCanceledException)
            {
                State.OnNext(BuildTaskState.Cancelled);
                throw;
            }
            catch (Exception ex)
            {
                Exception = ex;
                State.OnNext(BuildTaskState.Failed);
                throw;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;
            if (disposing)
            {
                cancellationTokenSource.Dispose();

                ItemCount.OnCompleted();
                ItemIndex.OnCompleted();
                ItemName.OnCompleted();
                State.OnCompleted();

                ItemCount.Dispose();
                ItemIndex.Dispose();
                ItemName.Dispose();
                State.Dispose();
            }
            isDisposed = true;
        }

        protected void NextItem(string name, int increment = 1)
        {
            ItemName.OnNext(name);
            if (increment > 0)
                ItemIndex.OnNext(ItemIndex.Value + increment);
        }

        protected void NextItemSync(string name)
        {
            NextItemSync(name, 1);
        }

        protected void NextItemSync(string name, int increment)
        {
            // Probably "recommended" to use Subject.Synchronize than explicit lock, but that doesn't give us the
            // ability to do atomic increment of the item index.
            lock (ItemName)
                NextItem(name, increment);
        }

        protected abstract Task<TResult> Run(BuildSettings settings);

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Intentional no-op")]
        protected void RunsAfter(params object[] results)
        {
            // This method is an intentional no-op. It is provided as a convenience for tasks that need to declare an
            // "order" dependency on some other task's result, but don't actually do anything with that result. This is
            // primarily oriented around mutable state - for example, saving a patch after all subtasks have written to
            // the patch. The only dependency is the patch itself, which was created at the beginning, but it cannot run
            // until all patchers have finished with it.
        }
    }
}
