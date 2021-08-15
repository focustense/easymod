using Autofac;
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
        Task<TResult> Outcome { get; }
        IObservable<IBuildTask> Tasks { get; }

        void Cancel();
    }

    record TaskRegistration(Delegate Factory, Type[] ArgumentTypes, Type ResultType);

    public class BuildPipelineConfiguration<TResult>
    {
        private readonly ILifetimeScope container;
        private readonly List<TaskRegistration> registrations = new();

        public BuildPipelineConfiguration(ILifetimeScope container)
        {
            this.container = container;
        }

        public IBuildPipeline<TSettings, TResult> CreatePipeline<TSettings>()
        {
            Validate();
            return new BuildPipeline<TSettings, TResult>(container, registrations);
        }

        public BuildPipelineConfiguration<TResult> RegisterTask<T>()
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
            registrations.Add(new(factory, argumentTypes, resultType));
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
        public Task<TResult> Outcome => outcomeSource.Task;
        public IObservable<IBuildTask> Tasks => tasks;

        private readonly Dictionary<Type, object> availableResults = new();
        private readonly List<IDisposable> completionDisposables = new();
        private readonly ILifetimeScope container;
        private readonly TaskCompletionSource<TResult> outcomeSource = new();
        private readonly List<TaskRegistration> remainingRegistrations;
        private readonly List<(IBuildTask, Task)> remainingTasks = new();
        private readonly ILifetimeScope scope;
        private readonly TSettings settings;
        private readonly ReplaySubject<IBuildTask> tasks = new();

        private bool ended = false;

        public BuildInstance(ILifetimeScope container, IEnumerable<TaskRegistration> registrations, TSettings settings)
        {
            this.container = container;
            this.settings = settings;

            remainingRegistrations = new(registrations);
            scope = container.BeginLifetimeScope();
        }

        public void Cancel()
        {
            outcomeSource.SetCanceled();
            Cleanup();
        }

        public void Start()
        {
            Continue();
        }

        private async void Cleanup()
        {
            if (ended)
                return;
            ended = true;
            foreach (var (buildTask, _) in remainingTasks)
                if (buildTask is ICancellableBuildTask cancellableTask)
                    cancellableTask.Cancel();
            try
            {
                await Task.WhenAll(remainingTasks.Select(x => x.Item2));
            }
            catch (AggregateException) { }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            foreach (var disposable in completionDisposables)
                disposable.Dispose();
            completionDisposables.Clear();
            scope.Dispose();
        }

        private void Complete()
        {
            var result = availableResults[typeof(TResult)];
            outcomeSource.SetResult((TResult)result);
            Cleanup();
        }

        private void Continue()
        {
            if (ended)
                return;
            if (remainingRegistrations.Count == 0 && remainingTasks.Count == 0)
            {
                Complete();
                return;
            }
            try
            {
                var ready = new List<(TaskRegistration registration, IBuildTask task)>();
                foreach (var reg in remainingRegistrations)
                {
                    var args = reg.ArgumentTypes.Select(t => TryResolve(t)).ToArray();
                    if (args.All(x => x is not null))
                    {
                        if (reg.Factory.DynamicInvoke(args) is not IBuildTask task)
                            throw new BuildException(
                                $"Task factory {reg.Factory.GetType().FullName} did not return a valid build task.");
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
                outcomeSource.TrySetException(new BuildException("Unexpected error during build.", ex));
                Cleanup();
            }
        }

        private void Fail(IBuildTask task, Exception? ex)
        {
            outcomeSource.TrySetException(new BuildException(
                $"Task '{task.Name}' ({task.GetType().FullName}) failed to complete", ex));
            Cleanup();
        }

        private Task StartAndWatch(IBuildTask buildTask)
        {
            var startMethod = buildTask.GetType().GetMethod(nameof(IBuildTask<object>.Start));
            if (startMethod is null)
                throw new BuildException(
                    $"Unable to find a Start method on build task '{buildTask.Name}' ({buildTask.GetType().FullName})");
            var runtimeTask = startMethod.Invoke(buildTask, new object?[] { settings }) as Task ?? Task.CompletedTask;
            remainingTasks.Add((buildTask, runtimeTask));
            runtimeTask.ContinueWith(t =>
            {
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
                        var result = TryGetResult(t);
                        if (result is null)
                            throw new BuildException(
                                $"Task for '{buildTask.Name}' ({buildTask.GetType().FullName}) did not return a " +
                                $"non-null result.");
                        availableResults.Add(result.GetType(), result);
                        Continue();
                        break;
                    default:
                        throw new BuildException($"Unexpected task status on continuation: {t.Status}");
                }
            });
            return runtimeTask;
        }

        private static object? TryGetResult(Task task)
        {
            var resultProperty = task.GetType().GetProperty(nameof(Task<object>.Result));
            return resultProperty?.GetGetMethod()?.Invoke(task, null);
        }

        private object? TryResolve(Type argumentType)
        {
            if (availableResults.TryGetValue(argumentType, out var result))
                return result;
            return container.ResolveOptional(argumentType);
        }
    }

    class BuildPipeline<TSettings, TResult> : IBuildPipeline<TSettings, TResult>
    {
        private readonly ILifetimeScope container;
        private readonly IReadOnlyCollection<TaskRegistration> registrations;

        public BuildPipeline(ILifetimeScope container, IEnumerable<TaskRegistration> registrations)
        {
            this.container = container;
            this.registrations = registrations.ToList().AsReadOnly();
        }

        public IBuildProgress<TResult> Start(TSettings settings)
        {
            var build = new BuildInstance<TSettings, TResult>(container, registrations, settings);
            build.Start();
            return build;
        }
    }
}
