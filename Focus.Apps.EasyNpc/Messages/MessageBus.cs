using System;
using System.Collections.Concurrent;

namespace Focus.Apps.EasyNpc.Messages
{
    public class MessageBus : IMessageBus
    {
        public static readonly IMessageBus Instance = new MessageBus();

        public static void Send<T>(T message)
            where T : notnull
        {
            Instance.Send(message);
        }

        public static void Subscribe<T>(Action<T> handler)
            where T : notnull
        {
            Instance.Subscribe(handler);
        }

        private readonly ConcurrentDictionary<Type, ConcurrentBag<Action<object>>> subscriptions = new();

        void IMessageBus.Send<T>(T message)
        {
            foreach (var action in GetSubscriptions(typeof(T)))
                action(message);
        }

        void IMessageBus.Subscribe<T>(Action<T> handler)
        {
            var subscriptions = GetSubscriptions(typeof(T));
            subscriptions.Add(msg => handler((T)msg));
        }

        private ConcurrentBag<Action<object>> GetSubscriptions(Type messageType)
        {
            return subscriptions.GetOrAdd(messageType, _ => new());
        }
    }
}