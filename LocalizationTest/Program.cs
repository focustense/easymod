using Focus.Localization;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LocalizationTest
{
    class Program
    {
        private static readonly string[] TranslationLanguages = new[] { "ru" };

        static void Main(string[] args)
        {
            var buildDir = @"D:\Projects\SSETools\Focus.Apps.EasyNpc\bin\Debug\net5.0-windows10.0.19041\win-x64";
            var asm = Assembly.LoadFrom(Path.Combine(buildDir, "en-US", "EasyNPC.Resources.dll"));
            var commentsFilePath = Path.Combine(buildDir, "EasyNPC.loc");
            var extractor = new ResourceExtractor();
            var resources = extractor.Extract(asm, commentsFilePath).ToList();
            var project = new TranslationProject
            {
                Id = "Focus.Apps.EasyNpc",
                Packages = TranslationLanguages.Select(lang => new TranslationPackage
                {
                    LanguageCode = lang,
                    ProjectId = "Focus.Apps.EasyNpc",
                    Resources = resources,
                }).ToList()
            };
            Console.WriteLine(JsonConvert.SerializeObject(project.Packages[0], Formatting.Indented));
            Console.WriteLine("Hello World!");
        }
    }
}
