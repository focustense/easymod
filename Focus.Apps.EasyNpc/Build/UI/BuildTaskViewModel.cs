using Focus.Apps.EasyNpc.Build.Pipeline;
using PropertyChanged;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Focus.Apps.EasyNpc.Build.UI
{
    public class BuildTaskViewModel : INotifyPropertyChanged, IDisposable
    {
        public delegate BuildTaskViewModel Factory(IBuildTask buildTask);

        public event PropertyChangedEventHandler? PropertyChanged;

        public int CurrentProgress { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;
        [DependsOn(nameof(MaxProgress), nameof(State))]
        public bool IsIndeterminate => MaxProgress == 0 && !IsEnded();
        public string ItemName { get; private set; } = string.Empty;
        public int MaxProgress { get; private set; }
        public int MinProgress { get; } = 0;
        public string Name => buildTask.Name;
        public BuildTaskState State { get; private set; } = BuildTaskState.NotStarted;

        private readonly IBuildTask buildTask;
        private readonly Subject<bool> disposed = new();

        public BuildTaskViewModel(IBuildTask buildTask)
        {
            this.buildTask = buildTask;
            buildTask.ItemCount.TakeUntil(disposed).Subscribe(count => MaxProgress = count);
            buildTask.ItemIndex.TakeUntil(disposed).Subscribe(index => CurrentProgress = index);
            buildTask.ItemName.TakeUntil(disposed).Subscribe(name => ItemName = name);
            buildTask.State.TakeUntil(disposed).Subscribe(state =>
            {
                State = state;
                if (state == BuildTaskState.Failed)
                    ErrorMessage = buildTask.Exception?.Message ?? "Unknown error";
                if (state == BuildTaskState.Completed && MaxProgress == 0)
                {
                    MaxProgress = 1;
                    CurrentProgress = 1;
                }
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed.IsDisposed)
                return;
            disposed.OnNext(true);
            disposed.Dispose();
        }

        private bool IsEnded()
        {
            return
                State == BuildTaskState.Cancelled || State == BuildTaskState.Completed ||
                State == BuildTaskState.Failed;
        }
    }
}
