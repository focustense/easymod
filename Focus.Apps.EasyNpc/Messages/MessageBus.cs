using System;
using System.Collections.Concurrent;

namespace Focus.Apps.EasyNpc.Messages
{
    public static class MessageBus
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentBag<Action<object>>> subscriptions = new();

        public static void Send<T>(T message)
        {
            foreach (var action in GetSubscriptions(typeof(T)))
                action(message);
        }

        public static void Subscribe<T>(Action<T> handler)
        {
            var subscriptions = GetSubscriptions(typeof(T));
            subscriptions.Add(msg => handler((T)msg));
        }

        private static ConcurrentBag<Action<object>> GetSubscriptions(Type messageType)
        {
            return subscriptions.GetOrAdd(messageType, _ => new());
        }
    }
}