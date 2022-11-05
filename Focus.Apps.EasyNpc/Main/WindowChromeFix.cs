using System;
using System.Windows;
using System.Windows.Interop;

namespace Focus.Apps.EasyNpc.Main
{
    // Workaround for a bug in the .NET framework.
    // See: https://developercommunity.visualstudio.com/t/overflow-exception-in-windowchrome/167357
    static class WindowChromeFix
    {
        public static void Install(Window window)
        {
            ((HwndSource)PresentationSource.FromVisual(window)).AddHook(HookProc);
        }

        private static IntPtr HookProc(
            IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0084 /*WM_NCHITTEST*/ )
            {
                // This prevents a crash in WindowChromeWorker._HandleNCHitTest
                try
                {
                    lParam.ToInt32();
                }
                catch (OverflowException)
                {
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }
    }
}
