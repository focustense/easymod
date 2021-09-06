using System;

namespace Focus.Apps.EasyNpc.Messages
{
    public interface IMessageBus
    {
        void Send<T>(T message) where T : notnull;
        void Subscribe<T>(Action<T> handler) where T : notnull;
    }
}