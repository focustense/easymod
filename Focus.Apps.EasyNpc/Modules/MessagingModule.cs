using Autofac;
using Focus.Apps.EasyNpc.Messages;

namespace Focus.Apps.EasyNpc.Modules
{
    public class MessagingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(MessageBus.Instance).As<IMessageBus>().SingleInstance();
        }
    }
}
