using System;
using System.Collections.Generic;

namespace Focus.Environment
{
    public interface IAfterGameSetup<T>
    {
        bool IsReady { get; }
        T Value { get; }

        void OnValue(Action<T> callback);
    }

    public interface IAfterGameSetupNotifier
    {
        void NotifyValue();
    }

    public class AfterGameSetup<T> : IAfterGameSetup<T>, IAfterGameSetupNotifier
    {
        public bool IsReady => setup.IsConfirmed;
        public T Value => ValueOrThrow();

        private readonly Lazy<T> lazy;
        private readonly IGameSetup setup;
        private readonly List<Action<T>> valueCallbacks = new();

        private bool isNotified;

        public AfterGameSetup(IGameSetup setup, Lazy<T> lazy)
        {
            this.lazy = lazy;
            this.setup = setup;
        }

        public void NotifyValue()
        {
            if (isNotified)
                return;
            foreach (var cb in valueCallbacks)
                cb(lazy.Value);
            valueCallbacks.Clear();
            isNotified = true;
        }

        public void OnValue(Action<T> callback)
        {
            if (isNotified)
                callback(lazy.Value);
            else
                valueCallbacks.Add(callback);
        }

        private T ValueOrThrow()
        {
            if (!setup.IsConfirmed)
                throw new InvalidOperationException("Value cannot be accessed until game setup is confirmed.");
            return lazy.Value;
        }
    }
}
