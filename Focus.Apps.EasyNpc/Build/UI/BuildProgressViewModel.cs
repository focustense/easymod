using Focus.Apps.EasyNpc.Build.Pipeline;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;

namespace Focus.Apps.EasyNpc.Build.UI
{
    public class BuildProgressViewModel<TResult> : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public delegate BuildProgressViewModel<TResult> Factory(IBuildProgress<TResult> model);

        public Task<TResult> Outcome => model.Outcome;
        public ObservableCollection<BuildTaskViewModel> Tasks { get; private init; } = new();

        private readonly Subject<bool> disposed = new();
        private readonly IBuildProgress<TResult> model;

        public BuildProgressViewModel(BuildTaskViewModel.Factory taskViewModelFactory, IBuildProgress<TResult> model)
        {
            this.model = model;

            model.Tasks
                .TakeUntil(disposed)
                .ObserveOn(Application.Current.Dispatcher)
                .Subscribe(t => Tasks.Add(taskViewModelFactory(t)));
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
