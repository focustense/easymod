using System;

namespace Focus.Apps.EasyNpc.Main
{
    public class StartupWarningViewModel
    {
        public bool IsFatal { get; set; } = true;
        public object Content { get; set; }
        public string Title { get; set; }
    }
}
