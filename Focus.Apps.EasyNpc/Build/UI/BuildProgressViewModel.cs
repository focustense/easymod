using Focus.Apps.EasyNpc.Build.Pipeline;
using PropertyChanged;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;

namespace Focus.Apps.EasyNpc.Build.UI
{
    [AddINotifyPropertyChangedInterface]
    public class BuildProgressViewModel<TResult> : IDisposable
    {
        public delegate BuildProgressViewModel<TResult> Factory(IBuildProgress<TResult> model);

        [DependsOn(nameof(RemainingTaskNames))]
        public bool HasRemainingTasks => RemainingTaskNames.Count > 0;
        public Task<TResult> Outcome => model.Outcome;
        public ObservableCollection<string> RemainingTaskNames { get; private init; } = new();
        public ObservableCollection<BuildTaskViewModel> Tasks { get; private init; } = new();

        private readonly Subject<bool> disposed = new();
        private readonly IBuildProgress<TResult> model;

        public BuildProgressViewModel(BuildTaskViewModel.Factory taskViewModelFactory, IBuildProgress<TResult> model)
        {
            this.model = model;
            RemainingTaskNames = new(model.AllTaskNames);

            model.Tasks
                .TakeUntil(disposed)
                .ObserveOn(Application.Current.Dispatcher)
                .Subscribe(t =>
                {
                    Tasks.Add(taskViewModelFactory(t));
                    RemainingTaskNames.Remove(t.Name);
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
    }
}
