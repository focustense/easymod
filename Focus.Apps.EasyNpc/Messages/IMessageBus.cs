using System;

namespace Focus.Apps.EasyNpc.Messages
{
    public interface IMessageBus
    {
        void Send<T>(T message);
        void Subscribe<T>(Action<T> handler);
    }
}