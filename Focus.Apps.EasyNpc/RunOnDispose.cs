using System;

namespace Focus.Apps.EasyNpc
{
    public class RunOnDispose : IDisposable
    {
        private Action? action;

        public RunOnDispose(Action action)
        {
            this.action = action;
        }

        public void Dispose()
        {
            if (action is null)
                return;
            action?.Invoke();
            action = null;
            GC.SuppressFinalize(this);
        }
    }
}
