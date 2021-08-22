using Focus.Localization;
using System;
using System.Reflection;

namespace LocalizationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var asm = Assembly.LoadFrom(@"D:\Projects\SSETools\Focus.Apps.EasyNpc\bin\Debug\net5.0-windows10.0.19041\en-US\EasyNPC.Resources.dll");
            var extractor = new PackageExtractor();
            extractor.Extract(asm);
            Console.WriteLine("Hello World!");
        }
    }
}
