using System.Runtime.InteropServices;

namespace Focus.Graphics.Formats
{
    [StructLayout(LayoutKind.Sequential)]
    struct DDSFile
    {
        public IntPtr Scan0;
        public int Width;
        public int Height;
        public int Stride;
    }

    static class NativeMethods
    {
        private const string DdsDllName = "ddsfile.dll";

        [DllImport(DdsDllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void FreeDDS([In, Out] ref DDSFile file);

        [DllImport(DdsDllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int LoadDDSFromMemory(
            [In] byte[] data, int size, [Out] out DDSFile file);
    }
}
