using Focus.Apps.EasyNpc.Build;
using Focus.Apps.EasyNpc.Build.UI;
using Focus.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Baml2006;
using System.Windows.Markup;
using System.Windows.Markup.Localizer;
using System.Xaml;

namespace Focus.Localization
{
    public class PackageExtractor
    {
        public TranslationPackage Extract(Assembly assembly)
        {
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                Console.WriteLine(resourceName);
                Console.WriteLine();
                if (!resourceName.EndsWith(".resources"))
                    continue;
                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new ResourceReader(stream);
                foreach (DictionaryEntry entry in reader)
                {
                    Console.WriteLine(entry.Key);
                    Console.WriteLine("----------");
                    if (entry.Value is Stream entryStream)
                    {
                        var resolver = new BamlLocalizabilityByReflection(typeof(BuildWarningHelp).Assembly);
                        var localizer = new BamlLocalizer(entryStream, resolver);
                        var resources = localizer.ExtractResources();
                        foreach (var res in resources)
                        {
                            var key = res.Key as BamlLocalizableResourceKey;
                            var value = res.Value as BamlLocalizableResource;
                            if (!IsLocalizable(value))
                                continue;
                            Console.WriteLine(
                                "    {0}.{1} = {2}",
                                key.Uid,
                                key.PropertyName,
                                value.Content
                                );
                        }
                    }
                }
            }
            return null;
        }

        private static bool IsLocalizable(BamlLocalizableResource resource)
        {
            return
                resource.Readable &&
                resource.Modifiable &&
                resource.Category != LocalizationCategory.Font &&
                resource.Category != LocalizationCategory.Hyperlink &&
                resource.Category != LocalizationCategory.Ignore &&
                resource.Category != LocalizationCategory.NeverLocalize &&
                resource.Category != LocalizationCategory.XmlData &&
                !string.IsNullOrWhiteSpace(resource.Content);
        }
    }
}