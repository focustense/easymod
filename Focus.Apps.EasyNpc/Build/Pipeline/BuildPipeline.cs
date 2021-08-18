using Autofac;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    public interface IBuildPipeline<TSettings, TResult>
    {
        IBuildProgress<TResult> Start(TSettings settings);
    }

    public interface IBuildProgress<TResult>
    {
        IReadOnlyList<string> AllTaskNames { get; }
        Task<TResult> Outcome { get; }
        IObservable<IBuildTask> Tasks { get; }

        void Cancel();
    }

    record TaskRegistration(string Name, Delegate Factory, Type[] ArgumentTypes, Type ResultType);

    public class BuildPipelineConfiguration<TResult>
    {
        private readonly ILifetimeScope container;
        private readonly ILogger log;
        private readonly List<TaskRegistration> registrations = new();

        public BuildPipelineConfiguration(ILifetimeScope container, ILogger log)
        {
            this.container = container;
            this.log = log;
        }

        public IBuildPipeline<TSettings, TResult> CreatePipeline<TSettings>()
        {
            Validate();
            var contextLog = log.ForContext<BuildPipeline<TSettings, TResult>>();
            return new BuildPipeline<TSettings, TResult>(container, contextLog, registrations);
        }

        public BuildPipelineConfiguration<TResult> RegisterTask<T>(string name)
            where T : Delegate
        {
            var invokeMethod = typeof(T).GetMethod("Invoke");
            if (invokeMethod is null)
                throw new ArgumentException($"Delegate {typeof(T).FullName} does not have an Invoke method.", nameof(T));
            var resultType = GetResultType(invokeMethod);
            if (resultType is null)
                throw new ArgumentException(
                    $"Delegate {typeof(T).FullName} does not return a type of {nameof(IBuildTask<T>)}.", nameof(T));
            var argumentTypes = invokeMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            var factory = (Delegate)container.Resolve(typeof(T));
            registrations.Add(new(name, factory, argumentTypes, resultType));
            return this;
        }

        private void Validate()
        {
            var resultTypesToFactoryTypes = registrations
                .GroupBy(x => x.ResultType, x => x.Factory.GetType())
                .ToDictionary(g => g.Key, g => g.ToList());
            if (!resultTypesToFactoryTypes.ContainsKey(typeof(TResult)))
                throw new BuildException(
                    $"No registered tasks produce the expected output type of {typeof(TResult).FullName}");
            var firstResultConflict = resultTypesToFactoryTypes.FirstOrDefault(x => x.Value.Count > 1);
            if (!firstResultConflict.Equals(default(KeyValuePair<Type, List<Type>>)))
                throw new BuildException(
                    $"Result type {firstResultConflict.Key.FullName} is provided by multiple tasks: " +
                    $"[{string.Join(", ", firstResultConflict.Value.Select(t => t.FullName))}]");
            var missingResultTypes = registrations
                .SelectMany(x => x.ArgumentTypes)
                .Distinct()
                .Where(x => !container.IsRegistered(x) && !resultTypesToFactoryTypes.ContainsKey(x))
                .ToList();
            if (missingResultTypes.Count > 0)
                throw new BuildException(
                    $"Some result types are not provided by any registered tasks: " +
                    $"[{string.Join(", ", missingResultTypes.Select(t => t.FullName))}]");
        }

        private static Type? GetResultType(MethodInfo taskFactoryMethod)
        {
            return taskFactoryMethod.ReturnType.GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IBuildTask<>))
                .Select(t => t.GetGenericArguments()[0])
                .FirstOrDefault();
        }
    }

    class BuildInstance<TSettings, TResult> : IBuildProgress<TResult>
    {
        public IReadOnlyList<string> AllTaskNames { get; private set; }
        public Task<TResult> Outcome => outcomeSource.Task;
        public IObservable<IBuildTask> Tasks => tasks;

        private readonly Dictionary<Type, object> availableResults = new();
        private readonly List<IDisposable> completionDisposables = new();
        private readonly ILifetimeScope container;
        private readonly ILogger log;
        private readonly TaskCompletionSource<TResult> outcomeSource = new();
        private readonly List<TaskRegistration> remainingRegistrations;
        private readonly List<(IBuildTask, Task)> remainingTasks = new();
        private readonly ILifetimeScope scope;
        private readonly TSettings settings;
        private readonly ReplaySubject<IBuildTask> tasks = new();

        private bool ended = false;

        public BuildInstance(
            ILifetimeScope container, ILogger log, IEnumerable<TaskRegistration> registrations, TSettings settings)
        {
            this.container = container;
            this.log = log;
            this.settings = settings;

            AllTaskNames = registrations.Select(x => x.Name).ToList().AsReadOnly();

            remainingRegistrations = new(registrations);
            scope = container.BeginLifetimeScope();
        }

        public void Cancel()
        {
            log.Information("Build cancellation requested");
            outcomeSource.SetCanceled();
            Cleanup();
        }

        public void Start()
        {
            log.Information("Build started");
            Continue();
        }

        private async void Cleanup()
        {
            log.Debug("Performing build cleanup actions");
            if (ended)
                return;
            ended = true;
            foreach (var (buildTask, _) in remainingTasks)
            {
                log.Debug("Attempting cancellation of task '{taskName}'", buildTask.Name);
                if (buildTask is ICancellableBuildTask cancellableTask)
                {
                    cancellableTask.Cancel();
                    log.Information("Sent cancellation request to task '{taskName}'", buildTask.Name);
                }
                else
                    log.Warning("Task '{taskName}' does not support cancellation", buildTask.Name);
            }
            try
            {
                log.Debug("Waiting for tasks to end");
                await Task.WhenAll(remainingTasks.Select(x => x.Item2));
                log.Information("All tasks ended");
            }
            catch (AggregateException ex)
            {
                log.Warning(ex, "Some tasks failed to complete or cancel");
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            log.Debug("Disposing all task state");
            foreach (var disposable in completionDisposables)
                disposable.Dispose();
            completionDisposables.Clear();
            scope.Dispose();
            log.Information("Cleanup succeeded, build has ended.");
        }

        private void Complete()
        {
            log.Information("All tasks completed");
            lock (availableResults)
            {
                var result = availableResults[typeof(TResult)];
                outcomeSource.SetResult((TResult)result);
            }
            Cleanup();
        }

        private void Continue()
        {
            if (ended)
                return;
            log.Debug("Checking for runnable tasks");
            if (remainingRegistrations.Count == 0 && remainingTasks.Count == 0)
            {
                log.Debug("No tasks left to run");
                Complete();
                return;
            }
            try
            {
                var ready = new List<(TaskRegistration registration, IBuildTask task)>();
                foreach (var reg in remainingRegistrations)
                {
                    log.Debug("Checking runnability for task '{taskName}'", reg.Name);
                    var args = reg.ArgumentTypes.Select(t => TryResolve(t)).ToArray();
                    if (args.All(x => x is not null))
                    {
                        log.Debug("All dependencies satisfied for task '{taskName}'", reg.Name);
                        if (reg.Factory.DynamicInvoke(args) is not IBuildTask task)
                            throw new BuildException(
                                $"Task factory {reg.Factory.GetType().FullName} did not return a valid build task.");
                        if (task is INameable nameable)
                            nameable.Name = reg.Name;
                        log.Information("Created task '{taskName}'", task.Name);
                        ready.Add((reg, task));
                    }
                }
                foreach (var (registration, buildTask) in ready)
                {
                    tasks.OnNext(buildTask);
                    if (buildTask is IDisposable disposable)
                        completionDisposables.Add(disposable);
                    remainingRegistrations.Remove(registration);
                    var runtimeTask = StartAndWatch(buildTask);
                }
                if (remainingTasks.Count == 0 && ready.Count == 0)
                {
                    var remainingRegistrationTypeNames =
                        remainingRegistrations.Select(x => x.Factory.GetType().FullName);
                    throw new BuildException(
                        $"Failed to start any new tasks after all current tasks completed. The build cannot " +
                        $"complete. Remaining tasks are: [{string.Join(", ", remainingRegistrationTypeNames)}]");
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "Unexpected error during build");
                outcomeSource.TrySetException(new BuildException("Unexpected error during build.", ex));
                Cleanup();
            }
        }

        private void Fail(IBuildTask task, Exception? ex)
        {
            log.Error(ex, "Task '{taskName}' failed due to an unhandled exception", task.Name);
            outcomeSource.TrySetException(new BuildException(
                $"Task '{task.Name}' ({task.GetType().FullName}) failed to complete", ex));
            Cleanup();
        }

        private Task StartAndWatch(IBuildTask buildTask)
        {
            log.Information("Attempting to start task '{taskName}'", buildTask.Name);
            var startMethod = buildTask.GetType().GetMethod(nameof(IBuildTask<object>.Start));
            if (startMethod is null)
                throw new BuildException(
                    $"Unable to find a Start method on build task '{buildTask.Name}' ({buildTask.GetType().FullName})");
            var runtimeTask = Task.Run(async () =>
            {
                var invokedTask = startMethod.Invoke(buildTask, new object?[] { settings }) as Task;
                if (invokedTask is not Task)
                {
                    log.Warning("Task '{taskName}' started, but didn't return a valid Task instance", buildTask.Name);
                    invokedTask = Task.CompletedTask;
                }
                else
                    log.Information("Task '{taskName}' successfully started", buildTask.Name);
                await invokedTask;
                var result = TryGetResult(invokedTask);
                if (result is null)
                    throw new BuildException(
                        $"Task for '{buildTask.Name}' ({buildTask.GetType().FullName}) did not return a " +
                        $"non-null result.");
                return result;
            });
            remainingTasks.Add((buildTask, runtimeTask));
            runtimeTask.ContinueWith(t =>
            {
                log.Information("Task '{taskName}' ended with status {taskStatus}", buildTask.Name, t.Status);
                remainingTasks.RemoveAll(x => x.Item1 == buildTask || x.Item2 == runtimeTask);
                switch (t.Status)
                {
                    case TaskStatus.Canceled:
                        // Cancellation should only have happened by calling the pipeline's Cancel method, which already
                        // takes care of propagating the cancellation.
                        break;
                    case TaskStatus.Faulted:
                        Fail(buildTask, t.Exception);
                        break;
                    case TaskStatus.RanToCompletion:
                        lock (availableResults)
                            availableResults.Add(t.Result.GetType(), t.Result);
                        Continue();
                        break;
                    default:
                        throw new BuildException($"Unexpected task status on continuation: {t.Status}");
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
            return runtimeTask;
        }

        private static object? TryGetResult(Task task)
        {
            var resultProperty = task.GetType().GetProperty(nameof(Task<object>.Result));
            return resultProperty?.GetGetMethod()?.Invoke(task, null);
        }

        private object? TryResolve(Type argumentType)
        {
            lock (availableResults)
                if (availableResults.TryGetValue(argumentType, out var result))
                    return result;
            return container.ResolveOptional(argumentType);
        }
    }

    class BuildPipeline<TSettings, TResult> : IBuildPipeline<TSettings, TResult>
    {
        private readonly ILifetimeScope container;
        private readonly ILogger log;
        private readonly IReadOnlyCollection<TaskRegistration> registrations;

        public BuildPipeline(ILifetimeScope container, ILogger log, IEnumerable<TaskRegistration> registrations)
        {
            this.container = container;
            this.log = log;
            this.registrations = registrations.ToList().AsReadOnly();
        }

        public IBuildProgress<TResult> Start(TSettings settings)
        {
            var contextLog = log.ForContext<BuildInstance<TSettings, TResult>>();
            var build = new BuildInstance<TSettings, TResult>(container, contextLog, registrations, settings);
            build.Start();
            return build;
        }
    }
}
